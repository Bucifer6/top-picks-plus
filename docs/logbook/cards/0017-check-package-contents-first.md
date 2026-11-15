---
id: 0017
kind: insight
title: Check what a pinned package actually holds before planning around it; one look at its file list can settle a question the plan left open for weeks
areas: [EmbyLink, proof, process]
tags: [nuget, sdk, sqlite, playback-reporting]
evidence: nuget.org catalog entries for MediaBrowser.Server.Core and MediaBrowser.Common 4.9.1.90 (2026-11-15): no SQLitePCL.pretty → D129; D25 and D77 had planned a proof step around it since 2026-11-08
date: 2026-11-15
status: live
seen: 1
promoted:
---
D25 left "does the SDK provide SQLitePCL.pretty?" to be found out later, and the proof plugin and two backlog rows were planned around reading Playback Reporting. The package's file list (nuget.org catalog, no download needed) showed it never could.
Do instead: when a plan depends on a package containing something, read the package's file list and dependencies first and record the answer as a decision; add a CI step that fails if it changes.
