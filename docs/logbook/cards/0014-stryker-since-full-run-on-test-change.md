---
id: 0014
kind: insight
title: Stryker --since runs every covered mutant when any test file changes
areas: [ci, tests]
tags: [stryker, ci-minutes]
evidence: CI run 42581310816 log: "756 mutants will be tested because: Non-CSharp files in test project were changed"
date: 2026-11-09
status: live
seen: 1
promoted:
---
Incremental mutation testing only saves time on PRs that leave tests alone. A PR that adds or edits tests is a full run; budget CI minutes for that.
