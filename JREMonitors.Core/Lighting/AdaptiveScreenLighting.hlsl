#define D2D_INPUT_COUNT 1
#define D2D_REQUIRES_SCENE_POSITION
#include "d2d1effecthelpers.hlsli"

static const float3 LumaWeights = float3(0.2126f, 0.7152f, 0.0722f);

cbuffer AdaptiveScreenLightingConstants : register(b0)
{
    row_major float4x4 HTotal; // 投影单应性矩阵
    float2 BrightExtent; // 亮光完全生效临界点
    float2 ShadowExtent; // 暗区完全生效临界点
    float4 CornerRect; // xy=中心 zw=半宽高（输入位图像素坐标）
    float CornerRadius; // 圆角半径；0 = 无圆角直通
    float Mode; // 0 = 完整光照物理，1 = brightness-only（替代原 ColorMatrix dimming）
    float AmbientFactor; // CPU 预计算：saturate(Ambient)
    float CompressThreshold; // CPU 预计算：lerp(CompressThresholdNight, 1, ambientFactor)
    float LinearBrightness; // CPU 预计算：pow(perceptualB*srgbAdapt, 2.2)
    float GlassReflectanceLight; // 玻璃亮部反射率
    float GlassReflectanceDark; // 玻璃暗部反射率
    float Brightness; // 屏幕硬件亮度（Mode=1 brightness-only 用）
    float3 Leakage; // CPU 预计算：LeakColor*(linearBrightness/PanelContrastRatio)
    float _pad2;
    float3 GlareColor; // 玻璃反光色调
    float _pad3;
}

float3 SRGBToLinear(float3 c)
{
    return pow(max(c, 0.0f), 2.2f);
}

float3 LinearToSRGB(float3 c)
{
    return pow(max(c, 0.0f), 1.0f / 2.2f);
}

float3 SoftCompressLuminance(float3 c, float threshold)
{
    float luma = max(dot(c, LumaWeights), 0.00001f);
    [branch] if (luma <= threshold) return c;
    float maxExtra = 1.0f - threshold;
    float overPart = luma - threshold;
    float compressedLuma = threshold + maxExtra * (overPart / (overPart + maxExtra * 1.2f));
    return c * (compressedLuma / luma);
}

D2D_PS_ENTRY (main)
{
    float4 colorContent = D2DGetInput(0);
    float2 scenePos = D2DGetScenePosition().xy;
    float2 q = abs(scenePos - CornerRect.xy) - (CornerRect.zw - CornerRadius);
    float dist = length(max(q, 0.0f)) + min(max(q.x, q.y), 0.0f) - CornerRadius;
    // 圆角边缘 1px 线性羽化（对齐 D2D FillRoundedRectangle 的栅格化 AA）；
    // radius=0 时 coverage 恒 1，直角矩形行为不变
    float coverage = 1.0f;
    [branch] if (CornerRadius > 0.5f)
    {
        coverage = saturate(0.5f - dist);
    }

    [branch] if (coverage <= 0.0f) return float4(0.0f, 0.0f, 0.0f, 0.0f);
    [branch] if (Mode > 0.5f)
    {
        float3 dimmed = colorContent.rgb * Brightness;
        return float4(saturate(dimmed) * coverage, colorContent.a * coverage);
    }

    // 1. 齐次矩阵投影与环境光入射包络
    float4 inputPos = float4(scenePos, 0.0f, 1.0f);
    float4 projectedPos = mul(inputPos, HTotal);
    float2 panelPos = projectedPos.xy / projectedPos.w;

    float2 ab = ShadowExtent - BrightExtent;
    float2 ap = panelPos - BrightExtent;
    float sqLen = dot(ab, ab);
    float t = sqLen > 0.0001f ? saturate(dot(ap, ab) / sqLen) : 0.0f;
    float localIncident = AmbientFactor * (1.0f - smoothstep(0.0f, 1.0f, t));

    // 2. 屏幕 UI 自发光 + LCD 面板漏光
    float3 uiLinear = SRGBToLinear(colorContent.rgb);
    float3 E_emission = uiLinear * LinearBrightness + Leakage;

    // 3. 镜面反射
    float effectiveReflectance = lerp(GlassReflectanceDark, GlassReflectanceLight, localIncident);
    float3 C_reflected = GlareColor * (effectiveReflectance * localIncident);

    // 4. 物理总光能加和
    float3 totalHdr = E_emission + C_reflected;

    // 5. 软压缩与 sRGB 编码输出
    float3 compressedLinear = SoftCompressLuminance(totalHdr, CompressThreshold);
    float3 finalColorSRGB = LinearToSRGB(compressedLinear);

    return float4(saturate(finalColorSRGB) * coverage, colorContent.a * coverage);
}
