using System;
using Microsoft.Xna.Framework;

namespace XianCraft.Utils;

public readonly struct GameDateTime
{
    public long TotalMinutes { get; }
    public const int DayLengthMinutes = 24 * 60;

    public GameDateTime(long totalMinutes) => TotalMinutes = totalMinutes;

    public int Day => (int)(TotalMinutes / DayLengthMinutes);
    public int MinuteOfDay => (int)(TotalMinutes % DayLengthMinutes);
    public int Hour => MinuteOfDay / 60;
    public int Minute => MinuteOfDay % 60;
    public float TimeOfDay01 => (float)MinuteOfDay / DayLengthMinutes;
    public float TimeOfDayHours => Hour + Minute / 60f;

    public GameDateTime AddMinutes(long minutes) => new GameDateTime(TotalMinutes + minutes);
    public GameDateTime AddDays(int days) => AddMinutes((long)days * DayLengthMinutes);
    public override string ToString() => $"Day {Day} {Hour:D2}:{Minute:D2}";
}

public class GameClock
{
    public GameDateTime Now { get; private set; } = new GameDateTime(0);
    public float TimeScale = 60f; // 1 real sec = 1 game minute
    private float _accSeconds;

    public void Advance(float realDeltaSeconds)
    {
        _accSeconds += realDeltaSeconds * TimeScale;
        if (_accSeconds >= 60f)
        {
            var addMinutes = (long)(_accSeconds / 60f);
            _accSeconds -= addMinutes * 60f;
            Now = Now.AddMinutes(addMinutes);
        }
    }

    public void FastForwardMinutes(long minutes) => Now = Now.AddMinutes(minutes);

    public void SetDayAndHour(int day, float hour)
    {
        hour = MathHelper.Clamp(hour, 0f, 24f);
        int minutesOfDay = (int)(hour * 60f);
        Now = new GameDateTime((long)day * GameDateTime.DayLengthMinutes + minutesOfDay);
    }
}

