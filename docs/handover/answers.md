# Answer key (only the marking goldfish reads this)
1. Branch `scaffold`, commit 9ba0e58; not merged, because D19 needs a passing fresh review and the review found spec Blockers (Where it stopped).
2. Spec v3 fixes, then a re-run of the three goldfish (comprehension, builder readiness, security) finding no Blockers (What is still owed 2 and 4).
3. The staged-changes pattern check from security.md; do not try to download and run gitleaks again (Watch out for).
4. Only GitHub Actions; not in the cloud workspace (no NuGet) and not on the owner's PC (D5) (Watch out for).
5. None any more: both were answered as D21 (no viewing history) and D22 (row-check user) (Waiting on the owner).
6. docs/reviews/2026-11-08-scaffold-review.md; the 8 Blockers first, then Should fix (What is still owed 2).
7. Read the SkiaSharp version Emby loads from the server's system folder, because D15 compiles against Emby's copy (Watch out for).
