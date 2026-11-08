---
id: 0002
kind: insight
title: A namespace wall test passes on an empty project, so it proves nothing until a fixture shows it can fail
areas: [tests, boundary]
tags: [netarchtest, boundary-check, vacuous-pass]
evidence: run 42455898332 green with no code; probe branch probe/boundary-bites failed 2 tests; self-test The_check_catches_a_real_wall_break (commit 9ba0e58); review Build 10a
date: 2026-11-08
status: live
seen: 1
promoted:
---
NetArchTest checks "types in namespace X must not depend on Y". With no types in X, the rule passes every time, so a green boundary check on a fresh scaffold is meaningless.
Do instead: keep deliberate wall-breaking fixtures in the test assembly (namespace Fixture.*) and a test that asserts the same rule function reports them. Also prove it once end to end with a throwaway branch that breaks a wall.
