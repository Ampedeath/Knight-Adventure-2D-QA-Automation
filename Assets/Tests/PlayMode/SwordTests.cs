using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SwordTests
{
    private GameObject _subject;
    private Sword _sword;
    private PolygonCollider2D _collider;

    [SetUp]
    public void SetUp()
    {
        _subject = new GameObject("SwordSubject");
        _collider = _subject.AddComponent<PolygonCollider2D>();
        _sword = _subject.AddComponent<Sword>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(_subject);
    }

    [UnityTest]
    public IEnumerator Sword_Start_DisablesAttackCollider()
    {
        yield return null;

        Assert.IsFalse(_collider.enabled);
    }

    [Test]
    public void Sword_Attack_InvokesOnSwordSwing()
    {
        bool swingRaised = false;
        _sword.OnSwordSwing += (sender, args) => swingRaised = true;

        _sword.Attack();

        Assert.IsTrue(swingRaised);
    }

    [Test]
    public void Sword_Attack_LeavesAttackColliderEnabled()
    {
        _sword.Attack();

        Assert.IsTrue(_collider.enabled);
    }

    [Test]
    public void Sword_AttackColliderTurnOff_DisablesCollider()
    {
        _sword.Attack();

        _sword.AttackColliderTurnOff();

        Assert.IsFalse(_collider.enabled);
    }
}
