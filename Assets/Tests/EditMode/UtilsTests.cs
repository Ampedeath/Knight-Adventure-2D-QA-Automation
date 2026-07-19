using NUnit.Framework;
using UnityEngine;
using KnightAdventure.Utils;

public class UtilsTests
{
    [Test]
    public void Utils_GetRandomDir_ReturnsNormalizedVector()
    {
        for (int i = 0; i < 100; i++)
        {
            Vector3 dir = Utils.GetRandomDir();

            Assert.AreEqual(1f, dir.magnitude, 0.0001f);
        }
    }

    [Test]
    public void Utils_GetRandomDir_KeepsZComponentZero()
    {
        for (int i = 0; i < 100; i++)
        {
            Vector3 dir = Utils.GetRandomDir();

            Assert.AreEqual(0f, dir.z);
        }
    }

    [Test]
    public void Utils_GetRandomDir_VariesBetweenCalls()
    {
        Vector3 first = Utils.GetRandomDir();

        bool foundDifferentDir = false;
        for (int i = 0; i < 20; i++)
        {
            if (Utils.GetRandomDir() != first)
            {
                foundDifferentDir = true;
                break;
            }
        }

        Assert.IsTrue(foundDifferentDir);
    }
}
