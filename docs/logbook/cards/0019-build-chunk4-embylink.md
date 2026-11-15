---
id: 0019
kind: build
title: Chunk 4 wired Logic and Storage to Emby; the only compile error was a System.Memory version clash
areas: [EmbyLink, ci]
tags: [emby-sdk, system-memory, review]
evidence: PR #10; CI run 43745309804 (CS1705 System.Memory 4.0.2.0 vs 4.0.1.2), 43745750029 (green); D134-D147
date: 2026-11-15
status: live
seen: 1
promoted:
---
What: src/TopPicksPlus/EmbyLink (entry point, task, run engine and steps, copy builder, order guard, play logger, watched sync), Plugin and PluginConfiguration.
How it compiled nearly first time: Emby signatures read, read-only, from the SDK's own DLL metadata with a small PowerShell reflection script, plus an "obsolete" marker (warnings are errors). Only the System.Memory clash got through (D146).
Review lesson: the reviewer found real-world failure modes the rules didn't name (offline media drive emptying the row, un-watching on a stopped play, dirty staging after a stop between two saves). Before building a run step, ask "what if the drive, the title or Emby goes away between these two lines?" (D147).
