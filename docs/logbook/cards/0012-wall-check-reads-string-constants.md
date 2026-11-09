---
id: 0012
kind: insight
title: The wall check reads string constants as type names
areas: [boundaries, tests, Logic]
tags: [netarchtest]
evidence: NetArchTest 1.3.2 source; D113
date: 2026-11-09
status: live
seen: 1
promoted:
---
NetArchTest treats a const string holding namespace-looking text (e.g. "Emby.TopPicks") as a dependency, so it trips the Emby wall. Keep such text out of constants in walled modules (build it at run time, or reword it).
