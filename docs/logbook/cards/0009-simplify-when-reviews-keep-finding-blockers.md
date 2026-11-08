---
id: 0009
kind: insight
title: When review rounds keep finding new Blockers in one area, simplify the design there instead of adding rules
areas: [docs, spec, Run, Store]
tags: [review, spec, design, simplicity]
evidence: docs/reviews/2026-11-08-spec-v3-review.md to 2026-11-08-spec-v11-review.md (every round found new Blockers, nearly all in in-place updates of copy folders); D58 (owner chose rebuild-fresh, 2026-11-08)
date: 2026-11-08
status: live
seen: 1
promoted:
---
From spec v3 to v11, each fix to the in-place update of copy folders (link sweeps, exact names, clear-outs, wrong-kind entries, temp names, replace calls) answered the last round's findings and created new ones, because every rule had to hold for every action on every odd thing someone might leave in a folder.
Why it matters: a fix that adds a rule to an area that already has many rules is likely to clash with one of them; nine rounds is a long time for a level 1 personal plugin.
Do instead: after two rounds in a row with new Blockers in the same area, stop and ask the owner whether that area can be designed away (here: never update in place; delete and add fresh). Count the rules a simpler design removes, not just the findings it fixes.
