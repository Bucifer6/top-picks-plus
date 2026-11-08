---
id: 0001
kind: insight
title: Emby only reads an .nfo's <dateadded> when it first creates the item; rewriting the .nfo later does not change the date
areas: [ordering]
tags: [emby, dateadded, nfo]
evidence: top-picks folder 2026-11-08 05:44 run (nfo dateadded rank-1h steps) vs owner's report that order breaks unless folders are deleted first
date: 2026-11-08
status: live
seen: 1
promoted:
---
The old plugin ordered the Latest row by writing <dateadded> (rank 1 = now, each rank 1 hour earlier) into each .nfo. When a folder already existed, Emby kept the item's old date, so the order broke unless folders were deleted first.
Do instead: set DateCreated on the Emby item directly after the rescan, and still write the same value into the .nfo so a refresh that re-reads it agrees.
