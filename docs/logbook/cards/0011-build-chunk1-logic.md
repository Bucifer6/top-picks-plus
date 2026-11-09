---
id: 0011
kind: build
title: Chunk 1 built the Logic rules with 245 tests, Stryker 91% and tests proved by deliberate breaks
areas: [Logic, ci, tests]
tags: [stryker, windows, global-json, triggers]
evidence: PR #7, branch build/chunk1-logic up to ae02758; CI runs 42582238197 (breaks: 19 tests failed across 12 rules), 42582467934 (final, 355 tests, Stryker 91.21%); docs/reviews/2026-11-09-chunk1-logic-review.md; D101-D116
date: 2026-11-09
status: live
seen: 1
promoted:
---
What: src/TopPicksPlus/Logic: choosing the 10, viewer days, naming and path rules, rank dates, takeover, swap and resume plans, guards (D116). Tests tagged [Trait("Check","Cxx")].
CI added (D105, D115): PRs (not drafts), main and v* tags only; cancel older runs; 20-minute timeouts; NuGet cache; global.json 8.0.400; Windows test job with Stryker 4.16.0, bar 80%, changed files only on PRs. About 11 billed minutes per PR run.
Backlog closed: B1-B3, B5, B6, B8, B9, B12, B16, B22, B24, B28, B35, B36. Opened: B38-B44.
Lesson: a draft PR no longer runs CI; mark it ready to build.
