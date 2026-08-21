#define D2D_INPUT_COUNT 3
#define D2D_INPUT2_COMPLEX

#define D2D_REQUIRES_SCENE_POSITION
#include "d2d1effecthelpers.hlsli"

cbuffer OffsetShadowConstants : register(b0)
{
    float2 Offset;
    float Choke;
    float4 Color;
};

D2D_PS_ENTRY (main)
{
    float4 accumColor = D2DGetInput(0);
    float baseAlpha = D2DGetInput(1).a;

    if (baseAlpha <= 0.001)
    {
        return accumColor;
    }
    float2 scenePosition = D2DGetScenePosition().xy;
    float2 blurredSamplePos = scenePosition - Offset;
    float blurredAlpha = D2DSampleInputAtPosition(2, blurredSamplePos).a;

    float blurMask = saturate((baseAlpha - blurredAlpha) / max(baseAlpha, 0.01));
    float choke = saturate(Choke);
    float chokeDenominator = max(1.0 - choke, 0.001);
    float chokedMask = saturate((blurMask - choke) / chokeDenominator);
    float effectiveShadowAlpha = chokedMask * Color.a;
    float3 premultipliedShadow = Color.rgb * effectiveShadowAlpha * baseAlpha;

    float4 finalColor = accumColor;
    finalColor.rgb = premultipliedShadow + finalColor.rgb * (1.0 - effectiveShadowAlpha);
    return finalColor;
}
