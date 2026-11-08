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

    private static TestResult Check(Assembly assembly, string ns, string[] forbidden) =>
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

    // Every type in the plugin must sit somewhere a wall applies. This also catches classes
    // left outside any namespace and extra classes hidden in Polyfills.cs.
    [Fact]
    public void Every_plugin_type_lives_inside_a_walled_part_or_is_allowed_in_the_root()
    {
        var stray = Plugin.GetTypes()
            .Where(t => t.DeclaringType == null)
            .Where(t => !t.Name.StartsWith("<", StringComparison.Ordinal))
            .Where(t => !IsCompilerMade(t))
            .Where(t => !(t.Namespace == Root && RootTypeNames.Contains(t.Name)))
            .Where(t => !Parts.Any(p => t.Namespace == Ns(p) || (t.Namespace ?? "").StartsWith(Ns(p) + ".", StringComparison.Ordinal)))
            .Where(t => t.FullName != $"{PolyfillNamespace}.IsExternalInit")
            .Select(t => t.FullName ?? t.Name)
            .ToArray();
        Assert.True(stray.Length == 0,
            "Types outside the walled parts (only Plugin, PluginConfiguration and IsExternalInit may be): " + string.Join(", ", stray));
    }

    private static bool IsCompilerMade(Type t) =>
        t.GetCustomAttributes(false).Any(a => a.GetType().FullName is
            "System.Runtime.CompilerServices.CompilerGeneratedAttribute" or "Microsoft.CodeAnalysis.EmbeddedAttribute");

    // Proves the rules bite: for every part, every forbidden entry has a canary type (Canary.cs)
    // that breaks exactly that wall, and the real rule catches it. Removing an entry from a
    // list, or a canary, fails here.
    [Theory]
    [InlineData("Logic")]
    [InlineData("Storage")]
    [InlineData("Artwork")]
    [InlineData("EmbyLink")]
    [InlineData("Settings")]
    public void Canaries_prove_every_wall_bites(string part)
    {
        var test = typeof(BoundaryTests).Assembly;
        var canaryNs = $"{Ns(part)}.BoundaryCanaries";
        var canaries = test.GetTypes()
            .Where(t => t.Namespace == canaryNs)
            .Select(t => (Type: t, Entry: ((BreaksAttribute?)Attribute.GetCustomAttribute(t, typeof(BreaksAttribute)))?.Entry))
            .Where(c => c.Entry != null)
            .ToArray();
        var forbidden = Forbidden(part);

        var uncovered = forbidden.Where(e => canaries.All(c => c.Entry != e)).ToArray();
        Assert.True(uncovered.Length == 0, $"{part}: no canary for {string.Join(", ", uncovered)}");

        foreach (var (type, entry) in canaries)
        {
            Assert.True(forbidden.Contains(entry!), $"{part}: canary {type.Name} breaks '{entry}', which is no longer forbidden");
            var result = Types.InAssembly(test)
                .That().ResideInNamespace(canaryNs).And().HaveName(type.Name)
                .ShouldNot().HaveDependencyOnAny(forbidden)
                .GetResult();
            Assert.False(result.IsSuccessful, $"{part}: canary {type.Name} breaking '{entry}' was not caught");
        }
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
