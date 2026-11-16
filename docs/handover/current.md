# Handover

## Job just finished
2026-11-16: B55 fixed (Emby hides `[ReadOnly(true)]` fields); v1.0.0 released before the proof (D149). The owner then installed v1.0.0 and uninstalled the old plugin. The first run stopped on a leftover path `L:\Movies` in the Movies library (removed with the owner's OK); after that the first run **finished** and the owner is happy with the row. Then: v1.0.1 (D150: a missing folder with no titles is skipped; run errors show as notices) and v1.1.0 (D151: create the Top Picks library if missing, on by default; repo README.md). Both released by CI (runs 139, 141).

## Where it stopped
Live on the owner's server with v1.0.0 (v1.1.0 not installed yet). The proof plugin is still installed and its steps 7–13 were skipped; the owner said proofing and testing wait until they ask. The owner asked about showing the picks as a slideshow on the home page; options were given (Emby's apps draw rows themselves; the owner's separate Random Slideshows plugin shuffles and can't mix movies and series; building a ranked slideshow into Top Picks Plus means injecting a script into Emby's web client, web/desktop only), waiting on the owner's choice.

## What is still owed (smallest first)
1. Owner installs v1.1.0 (swap the DLL, restart).
2. Slideshow: owner's choice; if built, it is its own decision and v1.2.0.
3. B56 (first row check failed at 07:02 but the row was right 15 minutes later: check the wait at the next run) and B57 (`Yellowstone (2018) (2018)`: don't add a year the title already ends with).
4. The proof (README steps 7–13) and the go-live checks, when the owner asks; then delete proof/ and the `proof` job (D130). Go-live notes: B26, B49, B50, B51, B53, B54.

## Watch out for
- **Stacked PRs:** merge in order. If one is merged out of order, its base may need changing by hand.
- **Draft pull requests don't run CI (D115)**, because a draft is work in progress: open it as a normal PR.
- **The repo is public:** never commit secrets or personal details, because anyone can read it. When recording proof results, leave out user names and file paths.
- **No local builds (D5),** because the owner chose CI as the only build; this session ran on the owner's PC, which is the Emby server (D133). Never touch Emby's folders from a session. Emby signatures can be read, read-only, with a reflection script over the SDK DLLs in the NuGet cache (card 0019, D145).
- **Don't push while CI is running,** because a new push cancels the running Windows job and starts again.
- **CI logs need a signed-in browser:** the public API gives job results and annotations but not the compiler output; open the job page in the browser pane and expand the failed step.
- **There is no GitHub CLI on the owner's PC:** push works through git; PRs are opened in the browser pane while the owner is signed in to GitHub. The pane wasn't signed in on 2026-11-16, so with the owner's OK fixes were merged into main directly; CI on main gated each release tag.
- **Git has no identity on this PC:** commit with `git -c user.name=Claude -c user.email=noreply@anthropic.com`; in Windows PowerShell pass commit messages with `-F <file>` (quotes in `-m` break).
- **Emby 4.10's remove-library-path API needs `Id`,** not just `name` (a name-only DELETE gave a 500).
- **Watched state only moves forward; an offline drive or vanished title never empties the row (D147),** because the owner's real library and watched ticks are at stake.
- **Windows-only tests are named `_junction`/`_windows`**; the Windows job fails unless at least 11 pass (D125), so raise the number when adding more.
- **No backup (D95, owner).** Never write "restore from backup", because there is none.
- Commit as Claude (noreply@anthropic.com); a stop hook checks.

## Waiting on the owner
- Run the rest of the proof (steps 7–13) and paste the results.

## Next job
Record the proof results (D132, D134, D149), v1.0.1 if they need a change, then go-live day (D86). Read first: CLAUDE.md, CODEMAP.md, docs/decisions.md D129–D148, docs/backlog.md Go-live rows.

## Cards that applied
0001, 0003, 0004, 0006, 0007, 0012–0015, 0017, 0018; new: 0019, 0020. None retired.
