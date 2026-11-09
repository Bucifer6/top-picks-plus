---
id: 0016
kind: build
title: Chunk 2 built Storage with tests per check, Windows-only tests proved to run, and 12 rules proved by deliberate breaks
areas: [Storage, ci, tests]
tags: [storage, junctions, stryker, atomic-writes]
evidence: PR #8, branch build/chunk2-storage; CI runs 42602733577 (first build, compiled first time), 42603645214 (green, 491 tests on Windows, 8 Windows-only tests passed, Stryker 88.28%); D117-D125
date: 2026-11-09
status: live
seen: 1
promoted:
---
What: src/TopPicksPlus/Storage: the four data files (safe loads, atomic saves, the data-newer gate before every write, read-modify-write under one lock), damaged-file handling and the state.json rebuild, the import step, takeover, and every file change in top-picks and staging with the D37/D82/D119 checks.
CI: the Windows job writes a trx log and fails unless the `_junction`/`_windows` tests ran and passed there (card 0003). Fixed the main-push Stryker `--since:HEAD~1` failure.
Backlog closed: B14, B15, B31, B39, B40, B42.
Lesson: card 0015 (short names). Without a compiler locally, writing the whole chunk before the first push and reading it carefully compiled first time.
