using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TopPicksPlus.Logic;
using TopPicksPlus.Storage;
using Xunit;

namespace TopPicksPlus.Tests.Storage;

/// <summary>
/// A throwaway folder per test (D36, D43): <c>plugins\TopPicksPlus</c> (the data folder, with staging), <c>data\top-picks</c> and
/// <c>outside</c> (a folder no delete may ever reach). Every Storage test is tagged [Trait("Check", "Cxx")].
/// </summary>
internal sealed class Sandbox : IDisposable
{
    public static readonly DateTime Now = new DateTime(2026, 11, 15, 5, 0, 30, DateTimeKind.Utc);
    public static readonly DateTime RankTime = new DateTime(2026, 11, 15, 5, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime Sunday = new DateTime(2026, 11, 15);

    public Sandbox()
    {
        Root = Path.Combine(Path.GetTempPath(), "tpp-" + Guid.NewGuid().ToString("N"));
        DataRoot = Path.Combine(Root, "plugins", "TopPicksPlus");
        TopPicks = Path.Combine(Root, "data", "top-picks");
        Outside = Path.Combine(Root, "outside");
        Directory.CreateDirectory(DataRoot);
        Directory.CreateDirectory(TopPicks);
        Directory.CreateDirectory(Outside);
        Data = new DataFolder(DataRoot);
        Copies = new CopyFolders(TopPicks, Data);
    }

    public string Root { get; }
    public string DataRoot { get; }
    public string TopPicks { get; }
    public string Outside { get; }
    public DataFolder Data { get; }
    public CopyFolders Copies { get; }

    public string Staging => Data.StagingPath;

    public void Dispose()
    {
        SafePaths.BeforeReplace = null;
        try
        {
            // Never follows a link, so a test's junction to "outside" can't take anything else with it.
            SafePaths.DeleteTree(Root);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public static string Write(string path, string text = "x")
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text, new UTF8Encoding(false));
        return path;
    }

    public string InTopPicks(string folder, string relative) => Path.Combine(TopPicks, folder, relative.Replace('\\', Path.DirectorySeparatorChar));

    /// <summary>Makes a folder in top-picks with these files (relative, <c>\</c>-separated).</summary>
    public string Folder(string name, params string[] files)
    {
        var dir = Path.Combine(TopPicks, name);
        Directory.CreateDirectory(dir);
        foreach (var f in files)
            Write(InTopPicks(name, f), name + "/" + f);
        return dir;
    }

    /// <summary>Prepares a copy in staging, as prepare would (D26).</summary>
    public void Staged(string name, params string[] files)
    {
        foreach (var f in files)
            Copies.WriteStagingText(name, f, name + "/" + f + "/new");
    }

    public static string Text(string path) => File.ReadAllText(path, Encoding.UTF8);

    public string DataFile(string name) => Path.Combine(DataRoot, name);

    /// <summary>Writes a data file with a chosen version, as a newer plugin would.</summary>
    public void WriteVersion(string fileName, int version, string body = "")
    {
        Write(DataFile(fileName), "{\n  \"version\": " + version + (body.Length > 0 ? ",\n  " + body : string.Empty) + "\n}");
    }

    /// <summary>Every file under the sandbox (links not entered) with a hash of its bytes, to prove nothing changed.</summary>
    public IReadOnlyDictionary<string, string> Snapshot()
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);
        Walk(new DirectoryInfo(Root), result);
        return result;
    }

    private void Walk(DirectoryInfo dir, SortedDictionary<string, string> result)
    {
        foreach (var e in dir.EnumerateFileSystemInfos())
        {
            var rel = Path.GetRelativePath(Root, e.FullName);
            if ((e.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            {
                result[rel] = "link";
                continue;
            }
            if ((e.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                result[rel + "/"] = "dir";
                Walk((DirectoryInfo)e, result);
            }
            else
            {
                result[rel] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(e.FullName)));
            }
        }
    }

    public IReadOnlyList<ManagedEntry> Managed() => Data.LoadManaged().Value!;

    public ManagedEntry Entry(string folder) => Managed().Single(e => e.Folder == folder);

    /// <summary>Stops the next atomic save to a file whose name ends with <paramref name="fileName"/>, just before its rename.</summary>
    public static void StopBeforeSaving(string fileName)
    {
        SafePaths.BeforeReplace = target =>
        {
            if (target.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
                throw new SimulatedStop(fileName);
        };
    }

    public static void NoStop() => SafePaths.BeforeReplace = null;
}

/// <summary>Stands for Emby stopping (or the server losing power) at that exact moment.</summary>
internal sealed class SimulatedStop : Exception
{
    public SimulatedStop(string where)
        : base("stopped before saving " + where)
    {
    }
}

/// <summary>
/// Junctions and symbolic links for the D37/D82/C45/C49/C80 tests. Junctions exist only on Windows, so those tests skip with a
/// reason elsewhere (D43, D71) and CI proves they ran and passed on the Windows job (build.yml, card 0003).
/// </summary>
internal static class Links
{
    public const string WindowsOnly = "junctions exist only on Windows; this test runs and must pass on the Windows CI job (D43, D71)";

    public static void Junction(string link, string target)
    {
        Skip.IfNot(OperatingSystem.IsWindows(), WindowsOnly);
        var info = new ProcessStartInfo("cmd.exe", "/c mklink /J \"" + link + "\" \"" + target + "\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var p = Process.Start(info)!;
        var err = p.StandardError.ReadToEnd();
        p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        Assert.Equal(string.Empty, err.Trim());
        Assert.Equal(0, p.ExitCode);
        Assert.True(SafePaths.IsLink(link), "the junction is not a reparse point");
    }

    /// <summary>A folder symbolic link (works on Linux, and on Windows CI, which runs as an administrator).</summary>
    public static void FolderSymlink(string link, string target)
    {
        try
        {
            Directory.CreateSymbolicLink(link, target);
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            Skip.If(true, "this machine doesn't allow symbolic links: " + e.Message);
        }
        Assert.True(SafePaths.IsLink(link));
    }

    public static void FileSymlink(string link, string target)
    {
        try
        {
            File.CreateSymbolicLink(link, target);
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            Skip.If(true, "this machine doesn't allow symbolic links: " + e.Message);
        }
        Assert.True(SafePaths.IsLink(link));
    }
}

/// <summary>Windows 8.3 short names (B42, D119), for the test that a short spelling of a folder name is refused.</summary>
internal static class ShortNames
{
    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
    private static extern uint GetShortPathName(string longPath, StringBuilder shortPath, uint size);

    /// <summary>Gives the folder a short name (tries <c>fsutil</c>; CI runs as an administrator) and returns it, or null if none.</summary>
    public static string? Make(string folder, string wanted)
    {
        if (!OperatingSystem.IsWindows())
            return null;
        try
        {
            var info = new ProcessStartInfo("fsutil.exe", "file setshortname \"" + folder + "\" " + wanted)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var p = Process.Start(info)!;
            p.StandardOutput.ReadToEnd();
            p.StandardError.ReadToEnd();
            p.WaitForExit();
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception || e is InvalidOperationException)
        {
        }
        var sb = new StringBuilder(1024);
        if (GetShortPathName(folder, sb, (uint)sb.Capacity) == 0)
            return null;
        var name = Path.GetFileName(sb.ToString());
        return string.Equals(name, Path.GetFileName(folder), StringComparison.OrdinalIgnoreCase) ? null : name;
    }
}
