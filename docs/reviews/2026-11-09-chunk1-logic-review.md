# Chunk 1 (Logic) review – 2026-11-09

Fresh reviewer, PR #7 at ae02758, CI run 42582467934 (355 tests, Stryker 91.21%). Verdict: **fix first** (one docs Blocker), code sound.

Blocker
1. C94 and Storage/README.md still described the old D93 refusal and "restore from a backup", contradicting D95, the code (RunRules.cs) and its test. **Fixed:** C94 marked "[Amended by D95; see C98]", Storage README reworded.

Below Blocker, set aside in docs/backlog.md (loops must end):
2. Subtitle suffix has no length cap (C51/D106) → B39.
3. "managed.json missing while takeoverDone" has no Logic input (D95/C100) → B40.
4. D116 guard order only partly tested → B41.
5–6. No second-layer name check on SwapPlan folder steps; 8.3 short names → B42.
7. OnTrigger at resumeCount 3 → B43.
8. No-SkiaSharp check can't catch an ExcludeAssets regression → B44.

Passed: walls (no Emby, IO, clock, TimeZoneInfo, packages, records, JSON attributes), no existing test edited, CI workflow (triggers, concurrency, paths-ignore, timeouts, Stryker pin and 80% bar, global.json). 21 checks sampled in depth, all assert the rule.
Note: docs/security.md doesn't exist (removed after c4f8b98); security was checked against D37, D82, D93–D96.
