---
id: 0005
kind: insight
title: Old-plugin series copies keep their .strm files only in Season subfolders, so any "has a .strm" rule must look inside subfolders
areas: [EmbyLink, Logic, takeover]
tags: [takeover, old-plugin, series]
evidence: top-picks listing 2026-11-08 (The Walking Dead (2010): tvshow-level files at top, .strm in Season N); review finding 2 (Blocker) → D38, C46
date: 2026-11-08
status: live
seen: 1
promoted:
---
Checking only the top level of a folder would never adopt old series copies, leaving them in the row forever and forcing " - 2" names: today's bug again.
Do instead: count files recursively for the takeover test, and test it with a series-shaped folder.
