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
    private const string Emby = "MediaBrowser"; // every Emby SDK namespace starts with this

    private static readonly string[] Parts = { "Logic", "Store", "Artwork", "EmbyBridge", "Run", "Settings" };

    /// <summary>What each part MAY use. Any other part, and Emby unless listed, is off-limits.</summary>
    private static readonly Dictionary<string, string[]> MayUse = new()
    {
        ["Logic"] = Array.Empty<string>(),
        ["Store"] = new[] { "Logic" },
        ["Artwork"] = new[] { "Logic" },
        ["EmbyBridge"] = new[] { "Logic", Emby },
        ["Run"] = new[] { "Logic", "Store", "Artwork", "EmbyBridge" },
        ["Settings"] = new[] { "Logic", Emby },
    };

    /// <summary>Extra bans beyond "other parts": Logic must stay pure, so no files, clocks or network.</summary>
    private static readonly Dictionary<string, string[]> AlsoBanned = new()
    {
        ["Logic"] = new[] { "System.IO.File", "System.IO.Directory", "System.Net", "System.Threading.Tasks", "SkiaSharp" },
        ["Store"] = new[] { "SkiaSharp" },
        ["EmbyBridge"] = new[] { "SkiaSharp" },
        ["Run"] = new[] { "SkiaSharp" },
        ["Settings"] = new[] { "SkiaSharp" },
    };

    private static Assembly Plugin => Assembly.Load(Root);

    public static IEnumerable<object[]> AllParts() => Parts.Select(p => new object[] { p });

    [Theory]
    [MemberData(nameof(AllParts))]
    public void Part_only_uses_what_its_README_allows(string part)
    {
        var allowed = MayUse[part];
        var banned = Parts.Where(p => p != part && !allowed.Contains(p)).Select(p => $"{Root}.{p}").ToList();
        if (!allowed.Contains(Emby)) banned.Add(Emby);
        if (AlsoBanned.TryGetValue(part, out var extra)) banned.AddRange(extra);

        var result = Types.InAssembly(Plugin)
            .That().ResideInNamespace($"{Root}.{part}")
            .ShouldNot().HaveDependencyOnAny(banned.ToArray())
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"{part} crosses a wall. Types: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}. " +
            $"Banned for {part}: {string.Join(", ", banned)}");
    }

    [Fact]
    public void Every_code_folder_has_a_boundary_rule_and_every_rule_has_a_folder()
    {
        // Stops a new folder sneaking in without walls (core rule 8: new folders need the owner's approval).
        var folders = Directory.GetDirectories(SourceDir())
            .Select(Path.GetFileName)
            .Where(n => n is not ("bin" or "obj" or "Properties"))
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

    private static string SourceDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "src", Root)))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "src", Root);
    }
}
