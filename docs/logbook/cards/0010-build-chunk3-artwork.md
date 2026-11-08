---
id: 0010
kind: build
title: Chunk 3 built the numbered artwork (RankArtwork) with layout tests proved by deliberate breaks
areas: [Artwork, ci, tests]
tags: [skiasharp, anton, layout, audit, samples]
evidence: branch build/chunk3-artwork 0a2f184..5b66ba9; CI runs 42503121978 (samples), 42502908461 (breaks failed 18 tests), 42504738112 (final, 105 tests); docs/reviews/2026-11-08-chunk3-artwork-review.md; D97-D100
date: 2026-11-08
status: live
seen: 1
promoted:
---
What: src/TopPicksPlus/Artwork (RankArtwork.cs, ArtworkResult.cs, Layout.cs, TitleLayout.cs, Anton font + OFL) draws 1000x562 PNGs or returns "failed", never throws. Tests in tests/TopPicksPlus.Tests/Artwork, with canaries per layout check.
Checks: C43, C65 (owner approved samples 2026-11-08). No-logo titles in capitals (D100).
CI added: SkiaSharp audit exception for GHSA-j7hp-h8jx-5ppr only (self-tested); a step failing if any SkiaSharp file ships; samples artifact.
Left out on purpose: Windows job, global.json, Stryker (chunk 1); backlog B32-B37.
Lesson: this workspace can't download CI artifacts (403); the builder printed samples into a CI log in temporary commits, later reverted.
