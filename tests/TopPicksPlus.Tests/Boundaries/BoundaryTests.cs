// Boundary check: fails the build if code crosses a wall in src/TopPicksPlus/README.md.
// Changing these rules needs the owner's approval (build-software-safely, Modular structure).
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NetArchTest.Rules;
using Xunit;

namespace TopPicksPlus.Tests.Boundaries;

public class BoundaryTests
{
    private const string Root = "TopPicksPlus";
    private static readonly string[] Parts = { "Logic", "Storage", "Artwork", "EmbyLink", "Settings" };
    private static readonly string[] Emby = { "MediaBrowser", "Emby" };
    private const string Skia = "SkiaSharp";
    private static readonly string[] RootTypes = { "TopPicksPlus.Plugin", "TopPicksPlus.PluginConfiguration" };

    private static Assembly Plugin => Assembly.Load("TopPicksPlus");

    private static string Ns(string part) => $"{Root}.{part}";

    private static void AssertNoDependency(string part, params string[] forbidden)
    {
        var result = Types.InAssembly(Plugin)
            .That().ResideInNamespace(Ns(part))
            .ShouldNot().HaveDependencyOnAny(forbidden)
            .GetResult();
        var offenders = result.FailingTypeNames ?? Enumerable.Empty<string>();
        Assert.True(result.IsSuccessful,
            $"{part} crosses a wall. Offending types: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Logic_uses_nothing_else() =>
        AssertNoDependency("Logic",
            Emby.Concat(new[] { Skia, Ns("Storage"), Ns("Artwork"), Ns("EmbyLink"), Ns("Settings") })
                .Concat(RootTypes).ToArray());

    [Fact]
    public void Storage_uses_only_Logic() =>
        AssertNoDependency("Storage",
            Emby.Concat(new[] { Skia, Ns("Artwork"), Ns("EmbyLink"), Ns("Settings") })
                .Concat(RootTypes).ToArray());

    [Fact]
    public void Artwork_uses_only_Logic_and_SkiaSharp() =>
        AssertNoDependency("Artwork",
            Emby.Concat(new[] { Ns("Storage"), Ns("EmbyLink"), Ns("Settings") })
                .Concat(RootTypes).ToArray());

    [Fact]
    public void EmbyLink_does_not_use_Settings() =>
        AssertNoDependency("EmbyLink", Ns("Settings"));

    [Fact]
    public void Settings_does_not_use_Artwork() =>
        AssertNoDependency("Settings", Ns("Artwork"));

    // The checks above go by namespace, so a file put in the wrong folder (or a
    // namespace typed wrongly) would slip past them. These two keep folders,
    // namespaces and the approved list of parts in step.

    [Fact]
    public void Only_approved_folders_exist()
    {
        var src = Path.Combine(RepoRoot(), "src", Root);
        var unexpected = Directory.GetDirectories(src)
            .Select(Path.GetFileName)
            .Where(d => d is not ("bin" or "obj"))
            .Where(d => !Parts.Contains(d!))
            .ToArray();
        Assert.True(unexpected.Length == 0,
            $"Folders not in the approved layout: {string.Join(", ", unexpected)}. New parts need the owner's approval.");
    }

    [Fact]
    public void Every_file_declares_the_namespace_of_its_folder()
    {
        var src = Path.Combine(RepoRoot(), "src", Root);
        var nsLine = new Regex(@"^\s*namespace\s+([\w.]+)", RegexOptions.Multiline);
        var wrong = Directory.GetFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(f =>
            {
                var rel = Path.GetRelativePath(src, f);
                var folder = Path.GetDirectoryName(rel);
                var expected = string.IsNullOrEmpty(folder)
                    ? Root
                    : $"{Root}.{folder.Split(Path.DirectorySeparatorChar)[0]}";
                var found = nsLine.Matches(File.ReadAllText(f)).Select(m => m.Groups[1].Value).ToArray();
                bool ok = found.Length > 0 && found.All(n => n == expected || n.StartsWith(expected + ".", StringComparison.Ordinal))
                          && (expected != Root || found.All(n => n == Root));
                return ok ? null : $"{rel} (expected namespace {expected}, found {string.Join("/", found)})";
            })
            .Where(x => x != null)
            .ToArray();
        Assert.True(wrong.Length == 0, "Files in the wrong namespace: " + string.Join("; ", wrong));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CLAUDE.md")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Repository root (CLAUDE.md) not found.");
    }
}
