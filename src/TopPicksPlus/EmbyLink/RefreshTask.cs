using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using TopPicksPlus.Logic;

namespace TopPicksPlus.EmbyLink;

/// <summary>
/// The "Refresh Top Picks" scheduled task (F1): every Sunday at 05:00 on the server's clock, and Run now in Scheduled Tasks.
/// Both go through the same trigger rules (D68, D94), so a waiting unfinished run is resumed rather than replaced.
/// </summary>
public sealed class RefreshTask : IScheduledTask
{
    public string Name => "Refresh Top Picks";

    public string Key => "TopPicksPlusRefresh";

    public string Description => "Puts the 10 most watched or pinned titles in the Latest Top Picks row, numbered and in order.";

    public string Category => "Top Picks Plus";

    public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
    {
        // With the old plugin loaded the guards refuse and only lastRefusal is written (D51, D94).
        var h = Host.Current;
        return h == null ? Task.CompletedTask : RunEngine.Request(h, Trigger.Sunday, cancellationToken);
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => new[]
    {
        new TaskTriggerInfo
        {
            Type = TaskTriggerInfo.TriggerWeekly,
            DayOfWeek = DayOfWeek.Sunday,
            TimeOfDayTicks = TimeSpan.FromHours(5).Ticks,
        },
    };
}
