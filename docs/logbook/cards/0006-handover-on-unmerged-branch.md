---
id: 0006
kind: method
title: Before trusting main's handover, list the remote branches; the newest handover may be on an unmerged branch
areas: [handover]
tags: [handover, git, branches]
evidence: 2026-11-08 session: main's handover said Stage 4 not started; branch scaffold (PR 1) already held the finished scaffold job and a newer handover; a duplicate scaffold was built and thrown away
date: 2026-11-08
status: live
seen: 1
promoted:
---
D19 keeps work off main until reviewed, so main's docs/handover/current.md can be one or more jobs behind.
1. Run `git ls-remote origin` before reading the handover.
2. If any branch or open pull request is newer than main, read its docs/handover/current.md instead (and take that quiz).
3. Only then change files.
