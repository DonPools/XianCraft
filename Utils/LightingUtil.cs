using System;
using Microsoft.Xna.Framework;

namespace XianCraft.Utils;

/// <summary>
/// 依据游戏时间计算环境光亮度的工具。
/// 曲线：夜晚保持最低亮度；日出到日落之间用正弦曲线从0升到1再降回0，再叠加夜晚的最小亮度与日间峰值缩放。
/// </summary>
public static class LightingUtil
{
    // 更细的时间分段
    private const float DawnStartHour = 5.0f;
    private const float DawnEndHour   = 6.5f;
    private const float DuskStartHour = 18.0f;
    private const float DuskEndHour   = 19.5f;

    // 调整：提高夜间最低亮度；再加一个环境底线，防止被其它计算拉得过低
    private const float NightMinLight = 0.08f;   // 原 0.15 太亮可再调；若之前全黑说明乘法后衰减，可保持 0.08
    private const float AmbientFloor  = 0.05f;   // 绝不低于此值（乘法后仍可见轮廓）
    private const float PostDawnBase  = 0.56f;
    private const float DayPeakLight  = 0.95f;

    private const float MiddayCurveExponent = 0.6f;

    /// <summary>
    /// 拟真日夜光照曲线：
    /// 夜晚（含午夜→黎明前 & 黄昏后→午夜）保持最低亮度并做极轻微月光起伏。
    /// 黎明/黄昏使用 SmoothStep 平滑过渡，白天用加宽的正弦峰。
    /// </summary>
    public static float GetLightFactor(GameDateTime t)
    {
        float h = t.TimeOfDayHours % 24f;
        float value;

        // 夜晚：DuskEndHour -> 24 以及 0 -> DawnStartHour
        bool isNight = h < DawnStartHour || h >= DuskEndHour;
        if (isNight)
        {
            // 计算夜间 0..1 进度用于微弱起伏（不影响玩法只为灵动感）
            float totalNight = (24f - DuskEndHour) + DawnStartHour;
            float nightPos = h >= DuskEndHour ? (h - DuskEndHour) : (h + (24f - DuskEndHour));
            float night01 = nightPos / totalNight;
            float moonWave = 0.015f * MathF.Cos(night01 * MathF.PI * 2f); // 轻微起伏
            value = NightMinLight + moonWave;
            return MathHelper.Clamp(MathF.Max(value, AmbientFloor), 0f, 1f);
        }

        // 黎明 DawnStartHour → DawnEndHour
        if (h < DawnEndHour)
        {
            float t01 = (h - DawnStartHour) / (DawnEndHour - DawnStartHour);
            float s = SmoothStep(t01); // 0→1
            value = MathHelper.Lerp(NightMinLight, PostDawnBase, s);
            return MathHelper.Clamp(MathF.Max(value, AmbientFloor), 0f, 1f);
        }

        // 黄昏 DuskStartHour → DuskEndHour
        if (h >= DuskStartHour)
        {
            float t01 = (h - DuskStartHour) / (DuskEndHour - DuskStartHour);
            float s = SmoothStep(t01); // 0→1
            // 从黄昏开始的基线衰减到夜晚最暗
            value = MathHelper.Lerp(PostDawnBase, NightMinLight, s);
            return MathHelper.Clamp(MathF.Max(value, AmbientFloor), 0f, 1f);
        }

        // 白天：DawnEndHour → DuskStartHour
        {
            float daySpan = DuskStartHour - DawnEndHour;
            float day01 = (h - DawnEndHour) / daySpan;          // 0..1
            float sine = MathF.Sin(day01 * MathF.PI);           // 0..1..0
            float broaden = MathF.Pow(sine, MiddayCurveExponent); // 拉宽顶峰
            value = PostDawnBase + broaden * (DayPeakLight - PostDawnBase);
            return MathHelper.Clamp(MathF.Max(value, AmbientFloor), 0f, 1f);
        }
    }

    private static float SmoothStep(float x)
    {
        x = MathHelper.Clamp(x, 0f, 1f);
        return x * x * (3f - 2f * x); // SmoothStep
    }
}