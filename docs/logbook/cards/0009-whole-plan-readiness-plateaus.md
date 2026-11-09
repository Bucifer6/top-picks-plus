---
id: 0009
kind: insight
title: A whole-plan readiness score can plateau, because each fix round grows the docs and gives the next reviewer new edges
areas: [docs, process]
tags: [readiness, review, gate, score]
evidence: docs/reviews/2026-11-08-readiness-1.md to -4.md (72, 71, 76, 75); D92
date: 2026-11-08
status: live
seen: 1
promoted:
---
Four fresh readiness reviews of the whole plan scored 72, 71, 76 and 75. Every finding was fixed between rounds (D43–D91), but the docs grew each time and the next reviewer found deeper detail, mostly in the part that talks to Emby, where behaviour can only be settled on the real server.
Why it matters: chasing one score for everything stalls parts that are already ready (Artwork was ready by review 4).
Instead: make the gate "no Blockers, no owner guesses, restatement agreed" (D92), limit review rounds, re-review only the fixes, and move everything below Blocker to the backlog for the chunk it affects.
