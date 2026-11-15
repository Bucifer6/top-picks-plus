using System;
using System.IO;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using TopPicksPlus.Storage;

namespace TopPicksPlus.EmbyLink;

/// <summary>
/// The Emby services EmbyLink uses (B27: this is the full list) and the plugin's folders. Built once by the entry point, which
/// Emby creates with these services; the scheduled task and the settings page reach it through <see cref="Current"/>.
/// </summary>
public sealed class Host
{
    public Host(
        IApplicationHost app,
        IApplicationPaths paths,
        ILibraryManager library,
        IUserManager users,
        IUserDataManager userData,
        IUserViewManager userViews,
        IProviderManager providers,
        IFileSystem fileSystem,
        ILogger log)
    {
        App = app;
        Library = library;
        Users = users;
        UserData = userData;
        UserViews = userViews;
        Providers = providers;
        FileSystem = fileSystem;
        Log = log;
        // D35: the Top Picks folder is Emby's data path + top-picks; the plugin's data folder is programdata\plugins\TopPicksPlus.
        TopPicksPath = Path.Combine(paths.DataPath, "top-picks");
        Data = new DataFolder(Path.Combine(paths.PluginsPath, "TopPicksPlus"));
        Copies = new CopyFolders(TopPicksPath, Data);
    }

    /// <summary>Set by the entry point when Emby starts; null before that.</summary>
    public static Host? Current { get; internal set; }

    public IApplicationHost App { get; }
    public ILibraryManager Library { get; }
    public IUserManager Users { get; }
    public IUserDataManager UserData { get; }
    public IUserViewManager UserViews { get; }
    public IProviderManager Providers { get; }
    public IFileSystem FileSystem { get; }
    public ILogger Log { get; }
    public string TopPicksPath { get; }
    public DataFolder Data { get; }
    public CopyFolders Copies { get; }

    /// <summary>The owner's settings (empty settings before the plugin is loaded).</summary>
    public static PluginConfiguration Settings => Plugin.Instance?.Configuration ?? new PluginConfiguration();

    /// <summary>A path inside the Top Picks folder, relative to it (<c>folder\...\x.strm</c>), or null (D28, D64).</summary>
    public string? RelativeToTopPicks(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        var root = TopPicksPath.TrimEnd('\\', '/') + "\\";
        var normal = path!.Replace('/', '\\');
        return normal.StartsWith(root, StringComparison.OrdinalIgnoreCase) && normal.Length > root.Length
            ? normal.Substring(root.Length)
            : null;
    }
}
