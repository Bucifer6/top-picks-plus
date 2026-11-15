using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MediaBrowser.Controller.Entities;
using TopPicksPlus.EmbyLink;
using TopPicksPlus.Logic;
using TopPicksPlus.Storage;

namespace TopPicksPlus.Settings;

/// <summary>The last run report in plain words (F8, D31, D81), read from run.json. Shown only on the administrators' page (F9).</summary>
public static class ReportText
{
    public static string Build(Host h, RunRecord? run, FileStatus status, IReadOnlyList<User> users)
    {
        var sb = new StringBuilder();
        if (status == FileStatus.Damaged)
            return sb.Append("run.json can't be read; the next run sets it aside and starts a new one.").ToString();
        if (status == FileStatus.Newer)
            return sb.Append("run.json was written by a newer Top Picks Plus; put that version back.").ToString();
        if (run == null || run.Status == null)
        {
            sb.Append("No run yet. The first run is at the next scheduled time (Sunday 05:00 unless changed in Scheduled Tasks), or press Run now.");
            AppendRefusal(sb, run);
            return sb.ToString();
        }

        sb.Append("Top Picks Plus ").Append(run.PluginVersion ?? "?").Append(" on Emby ").Append(run.EmbyVersion ?? "?")
          .Append(", time zone ").Append(run.TimeZone ?? "?").Append('\n');
        sb.Append("Run started ").Append(Local(run.StartedUtc)).Append(": ").Append(StatusText(run)).Append('\n');
        if (run.FailReason != null)
            sb.Append("Why it failed: ").Append(run.FailReason.Message ?? run.FailReason.Reason).Append('\n');
        if (!string.IsNullOrEmpty(run.Message))
            sb.Append(run.Message).Append('\n');
        if (run.ResumeCount > 0)
            sb.Append("Resumed ").Append(run.ResumeCount.ToString(CultureInfo.InvariantCulture)).Append(" time(s).\n");
        if (run.LastSuccessfulRunUtc.HasValue)
            sb.Append("Last successful run finished ").Append(Local(run.LastSuccessfulRunUtc)).Append(".\n");

        sb.Append('\n').Append("The picks:\n");
        foreach (var p in run.Picks.OrderBy(p => p.Rank))
        {
            sb.Append(p.Rank.ToString(CultureInfo.InvariantCulture)).Append(". ").Append(Name(h, p.SourceId)).Append(" – ").Append(Reason(p));
            if (!p.CopyApplied)
                sb.Append(", copy not applied");
            if (!p.DateApplied)
                sb.Append(", date not applied");
            if (p.ArtworkFailed)
                sb.Append(", artwork failed (plain picture used)");
            if (p.EpisodesSkipped > 0)
                sb.Append(", ").Append(p.EpisodesSkipped.ToString(CultureInfo.InvariantCulture)).Append(" episode(s) left out (no number)");
            sb.Append('\n');
        }

        foreach (var id in run.MissingPins)
            sb.Append("Missing pin: ").Append(Name(h, id)).Append(" (its title or file can't be found)\n");
        sb.Append("Plays recorded in the last 7 days: ").Append(run.PlaysLast7Days.ToString(CultureInfo.InvariantCulture)).Append('\n');
        if (run.SeedSource.HasValue)
        {
            sb.Append("First week seeded from: ").Append(run.SeedSource == SeedSource.LastPlayed ? "each person's last played dates" : "nothing");
            if (run.ImportIgnored > 0)
                sb.Append(" (").Append(run.ImportIgnored.ToString(CultureInfo.InvariantCulture)).Append(" ignored: their titles are gone)");
            sb.Append('\n');
        }

        if (run.RowCheck != null)
        {
            sb.Append("Row check: ").Append(run.RowCheck.Passed ? "passed" : "FAILED");
            var checkedAs = users.FirstOrDefault(u => string.Equals(EmbyFacts.UserKey(u), run.RowCheck.UserId, StringComparison.OrdinalIgnoreCase));
            if (checkedAs != null)
                sb.Append(" (as ").Append(checkedAs.Name).Append(')');
            if (run.RowCheck.Retried)
                sb.Append(" (after one retry)");
            if (run.RowCheck.FellBackToAdmin)
                sb.Append(" – no plain user, so an administrator was used");
            sb.Append('\n');
            if (!run.RowCheck.Passed && run.RowCheck.Returned.Count > 0)
                sb.Append("  Emby returned: ").Append(string.Join(", ", run.RowCheck.Returned)).Append('\n');
            if (run.RowCheck.Extras.Count > 0)
                sb.Append("  Also in the row (not ranked): ").Append(string.Join(", ", run.RowCheck.Extras)).Append('\n');
        }

        foreach (var r in run.RefusedDeletes)
            sb.Append("Refused to touch ").Append(r.Folder).Append(r.Path == null ? string.Empty : "\\" + r.Path).Append(": ").Append(r.Reason).Append('\n');
        if (run.UnmanagedFolders.Count > 0)
            sb.Append("Folders in top-picks not made by Top Picks Plus (they can show in the row): ").Append(string.Join(", ", run.UnmanagedFolders)).Append('\n');
        foreach (var n in run.Notices)
            sb.Append("Notice ").Append(Local(n.Time)).Append(": ").Append(n.Detail).Append('\n');
        AppendRefusal(sb, run);
        return sb.ToString();
    }

    private static void AppendRefusal(StringBuilder sb, RunRecord? run)
    {
        var r = run?.LastRefusal;
        if (r == null)
            return;
        sb.Append('\n').Append("Last refused run ").Append(Local(r.Time)).Append(" (").Append(RefusalText(r.Reason)).Append("): ").Append(r.Detail ?? string.Empty).Append('\n');
    }

    private static string RefusalText(Refusal reason) => reason switch
    {
        Refusal.AlreadyRunning => "a run was already in progress",
        Refusal.DataNewer => "a data file is from a newer version",
        Refusal.OldPluginLoaded => "the old Top Picks plugin is installed",
        Refusal.LibraryMissing => "the Top Picks library is missing",
        Refusal.LibrarySetup => "the Top Picks library is set up wrongly",
        Refusal.DataDamaged => "managed.json can't be read",
        Refusal.StateDamaged => "state.json can't be read or rebuilt",
        _ => reason.ToString(),
    };

    private static string StatusText(RunRecord run) => run.Status switch
    {
        RunStatus.Running => "unfinished (at step " + run.Step + ")",
        RunStatus.Finished => "finished",
        RunStatus.Incomplete => "incomplete: not every copy appeared in time; the order guard finishes the job",
        RunStatus.Failed => "failed",
        _ => "?",
    };

    private static string Reason(RunPick p) => p.Reason switch
    {
        PickReason.Pinned => "pinned",
        PickReason.ViewerDays => p.ViewerDays.ToString(CultureInfo.InvariantCulture) + " viewer-day(s)",
        _ => "filler (recently added)",
    };

    private static string Name(Host h, long id)
    {
        var item = h.Library.GetItemById(id);
        return item?.Name ?? "title " + id.ToString(CultureInfo.InvariantCulture);
    }

    private static string Local(DateTime? utc) =>
        utc.HasValue
            ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc), TimeZoneInfo.Local).ToString("ddd d MMM yyyy HH:mm", CultureInfo.InvariantCulture)
            : "?";
}
