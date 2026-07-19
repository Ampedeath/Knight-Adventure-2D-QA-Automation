# Repository analysis

Snapshot of `Assets/Scripts/` as of the classes actually read while writing tests. This is not exhaustive documentation of every file — it covers the gameplay-logic classes and their real dependencies, which is what test design needs.

## Gameplay-logic classes (test target)

### Player — `Assets/Scripts/Player/Player.cs`
Singleton (`Player.Instance`, set in `Awake`). Owns health, damage recovery window, dash, death.
- `Awake()`: caches `GetComponent<Rigidbody2D>()`, `GetComponent<KnockBack>()`, `Camera.main`.
- `Start()`: subscribes to `GameInput.Instance.OnPlayerAttack` / `OnPlayerDash` — **requires `GameInput.Instance` to already exist** when `Start()` runs.
- `TakeDamage(Transform damageSource, int damage)`: only applies while `_canTakeDamage && _isAlive`; sets a `damageRecoveryTime`-long invincibility window via `DamageRecoveryRoutine`; always calls `DetectDeath()` afterward (so overkill/duplicate calls are safe).
- `DetectDeath()`: on health reaching 0, disables input via `GameInput.Instance.DisableMovement()`, raises `OnPlayerDeath`.
- Public surface: `IsAlive()`, `IsRunning()`, `GetPlayerScreenPosition()`, `OnPlayerDeath`, `OnFlashBlink`. No public health getter.
- `Dash()` requires a `[SerializeField] private TrailRenderer trailRenderer` — only assignable via the Inspector, no public setter.

### GameInput — `Assets/Scripts/GameInput.cs`
Singleton (`GameInput.Instance`). Thin wrapper around a generated `PlayerInputActions` (new Input System). `Awake()` constructs and enables it — no scene-asset dependency, safe to instantiate directly in a test. Exposes `OnPlayerAttack`, `OnPlayerDash` events (fired from Input System callbacks, not from any public method), `GetMovementVector()`, `GetMousePosition()`, `DisableMovement()`.

### ActiveWeapon — `Assets/Scripts/Weapon/ActiveWeapon.cs`
Singleton (`ActiveWeapon.Instance`) holding the equipped `Sword` (`[SerializeField] private Sword sword`). `GetActiveWeapon()` is a trivial getter. `Update()`/`FollowMousePosition()` rotates to face the mouse, but only runs `if (Player.Instance.IsAlive())` and reads `GameInput.Instance.GetMousePosition()` + `Player.Instance.GetPlayerScreenPosition()` — same singleton-wiring cost as Player tests.

### Sword — `Assets/Scripts/Weapon/Sword/Sword.cs`
`PolygonCollider2D`-based melee weapon, no singleton dependency. `Start()` disables its collider; `Attack()` re-enables it and raises `OnSwordSwing`; `OnTriggerEnter2D` deals `damageAmount` to any `EnemyEntity` it touches.

### EnemyEntity — `Assets/Scripts/Skeleton/EnemyEntity.cs`
`[RequireComponent(PolygonCollider2D, BoxCollider2D, EnemyAI)]`. Health comes from `[SerializeField] private EnemySO enemySO` — no public setter, so the only way to give it real values without reflection is to load the actual configured prefab. `TakeDamage(int)` raises `OnTakeHit`, then `DetectDeath()` disables both colliders, calls `EnemyAI.SetDeathState()`, and raises `OnDeath`. `OnTriggerStay2D` deals damage to any `Player` standing in it.

### EnemyAI — `Assets/Scripts/Skeleton/EnemyAI.cs`
`NavMeshAgent`-driven state machine: `Idle / Roaming / Chasing / Attacking / Death`. Reads `Player.Instance.transform.position` / `Player.Instance.IsAlive()` every state check. `SetDeathState()` calls `_navMeshAgent.ResetPath()` unconditionally — logs an error if the agent isn't on a baked NavMesh (true of any test scene without a bake).

### EnemySO — `Assets/Scripts/ScriptableObjects/EnemySO.cs`
Plain `ScriptableObject` config: `enemyName`, `enemyHealth`, `enemyDamageAmount`. The project's only ScriptableObject-config pattern currently in use.

### KnockBack — `Assets/Scripts/Other/KnockBack.cs`
`[RequireComponent(Rigidbody2D)]`. Self-contained physical knockback: `GetKnockedBack(Transform damageSource)` applies an impulse away from the source and starts a timer; `Update()` auto-stops via `StopKnockBackMovement()` once the timer expires. No singleton or scene-asset dependency — fully unit-testable with a plain `GameObject`.

### DestructiblePlant — `Assets/Scripts/Plants/DestructiblePlant.cs`
`OnTriggerEnter2D`: if the colliding object has a `Sword` component, raises `OnDestructibleTakeDamage`, destroys itself, then calls `NavMeshSurfaceManagement.Instance.RebakeNavmeshSurface()` — **unconditionally**, no null-check.

### NavMeshSurfaceManagement — `Assets/Scripts/NavMeshSurfaceManagement.cs`
Singleton (`NavMeshSurfaceManagement.Instance`) wrapping a `NavMeshPlus.Components.NavMeshSurface`. `RebakeNavmeshSurface()` calls `BuildNavMesh()` synchronously.

### Utils — `Assets/Scripts/Utils.cs`
`namespace KnightAdventure.Utils`, static class with one method: `GetRandomDir()` → random normalized `Vector3` direction (z is implicitly 0, `Vector3(x, y)` constructor). Used by `EnemyAI.Roaming()`.

## Presentation layer (`*Visual` classes + `Misc/`)

Per project convention, these react to the logic-layer's public events/state and drive `Animator`/`SpriteRenderer`/VFX — they hold no gameplay state of their own and are not test targets:

- **PlayerVisual** — animates run/die states off `Player.Instance`, flips sprite based on mouse position vs. `Player.Instance.GetPlayerScreenPosition()`.
- **SkeletonVisual** — animates run/attack/hit/die off `EnemyAI`/`EnemyEntity` events; also exposes `TriggerAttackAnimationTurnOn/Off()` which just forward to `EnemyEntity.PolygonColliderTurnOn/Off()` (likely called from Animation Events).
- **SwordVisual**, **SwordSlashVisual** — trigger an attack animation off `Sword.OnSwordSwing`.
- **DestructiblePlantVisual** — spawns a death VFX prefab off `DestructiblePlant.OnDestructibleTakeDamage`.
- **Misc/FlashBlink** — swaps sprite material briefly on `Player.OnFlashBlink`, has a public `StopBlinking()`.
- **Misc/SelfDestoyVFX** — destroys its own GameObject once its `ParticleSystem` finishes playing.
- **Misc/TransparencyDetection** — fades a `SpriteRenderer`'s alpha when a `Player` with a `CapsuleCollider2D` enters/exits its trigger (used for occluding scenery like trees).

## Singleton graph

Four `static X Instance { get; private set; }` singletons, all set in `Awake()` with **no null-checks anywhere they're consumed**:

- `Player.Instance`
- `GameInput.Instance`
- `ActiveWeapon.Instance`
- `NavMeshSurfaceManagement.Instance`

Every consumer assumes exactly one instance already exists in the scene by the time it runs. This is the dominant testability constraint across the codebase — see `automation-feasibility.md`.

## Confirmed absent from the repo

No inventory, save/load, farming/growth-cycle, or NPC dialogue/quest systems exist anywhere in `Assets/Scripts/`. Don't assume or fabricate these if asked to test or extend them — say so directly.
