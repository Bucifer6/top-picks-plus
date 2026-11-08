---
summary: Fresh-reviewer re-review of spec v11 (comprehension, builder readiness, security). Not passed: 3 Blockers (one raised twice). Owner asked whether to simplify copy handling before spec v12.
links: [../spec.md, ../checks.md, ../data.md, ../decisions.md, ../security.md, 2026-11-08-spec-v10-handling.md]
---

# Spec v11 re-review, 2026-11-08

Reviewers: **Comp** (2 Blocker, 5 Should, 4 Nit), **Build** (1 Blocker, 7 Should, 3 Nit), **Sec** (1 Blocker, 4 Should, 3 Nit). Run at the same time on commit 414d163. Stage 4 **not passed**; pull request 1 stays unmerged.

Comp confirms every v10 finding is in the v11 text. Build confirms ReadOnly clearing, link removal, data files that are folders, a copy folder that is a link, every resume point and several quick starts are buildable and testable. Sec confirms the exact path, `CreateNew`, non-recursive link removal and the clear-out keep real media safe.

## Blockers
1. **`File.Move(…, overwrite: true)` doesn't exist on netstandard2.0 (Comp 1, Build 1).** D14 targets netstandard2.0; v11 banned `File.Replace` and native calls are banned (D52), so no atomic replace is left. Fix: go back to `File.Replace` for a target that exists (ReadOnly is now cleared first, which was Sec's v10 reason), or change D14.
2. **The waiter's re-check waits on itself (Comp 2, Build S5).** "Checks again exactly as if it had just arrived" while the holder reads `guard`, and an arrival that sees `guard` waits. Fix: the re-check lists its own conditions (a run waiting to resume, and the other turn-down conditions), never the holder test.
3. **A read-only link blocks Store for ever (Sec 1).** The "link is never given new attributes" acceptance was written for F4 step 7 only; in Store a read-only link at `staging`, a data file name, a `*.tmp` name or this minute's backup name fails every run. Fix: clear ReadOnly on the link itself (with a test that the link's target is unchanged), or the owner accepts it and F9 and security.md say so.

## Should fix
- **Every folder deleted with what is in it uses the delete-guard walk** (Comp 4, Build 8, Sec 4): a folder called `run.json`, `staging` or `Heat (1995).nfo`; tests with a junction and an `x.` inside one.
- **Making folders** (Comp 5, Build 4): the clear-out can remove planned folders (an emptied `Season 1`, `backdrops`), and new season folders appear; the write step must make missing folders, with check 4 first.
- **Retry** (Comp 6, Build 6, Sec 3): repeat the whole created-new procedure and every check first; Store deletes its `.tmp` after the second failure; Store repeats its no-link check before each listing, delete and move; S-layer and delete retry tests.
- **The new set** (Build 3): planned entries list only .strm files; say the full set comes from the staging folder's listing plus `backdrops\theme.mp4`.
- **Exact path from the tidied path** (Build 2, Comp N10).
- **Found-entry scope includes the Top Picks folder itself** (Comp 7): the copy-looking-folder scan, name clash and add-target checks; a top-level `x.` or junction.
- **Wrong-kind vs add** (Comp 3): a loose file at an add's target must be left and the add skipped; limit the wrong-kind rule to updates and Store's fixed names; a resumed add whose target became a link.
- **A managed copy folder replaced by a file** (Sec 5): remove it as wrong kind, then treat as missing; C85(d).
- **ReadOnly and hard links** (Sec 2): clearing ReadOnly on a hard-linked file clears it on the other name too; accept in writing, reword C76 and security.md.
- **Test junction with a trailing space** (Build 7): `mklink /J` can't make it; say how (`\\?\` symbolic link or a test helper).

## Nits
Comp 8 (staging files copied, not moved, into the copy folder), 9 (ignoring case is ordinal, no trimming), 11 (`complete` marker must be a regular file). Build 9 (a skipped add always removes its `incoming` entry), 10 (C85(e) needs a real wait or a release hook), 11 (Run gets the clock for the 30-second wait). Sec 6 (reparse points that aren't links: OneDrive placeholders), 7 (case-sensitive folders, harmless), 8 (a failed `plays.json.tmp` delete keeps user IDs until the next tidy; accept in writing).

## Lesson
Blocker 1 came from adopting a reviewer's API suggestion without checking it against D14 (card 0005 step 4: check every new rule against the decisions; seen 8). Blockers 2 and 3 are again a new rule checked at one place (the arrival rule; Run's swap) but not at every place it applies (the re-check; Store).

## Pattern
Every round from v3 to v11 has found new Blockers, almost all in in-place handling of copy folders and the plugin's own folders. The handover from v10 said to ask the owner, if v11 didn't pass, whether a simpler copy-handling design is wanted. Asked 2026-11-08.
