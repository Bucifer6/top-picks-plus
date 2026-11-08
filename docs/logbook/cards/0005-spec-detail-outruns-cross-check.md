---
id: 0005
kind: insight
title: Adding detail to fix review findings creates new contradictions unless every new rule is cross-checked against the step list, data.md and the walls
areas: [docs, spec]
tags: [review, spec, consistency]
evidence: docs/reviews/2026-11-08-spec-v3-review.md (8 Blockers on commit 1f81762, most caused by v3's own additions); docs/reviews/2026-11-08-spec-v4-review.md (7 Blockers on 84d57f2, again mostly from v4's additions); docs/reviews/2026-11-08-spec-v6-review.md Comp 6 (removing F7 changed what F6 "finished" means; R1 checks new rules, not old rules a removal affects); docs/reviews/2026-11-08-spec-v7-review.md Blockers 1 and Build 4 (v7's "check everything before the first change" clashed with the link-inside-folder rule and with adds not yet on the managed list)
date: 2026-11-08
status: live
seen: 4
promoted: R1
---
Spec v3 answered every v2 finding, but backups, resume, damaged files and takeover each added a rule that clashed with something else: the F4 step order, a run.json field, a part's "must not use", or D21.
Do instead: after writing a new rule, check it against (1) the F4 step list and run.json `step`, (2) every data.md field it reads or writes, (3) the README walls of the part that does it, (4) the decisions. Fix the clash in the same edit. When a feature is removed, also re-read every rule that mentions what it touched (here: played state) and ask whether its meaning changed. When a new rule runs before or around older ones (a pre-check, a lock, a wait), walk one concrete case through every older rule it now touches (here: a delete of a folder with a junction inside, and a brand-new add).
