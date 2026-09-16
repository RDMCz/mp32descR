using System;

namespace mp32descR.Service;

/// <summary>
/// Helper struct to generate YouTube-like timestamps. Internal counter starts at 0:00,
/// and it can be incremented and converted to string. 
/// </summary>
public struct TimestampCounter(TimeSpan totalDuration = new())
{
    private TimeSpan _totalDuration = totalDuration;

    /// <summary>
    /// Increments the internal counter by given milliseconds. 
    /// </summary>
    public void IncrementMs(double ms)
    {
        _totalDuration += TimeSpan.FromMilliseconds(ms);
    }

    /// <returns>YouTube-like timestamp reflecting the value of the internal counter</returns>
    public readonly override string ToString()
    {
        // <0:00 — 9:59> → <10:00 — 59:59> → <1:00:00 — 9:59:59> → <10:00:00 — 23:59:59> → <1:00:00:00 — 9:23:59:59>...
        var seconds = _totalDuration.Seconds.ToString().PadLeft(2, '0');
        var minutes = _totalDuration.Minutes.ToString();
        string hoursWithColon;
        string daysWithColon;
        if (_totalDuration.Days == 0)
        {
            daysWithColon = "";
            if (_totalDuration.Hours == 0)
            {
                // <0:00 — 9:59> or <10:00 — 59:59>
                hoursWithColon = "";
            }
            else
            {
                // <1:00:00 — 9:59:59> or <10:00:00 — 23:59:59>
                hoursWithColon = _totalDuration.Hours + ":";
                minutes = minutes.PadLeft(2, '0');
            }
        }
        else
        {
            // <1:00:00:00 — 9:23:59:59>...
            daysWithColon = _totalDuration.Days + ":";
            hoursWithColon = (_totalDuration.Hours + ":").PadLeft(3, '0');
            minutes = minutes.PadLeft(2, '0');
        }

        return $"{daysWithColon}{hoursWithColon}{minutes}:{seconds}";
    }
}