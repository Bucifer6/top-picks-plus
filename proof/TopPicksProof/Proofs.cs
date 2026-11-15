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
using MediaBrowser.Model.Querying;

namespace TopPicksProof;

/// <summary>The proofs themselves (D76, D77, D129): what Emby reports, and the one write into top-picks-proof.</summary>
internal sealed class Proofs
{
    private const string TimeFormat = "yyyy-MM-dd HH:mm:ss";

    // C62: one date in summer time and one in winter time, both written as server local time like D50's format.
    private static readonly TestCopy[] TestCopies =
    {
        new TestCopy("Top Picks Proof July", "2026-07-15 12:00:00"),
        new TestCopy("Top Picks Proof January", "2026-01-15 12:00:00"),
    };

    private readonly IApplicationHost host;

    public Proofs(IApplicationHost host) => this.host = host;

    private string ProofRoot => Path.Combine(host.Resolve<IApplicationPaths>().DataPath, "top-picks-proof");

    public string Report(ProofOptions options, Version pluginVersion)
    {
        var text = new StringBuilder();
        text.AppendLine("TOP PICKS PROOF RESULTS");
        text.AppendLine(Part("1. Loaded", () => Loaded(pluginVersion)));
        text.AppendLine("2. Playback Reporting file: not read. The pinned Emby SDK (4.9.1.90) has no SQLite library to compile against, "
            + "so the first week comes from each person's last played dates instead (D25, D129).");
        text.AppendLine();
        text.AppendLine(Part("3. Last played date", () => LastPlayed(options.LastPlayedFile)));
        text.AppendLine(Part("4. Date added from .nfo", DateAdded));
        return text.ToString();
    }

    /// <summary>Writes the two test folders, once. Never overwrites or deletes anything.</summary>
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

        return heading + "\n" + body + "\n";
    }

    private string Loaded(Version pluginVersion)
    {
        var zone = TimeZoneInfo.Local;
        return $"Top Picks Proof {pluginVersion} is running in {host.Name} {host.ApplicationVersion} ({host.OperatingSystemDisplayName}).\n"
            + $"Server time zone: {zone.Id}, now UTC{DateTimeOffset.Now.ToString("zzz", CultureInfo.InvariantCulture)}. "
            + $"Checked {DateTimeOffset.UtcNow.ToString(TimeFormat, CultureInfo.InvariantCulture)} UTC.";
    }

    private string LastPlayed(string videoFile)
    {
        if (string.IsNullOrWhiteSpace(videoFile))
        {
            return "Not checked yet: pick a video you last watched in summer, then press Save.";
        }

        var item = host.Resolve<ILibraryManager>().FindByPath(videoFile, false);
        if (item == null)
        {
            return "Emby has no item for that file. Pick the file of a film or episode in your libraries.";
        }

        var userData = host.Resolve<IUserDataManager>();
        var lines = new List<string>();
        foreach (var user in host.Resolve<IUserManager>().GetUserList(new UserQuery()))
        {
            var data = userData.GetUserData(user, item);
            if (data?.LastPlayedDate is DateTimeOffset played)
            {
                lines.Add($"{user.Name}: as stored {played.ToString(TimeFormat + " zzz", CultureInfo.InvariantCulture)}"
                    + $" = {ServerTime(played)} server time (played: {data.Played}, play count: {data.PlayCount})");
            }
        }

        return $"\"{item.Name}\" (Emby id {item.InternalId})\n"
            + (lines.Count == 0 ? "Nobody has a last played date for it." : string.Join("\n", lines));
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

            var foundBy = "its path";
            var item = library.FindByPath(strm, false);
            if (item == null)
            {
                foundBy = "its name";
                item = library.GetItemList(new InternalItemsQuery { Name = copy.Title, IncludeItemTypes = new[] { "Movie" }, Recursive = true })
                    .FirstOrDefault();
            }

            if (item == null)
            {
                lines.Add($"{copy.Title}: written as {copy.DateAdded} server time, not in Emby yet. Refresh the test library, then open this page again.");
                continue;
            }

            lines.Add($"{copy.Title}: written as {copy.DateAdded} server time. Emby holds "
                + $"{item.DateCreated.UtcDateTime.ToString(TimeFormat, CultureInfo.InvariantCulture)} UTC = {ServerTime(item.DateCreated)} server time. "
                + Verdict(copy.DateAdded, item.DateCreated) + $" (found by {foundBy}, Emby id {item.InternalId})");
        }

        return string.Join("\n", lines);
    }

    private static string Verdict(string written, DateTimeOffset held)
    {
        var writtenLocal = DateTime.ParseExact(written, TimeFormat, CultureInfo.InvariantCulture);
        var difference = TimeZoneInfo.ConvertTime(held, TimeZoneInfo.Local).DateTime - writtenLocal;
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
        if (movie.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return "Not written: the movie file must not be inside top-picks-proof.";
        }

        if (Directory.Exists(root))
        {
            var found = WhatIsThere(root);
            if (found.Count > 0)
            {
                var expected = TestCopies.SelectMany(c => new[] { c.FolderPath(root), c.StrmPath(root), c.NfoPath(root) });
                return found.SetEquals(expected)
                    ? "Already written, so nothing was done (it writes only once)."
                    : "Not written: top-picks-proof already holds something this plugin didn't write, or an unfinished write. "
                        + "Delete that folder by hand, then tick the box again.";
            }
        }

        Directory.CreateDirectory(root);
        foreach (var copy in TestCopies)
        {
            Directory.CreateDirectory(copy.FolderPath(root));
            WriteNewFile(copy.StrmPath(root), movie);
            WriteNewFile(copy.NfoPath(root), Nfo(copy));
        }

        return $"Written {DateTimeOffset.UtcNow.ToString(TimeFormat, CultureInfo.InvariantCulture)} UTC: two test folders in {root}, "
            + $"each pointing to {movie}. Now refresh the test library.";
    }

    // Everything at the top of top-picks-proof, plus what is inside the two expected folders. A link (junction) or a
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

        public string DateAdded { get; }

        private string FolderName => Title + " (2026)";

        public string FolderPath(string root) => Path.Combine(root, FolderName);

        public string StrmPath(string root) => Path.Combine(FolderPath(root), FolderName + ".strm");

        public string NfoPath(string root) => Path.Combine(FolderPath(root), FolderName + ".nfo");
    }
}
