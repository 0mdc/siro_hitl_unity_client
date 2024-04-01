using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Habitat.Editor;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Habitat.Tests.EditMode
{
    
    public class TestAddressablesEditor
    {
        [Test]
        public void TestCreateLabelShortName()
        {
            for (uint counter = 0u; counter < 26u; ++counter)
            {
                string labelName = AddressablesEditor.CreateShortLabelName(counter);
                Assert.IsTrue(labelName.Length == 1);
                Assert.IsTrue(char.IsLower(labelName[0]));
            }
            for (uint counter = 27u; counter < 52u; ++counter)
            {
                string labelName = AddressablesEditor.CreateShortLabelName(counter);
                Assert.IsTrue(labelName.Length == 1);
                Assert.IsTrue(char.IsUpper(labelName[0]));
            }
            for (uint counter = 53u; counter < 10000u; ++counter)
            {
                string labelName = AddressablesEditor.CreateShortLabelName(counter);
                Assert.IsTrue(labelName.Length > 1);
                Assert.IsTrue(char.IsLetter(labelName[0]));
                for (int i = 1; i < labelName.Length; ++i)
                {
                    Assert.IsTrue(char.IsDigit(labelName[i]));
                }
            }
        }
    }
}