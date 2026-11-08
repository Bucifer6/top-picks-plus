---
id: 0007
kind: method
title: When a new decision changes an older one, mark the older row and fix every README that repeats it, in the same commit
areas: [docs, process]
tags: [decisions, readme, contradiction, review]
evidence: docs/reviews/2026-11-08-readiness-2.md, items 3, 7, 8 (D26/D44, D30/D43, D34/D62, D57 vs Logic README) → marked in commit 1c2cf16; again in docs/reviews/2026-11-08-readiness-4.md item 13 (D38, D62, D28, D54, D68)
date: 2026-11-08
status: live
seen: 2
promoted:
---
A builder reads decisions top-down and READMEs first, so an unmarked old rule wins.
1. Write the new decision and name the old one it changes.
2. Add "[Amended by Dxx]" or "[Replaced by Dxx]" at the start of the old row.
3. grep the repo for the old rule's key words (IDs, names, values) and fix each README, spec and checks line that repeats it.
4. Re-read the changed READMEs before committing.
