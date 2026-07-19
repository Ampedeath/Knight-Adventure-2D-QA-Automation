using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Player.Start() subscribes to GameInput.Instance events, so a real GameInput singleton
// must exist in the scene before Player's first frame. Player.Awake() also caches
// Camera.main, so a tagged "MainCamera" must exist before Player is added.
//
// Not covered here: Dash() and GetPlayerScreenPosition() correctness relies on
// TrailRenderer/Camera wiring the Inspector normally provides; Dash() reads a private
// [SerializeField] trailRenderer field with no public setter, so without a prefab
// reference it stays null and DashRoutine() would NullReferenceException. Attack()
// (GameInput_OnPlayerAttack) only fires from a real Input System device event, which
// would require simulating hardware input rather than calling public API. Both are
// real testability issues, not omissions.
public class PlayerTests
{
    private GameObject _gameInputObject;
    private GameObject _cameraObject;
    private GameObject _subject;
    private GameObject _damageSource;
    private Player _player;

    [SetUp]
    public void SetUp()
    {
        _cameraObject = new GameObject("MainCamera");
        _cameraObject.tag = "MainCamera";
        _cameraObject.AddComponent<Camera>();

        _gameInputObject = new GameObject("GameInput");
        _gameInputObject.AddComponent<GameInput>();

        _subject = new GameObject("PlayerSubject");
        _subject.AddComponent<Rigidbody2D>();
        _subject.AddComponent<KnockBack>();
        _player = _subject.AddComponent<Player>();

        _damageSource = new GameObject("DamageSource");
        _damageSource.transform.position = _subject.transform.position + Vector3.left;
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(_subject);
        Object.Destroy(_damageSource);
        Object.Destroy(_gameInputObject);
        Object.Destroy(_cameraObject);
    }

    [UnityTest]
    public IEnumerator Player_TakeDamage_NonLethal_DoesNotInvokeOnPlayerDeath()
    {
        yield return null; // let Start() run (health init, GameInput subscription)

        bool deathRaised = false;
        _player.OnPlayerDeath += (sender, args) => deathRaised = true;

        _player.TakeDamage(_damageSource.transform, 5);

        Assert.IsFalse(deathRaised);
        Assert.IsTrue(_player.IsAlive());
    }

    [UnityTest]
    public IEnumerator Player_TakeDamage_InvokesOnFlashBlink()
    {
        yield return null;

        bool flashRaised = false;
        _player.OnFlashBlink += (sender, args) => flashRaised = true;

        _player.TakeDamage(_damageSource.transform, 1);

        Assert.IsTrue(flashRaised);
    }

    [UnityTest]
    public IEnumerator Player_TakeDamage_Lethal_InvokesOnPlayerDeathAndBecomesNotAlive()
    {
        yield return null;

        bool deathRaised = false;
        _player.OnPlayerDeath += (sender, args) => deathRaised = true;

        _player.TakeDamage(_damageSource.transform, 10); // default maxHealth

        Assert.IsTrue(deathRaised);
        Assert.IsFalse(_player.IsAlive());
    }

    [UnityTest]
    public IEnumerator Player_TakeDamage_AfterDeath_DoesNotInvokeOnPlayerDeathAgain()
    {
        yield return null;
        _player.TakeDamage(_damageSource.transform, 10);

        int deathCount = 0;
        _player.OnPlayerDeath += (sender, args) => deathCount++;

        _player.TakeDamage(_damageSource.transform, 1);

        Assert.AreEqual(0, deathCount);
    }

    [UnityTest]
    public IEnumerator Player_TakeDamage_WithinRecoveryWindow_DoesNotInvokeOnFlashBlinkAgain()
    {
        yield return null;
        _player.TakeDamage(_damageSource.transform, 1);

        bool flashRaised = false;
        _player.OnFlashBlink += (sender, args) => flashRaised = true;

        _player.TakeDamage(_damageSource.transform, 1); // still within damageRecoveryTime

        Assert.IsFalse(flashRaised);
    }

    [Test]
    public void Player_GetPlayerScreenPosition_MatchesMainCameraWorldToScreenPoint()
    {
        Vector3 expected = Camera.main.WorldToScreenPoint(_subject.transform.position);

        Assert.AreEqual(expected, _player.GetPlayerScreenPosition());
    }
}
