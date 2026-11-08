---
id: 0003
kind: insight
title: dotnet test rebuilds referenced projects without your -p:Version, overwriting the versioned DLL
areas: [build]
tags: [github-actions, versioning, dotnet]
evidence: review Build 4 on first build.yml (commit 05e41d4); fixed in 9ba0e58 by building both projects with the version and testing with --no-build
date: 2026-11-08
status: live
seen: 1
promoted:
---
Building the plugin with -p:Version=X and then running dotnet test (without --no-build) rebuilds the plugin at the csproj default version into the same bin folder. The uploaded or released DLL then carries the wrong version.
Do instead: build every project with the same -p:Version, then dotnet test --no-build, and upload only after that.
