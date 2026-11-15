using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace TopPicksProof;

/// <summary>The proofs themselves (D76, D77, D129, D131): what Emby reports, and the one write into top-picks-proof.</summary>
internal sealed class Proofs
{
    private const string TimeFormat = "yyyy-MM-dd HH:mm:ss";

    // C62: one date in summer time and one in winter time, written as server local time like D50's format, plus the
    // summer date with its offset written out, which tells "read as UTC" apart from "converted with today's offset"
    // even when the proof runs in winter (review finding 1, D132).
    private static readonly TestCopy[] TestCopies =
    {
        new TestCopy("Top Picks Proof July", "2026-07-15 12:00:00"),
        new TestCopy("Top Picks Proof January", "2026-01-15 12:00:00"),
        new TestCopy("Top Picks Proof July Offset", "2026-07-15T12:00:00+01:00"),
    };

    private readonly IApplicationHost host;

    public Proofs(IApplicationHost host) => this.host = host;

    private string ProofRoot => Path.Combine(host.Resolve<IApplicationPaths>().DataPath, "top-picks-proof");

    public string Report(string pluginVersion)
    {
        var text = new StringBuilder();
        text.AppendLine("TOP PICKS PROOF RESULTS");
        text.AppendLine();
        text.AppendLine(Part("1. Loaded", () => Loaded(pluginVersion)));
        text.AppendLine("2. Playback Reporting file: not read. The pinned Emby SDK (4.9.1.90) has no SQLite library to compile against, "
            + "so the first week comes from each person's last played dates instead (D25, D129).");
        text.AppendLine();
        text.AppendLine(Part("3. Date added from .nfo", DateAdded));
        return text.ToString();
    }

    /// <summary>Writes the test folders, once. Never overwrites or deletes anything.</summary>
    public string WriteTestFolders(string movieFile)
    {
        try
        {
            return Write(movieFile);
        }
        catch (Exception ex)
        {
            return "Not written: " + ex.Message;
        }
    }

    // Each part is fenced, so one failing proof still shows the others.
    private static string Part(string heading, Func<string> proof)
    {
        string body;
        try
        {
            body = proof();
        }
        catch (Exception ex)
        {
            body = "Failed: " + ex.GetType().Name + ": " + ex.Message;
        }

        return heading + "\n" + body;
    }

    private string Loaded(string pluginVersion)
    {
        var zone = TimeZoneInfo.Local;
        return $"Top Picks Proof {pluginVersion} is running in {host.Name} {host.ApplicationVersion} ({host.OperatingSystemDisplayName}).\n"
            + $"Server time zone: {zone.Id}, now UTC{DateTimeOffset.Now.ToString("zzz", CultureInfo.InvariantCulture)}. "
            + $"Checked {DateTimeOffset.UtcNow.ToString(TimeFormat, CultureInfo.InvariantCulture)} UTC.\n";
    }

    private string DateAdded()
    {
        var root = ProofRoot;
        var library = host.Resolve<ILibraryManager>();
        var lines = new List<string>();
        foreach (var copy in TestCopies)
        {
            var strm = copy.StrmPath(root);
            if (!File.Exists(strm))
            {
                lines.Add($"{copy.Title}: not written yet.");
                continue;
            }

            // By path first; by title if Emby keeps the item under another path.
            var item = library.FindByPath(strm, false)
                ?? library.GetItemList(new InternalItemsQuery { Name = copy.Title, IncludeItemTypes = new[] { "Movie" }, Recursive = true })
                    .FirstOrDefault();
            if (item == null)
            {
                lines.Add($"{copy.Title}: written as {copy.DateAdded}, not in Emby yet. Scan the test library, then open this page again.");
                continue;
            }

            lines.Add($"{copy.Title}: written as {copy.DateAdded} (= {copy.ExpectedUtc().ToString(TimeFormat, CultureInfo.InvariantCulture)} UTC). "
                + $"Emby holds {item.DateCreated.UtcDateTime.ToString(TimeFormat, CultureInfo.InvariantCulture)} UTC "
                + $"= {ServerTime(item.DateCreated)} server time. {Verdict(copy.ExpectedUtc(), item.DateCreated)} (Emby id {item.InternalId})");
        }

        return string.Join("\n", lines) + "\n";
    }

    private static string Verdict(DateTime expectedUtc, DateTimeOffset held)
    {
        var difference = held.UtcDateTime - expectedUtc;
        if (Math.Abs(difference.TotalMinutes) < 1)
        {
            return "SAME MOMENT.";
        }

        return Math.Abs(difference.TotalDays) > 2
            ? "NOT THE .NFO DATE (Emby used another date)."
            : $"OUT BY {difference.TotalHours.ToString("+0.##;-0.##", CultureInfo.InvariantCulture)} HOURS.";
    }

    private static string ServerTime(DateTimeOffset moment) =>
        TimeZoneInfo.ConvertTime(moment, TimeZoneInfo.Local).ToString(TimeFormat, CultureInfo.InvariantCulture);

    private string Write(string movieFile)
    {
        if (string.IsNullOrWhiteSpace(movieFile) || !File.Exists(movieFile))
        {
            return "Not written: pick an existing movie file first.";
        }

        var root = ProofRoot;
        var movie = Path.GetFullPath(movieFile);
        if (Directory.Exists(root))
        {
            var found = WhatIsThere(root);
            if (found.Count > 0)
            {
                var expected = TestCopies.SelectMany(c => new[] { c.FolderPath(root), c.StrmPath(root), c.NfoPath(root) });
                return found.SetEquals(expected)
                    ? "Already written, so nothing was done (it writes only once)."
                    : "Not written: top-picks-proof already holds something this plugin didn't write, or an unfinished write. "
                        + "Remove the test library if you made it, delete the top-picks-proof folder by hand, then tick the box again.";
            }
        }

        Directory.CreateDirectory(root);
        foreach (var copy in TestCopies)
        {
            Directory.CreateDirectory(copy.FolderPath(root));
            WriteNewFile(copy.StrmPath(root), movie);
            WriteNewFile(copy.NfoPath(root), Nfo(copy));
        }

        return $"Written {DateTimeOffset.UtcNow.ToString(TimeFormat, CultureInfo.InvariantCulture)} UTC. "
            + $"Test library folder: {root}";
    }

    // Everything at the top of top-picks-proof, plus what is inside the expected folders. A link (junction) or a
    // folder of another name counts as something we didn't write; links are never followed.
    private static HashSet<string> WhatIsThere(string root)
    {
        if (IsLink(root))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { root };
        }

        var found = new HashSet<string>(Directory.GetFileSystemEntries(root), StringComparer.OrdinalIgnoreCase);
        foreach (var copy in TestCopies)
        {
            var folder = copy.FolderPath(root);
            if (Directory.Exists(folder) && !IsLink(folder))
            {
                found.UnionWith(Directory.GetFileSystemEntries(folder));
            }
        }

        return found;
    }

    private static bool IsLink(string path) => (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    private static void WriteNewFile(string path, string content)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string Nfo(TestCopy copy) =>
        "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>\n"
        + "<movie>\n"
        + $"  <title>{copy.Title}</title>\n"
        + $"  <dateadded>{copy.DateAdded}</dateadded>\n"
        + "</movie>\n";

    private sealed class TestCopy
    {
        public TestCopy(string title, string dateAdded)
        {
            Title = title;
            DateAdded = dateAdded;
        }

        public string Title { get; }

        /// <summary>The text written into the .nfo: server local time, or a time with its offset written out.</summary>
        public string DateAdded { get; }

        private string FolderName => Title + " (2026)";

        // A plain time means server local time on that date (D50, D67); a time with an offset means exactly that moment.
        public DateTime ExpectedUtc() =>
            DateAdded.Contains("T")
                ? DateTimeOffset.ParseExact(DateAdded, "yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture).UtcDateTime
                : TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(DateTime.ParseExact(DateAdded, TimeFormat, CultureInfo.InvariantCulture), DateTimeKind.Unspecified),
                    TimeZoneInfo.Local);

        public string FolderPath(string root) => Path.Combine(root, FolderName);

        public string StrmPath(string root) => Path.Combine(FolderPath(root), FolderName + ".strm");

        public string NfoPath(string root) => Path.Combine(FolderPath(root), FolderName + ".nfo");
    }
}
