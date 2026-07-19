# Automation feasibility

What's actually testable via public API in this codebase, what isn't, and why — based on the classes covered so far. See `repository-analysis.md` for the full class breakdown and the singleton graph these issues stem from.

## Recurring constraint: unchecked singletons

`Player.Instance`, `GameInput.Instance`, `ActiveWeapon.Instance`, `NavMeshSurfaceManagement.Instance` are all set in `Awake()` and consumed with no null-checks. Any test that exercises a consumer must construct the producer singleton itself, in the same frame, before the consumer's `Start()` runs (Unity calls all `Awake()`s before any `Start()`, so ordering between the two `AddComponent` calls in a `[SetUp]` doesn't matter as long as neither happens after the first frame). This is the recurring setup cost behind `PlayerTests` and would apply equally to `ActiveWeapon`/`SkeletonVisual`/`PlayerVisual` tests.

## Per-class findings

### EnemyAI — NavMeshAgent without a baked mesh
`SetDeathState()` calls `_navMeshAgent.ResetPath()` unconditionally. In any test scene without a baked NavMesh, this logs `"ResetPath" can only be called on an active agent that has been placed on a NavMesh.` as an `Error`-level log. When it fires **synchronously inside a direct method call** (as in `EnemyEntityTests.EnemyEntity_TakeDamage_ReducesHealthToZero_InvokesOnDeath`, via `TakeDamage()` → `SetDeathState()`), `LogAssert.Expect` reliably absorbs it — not a blocker there.

It's a much harder blocker for testing `EnemyAI`'s own `Update()`-driven state machine (Roaming → Chasing → Attacking), attempted and abandoned while writing `EnemyAITests`. With no baked NavMesh, `Roaming()` calls `SetDestination()` on the very first `Update()` after spawn (the roaming timer starts at 0, so it always elapses immediately) and `ChasingTarget()` calls `SetDestination()` on *every* `Update()` while Chasing — both log the same kind of Error. Because `EnemyAI.Update()` only runs as part of Unity's own frame dispatch (there's no public method to invoke it directly), verifying anything past the first frame requires a `[UnityTest]` coroutine with `yield return null`, putting the log **on the far side of a coroutine yield** instead of inside the test's own call stack.

That distinction turned out to matter: every log-suppression mechanism tried for the yield-boundary case failed to prevent a test failure —
- `LogAssert.Expect(LogType.Error, exactString)`, registered in the right order right before the yield (the exact pattern that works in `EnemyEntityTests`) — still reported "Unhandled log message."
- `LogAssert.Expect(LogType.Error, new Regex(".*"))` registered several times as a catch-all — same result, or (when registered in `[SetUp]`, which never yields) "Expected log did not appear" instead, since no `Update()` runs during a synchronous `[SetUp]`.
- `LogAssert.ignoreFailingMessages = true`, scoped narrowly around a single yield or broadly across the whole `SetUp`-to-`TearDown` span — still reported as failed, even though the test's own captured output showed `IgnoreFailingMessages:true` was active at the time.

Also worth recording since it cost real debugging time: a `[Test]` (no coroutine, never yields) that creates GameObjects and destroys them via `Object.Destroy()` in `[TearDown]` doesn't actually remove them before the next test's first frame — `Destroy()` is deferred to end-of-frame, and a fully synchronous test never reaches one. The leftover, unmoved objects then run their own `Update()` during the *next* test and corrupt distance-based assertions. `Object.DestroyImmediate()` in `[TearDown]` fixes that specific issue, but did not fix the log-suppression problem above.

Net effect: only same-frame, non-`Update()`-driven behavior of `EnemyAI` is covered (`GetRoamingAnimationSpeed()` right after spawn). Verifying the actual state transitions needs a baked NavMesh (e.g., a dedicated test scene with baked geometry) so the errors never fire in the first place — that's the real fix, not more log-suppression attempts.

### EnemyEntity — health only comes from a private SerializeField
`enemySO` is `[SerializeField] private EnemySO enemySO` with no public setter. Without reflection, the only way to give `EnemyEntity` real values is to instantiate the actual configured `Skeleton.prefab` (and its `SkeletonSO.asset`) via `AssetDatabase`, which only works inside the Editor (`#if UNITY_EDITOR`), not in a standalone Player build. Already the approach used in `EnemyEntityTests`.

### Player — three separate blockers
- `Start()` requires `GameInput.Instance` to already exist (see singleton constraint above).
- `Awake()` caches `Camera.main` — needs a `GameObject` tagged `"MainCamera"` created before `Player`'s `Awake` runs.
- `Dash()` reads `[SerializeField] private TrailRenderer trailRenderer`, no public setter, **and there is no Player prefab in the repo** (`Assets/Prefabs/` has no Player asset — the Player lives directly in `SampleScene`). So unlike `EnemyEntity`, there's no `AssetDatabase.LoadAssetAtPath` shortcut available; testing `Dash()` would require either adding a prefab, reflection, or accepting the gap. Currently untested — documented as a gap, not worked around.
- `Attack()` only fires through `GameInput_OnPlayerAttack`, itself only invoked by a real Input System device callback (`_playerInputActions.Combat.Attack.started`). Exercising it needs `InputTestFixture` + simulated virtual devices, not a call to public API. Not attempted yet.

### ActiveWeapon — mouse-follow needs the full singleton chain
`GetActiveWeapon()` is a trivial, freely testable getter. `FollowMousePosition()` (called from `Update()`, gated on `Player.Instance.IsAlive()`) needs `GameInput.Instance.GetMousePosition()` (real `Mouse.current`) and `Player.Instance` fully wired — same cost as the Player singleton constraint, not yet attempted.

### DestructiblePlant — hard NavMeshSurfaceManagement dependency
`OnTriggerEnter2D` calls `NavMeshSurfaceManagement.Instance.RebakeNavmeshSurface()` unconditionally once a `Sword`-bearing collider touches it. With no `NavMeshSurfaceManagement` singleton in the test scene, this throws a `NullReferenceException` and the test fails outright (unlike `EnemyAI`'s NavMeshAgent case, this isn't just a logged error — it aborts the trigger callback). Testing the full trigger path requires a real `NavMeshSurface` (from `com.unity.ai.navigation` / NavMeshPlus) component wired into a `NavMeshSurfaceManagement` in the test scene. Not yet attempted.

### GameInput, Sword, KnockBack — no blockers
All three are directly instantiable with plain `GameObject`s + `AddComponent`, no singleton or scene-asset dependency. `GameInput.Awake()` does construct and enable a real `PlayerInputActions` instance, which works fine in PlayMode tests without any input devices attached. Fully covered already (`SwordTests`, `KnockBackTests`; `GameInput` used as test infrastructure inside `PlayerTests`).

## Not test targets by design

`*Visual` classes (`PlayerVisual`, `SkeletonVisual`, `SwordVisual`, `SwordSlashVisual`, `DestructiblePlantVisual`) and everything in `Misc/` (`FlashBlink`, `SelfDestoyVFX`, `TransparencyDetection`) hold no gameplay state — they only react to logic-layer events and drive `Animator`/`SpriteRenderer`/VFX. These are out of scope for regression tests unless a specific visual bug is being chased.
