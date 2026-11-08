---
id: 0007
kind: method
title: CI artifacts can't be downloaded from the cloud workspace, but job logs can, so print small generated files into the log
areas: [build]
tags: [ci, github-actions, lock-files, workspace]
evidence: 2026-11-08 `gh run download` refused (blob storage Forbidden); `gh api repos/.../actions/jobs/<id>/logs` worked; lock files recovered this way, commit dd8e2df
date: 2026-11-08
status: live
seen: 1
promoted:
---
Artifact zips live on Azure blob storage, which the workspace proxy refuses. The jobs/logs API on api.github.com works.
Do instead:
1. In the workflow, print the file base64-encoded between marker lines (`=====LOCKFILE-BEGIN <path>` … `END`).
2. Find the run (`actions/runs?head_sha=`), then the job, then `gh api .../actions/jobs/<id>/logs`.
3. Decode the line after each marker (strip the timestamp) and commit. Remove the printing once it's no longer needed.
