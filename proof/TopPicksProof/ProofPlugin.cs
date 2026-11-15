using System;
using MediaBrowser.Common;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;

namespace TopPicksProof;

/// <summary>
/// "Top Picks Proof" (D76, D77): one settings page that proves things on the owner's server before chunk 4 is kept.
/// It listens to nothing, deletes nothing and never touches the Top Picks folder.
/// </summary>
public sealed class ProofPlugin : BasePluginSimpleUI<ProofOptions>
{
    private static readonly Guid PluginId = new Guid("f48a4d65-31b3-42a1-bb92-ca577d011926");

    private readonly Proofs proofs;
    private readonly ILogger logger;

    public ProofPlugin(IApplicationHost applicationHost, ILogManager logManager)
        : base(applicationHost)
    {
        logger = logManager.GetLogger("TopPicksProof");
        proofs = new Proofs(applicationHost);
    }

    public override string Name => "Top Picks Proof";

    public override string Description => "Temporary proof for Top Picks Plus. Uninstall it after sending the results.";

    public override Guid Id => PluginId;

    // Runs each time the page opens, so the results are always fresh and nothing is kept between openings.
    protected override ProofOptions OnBeforeShowUI(ProofOptions options)
    {
        options.Results = proofs.Report(options, Version);
        logger.Info("Top Picks Proof results:\n{0}", options.Results);
        return options;
    }

    // The page has no buttons (Simple UI), so writing the test folders is a tick box acted on when the owner saves.
    protected override bool OnOptionsSaving(ProofOptions options)
    {
        if (options.WriteTestFolders)
        {
            options.WriteOutcome = proofs.WriteTestFolders(options.TestMovieFile);
            options.WriteTestFolders = false;
            logger.Info("Top Picks Proof write: {0}", options.WriteOutcome);
        }

        return true;
    }
}
