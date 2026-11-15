---
id: 0018
kind: build
title: The proof plugin was built as an Emby Simple UI page, with no HTML, script or endpoint of its own
areas: [proof, ci]
tags: [simple-ui, emby-sdk, proof]
evidence: branch build/proof-plugin; D129-D133; Emby.SDK repo SampleCode/Templates/EmbyPluginSimpleUiTemplate (SDK 4.9.1.90)
date: 2026-11-15
status: live
seen: 1
promoted:
---
What: proof/TopPicksProof (ProofPlugin, ProofOptions, Proofs) and the `proof` CI job (build, no-SQLite check, version, audit, 3-day artifact).
How the API was found without a local build: Emby's own SDK repo on GitHub (MediaBrowser/Emby.SDK, SampleCode/Templates) targets 4.9.1.90; dev.emby.media/reference gives member signatures. Use both before writing chunk 4.
Simple UI has no buttons: an action is a tick box handled in OnOptionsSaving; results are filled in OnBeforeShowUI.
Lesson: the session ran on the owner's PC (the Emby server), not in the cloud; read the environment before assuming (D133).
