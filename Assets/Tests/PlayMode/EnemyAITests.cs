using NUnit.Framework;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Requires running inside the Editor: EnemyAI's tuning fields are private [SerializeField]s with
// no public setters, so the only way to get real production values is to instantiate the
// configured Skeleton prefab via AssetDatabase, same as EnemyEntityTests.
//
// Only checks the state right after spawn, before any Update() runs. Testing the actual
// Roaming/Chasing/Attacking behavior needs at least one Update() frame, but the test scene has no
// baked NavMesh, so that first Update() always logs an engine-level NavMesh error — and unlike
// EnemyEntityTests (same kind of error, but fired synchronously by a direct method call), nothing
// in Unity Test Framework reliably suppresses this specific error once it happens one frame later,
// after a coroutine yield. Tried several suppression approaches, none worked. Documented as a
// testability gap in docs/automation-feasibility.md instead of faking a pass.
public class EnemyAITests
{
    private const string SkeletonPrefabPath = "Assets/Prefabs/Enemies/Skeleton/Skeleton.prefab";

    private GameObject _subject;
    private EnemyAI _enemyAI;

    [SetUp]
    public void SetUp()
    {
#if UNITY_EDITOR
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkeletonPrefabPath);
        Assert.IsNotNull(prefab, $"Expected Skeleton prefab at {SkeletonPrefabPath}");
        _subject = Object.Instantiate(prefab);
#endif
        _enemyAI = _subject.GetComponent<EnemyAI>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(_subject);
    }

    [Test]
    public void EnemyAI_Awake_RoamingAnimationSpeedRatioStartsAtOne()
    {
        Assert.AreEqual(1f, _enemyAI.GetRoamingAnimationSpeed(), 0.0001f);
    }
}
