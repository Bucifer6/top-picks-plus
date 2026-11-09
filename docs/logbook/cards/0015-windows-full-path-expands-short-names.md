---
id: 0015
kind: insight
title: Windows full-path resolution expands 8.3 short names, so a "direct child" check alone lets LONGFO~1 delete the long folder
areas: [Storage, safety, tests]
tags: [delete, paths, windows, short-names]
evidence: PR #8, CI run 42602733577 (C45_B42 short-name test failed on Windows: the long folder was deleted through its short name); fixed in 01f50df (D119), green in run 42603645214
date: 2026-11-09
status: live
seen: 1
promoted:
---
`Path.GetFullPath(root + "\\LONGFO~1")` returns `root\Long folder name (2020)` on Windows, so a containment check that compares the resolved parent passes and the delete reaches a different folder than the one on the list. Linux CI can't show it.
Do instead: after resolving, check every name part came back exactly as written (ignoring case), and that an existing entry is listed in its parent under that exact name; prove it with a Windows-only test that creates a real short name (`fsutil file setshortname`) and that CI requires to run and pass.
