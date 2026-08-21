#define D2D_INPUT_COUNT 3
#include "d2d1effecthelpers.hlsli"

cbuffer ImageShadowConstants : register(b0)
{
    float Blur;
    float Choke;
    float2 Padding;
    float4 Color;
};

D2D_PS_ENTRY (main)
{
    float4 baseColor = D2DGetInput(0);
    float baseAlpha = D2DGetInput(1).a;

    if (baseAlpha <= 0.0001)
    {
        return baseColor;
    }

    float blurredShadowAlpha = D2DGetInput(2).a;
    float chokeDenominator = max(1.0 - Choke, 0.001);
    float chokedMask = saturate(blurredShadowAlpha / chokeDenominator);
    float effectiveShadowAlpha = chokedMask * Color.a;
    float3 premultipliedShadow = Color.rgb * effectiveShadowAlpha * baseAlpha;

    baseColor.rgb = premultipliedShadow + baseColor.rgb * (1.0 - effectiveShadowAlpha);
    return baseColor;
}
