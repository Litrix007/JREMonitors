#define D2D_INPUT_COUNT 2
#include "d2d1effecthelpers.hlsli"

cbuffer GhostingConstants : register(b0)
{
    float DecayAlpha; // EMA 衰减系数 α = pow(1/255, elapsed/decayTime)；JustFrozen 帧 = 0（new = current）
    float ConvergeBand; // 收敛带半径：max(0.6/(1−α), 4)/255，覆盖 8bit round 停滞区；JustFrozen 帧 = 0
    float2 _pad;
};

D2D_PS_ENTRY (main)
{
    float4 cur = D2DGetInput(0); // 当前帧（ActualBitmap 前景渲染结果）
    float4 prev = D2DGetInput(1); // 上一帧余影状态（StateOld）
    // 纯 EMA（无负偏置）：实数稳态 = 精确 cur，无稳态暗化
    float4 v = saturate(DecayAlpha * prev + (1.0f - DecayAlpha) * cur);
    // 收敛收缩：带内每帧到 cur 的距离减半并扣除一个量子步，
    // 有限步内精确收敛到 cur（渐进无跳变），静止/动态/冻结画面颜色一致。
    // 兼任 8bit round 停滞消除：停滞区 k ≤ 0.5/(1−α) 灰阶 ⊂ 收敛带 ⇒ 无永久残留
    float4 d = v - cur;
    float3 adr = abs(d.rgb);
    float ada = abs(d.a);
    [branch] if (adr.x < ConvergeBand && adr.y < ConvergeBand && adr.z < ConvergeBand && ada < ConvergeBand)
    {
        v.rgb = cur.rgb + sign(d.rgb) * max(adr * 0.5 - 0.3 / 255.0, 0.0);
        v.a = cur.a + sign(d.a) * max(ada * 0.5 - 0.3 / 255.0, 0.0);
    }
    return v;
}
