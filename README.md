# Knight Adventure 2D — QA Automation

![Thumbnail2](https://github.com/tinygamesunity/Knight-Adventure-2D/assets/153363846/f3a99d92-a6b9-4993-9905-33785b23bd93)

A 2D action-adventure sample (knight vs. skeleton enemies, melee combat, destructible plants), originally from [tinygamesunity/Knight-Adventure-2D](https://github.com/tinygamesunity/Knight-Adventure-2D). This fork builds out **automated regression testing** for the gameplay logic using Unity Test Framework — EditMode and PlayMode tests, no game-design changes.

## Tech stack

- Unity **6000.0.78f1** (pinned in `ProjectSettings/ProjectVersion.txt`)
- C#, standard Unity 2D package stack (`com.unity.2d.*`), `com.unity.ai.navigation` / NavMeshPlus for 2D NavMesh, new Input System
- Unity Test Framework (NUnit), EditMode + PlayMode

## Project structure

```
Assets/
  Scripts/                — gameplay logic
    Player/, Skeleton/, Weapon/, Plants/, Other/, Misc/, ScriptableObjects/
  Tests/
    EditMode/              — pure-logic tests, no scene/frame dependency
    PlayMode/               — tests needing Awake/Start/Update, physics, or a real prefab
docs/
  repository-analysis.md    — class-by-class breakdown of gameplay logic and its dependencies
  automation-feasibility.md — what's testable and what isn't, with concrete findings from writing these tests
  regression-suite.md       — current list of automated tests, one line per test
  how-to-run-tests.md       — CLI/batchmode command + Editor Test Runner instructions
```

## Running the tests

### Unity Editor (simplest)

Open the project in Unity Editor, then `Window → General → Test Runner` → PlayMode or EditMode tab → **Run All**. This is the most reliable option since it always runs in the one already-open Editor instance.

### Rider

Rider needs a live connection to a running Unity Editor to discover **PlayMode** tests — without it, the Unit Tests window only shows EditMode tests. To get PlayMode tests to show up:

1. Install/enable the **Unity Support** plugin (`Settings → Plugins`).
2. Open the project via the generated `.sln` (in Unity: `Assets → Open C# Project`), not by opening the repo folder directly.
3. Keep Unity Editor open at the same time as Rider. Check the Unity connection status via the Unity icon in Rider's top toolbar — it should say **Connected**.

Once connected, either run tests from Rider's Unit Tests window, or use its **"Unity Tests (batch mode)"** run configuration — but note that spins up a *separate* Unity process, which Unity refuses if the project is already open in an Editor window (`Multiple Unity instances cannot open the same project`). Simplest fix: just use Unity's own Test Runner window (above) instead of the batch-mode configuration while the Editor is already open.

### CLI / CI

See [`docs/how-to-run-tests.md`](docs/how-to-run-tests.md) for the batchmode command — note it deliberately omits `-quit`, which races with `-runTests` and can silently leave stale results in place.

## Current status

**19/19 PlayMode tests passing, 3/3 EditMode tests passing.**

Covered: `Player` (damage, death, invincibility window), `EnemyEntity` (damage, death, colliders), `EnemyAI` (baseline only — see below), `Sword`, `KnockBack`, `Utils.GetRandomDir()`.

Not yet covered, with reasons: `DestructiblePlant`, `ActiveWeapon.FollowMousePosition()`, `EnemyAI`'s Roaming/Chasing/Attacking state machine (a real Unity Test Framework limitation, not skipped out of laziness — full account in `docs/automation-feasibility.md`), `Player.Dash()` / `Player.Attack()` (need a Player prefab / Input System device simulation that don't exist in this repo). See [`docs/regression-suite.md`](docs/regression-suite.md) for the full breakdown.
