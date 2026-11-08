---
id: 0004
kind: build
title: Stage 4 scaffold built on branch scaffold (pull request 1), not yet merged
areas: [Logic, Store, Artwork, EmbyBridge, Run, Settings, tests, build]
tags: [scaffold]
evidence: commits 05e41d4, 9ba0e58; build run on 9ba0e58 green, 15 tests
date: 2026-11-08
status: live
seen: 1
promoted:
---
What: six part folders with READMEs (D20), TopPicksPlus.csproj (netstandard2.0, Emby SDK 4.9.1.90 pinned), test project (xUnit, NetArchTest) with BoundaryTests.cs and BoundaryFixtures.cs, .github/workflows/build.yml (pinned actions, gitleaks, JSON dependency audit, Stryker 4.5.1 on Logic, 3-day artifacts, Release from main with SHA-256), docs modules/security/protected/releases.
Checks covering it: the 15 boundary/README/folder tests; the probe branch run.
Left out on purpose: no plugin code at all (Stage 5 not passed); no SkiaSharp reference (version on the server unknown); no lock file yet.
