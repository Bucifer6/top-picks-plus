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
    private static readonly string[] RootTypeNames = { "Plugin", "PluginConfiguration" };
    private static readonly string[] RootTypes = RootTypeNames.Select(n => $"{Root}.{n}").ToArray();
    // Logic is pure: no files, network, time-zone lookups or Emby's third-party dependencies.
    private static readonly string[] LogicExtra =
        { "System.IO", "System.Net", "System.TimeZoneInfo", "Newtonsoft", "Microsoft.Extensions", "SQLitePCL" };
    // The one file allowed to declare a namespace outside TopPicksPlus (IsExternalInit for netstandard2.0).
    private const string PolyfillFile = "Polyfills.cs";
    private const string PolyfillNamespace = "System.Runtime.CompilerServices";

    private static Assembly Plugin => Assembly.Load("TopPicksPlus");

    private static string Ns(string part) => $"{Root}.{part}";

    private static string[] Forbidden(string part) => part switch
    {
        "Logic" => Emby.Concat(LogicExtra)
            .Concat(new[] { Skia, Ns("Storage"), Ns("Artwork"), Ns("EmbyLink"), Ns("Settings") })
            .Concat(RootTypes).ToArray(),
        "Storage" => Emby.Concat(new[] { Skia, Ns("Artwork"), Ns("EmbyLink"), Ns("Settings") })
            .Concat(RootTypes).ToArray(),
        "Artwork" => Emby.Concat(new[] { Ns("Storage"), Ns("EmbyLink"), Ns("Settings") })
            .Concat(RootTypes).ToArray(),
        "EmbyLink" => new[] { Skia, Ns("Settings") },
        "Settings" => new[] { Skia, Ns("Artwork") },
        _ => throw new ArgumentException(part),
    };

    internal static TestResult Check(Assembly assembly, string ns, string[] forbidden) =>
        Types.InAssembly(assembly)
            .That().ResideInNamespace(ns)
            .ShouldNot().HaveDependencyOnAny(forbidden)
            .GetResult();

    [Theory]
    [InlineData("Logic")]
    [InlineData("Storage")]
    [InlineData("Artwork")]
    [InlineData("EmbyLink")]
    [InlineData("Settings")]
    public void Part_stays_inside_its_walls(string part)
    {
        var result = Check(Plugin, Ns(part), Forbidden(part));
        var offenders = result.FailingTypeNames ?? Enumerable.Empty<string>();
        Assert.True(result.IsSuccessful, $"{part} crosses a wall. Offending types: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Project_root_holds_only_Plugin_and_PluginConfiguration()
    {
        var extra = Plugin.GetTypes()
            .Where(t => t.Namespace == Root && !t.Name.StartsWith("<", StringComparison.Ordinal))
            .Where(t => t.DeclaringType == null)
            .Select(t => t.Name)
            .Where(n => !RootTypeNames.Contains(n))
            .ToArray();
        Assert.True(extra.Length == 0,
            $"Only Plugin and PluginConfiguration may live in the project root. Found: {string.Join(", ", extra)}");
    }

    // Proves the rules bite: the Canary types below deliberately break Logic's walls.
    [Fact]
    public void Canary_a_Logic_type_using_files_is_caught()
    {
        var result = Check(typeof(Canary.Logic.ReadsAFile).Assembly, "TopPicksPlus.Tests.Boundaries.Canary.Logic", Forbidden("Logic"));
        Assert.False(result.IsSuccessful);
        Assert.Contains(typeof(Canary.Logic.ReadsAFile).FullName, result.FailingTypeNames ?? Array.Empty<string>());
    }

    [Fact]
    public void Canary_a_Logic_type_using_another_part_is_caught()
    {
        var result = Check(typeof(Canary.Logic.UsesEmbyLink).Assembly, "TopPicksPlus.Tests.Boundaries.Canary.Logic",
            new[] { "TopPicksPlus.Tests.Boundaries.Canary.EmbyLink" });
        Assert.False(result.IsSuccessful);
    }

    // The checks above go by namespace, so a file put in the wrong folder (or a
    // namespace typed wrongly) would slip past them. These keep folders,
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
        var sep = Path.DirectorySeparatorChar;
        var wrong = Directory.GetFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{sep}obj{sep}") && !f.Contains($"{sep}bin{sep}"))
            .Select(f =>
            {
                var rel = Path.GetRelativePath(src, f);
                var folder = Path.GetDirectoryName(rel);
                var found = nsLine.Matches(File.ReadAllText(f)).Select(m => m.Groups[1].Value).ToArray();
                if (rel == PolyfillFile)
                    return found.Length > 0 && found.All(n => n == PolyfillNamespace)
                        ? null : $"{rel} (may only declare {PolyfillNamespace})";
                var expected = string.IsNullOrEmpty(folder) ? Root : $"{Root}.{folder.Split(sep)[0]}";
                bool ok = found.Length > 0
                          && found.All(n => n == expected || n.StartsWith(expected + ".", StringComparison.Ordinal))
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
