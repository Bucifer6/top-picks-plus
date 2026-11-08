---
id: 0006
kind: insight
title: A plan to release part of a replacement early must say how it handles what the old system is still doing
areas: [EmbyLink, Logic, process]
tags: [release, takeover, play log, import, review]
evidence: docs/reviews/2026-11-08-readiness-2.md, Blockers 1 and 2 (against D58/D59) → withdrawn by D63, D64
date: 2026-11-08
status: live
seen: 1
promoted:
---
Claude added "release the play logger first so data builds up" (D58/D59) as a fix. The next fresh review found it broke two things: plays on the old plugin's copies had no source to credit, and the import cut-off meant the first-week import could never import anything. The readiness score did not go up.
Why it matters: a fix that adds a new phase brings new interactions with the old system and with earlier decisions (here D25, D28, D46).
Do instead: before adding an early or partial release, list what the old system is still doing during the overlap and check every earlier decision that touches the same data. Prefer a single go-live unless the owner asks otherwise.
