# Regression suite

Current state of `Assets/Tests/`. Update this file whenever a test class is added, removed, or its coverage changes meaningfully.

## `[Test]` vs `[UnityTest]`

`[Test]` runs synchronously — no game frame passes while it executes. `[UnityTest]` is a coroutine (`IEnumerator` + `yield`) that can wait for one or more real frames, letting Unity actually run `Awake()`/`Start()`/`Update()`/physics in between.

Rule used across this suite: `[Test]` is enough when the thing being asserted happens **synchronously inside the method call itself**, or was already set in `Awake()` (which runs synchronously on `AddComponent`/`Instantiate`). `[UnityTest]` is required when it depends on `Start()`, `Update()`, physics, or real elapsed time — none of which happen until Unity actually processes a frame.

Two concrete examples from this suite:
- `KnockBack.GetKnockedBack()` sets `IsGettingKnockedBack = true` directly in the method body → the corresponding test is `[Test]`, asserting right after the call.
- `KnockBack.GetKnockedBack()` also calls `_rb.AddForce(...)`, but a Rigidbody2D only applies accumulated forces on the *next physics step* → the test that checks the resulting velocity is `[UnityTest]` with `yield return new WaitForFixedUpdate()` first.

Same logic applies to anything that depends on `Start()`: `Player.Start()` sets `_isAlive = true`, but `Start()` only runs on the frame *after* the component is created — not during `AddComponent()` itself. Tests that call `Player.TakeDamage()` must `yield return null` once first, or `_isAlive` is still at its C# default (`false`) and the call silently does nothing.

## EditMode

### UtilsTests (3 tests)
- `Utils_GetRandomDir_ReturnsNormalizedVector`
- `Utils_GetRandomDir_KeepsZComponentZero`
- `Utils_GetRandomDir_VariesBetweenCalls`

`KnightAdventure.Utils.Utils.GetRandomDir()` — pure function, zero dependencies, no `[SetUp]`/`[TearDown]` needed. First EditMode test in the project.

## PlayMode

### KnockBackTests (4 tests)
- `KnockBack_GetKnockedBack_SetsIsGettingKnockedBackTrue` (`[Test]` — state is set synchronously inside `GetKnockedBack()`, no frame needed)
- `KnockBack_GetKnockedBack_AppliesForceAwayFromDamageSource` (`[UnityTest]` — `AddForce` only takes effect on the next physics step)
- `KnockBack_TimerExpires_StopsAutomatically` (`[UnityTest]` — timer decrements in `Update()`, needs real time to pass)
- `KnockBack_StopKnockBackMovement_ResetsStateAndVelocity` (`[Test]`)

Plain `GameObject` + `AddComponent<Rigidbody2D>/<KnockBack>`, no external dependencies.

### SwordTests (4 tests)
- `Sword_Start_DisablesAttackCollider` (`[UnityTest]` — the collider is disabled in `Start()`, not `Awake()`)
- `Sword_Attack_InvokesOnSwordSwing` (`[Test]`)
- `Sword_Attack_LeavesAttackColliderEnabled` (`[Test]`)
- `Sword_AttackColliderTurnOff_DisablesCollider` (`[Test]`)

Plain `GameObject` + `AddComponent<PolygonCollider2D>/<Sword>`, no external dependencies.

### EnemyEntityTests (4 tests)
- `EnemyEntity_TakeDamage_InvokesOnTakeHit`
- `EnemyEntity_TakeDamage_LessThanHealth_DoesNotInvokeOnDeath`
- `EnemyEntity_TakeDamage_ReducesHealthToZero_InvokesOnDeath`
- `EnemyEntity_TakeDamage_Lethal_DisablesColliders`

Loads the real `Assets/Prefabs/Enemies/Skeleton/Skeleton.prefab` and `Assets/ScriptableObjects/SkeletonSO.asset` via `AssetDatabase` (Editor-only) because `EnemyEntity.enemySO` has no public setter. Disables `EnemyAI` in `[SetUp]` to avoid NavMeshAgent pathing every frame in a scene with no baked NavMesh. The death tests still trigger `EnemyAI.SetDeathState()` → `NavMeshAgent.ResetPath()`, which logs an `Error` in a scene without a bake; absorbed with `LogAssert.Expect` (see `automation-feasibility.md`).

### PlayerTests (6 tests)
- `Player_TakeDamage_NonLethal_DoesNotInvokeOnPlayerDeath` (`[UnityTest]` — needs `Start()` to run first)
- `Player_TakeDamage_InvokesOnFlashBlink` (`[UnityTest]`)
- `Player_TakeDamage_Lethal_InvokesOnPlayerDeathAndBecomesNotAlive` (`[UnityTest]`)
- `Player_TakeDamage_AfterDeath_DoesNotInvokeOnPlayerDeathAgain` (`[UnityTest]`)
- `Player_TakeDamage_WithinRecoveryWindow_DoesNotInvokeOnFlashBlinkAgain` (`[UnityTest]`)
- `Player_GetPlayerScreenPosition_MatchesMainCameraWorldToScreenPoint` (`[Test]` — only reads `_mainCamera`, cached in `Awake()`, not `Start()`)

Builds a `Player` from a plain `GameObject` plus a real `GameInput` singleton and a tagged `"MainCamera"` `Camera`, both created in `[SetUp]` before `Player` is added (see `automation-feasibility.md` for why). Does **not** cover `Dash()` or `Attack()` — both are real testability gaps, not omissions, documented in the test file's header comment and in `automation-feasibility.md`.

### EnemyAITests (1 test)
- `EnemyAI_Awake_RoamingAnimationSpeedRatioStartsAtOne`

Loads the real `Skeleton.prefab` like `EnemyEntityTests`. Only covers `GetRoamingAnimationSpeed()` immediately after spawn (no `Update()` involved). Verifying the actual Roaming/Chasing/Attacking transitions was attempted and abandoned — every NavMesh-log-suppression approach tried failed specifically when the log fires from Unity's own `Update()` dispatch after a coroutine `yield`, as opposed to synchronously inside a direct method call (which works fine, see `EnemyEntityTests`). Full account in `automation-feasibility.md`.

## Not covered yet

- `DestructiblePlant` destruction trigger — needs a real `NavMeshSurfaceManagement` singleton in the test scene.
- `ActiveWeapon.FollowMousePosition()` — needs the same `Player` + `GameInput` singleton wiring as `PlayerTests`, plus a simulated mouse position.
- `EnemyAI` state transitions (Roaming / Chasing / Attacking) — needs a baked NavMesh; log-suppression workarounds were tried and abandoned as unreliable (see `automation-feasibility.md`).
- `Player.Dash()`, `Player.Attack()` — see `automation-feasibility.md` (private `TrailRenderer` field with no prefab; requires Input System device simulation).
- All `*Visual` classes and `Misc/*` — out of scope by design, they're the presentation layer (see `repository-analysis.md`).

## Last verified run

- PlayMode: 19/19 passing (4 KnockBack + 4 Sword + 4 EnemyEntity + 6 Player + 1 EnemyAI)
- EditMode: 3/3 passing (3 Utils)

Both via the corrected batchmode command in `how-to-run-tests.md` (without `-quit`).
