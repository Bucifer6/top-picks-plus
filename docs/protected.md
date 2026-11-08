---
summary: Protected journeys: what must never break for the people using the Emby server.
links: [checks.md, spec.md]
---

# Protected journeys

| ID | Part | Who relies on it | Journey | Test |
|----|------|------------------|---------|------|
| P1 | Run, EmbyBridge | Every Emby user | Open the Emby home screen: the Latest Top Picks row shows 10 titles numbered 1–10 left to right, and playing one plays the real film or episode | Owner check C2 + C16 after each release (needs the real server, so no automatic browser test); the plugin's own row check (F4 step 6) runs on every weekly run and reports a failure |
| P2 | EmbyBridge | Every Emby user | Watched ticks and resume points on the real titles are never lost or wrongly changed | Owner check C14 after each release |

A failed protected journey blocks the release. Changing one needs the owner's OK.
