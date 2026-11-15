using System;
using System.Reflection;
using MediaBrowser.Common;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;

namespace TopPicksProof;

/// <summary>
/// "Top Picks Proof" (D76, D77, D131): one settings page that proves things on the owner's server before chunk 4 is kept.
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

    // The CI stamp (0.0.0-proof.<run>) is only in the informational version; the assembly version is 0.0.0.0.
    private static string BuildVersion =>
        typeof(ProofPlugin).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

    // Runs each time the page opens, so the results are always fresh.
    protected override ProofOptions OnBeforeShowUI(ProofOptions options)
    {
        options.Results = proofs.Report(BuildVersion);
        logger.Info("Top Picks Proof results:\n{0}", options.Results);
        return options;
    }

    // The page has no buttons (Simple UI), so writing the test folders is a tick box acted on when the owner saves.
    // Results are worked out again on every opening, so they are never saved with the page.
    protected override bool OnOptionsSaving(ProofOptions options)
    {
        options.Results = string.Empty;
        if (options.WriteTestFolders)
        {
            options.WriteOutcome = proofs.WriteTestFolders(options.TestMovieFile);
            options.WriteTestFolders = false;
            logger.Info("Top Picks Proof write: {0}", options.WriteOutcome);
        }

        return true;
    }
}
