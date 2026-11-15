using System;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using TopPicksPlus.Logic;

namespace TopPicksPlus.EmbyLink;

/// <summary>
/// The order guard (F5, D44, D52, D101): puts back any ranked copy's date added (the movie, or the series and every episode) that
/// differs from its rank date. Runs after library changes in the Top Picks folder and 2 minutes after start-up, never while a run
/// holds the lock, and only while allowed (D114). It reads managed.json and writes only Emby's dates.
/// </summary>
public static class OrderGuard
{
    private static readonly object Gate = new object();

    /// <summary>Sets one item's date added to the rank date if it differs (compared to the second, D52).</summary>
    public static void SetDate(BaseItem item, DateTime rankDateUtc)
    {
        if (!RankTimes.NeedsReset(rankDateUtc, item.DateCreated.UtcDateTime))
            return;
        item.DateCreated = new DateTimeOffset(DateTime.SpecifyKind(rankDateUtc, DateTimeKind.Utc));
        item.UpdateToRepository(ItemUpdateType.MetadataEdit);
    }

    /// <summary>One pass of the guard. Never throws; problems are logged.</summary>
    public static void Run(Host h)
    {
        if (!System.Threading.Monitor.TryEnter(Gate))
            return;
        try
        {
            var health = h.Data.Inspect();
            var allowed = EmbyRules.Allowed(Startup.OldPluginLoaded, health.DataNewer, health.Managed != Storage.FileStatus.Damaged);
            if (!RunRules.OrderGuardMayRun(RunEngine.LockHeld, allowed))
                return;
            var managed = h.Data.LoadManaged().Value;
            if (managed == null)
                return;
            var reset = 0;
            foreach (var entry in SwapPlan.Guarded(managed))
            {
                var folder = Path.Combine(h.TopPicksPath, entry.Folder);
                var items = entry.Kind == TitleKind.Series
                    ? new[] { h.Library.FindByPath(folder, true) }.Concat(entry.Items.Keys.Select(k => h.Library.FindByPath(Path.Combine(folder, k), false)))
                    : entry.Items.Keys.Select(k => h.Library.FindByPath(Path.Combine(folder, k), false));
                foreach (var item in items.Where(i => i != null))
                {
                    if (RunEngine.LockHeld)
                        return;
                    if (!RankTimes.NeedsReset(entry.RankDateUtc, item!.DateCreated.UtcDateTime))
                        continue;
                    SetDate(item, entry.RankDateUtc!.Value);
                    reset++;
                }
            }
            if (reset > 0)
                h.Log.Info("Top Picks Plus: order guard put back {0} date(s)", reset);
        }
        catch (Exception e)
        {
            h.Log.ErrorException("Top Picks Plus: the order guard stopped with an error", e);
        }
        finally
        {
            System.Threading.Monitor.Exit(Gate);
        }
    }
}
