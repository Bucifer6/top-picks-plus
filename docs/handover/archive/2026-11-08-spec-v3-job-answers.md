# Answer key (only the marking goldfish reads this)
1. Branch `scaffold`. main's handover is two jobs old because work stays off main until reviewed (card 0006). PR 1 is not merged. (Where it stopped)
2. Spec v4 (applying D26 first) fixes the review file (8 Blockers first, then Should-fix, then Nits); the three goldfish re-run and find no Blocker; then merge PR 1. (What is still owed 2–5)
3. By the owner: D26, copies never look watched (played state goes to the real title, copy reset to unwatched). F7, C14, C15 and the row check must change. (Waiting on the owner; What is still owed 2)
4. Check each new rule against the F4 step list, run.json `step`, data.md and the part's walls, in the same edit. (What is still owed 3)
5. A change to a wall (a part's may use / must not use). READMEs, docs/modules.md and tests/TopPicksPlus.Tests/BoundaryTests.cs. (What is still owed 4)
6. SkiaSharp (D15) and Emby's SQLite (D24); System.Text.Json is now in question. Their versions must be read from the server before their build chunks. (Watch out for)
7. The staged-changes secret pattern check from security.md. Don't try to download gitleaks. (Watch out for)

## Quiz result (spec v4 job, 2026-11-08)
All 7 right (goldfish marker). Marker notes: key answer 1 gives a reason not in the handover; key answer 3 asks for detail not in the handover; item 3 still said "Blocker 1 needs the owner's answer" though it was answered; question 6 says "three libraries" where two are confirmed.
