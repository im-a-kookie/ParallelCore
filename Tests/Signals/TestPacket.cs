using Containers.Signals;

namespace Tests.Signals
{
    [TestClass]
    public class PacketTests
    {
        [TestMethod]
        public void Test_EmptyPacket_ReturnsNullOrDefault()
        {
            // Arrange empty packet
            var packet = new Packet();

            // Nullable should be null
            var nullResult = packet.GetData<int?>();
            Assert.IsNull(nullResult, "GetData<int?>() expects null return.");
            // Value type should default to 0
            var intResult = packet.GetData<int>();
            Assert.AreEqual(intResult, 0, "GetData<int>() expects 0 return.");

        }

        [TestMethod]
        public void Test_PacketWithData_ShouldReturnCorrectType()
        {
            // Set packet with data
            var expectedData = 42;
            var packet = new Packet(expectedData);

            // Validate
            var result = packet.GetData<int>();
            Assert.AreEqual(expectedData, result, "GetData<int>() expects Integer return.");
        }

        [TestMethod]
        public void Test_PacketWithData_ReturnsNullTypeInvalid()
        {
            // Create a packet with an int
            var expectedData = 42;
            var packet = new Packet(expectedData);

            // Request a not-int (nullable)
            var result = packet.GetData<string>();
            Assert.IsNull(result, "Expected GetData<string>() to return null for a packet containing an integer.");
        }

        [TestMethod]
        public void Test_GetData_ShouldReturnOriginalData()
        {
            // Create with a reference type
            var expectedData = "Test String";
            var packet = new Packet(expectedData);

            // Ask for it back
            var result = packet.GetData();
            Assert.AreEqual(expectedData, result, "Expected GetData() to return the original object.");
        }

        [TestMethod]
        public void Test_GetDataFromEmptyPacket_ShouldReturnNull()
        {
            var packet = new Packet();
            var result = packet.GetData();
            Assert.IsNull(result, "Expected GetData() to return null for an empty packet.");
        }

        [TestMethod]
        public void Test_PacketWithNullData_ShouldReturnNull()
        {
            var packet = new Packet(null);
            var result = packet.GetData<object>();
            Assert.IsNull(result, "Expected GetData<object>() to return null for a packet containing null.");
        }
    }
}