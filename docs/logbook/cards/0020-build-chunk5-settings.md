---
id: 0020
kind: build
title: Chunk 5 built the settings page with Emby's plugin UI, so there is no web endpoint or script of the plugin's own
areas: [Settings]
tags: [emby-ui, settings, review]
evidence: PR #11; D148; Emby.SDK SampleCode (EmbyPluginUiTemplate, EmbyPluginUiDemo, SDK 4.9.1.90)
date: 2026-11-15
status: live
seen: 1
promoted:
---
What: src/TopPicksPlus/Settings (SettingsPage fields, SettingsView buttons and Save, ReportText), Plugin implements IHasUIPages.
Emby's UI framework: a ButtonItem's Data1 arrives as the command; Emby replaces the view's ContentData with what the page posts before each command. Buttons inside list rows aren't documented, so only top-level buttons are used.
Review lesson: never trust lists the page posts back (hidden choice lists may not survive, and they are stale); work out allowed values from Emby when saving. And don't reset unsaved fields when another button is pressed.
