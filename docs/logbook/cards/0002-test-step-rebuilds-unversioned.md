---
id: 0002
kind: insight
title: "dotnet test" without --no-build rebuilds referenced projects, so a version stamped in an earlier build step is silently lost
areas: [ci]
tags: [github-actions, dotnet, versioning]
evidence: scaffold review 2026-11-08 findings C34 and B1 on .github/workflows/build.yml (commit b0f2d55)
date: 2026-11-08
status: live
seen: 1
promoted:
---
The first build.yml passed -p:Version only to the plugin build, then ran dotnet test, which rebuilt the plugin into the same folder as 0.0.0. Every Release DLL would have said 0.0.0, breaking the version line in the run report (F10) and making rollback by version guesswork.
Do instead: build once with the version (the test project, which builds the plugin too), run dotnet test --no-build, and add a step that fails if the built DLL doesn't carry the expected version.
