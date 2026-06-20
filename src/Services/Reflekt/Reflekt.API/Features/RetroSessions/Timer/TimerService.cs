using System.Collections.Concurrent;

namespace Reflekt.API.Features.RetroSessions.Timer;

public enum TimerStatus { Stopped, Running, Paused, Expired }

public sealed record TimerSnapshot(
    TimerStatus Status,
    int RemainingSeconds,
    DateTime? EndTimeUtc);

internal sealed class TimerEntry
{
    public TimerStatus Status { get; set; } = TimerStatus.Stopped;
    public int RemainingSeconds { get; set; }
    public DateTime? EndTimeUtc { get; set; }
    public System.Threading.Timer? ExpiryTimer { get; set; }
    public readonly object Lock = new();
}

public sealed class TimerService
{
    private readonly ConcurrentDictionary<int, TimerEntry> _state = new();
    private Func<int, Task>? _onExpired;

    public void RegisterExpiryCallback(Func<int, Task> onExpired)
    {
        _onExpired = onExpired;
    }

    public TimerSnapshot Start(int retroSessionId, int durationSeconds)
    {
        var entry = _state.GetOrAdd(retroSessionId, _ => new TimerEntry());
        lock (entry.Lock)
        {
            if (entry.Status != TimerStatus.Stopped)
                throw new InvalidOperationException($"Cannot start timer in state {entry.Status}.");

            entry.RemainingSeconds = durationSeconds;
            entry.EndTimeUtc = DateTime.UtcNow.AddSeconds(durationSeconds);
            entry.Status = TimerStatus.Running;
            ScheduleExpiry(retroSessionId, entry, durationSeconds);
            return Snapshot(entry);
        }
    }

    public TimerSnapshot Pause(int retroSessionId)
    {
        var entry = GetEntryOrThrow(retroSessionId);
        lock (entry.Lock)
        {
            if (entry.Status != TimerStatus.Running)
                throw new InvalidOperationException($"Cannot pause timer in state {entry.Status}.");

            entry.ExpiryTimer?.Dispose();
            entry.ExpiryTimer = null;
            entry.RemainingSeconds = ComputeRemaining(entry);
            entry.EndTimeUtc = null;
            entry.Status = TimerStatus.Paused;
            return Snapshot(entry);
        }
    }

    public TimerSnapshot Resume(int retroSessionId)
    {
        var entry = GetEntryOrThrow(retroSessionId);
        lock (entry.Lock)
        {
            if (entry.Status != TimerStatus.Paused)
                throw new InvalidOperationException($"Cannot resume timer in state {entry.Status}.");

            entry.EndTimeUtc = DateTime.UtcNow.AddSeconds(entry.RemainingSeconds);
            entry.Status = TimerStatus.Running;
            ScheduleExpiry(retroSessionId, entry, entry.RemainingSeconds);
            return Snapshot(entry);
        }
    }

    public TimerSnapshot Reset(int retroSessionId, int durationSeconds)
    {
        var entry = GetEntryOrThrow(retroSessionId);
        lock (entry.Lock)
        {
            if (entry.Status is not (TimerStatus.Running or TimerStatus.Paused or TimerStatus.Expired))
                throw new InvalidOperationException($"Cannot reset timer in state {entry.Status}.");

            entry.ExpiryTimer?.Dispose();
            entry.ExpiryTimer = null;
            entry.RemainingSeconds = durationSeconds;
            entry.EndTimeUtc = null;
            entry.Status = TimerStatus.Paused;
            return Snapshot(entry);
        }
    }

    public TimerSnapshot AddTime(int retroSessionId, int minutes)
    {
        var entry = GetEntryOrThrow(retroSessionId);
        lock (entry.Lock)
        {
            if (entry.Status is not (TimerStatus.Running or TimerStatus.Paused or TimerStatus.Expired))
                throw new InvalidOperationException($"Cannot add time in state {entry.Status}.");

            var addedSeconds = minutes * 60;

            if (entry.Status == TimerStatus.Running)
            {
                var newEndTime = (entry.EndTimeUtc ?? DateTime.UtcNow).AddSeconds(addedSeconds);
                entry.EndTimeUtc = newEndTime;
                entry.RemainingSeconds = ComputeRemaining(entry);
                entry.ExpiryTimer?.Dispose();
                ScheduleExpiry(retroSessionId, entry, Math.Max(0, entry.RemainingSeconds));
            }
            else if (entry.Status == TimerStatus.Paused)
            {
                entry.RemainingSeconds += addedSeconds;
            }
            else // Expired
            {
                entry.ExpiryTimer?.Dispose();
                entry.RemainingSeconds = addedSeconds;
                entry.EndTimeUtc = DateTime.UtcNow.AddSeconds(addedSeconds);
                entry.Status = TimerStatus.Running;
                ScheduleExpiry(retroSessionId, entry, addedSeconds);
            }

            return Snapshot(entry);
        }
    }

    public void ClearTimer(int retroSessionId)
    {
        if (_state.TryRemove(retroSessionId, out var entry))
        {
            lock (entry.Lock)
            {
                entry.ExpiryTimer?.Dispose();
                entry.ExpiryTimer = null;
            }
        }
    }

    public TimerSnapshot GetSnapshot(int retroSessionId)
    {
        if (!_state.TryGetValue(retroSessionId, out var entry))
            return new TimerSnapshot(TimerStatus.Stopped, 0, null);

        lock (entry.Lock)
        {
            if (entry.Status == TimerStatus.Running)
                entry.RemainingSeconds = ComputeRemaining(entry);
            return Snapshot(entry);
        }
    }

    private void ScheduleExpiry(int retroSessionId, TimerEntry entry, int delaySeconds)
    {
        var delay = TimeSpan.FromSeconds(Math.Max(0, delaySeconds));
        entry.ExpiryTimer = new System.Threading.Timer(_ => OnExpiry(retroSessionId), null, delay, Timeout.InfiniteTimeSpan);
    }

    private void OnExpiry(int retroSessionId)
    {
        if (!_state.TryGetValue(retroSessionId, out var entry)) return;

        lock (entry.Lock)
        {
            if (entry.Status != TimerStatus.Running) return;
            entry.Status = TimerStatus.Expired;
            entry.RemainingSeconds = 0;
            entry.EndTimeUtc = null;
            entry.ExpiryTimer?.Dispose();
            entry.ExpiryTimer = null;
        }

        _onExpired?.Invoke(retroSessionId);
    }

    private TimerEntry GetEntryOrThrow(int retroSessionId)
    {
        if (!_state.TryGetValue(retroSessionId, out var entry))
            throw new InvalidOperationException($"No timer state found for retro {retroSessionId}.");
        return entry;
    }

    private static int ComputeRemaining(TimerEntry entry)
    {
        if (entry.EndTimeUtc is null) return entry.RemainingSeconds;
        return Math.Max(0, (int)(entry.EndTimeUtc.Value - DateTime.UtcNow).TotalSeconds);
    }

    private static TimerSnapshot Snapshot(TimerEntry entry) =>
        new(entry.Status, entry.RemainingSeconds, entry.EndTimeUtc);
}
