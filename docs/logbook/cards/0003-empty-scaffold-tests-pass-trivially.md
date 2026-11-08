---
id: 0003
kind: insight
title: Architecture rules over an empty project pass whether or not they work; prove them with a canary that must fail
areas: [tests, boundaries]
tags: [netarchtest, boundary-check]
evidence: first CI run 42473587570 (7 boundary tests green with no plugin code); scaffold review finding B8
date: 2026-11-08
status: live
seen: 1
promoted:
---
The boundary tests went green on the first build because there were no types to check, which says nothing about whether a wall-crossing would be caught.
Do instead: keep canary types in the test project that deliberately break a wall, and assert the rule reports them as failing.
