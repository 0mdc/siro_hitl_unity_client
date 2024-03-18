using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Habitat.Tests.EditMode
{
    public class TestPathUtils
    {
        [Test]
        public void TestSimplifyRelativePath()
        {
            Assert.AreEqual(
                "a/b/c.test",
                PathUtils.SimplifyRelativePath("a/././b/c.test")
            );
            Assert.AreEqual(
                "b/c.test",
                PathUtils.SimplifyRelativePath("a/./../b/c.test")
            );
            Assert.AreEqual(
                "test",
                PathUtils.SimplifyRelativePath("test")
            );
            Assert.AreEqual(
                "",
                PathUtils.SimplifyRelativePath("")
            );
            Assert.AreEqual(
                "",
                PathUtils.SimplifyRelativePath("./././.")
            );
        }

        [Test]
        public void TestRemoveExtension()
        {
            Assert.AreEqual(
                "test",
                PathUtils.RemoveExtension("test.test")
            );
            Assert.AreEqual(
                "a.b",
                PathUtils.RemoveExtension("a.b.c")
            );
            Assert.AreEqual(
                "/home/test/test",
                PathUtils.RemoveExtension("/home/test/test.test")
            );
            Assert.AreEqual(
                "",
                PathUtils.RemoveExtension("")
            );
        }

        [Test]
        public void TestHabitatPathToUnityAddress()
        {
            Assert.AreEqual(
                "data/test_dataset/a/b/c/d/e/asset",
                PathUtils.HabitatPathToUnityAddress("data/test_dataset/a/b/c/d/e/asset.glb")
            );
            Assert.AreEqual(
                "data/test_dataset/a/b/c/d/e/asset",
                PathUtils.HabitatPathToUnityAddress("data/test_dataset/a/b/c/d/e/asset")
            );
            Assert.AreEqual(
                "data/test_dataset/a/b/c/d/e/asset",
                PathUtils.HabitatPathToUnityAddress("data/123/../test_dataset/a/b/./c/d/f/g/.././../e/asset.glb")
            );
            Assert.AreEqual(
                "",
                PathUtils.HabitatPathToUnityAddress("")
            );
        }
    }
}
