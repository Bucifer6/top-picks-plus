using System.IO;
using TopPicksPlus.Storage;
using Xunit;

namespace TopPicksPlus.Tests.Storage;

/// <summary>D151: the Top Picks folder is made before a library is created on it.</summary>
public class LibraryFolderTests
{
    [Fact, Trait("Check", "C102")]
    public void C102_the_top_picks_folder_is_made_when_missing_and_left_alone_when_there()
    {
        using var s = new Sandbox();
        Directory.Delete(s.TopPicks);
        Assert.False(Directory.Exists(s.TopPicks));

        s.Copies.EnsureTopPicksFolder();
        Assert.True(Directory.Exists(s.TopPicks));

        Sandbox.Write(Path.Combine(s.TopPicks, "Keep me", "note.txt"));
        s.Copies.EnsureTopPicksFolder();
        Assert.True(File.Exists(Path.Combine(s.TopPicks, "Keep me", "note.txt")));
    }
}
