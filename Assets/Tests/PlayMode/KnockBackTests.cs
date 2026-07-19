using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class KnockBackTests
{
    private GameObject _subject;
    private KnockBack _knockBack;
    private Rigidbody2D _rigidbody2D;
    private GameObject _damageSource;

    [SetUp]
    public void SetUp()
    {
        _subject = new GameObject("KnockBackSubject");
        _rigidbody2D = _subject.AddComponent<Rigidbody2D>();
        _knockBack = _subject.AddComponent<KnockBack>();

        _damageSource = new GameObject("DamageSource");
        _damageSource.transform.position = _subject.transform.position + Vector3.left;
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(_subject);
        Object.Destroy(_damageSource);
    }

    [Test]
    public void KnockBack_GetKnockedBack_SetsIsGettingKnockedBackTrue()
    {
        _knockBack.GetKnockedBack(_damageSource.transform);

        Assert.IsTrue(_knockBack.IsGettingKnockedBack);
    }

    [UnityTest]
    public IEnumerator KnockBack_GetKnockedBack_AppliesForceAwayFromDamageSource()
    {
        _knockBack.GetKnockedBack(_damageSource.transform);
        yield return new WaitForFixedUpdate();
        yield return null;

        Assert.Greater(_rigidbody2D.linearVelocity.x, 0f);
    }

    [UnityTest]
    public IEnumerator KnockBack_TimerExpires_StopsAutomatically()
    {
        _knockBack.GetKnockedBack(_damageSource.transform);

        yield return new WaitForSeconds(0.5f);

        Assert.IsFalse(_knockBack.IsGettingKnockedBack);
    }

    [Test]
    public void KnockBack_StopKnockBackMovement_ResetsStateAndVelocity()
    {
        _rigidbody2D.linearVelocity = new Vector2(5f, 5f);

        _knockBack.StopKnockBackMovement();

        Assert.IsFalse(_knockBack.IsGettingKnockedBack);
        Assert.AreEqual(Vector2.zero, _rigidbody2D.linearVelocity);
    }
}
