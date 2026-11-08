// Deliberate wall-breakers used only by the canary tests in BoundaryTests.cs,
// to prove the boundary rules can fail. Never copy these into the plugin.
namespace TopPicksPlus.Tests.Boundaries.Canary.EmbyLink
{
    public static class Something
    {
        public static int Value() => 1;
    }
}

namespace TopPicksPlus.Tests.Boundaries.Canary.Logic
{
    public static class ReadsAFile
    {
        public static string Read(string path) => System.IO.File.ReadAllText(path);
    }

    public static class UsesEmbyLink
    {
        public static int Read() => EmbyLink.Something.Value();
    }
}
