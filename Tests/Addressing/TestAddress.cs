using Containers.Addressing;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Addressing
{
    /// <summary>
    /// Tests the generation of unique addressing values and their associated <see cref="IAddressProvider{T}"/> implementation
    /// </summary>
    [TestClass]
    public class TestAddress
    {


        // DynamicData source for generic type tests
        public static IEnumerable<object[]> TestTypes
        {
            get
            {
                yield return new object[] { typeof(short) };
                yield return new object[] { typeof(int) };
                yield return new object[] { typeof(long) };
            }
        }


        /// <summary>
        /// Tests that the maximum expected address is calculated correctly
        /// </summary>
        [TestMethod]
        public void Test_MaximumArgument_Long()
        {
            long max = 1L << Address<long>.BitDensity;
            long pre = max - 1L;
            Address<long>.HashToBits(pre);
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => Address<long>.HashToBits(max), $"Address successfully calculated for expected failure value {max}.");
        }

        /// <summary>
        /// Tests the maximum value of the given type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void TestMaxValue<T>() where T : struct
        {
            typeof(Address<T>).GetMethod("ResetCounter", BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, null);

            // This calculates the maximum value as a byte array (bitset style)
            byte[] maximumValue = new byte[Address<T>.ByteSize];
            for (int i = 0; i < Address<T>.BitDensity; ++i)
            {
                // Stamp the bit into the right spot
                maximumValue[i / 8] |= (byte)(1 << i % 8);
            }


            try
            {
                Address<T>.HashToBits(Address<T>.FromByteArray(maximumValue));
            }
            catch (Exception e)
            {
                Assert.Fail($"Expected max argument (2^{Address<T>.BitDensity} - 1) throws exception: {e}");
            }

            // This calculates the maximum value as a byte array (bitset style)
            byte[] tooLargeValue = new byte[Address<T>.ByteSize];
            tooLargeValue[Address<T>.BitDensity / 8] |= (byte)(1 << Address<T>.BitDensity % 8);

            // Now test the overshot
            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => Address<T>.HashToBits(Address<T>.FromByteArray(tooLargeValue))
            );

        }

        /// <summary>
        /// Tests the given addressor iteratively with the provided <typeparamref name="T"/> as the backing struct.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="max_iterations"></param>
        public void TestIteratively<T>(int max_iterations = 1_000_000) where T : struct
        {
            //make the provider
            IAddressProvider<T> provider = new AddressProvider<T>();

            // Calculate the actual iterations as the max of the provided limit, and the capacity of the data type
            long iterations = long.Min(max_iterations, (1 << Address<T>.BitDensity) - 1);

            // We must ensure uniqueness of hashes
            HashSet<string> included = new HashSet<string>((int)iterations);
            for (int i = 0; i < iterations; ++i)
            {
                var testAddress = provider.Get();

                // Check the text is valid in length/mapping
                string text = testAddress.Text;
                Assert.AreEqual(text.Length, testAddress.Size, $"The hash for {i} produces {testAddress.Text.Length} characters, {testAddress.Size} expected");
              
                // Check the text is not a hash collision
                if (!included.Add(text))
                    Assert.Fail($"A hash collision exists at {i}. Addresses should not collide.");

            }
        }

        /// <summary>
        /// Runs all expected tests for the given backing type
        /// </summary>
        /// <param name="t"></param>
        [TestMethod]
        [DynamicData("TestTypes")]
        public void Test_Address(Type t)
        {

            Assert.IsTrue(t.IsValueType, $"TEST ERROR: The type {t} is not a value type and cannot be used for addressing.");

            // Get the methods
            var m = GetType().GetMethod("TestMaxValue", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(m, "Test failed to retrieve internal procedure 'TestMaxValue'");
            m = m.MakeGenericMethod(t); //genericize to the type
            Assert.IsNotNull(m, $"Test failed to genericize procecure 'TestMaxValue' to {t}");
            // Now invoke
            m.Invoke(this, null);

            // And again with the iterative test
            m = GetType().GetMethod("TestIteratively", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(m, "Test failed to retrieve internal procedure 'TestIteratively'");
            m = m.MakeGenericMethod(t);
            Assert.IsNotNull(m, $"Test failed to genericize procecure 'TestIteratively' to {t}");

        }


        [TestMethod]
        public void Test_Address_ConcurrentSafety()
        {
            //create a new provider
            IAddressProvider<long> provider = new AddressProvider<long>();


            // Configure environment variables
            int iterations = 30_000; //loops to run on each thread
            int threads = 8; //total threads
            int counters = threads; //debug counter
            bool stable = true; //flag for test stability
            Exception? failure = null; //ensure catchability of internal thrown exceptions outside of loop


            // Cache some stuff
            int byteSize = sizeof(long);
            Assert.AreEqual(byteSize, Address<long>.ByteSize, "Critical error calculating size of address data!!!");

            // Collection pool for hashes... painnn
            // Unnecessary extra effort option: one hashset per thread and combine afterwards
            ConcurrentDictionary<string, int> flags = new ConcurrentDictionary<string, int>(threads, iterations);

            Task[] tasks = new Task[threads];
            for (int i = 0; i < threads; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    uint lastIndex = 0;
                    try
                    {
                        for (int i = 0; stable && i < iterations; i++)
                        {
                            var testAddress = provider.Get(out lastIndex);
                            string text = testAddress.Text;
                            // Verify length of address/text form
                            Assert.AreEqual(text.Length, testAddress.Size, $"The hash for {i} produces {testAddress.Text.Length} characters, {testAddress.Size} expected");


                            // Verify uniqueness of address
                            if (!flags.TryAdd(text, i))
                            {
                                Assert.Fail($"A hash collision exists. Addresses should not collide.");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        // Safely set the first exception that was thrown
                        bool state = Interlocked.CompareExchange(ref stable, true, false);
                        if (!state) failure = new Exception($"Failure occurred at index {lastIndex}", e);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref counters);
                    }
                });
            }

            // Wait and validate everything
            Task.WaitAll(tasks);
            Assert.AreEqual(counters, 0, "TEST FAIL: counters not correctly reset");
            Assert.AreEqual(flags.Count, iterations * threads, $"A hash collision exists. Addresses should not collide.");
            Assert.IsTrue(stable, $"A failure state occured while performing concurrent tests. Exception: {failure ?? null}");
        }

    }
}
