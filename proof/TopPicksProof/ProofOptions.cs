using System.ComponentModel;
using System.IO;
using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Elements;
using Emby.Web.GenericEdit.Validation;
using MediaBrowser.Model.Attributes;

namespace TopPicksProof;

/// <summary>The proof page's fields. Emby draws the page from these properties (Simple UI); there is no HTML.</summary>
public class ProofOptions : EditableOptionsBase
{
    public override string EditorTitle => "Top Picks Proof";

    public override string EditorDescription =>
        "Temporary proof for Top Picks Plus. It never touches the Top Picks folder or row and deletes nothing. "
        + "Pick a movie file, tick the box, press Save, then close this page and open it again to see the results.";

    public CaptionItem CaptionDateAdded { get; set; } = new CaptionItem("Date added from .nfo files");

    [DisplayName("Any movie file")]
    [Description("The test folders point to this file. It is only read, never changed.")]
    [EditFilePicker]
    public string TestMovieFile { get; set; } = string.Empty;

    [DisplayName("Write the three test folders when I press Save (only once)")]
    public bool WriteTestFolders { get; set; }

    // No [ReadOnly(true)] here or on Results: Emby 4.10.2 leaves such properties off the page entirely (B55).
    [DisplayName("What happened when the test folders were written")]
    public string WriteOutcome { get; set; } = string.Empty;

    public SpacerItem Spacer { get; set; } = new SpacerItem();

    [DisplayName("Results (copy all of this into the chat)")]
    [EditMultiline(24)]
    public string Results { get; set; } = string.Empty;

    protected override void Validate(ValidationContext context)
    {
        if (WriteTestFolders && !File.Exists(TestMovieFile))
        {
            context.AddValidationError(nameof(TestMovieFile), "Pick an existing movie file before ticking the box.");
        }
    }
}
