using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Hypostasis.Debug;

public static unsafe class DebugUtil
{
    public static T LogDebug<T>(this T o, string format = null)
    {
        DalamudApi.LogDebug(GetString(o, format));
        return o;
    }

    public static T* LogDebug<T>(T* o) where T : unmanaged
    {
        DalamudApi.LogDebug($"{(nint)o:X}");
        return o;
    }

    public static T LogInfo<T>(this T o, string format = null)
    {
        DalamudApi.LogInfo(GetString(o, format));
        return o;
    }

    public static T* LogInfo<T>(T* o) where T : unmanaged
    {
        DalamudApi.LogInfo($"{(nint)o:X}");
        return o;
    }

    public static T LogError<T>(this T o, string format = null)
    {
        DalamudApi.LogError(GetString(o, format));
        return o;
    }

    public static T* LogError<T>(T* o) where T : unmanaged
    {
        DalamudApi.LogError($"{(nint)o:X}");
        return o;
    }

    private static string GetString(object o, string format) => string.IsNullOrEmpty(format)
        ? o.ToString()
        : (string)(o.GetType().GetMethod(nameof(ToString), [ typeof(string) ])?.Invoke(o, [ format ]) ?? o.ToString());

    public sealed class Profiler : IDisposable
    {
        private static readonly Dictionary<string, Profiler> profilers = [];

        private readonly string id;
        private readonly Stopwatch stopwatch = new();
        private readonly Stopwatch durationStopwatch = new();
        private long duration;
        private long count;
        private long totalTicks;
        private long highestTicks;
        private long lowestTicks;

        private Profiler(string i) => id = i;

        public static Profiler Begin(string id = "", float duration = 0)
        {
            if (!profilers.TryGetValue(id, out var profiler))
                profilers[id] = profiler = new(id);

            var newDuration = (long)(duration * Stopwatch.Frequency);
            if (newDuration != profiler.duration)
            {
                profiler.duration = newDuration;
                profiler.durationStopwatch.Restart();
            }

            profiler.stopwatch.Restart();
            return profiler;
        }

        public void Dispose()
        {
            stopwatch.Stop();
            var ticks = stopwatch.ElapsedTicks;
            count++;
            totalTicks += ticks;

            if (duration > 0)
            {
                if (highestTicks < ticks)
                    highestTicks = ticks;
                if (count == 1 || lowestTicks > ticks)
                    lowestTicks = ticks;
            }

            if (durationStopwatch.ElapsedTicks < duration) return;

            var ticksPerMS = Stopwatch.Frequency / 1000f;
            var name = !string.IsNullOrEmpty(id) ? $"{id}, " : string.Empty;

            if (duration > 0)
            {
                DalamudApi.LogWarning($"{name}A: {totalTicks / ticksPerMS / count:F4} / S: {lowestTicks / ticksPerMS:F4} / L: {highestTicks / ticksPerMS:F4} ({count} calls)");
                highestTicks = 0;
                lowestTicks = 0;
            }
            else
            {
                DalamudApi.LogWarning($"{name}{totalTicks / ticksPerMS:F4} ms ({totalTicks})");
            }

            count = 0;
            totalTicks = 0;
            durationStopwatch.Restart();
        }
    }
}