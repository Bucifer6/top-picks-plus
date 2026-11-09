---
id: 0013
kind: method
title: Deliberate breaks must still compile under warnings-as-errors
areas: [tests, ci]
tags: [canary, warnings]
evidence: CI run 42582085693 (break commit failed to compile, no tests ran)
date: 2026-11-09
status: live
seen: 1
promoted:
---
With warnings as errors, a break like `if (false)` is unreachable code and fails the build, so the tests never run and prove nothing. Change a number or a comparison instead, so the break compiles and the tests get to fail.
