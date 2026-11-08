---
id: 0008
kind: insight
title: A rule that depends on how Emby stores data can't be proved by tests with fake adapters; it needs a proof test on the real server before the design is settled
areas: [EmbyBridge, Run, tests]
tags: [emby, user-data, fakes, proof-test]
evidence: docs/reviews/2026-11-08-spec-v5-review.md Blocker 5 (Sec 1): copies carry provider IDs, Emby may key watched state by provider ID, and C49/C59 run against fakes that keep state per item; card 0001 is the same pattern for date added
date: 2026-11-08
status: live
seen: 1
promoted:
---
The fakes behave the way the spec assumes Emby behaves, so a test against them passes even when the real server does something else. Card 0001 (Emby ignores .nfo dateadded for known items) was found the same way: by the real server, not by the docs.
Do instead: when a rule's safety depends on Emby's own storage (watched state, dates, ordering), write a short owner proof test on the real server and settle it before the spec relies on it, as D12 did for date added.
