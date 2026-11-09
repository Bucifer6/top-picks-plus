// C64 and C86 (D75, D89): how a forbidden entry matches. The wall check (BoundaryTests) uses NetArchTest's
// HaveDependencyOnAny, which matches by whole name parts: it splits names at '.', '+' and '/', so an entry matches
// itself and anything inside it (a namespace's types, a type's nested types), never a name that only starts the same.
// These tests prove that against the real Logic wall list, and the canaries prove each test can fail (card 0003).
// Adding tests here changes no wall.
#nullable disable
using System;
using System.Linq;
using System.Reflection;
using NetArchTest.Rules;
using Xunit;
using Canaries = TopPicksPlus.Logic.MatcherCanaries;

namespace TopPicksPlus.Tests.Boundaries
{
    public class MatcherTests
    {
        private const string Ns = "TopPicksPlus.Logic.MatcherCanaries";

        /// <summary>The real Logic list from BoundaryTests, so these tests follow any change to it.</summary>
        private static string[] LogicForbidden() =>
            (string[])typeof(BoundaryTests).GetMethod("Forbidden", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { "Logic" });

        private static bool Caught(string typeName, params string[] forbidden) =>
            !Types.InAssembly(typeof(MatcherTests).Assembly)
                .That().ResideInNamespace(Ns).And().HaveName(typeName)
                .ShouldNot().HaveDependencyOnAny(forbidden)
                .GetResult().IsSuccessful;

        /// <summary>What a plain "starts with" matcher would say: the look-alike would be a false alarm under it.</summary>
        private static bool NaivePrefixHit(string usedFullName, string[] forbidden) =>
            forbidden.Any(f => usedFullName.StartsWith(f, StringComparison.Ordinal));

        [Fact, Trait("Check", "C64")]
        public void C64_look_alike_of_System_IO_passes_the_Logic_wall()
        {
            var forbidden = LogicForbidden();
            Assert.Contains("System.IO", forbidden);
            Assert.False(Caught(nameof(Canaries.UsesIObservable), forbidden), "System.IObservable<T> was wrongly caught by the Logic wall");
        }

        [Fact, Trait("Check", "C64")]
        public void C64_canary_the_look_alike_really_is_one_and_the_real_namespace_is_still_caught()
        {
            var forbidden = LogicForbidden();
            // Under a plain prefix rule this would be a false alarm, so C64 above can fail.
            Assert.True(NaivePrefixHit(typeof(IObservable<int>).FullName, forbidden));
            // The wall still bites on the namespace itself and on a namespace inside it.
            Assert.True(Caught(nameof(Canaries.UsesSystemIO), forbidden));
            Assert.True(Caught(nameof(Canaries.UsesSystemIOCompression), forbidden));
        }

        [Fact, Trait("Check", "C86")]
        public void C86_PluginConfiguration_is_caught_by_its_own_entry_not_by_TopPicksPlus_Plugin()
        {
            Assert.False(Caught(nameof(Canaries.UsesPluginConfiguration), "TopPicksPlus.Plugin"),
                "TopPicksPlus.Plugin wrongly matched TopPicksPlus.PluginConfiguration");
            Assert.True(Caught(nameof(Canaries.UsesPluginConfiguration), "TopPicksPlus.PluginConfiguration"));
            Assert.True(Caught(nameof(Canaries.UsesPluginConfiguration), LogicForbidden()));
        }

        [Fact, Trait("Check", "C86")]
        public void C86_a_nested_type_inside_a_forbidden_type_is_caught()
        {
            Assert.Contains("System.TimeZoneInfo", LogicForbidden());
            Assert.True(Caught(nameof(Canaries.UsesNestedTimeZoneType), "System.TimeZoneInfo"));
            Assert.True(Caught(nameof(Canaries.UsesNestedTimeZoneType), LogicForbidden()));
        }

        [Fact, Trait("Check", "C86")]
        public void C86_canary_type_entries_are_not_namespace_prefixes()
        {
            // A plain prefix rule would have caught PluginConfiguration through TopPicksPlus.Plugin, so C86 can fail.
            Assert.True(NaivePrefixHit("TopPicksPlus.PluginConfiguration", new[] { "TopPicksPlus.Plugin" }));
            // A type that merely shares the start of a forbidden type's name passes (a prefix rule would catch it).
            Assert.True(NaivePrefixHit(typeof(System.TimeZoneInfoish).FullName, new[] { "System.TimeZoneInfo" }));
            Assert.False(Caught(nameof(Canaries.UsesTimeZoneLookAlike), "System.TimeZoneInfo"));
            // And a type that uses nothing forbidden passes the whole Logic list.
            Assert.False(Caught(nameof(Canaries.UsesNothing), LogicForbidden()));
        }
    }
}

namespace System
{
    /// <summary>A test-only type whose full name starts with "System.TimeZoneInfo" but is not that type or inside it.</summary>
    public class TimeZoneInfoish { }
}

namespace TopPicksPlus.Logic.MatcherCanaries
{
    public class UsesIObservable { public static System.IObservable<int> Field; }
    public class UsesSystemIO { public static System.IO.FileInfo Field; }
    public class UsesSystemIOCompression { public static System.IO.Compression.CompressionLevel Field; }
#pragma warning disable CS0436 // the test assembly's fake TopPicksPlus.PluginConfiguration (Canary.cs) wins over the plugin's
    public class UsesPluginConfiguration { public static global::TopPicksPlus.PluginConfiguration Field; }
#pragma warning restore CS0436
    public class UsesNestedTimeZoneType { public static System.TimeZoneInfo.AdjustmentRule Field; }
    public class UsesTimeZoneLookAlike { public static System.TimeZoneInfoish Field; }
    public class UsesNothing { public static int Field; }
}
