using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace TopPicksPlus.Tests;

/// <summary>
/// The boundary check (decision D20). Each folder of the plugin may only use the parts listed in
/// its README under "May use". This test fails the build if code crosses a wall.
/// The table below must match the READMEs and docs/modules.md.
/// </summary>
public class BoundaryTests
{
    private const string Root = "TopPicksPlus";
    private const string Emby = "MediaBrowser"; // the Emby SDK's main namespaces start with this
    private const string EmbyToo = "Emby";      // and some start with this (Emby.*); banned wherever Emby is

    private static readonly string[] Parts = { "Logic", "Store", "Artwork", "EmbyBridge", "Run", "Settings" };

    /// <summary>What each part MAY use. Any other part, and Emby unless listed, is off-limits.</summary>
    private static readonly Dictionary<string, string[]> MayUse = new()
    {
        ["Logic"] = Array.Empty<string>(),
        ["Store"] = new[] { "Logic" },
        ["Artwork"] = new[] { "Logic" },
        ["EmbyBridge"] = new[] { "Logic", "Settings", Emby },
        ["Run"] = new[] { "Logic", "Store", "Artwork", "EmbyBridge" },
        ["Settings"] = new[] { "Logic", Emby },
    };

    private static readonly string[] NoFiles =
        { "System.IO.File", "System.IO.Directory", "System.IO.FileStream", "System.IO.FileInfo", "System.IO.DirectoryInfo" };

    /// <summary>Extra bans beyond "other parts" (docs/modules.md "Banned for every part").
    /// No part may use the network or start programs; Logic and Artwork never touch files; only Artwork may use SkiaSharp.</summary>
    private static readonly Dictionary<string, string[]> AlsoBanned = new()
    {
        ["Logic"] = NoFiles.Concat(new[] { "System.Net", "System.Diagnostics.Process", "System.Threading.Tasks", "SkiaSharp" }).ToArray(),
        ["Store"] = new[] { "System.Net", "System.Diagnostics.Process", "SkiaSharp" },
        ["Artwork"] = NoFiles.Concat(new[] { "System.Net", "System.Diagnostics.Process" }).ToArray(),
        ["EmbyBridge"] = new[] { "System.Net", "System.Diagnostics.Process", "SkiaSharp" },
        ["Run"] = new[] { "System.Net", "System.Diagnostics.Process", "SkiaSharp" },
        ["Settings"] = new[] { "System.Net", "System.Diagnostics.Process", "SkiaSharp" },
    };

    private static Assembly Plugin => Assembly.Load(Root);

    public static IEnumerable<object[]> AllParts() => Parts.Select(p => new object[] { p });

    [Theory]
    [MemberData(nameof(AllParts))]
    public void Part_only_uses_what_its_README_allows(string part)
    {
        // Parts must not reach each other through the root class either, so root types are banned too.
        var rootTypes = Plugin.GetTypes().Where(t => t.Namespace == Root).Select(t => t.FullName!).ToArray();
        var failing = Violations(Plugin, Root, part, rootTypes);
        Assert.True(failing.Length == 0, $"{part} crosses a wall. Types: {string.Join(", ", failing)}");
    }

    [Fact]
    public void The_check_catches_a_real_wall_break()
    {
        // Self-test: Fixture.Logic uses Emby and Fixture.Store uses Fixture.Run, so both must be caught.
        // Without this the wall tests above could pass only because the plugin has no code yet.
        var tests = typeof(BoundaryTests).Assembly;
        Assert.NotEmpty(Violations(tests, "Fixture", "Logic", Array.Empty<string>()));
        Assert.NotEmpty(Violations(tests, "Fixture", "Store", Array.Empty<string>()));
        Assert.Empty(Violations(tests, "Fixture", "Run", Array.Empty<string>()));
        // The network ban bites too (Fixture.Settings opens a socket type).
        Assert.NotEmpty(Violations(tests, "Fixture", "Settings", Array.Empty<string>()));
        // Each extra ban bites on its own (v4 review Build 24): Task in Logic, file types in Artwork,
        // starting a program in Store, and a root type used by a part.
        Assert.Contains("Fixture.Logic.UsesTask", Violations(tests, "Fixture", "Logic", Array.Empty<string>()));
        Assert.Contains("Fixture.Artwork.UsesFileInfo", Violations(tests, "Fixture", "Artwork", Array.Empty<string>()));
        Assert.Contains("Fixture.Store.UsesProcess", Violations(tests, "Fixture", "Store", Array.Empty<string>()));
        var fixtureRoot = tests.GetTypes().Where(t => t.Namespace == "Fixture").Select(t => t.FullName!).ToArray();
        Assert.NotEmpty(fixtureRoot);
        Assert.Contains("Fixture.EmbyBridge.UsesRoot", Violations(tests, "Fixture", "EmbyBridge", fixtureRoot));
        Assert.Empty(Violations(tests, "Fixture", "EmbyBridge", Array.Empty<string>()));
    }

    internal static string[] Violations(Assembly assembly, string root, string part, string[] alsoBanned)
    {
        var allowed = MayUse[part];
        var banned = Parts.Where(p => p != part && !allowed.Contains(p)).Select(p => $"{root}.{p}").ToList();
        if (!allowed.Contains(Emby)) { banned.Add(Emby); banned.Add(EmbyToo); }
        if (AlsoBanned.TryGetValue(part, out var extra)) banned.AddRange(extra);
        banned.AddRange(alsoBanned);

        var result = Types.InAssembly(assembly)
            .That().ResideInNamespace($"{root}.{part}")
            .ShouldNot().HaveDependencyOnAny(banned.ToArray())
            .GetResult();
        return (result.FailingTypeNames ?? Enumerable.Empty<string>()).ToArray();
    }

    [Fact]
    public void Every_code_folder_has_a_boundary_rule_and_every_rule_has_a_folder()
    {
        // Stops a new folder sneaking in without walls (core rule 8: new folders need the owner's approval).
        var folders = Directory.GetDirectories(SourceDir())
            .Select(Path.GetFileName)
            .Where(n => n is not ("bin" or "obj" or "Properties") && !n!.StartsWith(".", StringComparison.Ordinal))
            .OrderBy(n => n)
            .ToArray();
        Assert.Equal(Parts.OrderBy(p => p).ToArray(), folders);
    }

    [Theory]
    [MemberData(nameof(AllParts))]
    public void Every_part_has_a_README_with_the_five_sections(string part)
    {
        var readme = Path.Combine(SourceDir(), part, "README.md");
        Assert.True(File.Exists(readme), $"{part} has no README.md");
        var text = File.ReadAllText(readme);
        foreach (var heading in new[] { "## Responsible for", "## Belongs here", "## Does not belong", "## May use", "## Must not use" })
            Assert.Contains(heading, text);
    }

    [Fact]
    public void Plugin_code_lives_in_the_parts_or_the_root_only()
    {
        // A type in an unknown namespace (e.g. TopPicksPlus.Helpers) would escape every rule above.
        var stray = Plugin.GetTypes()
            .Select(t => t.Namespace ?? "")
            .Where(ns => ns.StartsWith(Root, StringComparison.Ordinal))
            .Where(ns => ns != Root && !Parts.Any(p => ns == $"{Root}.{p}" || ns.StartsWith($"{Root}.{p}.", StringComparison.Ordinal)))
            .Distinct()
            .ToArray();
        Assert.Empty(stray);
    }

    [Fact]
    public void Every_plugin_type_is_in_the_root_or_a_part_namespace()
    {
        // A type outside TopPicksPlus.* (e.g. namespace Helpers) would escape every wall (v4 review Build 7).
        // Types the compiler adds by itself (nullable attributes, <Module>, lambdas' hidden classes) are allowed.
        var outside = Plugin.GetTypes()
            .Where(t => !t.Name.StartsWith("<", StringComparison.Ordinal))
            .Where(t => t.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false).Length == 0)
            .Where(t => t.Namespace is not ("Microsoft.CodeAnalysis" or "System.Runtime.CompilerServices"))
            .Where(t => t.Namespace == null || !(t.Namespace == Root || t.Namespace.StartsWith(Root + ".", StringComparison.Ordinal)))
            .Select(t => t.FullName)
            .ToArray();
        Assert.Empty(outside);
    }

    private static string SourceDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "src", Root)))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "src", Root);
    }
}
