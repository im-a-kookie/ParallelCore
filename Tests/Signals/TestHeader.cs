using Containers.Signals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Signals
{
    [TestClass]
    public class TestHeaders
    {
        [TestMethod]
        public void Header_SetsIDCorrectly()
        {
            // Arrange
            ushort expectedId = 123;
            Header header = new Header(expectedId);
            Assert.AreEqual(expectedId, header.ID, "The ID is not set correctly.");
        }

        [TestMethod]
        public void Header_WithFlag_HasFlag()
        {
            ushort idValue = 123;
            bool expectedFlag = true;
            Header header = new Header(idValue, expectedFlag);
            Assert.IsTrue(header.DataFlag, "Data Flag should be true when set to true.");
        }

        [TestMethod]
        public void Header_WithFlag_HasID()
        {
            ushort expectedId = 123;
            bool flagValue = true;
            Header header = new Header(expectedId, flagValue);
            Assert.AreEqual(expectedId, header.ID, "The ID is not set correctly.");
        }


        [TestMethod]
        public void Header_WithoutFlag_NoFlag()
        {
            ushort idValue = 123;
            bool expectedFlag = false;
            Header header = new Header(idValue, expectedFlag);
            Assert.IsFalse(header.DataFlag, "Data Flag should be false when set to false.");
        }


        [TestMethod]
        public void Header_WithoutFlag_HasID()
        {
            ushort expectedId = 123;
            bool flagValue = false;
            Header header = new Header(expectedId, flagValue);
            Assert.AreEqual(expectedId, header.ID, "The ID is not set correctly.");
        }

        [TestMethod]
        public void Header_AppliesFlag_HasFlag()
        {
            ushort initialId = 123;
            Header header = new Header(initialId, false);
            header.DataFlag = true;
            Assert.IsTrue(header.DataFlag, "The DataFlag should be updated correctly after creation.");
        }

        [TestMethod]
        public void Header_AppliesFlag_KeepsID()
        {
            ushort expectedId = 123;
            Header header = new Header(expectedId, false);
            header.DataFlag = true;
            Assert.AreEqual(expectedId, header.ID, "The ID is not set correctly.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Header_RejectsNegativeValue()
        {
            short value = -5;
            Header header = new Header(value, false);
        }
        
        [TestMethod]
        public void Header_SignedConstructorSetsCorrectly()
        {
            short value = 123; 
            bool flag = true;
            Header header = new Header(value, flag);
            Assert.AreEqual((ushort)value, header.ID, "The ID property should be set correctly when using the short constructor.");
            Assert.IsTrue(header.DataFlag, "The DataFlag should be set correctly when using the short constructor.");
        }

        [TestMethod]
        public void Header_TestOverflowSetsFlag()
        {
            int bitLength = Marshal.SizeOf(typeof(Header)) * 8;
            // Calculate the length
            int predictedMaximum = (1 << bitLength) - 1;
            // Get the header value now
            Header header = new Header((ushort)predictedMaximum);
            Assert.IsTrue(header.DataFlag, "The header data flag expects True when MSB is set.");
        }

    }
}
