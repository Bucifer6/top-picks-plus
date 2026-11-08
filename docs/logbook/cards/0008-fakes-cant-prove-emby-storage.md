---
id: 0008
kind: insight
title: A rule that depends on how Emby stores data can't be proved by tests with fake adapters; it needs a proof test on the real server before the design is settled
areas: [EmbyBridge, Run, tests]
tags: [emby, user-data, fakes, proof-test]
evidence: docs/reviews/2026-11-08-spec-v5-review.md Blocker 5 (Sec 1): copies carry provider IDs, Emby may key watched state by provider ID, and C49/C59 run against fakes that keep state per item; card 0001 is the same pattern for date added; docs/reviews/2026-11-08-spec-v6-review.md Blocker 1 (Sec 1): D39 rested on one direction of sharing, for films only; deleting a copy and episode sharing still unproved; docs/reviews/2026-11-08-spec-v7-review.md Comp 8–10, Sec 8–10: D43 proved deleting film copies, not episode copies; setting date added and the artwork refresh may make Emby save the .nfo
date: 2026-11-08
status: live
seen: 3
promoted:
---
The fakes behave the way the spec assumes Emby behaves, so a test against them passes even when the real server does something else. Card 0001 (Emby ignores .nfo dateadded for known items) was found the same way: by the real server, not by the docs.
Removing a feature that worked around Emby (F7's sync) doesn't remove the dependency: it moves it onto Emby's own behaviour, which then needs proving in every direction the plan relies on (write, read, delete).
Do instead: when a rule's safety depends on Emby's own storage (watched state, dates, ordering), write a short owner proof test on the real server and settle it before the spec relies on it, as D12 did for date added.
