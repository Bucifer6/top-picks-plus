---
id: 0004
kind: insight
title: "Only delete folders on the managed list" is not safe on its own; the stored name must be checked to resolve to a direct child of the folder
areas: [Storage, EmbyLink, Logic, safety]
tags: [delete, paths, junction, review]
evidence: docs/reviews/2026-11-08-scaffold-review.md, fresh review of spec gaps, finding 1 (Blocker) → D37, C45
date: 2026-11-08
status: live
seen: 1
promoted:
---
A list of "folders I made" is data, and data can be wrong: an empty name, "..", a full path or a junction would turn "delete my folder" into "delete top-picks" or something outside it.
Do instead: before any delete, check the name is a plain single name, resolves to a direct child of the expected folder, and is not a reparse point; refuse and report otherwise. Give it an automatic test with bad entries.
