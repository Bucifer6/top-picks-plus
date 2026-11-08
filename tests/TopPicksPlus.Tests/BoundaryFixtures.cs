// Deliberate wall breaks used only by BoundaryTests.The_check_catches_a_real_wall_break.
// They live in the test assembly, never in the plugin.
namespace Fixture.Logic
{
    internal static class UsesEmby { internal static System.Type T = typeof(MediaBrowser.Model.Entities.MediaType); }
}

namespace Fixture.Run
{
    internal static class Allowed { internal static int Value = 1; }
}

namespace Fixture.Store
{
    internal static class UsesRun { internal static int V = Fixture.Run.Allowed.Value; }
}

namespace Fixture.Settings
{
    internal static class UsesNetwork { internal static System.Type T = typeof(System.Net.Sockets.TcpClient); }
}
