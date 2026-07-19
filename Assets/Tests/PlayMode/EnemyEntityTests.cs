using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Requires running inside the Editor (not a standalone Player build): EnemyEntity's
// enemySO field is a private [SerializeField] with no public setter, so the only way
// to exercise it without reflection is to instantiate the real, already-configured
// Skeleton prefab via AssetDatabase.
public class EnemyEntityTests
{
    private const string SkeletonPrefabPath = "Assets/Prefabs/Enemies/Skeleton/Skeleton.prefab";
    private const string SkeletonSOPath = "Assets/ScriptableObjects/SkeletonSO.asset";

    private GameObject _subject;
    private EnemyEntity _enemyEntity;
    private int _enemyHealth;

    [SetUp]
    public void SetUp()
    {
#if UNITY_EDITOR
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkeletonPrefabPath);
        Assert.IsNotNull(prefab, $"Expected Skeleton prefab at {SkeletonPrefabPath}");
        _subject = Object.Instantiate(prefab);

        EnemySO enemySO = AssetDatabase.LoadAssetAtPath<EnemySO>(SkeletonSOPath);
        Assert.IsNotNull(enemySO, $"Expected EnemySO asset at {SkeletonSOPath}");
        _enemyHealth = enemySO.enemyHealth;
#endif
        _enemyEntity = _subject.GetComponent<EnemyEntity>();

        // The prefab starts in the Roaming state and would try to path-find via
        // NavMeshAgent every frame; there is no baked NavMesh in the test scene.
        // Disabling EnemyAI keeps these tests focused on EnemyEntity's damage/death logic.
        _subject.GetComponent<EnemyAI>().enabled = false;
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(_subject);
    }

    [UnityTest]
    public IEnumerator EnemyEntity_TakeDamage_InvokesOnTakeHit()
    {
        yield return null; // let Start() initialize health from EnemySO

        bool hitRaised = false;
        _enemyEntity.OnTakeHit += (sender, args) => hitRaised = true;

        _enemyEntity.TakeDamage(1);

        Assert.IsTrue(hitRaised);
    }

    [UnityTest]
    public IEnumerator EnemyEntity_TakeDamage_LessThanHealth_DoesNotInvokeOnDeath()
    {
        yield return null;

        bool deathRaised = false;
        _enemyEntity.OnDeath += (sender, args) => deathRaised = true;

        _enemyEntity.TakeDamage(_enemyHealth - 1);

        Assert.IsFalse(deathRaised);
    }

    [UnityTest]
    public IEnumerator EnemyEntity_TakeDamage_ReducesHealthToZero_InvokesOnDeath()
    {
        yield return null;

        bool deathRaised = false;
        _enemyEntity.OnDeath += (sender, args) => deathRaised = true;

        // EnemyAI.SetDeathState() calls NavMeshAgent.ResetPath() unconditionally, which
        // logs this error whenever the agent isn't on a baked NavMesh (as in this test
        // scene). Real testability issue in EnemyAI, not specific to this test.
        LogAssert.Expect(LogType.Error, "\"ResetPath\" can only be called on an active agent that has been placed on a NavMesh.");
        _enemyEntity.TakeDamage(_enemyHealth);

        Assert.IsTrue(deathRaised);
    }

    [UnityTest]
    public IEnumerator EnemyEntity_TakeDamage_Lethal_DisablesColliders()
    {
        yield return null;

        var boxCollider = _subject.GetComponent<BoxCollider2D>();
        var polygonCollider = _subject.GetComponent<PolygonCollider2D>();

        LogAssert.Expect(LogType.Error, "\"ResetPath\" can only be called on an active agent that has been placed on a NavMesh.");
        _enemyEntity.TakeDamage(_enemyHealth);

        Assert.IsFalse(boxCollider.enabled);
        Assert.IsFalse(polygonCollider.enabled);
    }
}
