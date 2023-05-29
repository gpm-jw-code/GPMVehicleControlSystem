using Microsoft.VisualStudio.TestTools.UnitTesting;
using AGVSystemCommonNet6;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGVSystemCommonNet6.Tests
{
    [TestClass()]
    public class ExtensionsTests
    {
        [TestMethod()]
        public void ToIntTest()
        {
            int val = 5;
            bool[] bools = new bool[4] { false, true, false, true };
            Assert.AreEqual(val, bools.ToInt());

        }
    }
}