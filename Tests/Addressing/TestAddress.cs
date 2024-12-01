using Containers.Addressing;
using System.Diagnostics;
using System.Reflection;

namespace Tests.Addressing
{
    /// <summary>
    /// Tests the generation of unique addressing values and their associated <see cref="IAddressProvider{T}"/> implementation
    /// </summary>
    [TestClass]
    public class TestAddress
    {
        public TestContext? TestContext { get; set; }


        // DynamicData source for generic type tests
        public static IEnumerable<object[]> TestTypes
        {
            get
            {
                yield return new object[] { typeof(short) };
                yield return new object[] { typeof(int) };
                yield return new object[] { typeof(long) };
                yield return new object[] { typeof(Int128) };
            }
        }

        /// <summary>
        /// Tests that the maximum expected address is calculated correctly
        /// </summary>
        [TestMethod]
        public void Explicit_TestMaxArgument()
        {
            long bonk = 1L << Address<long>.BitDensity;
            long max = bonk - 1L;

            // This is expected to workas the last possible value
            Address<long>.HashToBits(max);

            // This is expected to fail (max value + 1)
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                Address<long>.HashToBits(bonk), $"Address successfully calculated for expected failure value: {bonk}.");
        }


        /// <summary>
        /// Runs basic performance benchmarking
        /// </summary>
        [TestMethod]
        public void Test_PerformanceAndMemoryBenchmark()
        {
            // Warmup
            IAddressProvider<long> provider = new AddressProvider<long>();
            Iterate(provider);

            long initialMemory = GC.GetTotalMemory(true); //clean 
            Stopwatch s = Stopwatch.StartNew(); //start after clean
            int total = 0;
            for (int i = 0; i < 9; i++) total += Iterate(provider);
            double elapsed = s.Elapsed.TotalNanoseconds; // get time before clean
            elapsed = elapsed / total; // average it

            // Now calculate memory usage before collection
            long preCleanMemory = GC.GetTotalMemory(false);
            GC.Collect(); // Force a full sweeep
            GC.WaitForPendingFinalizers();
            long finalMemory = GC.GetTotalMemory(true); //Stop after clean
            // And count how much memory was freed by GC (insights into memory burden of address generation)
            long memoryCleaned = (preCleanMemory - initialMemory) - (finalMemory - initialMemory);

            // If the memory burden is high, then there's probably a memory leak
            if ((finalMemory - initialMemory) > 1024 * 1024)
                Assert.Fail($"Unexpected large memory consumption ({(finalMemory - initialMemory) / 1024}kb). Memory leak?");

            // Output the messages and stuff
            TestContext?.WriteLine($"Average Hash Time:   {Unitify(elapsed)}");
            TestContext?.WriteLine($"Uncollected Memory:  {finalMemory - initialMemory}b");
            TestContext?.WriteLine($"Collected Memory:    {memoryCleaned / 1024}kb");

        }


        /// <summary>
        /// Tests the maximum value of the given type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void TestMaxValue<T>() where T : struct
        {
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
                Assert.Fail($"Expected max argument ({typeof(T).Name})(2^{Address<T>.BitDensity} - 1) throws exception: {e}");
            }

            // This calculates the maximum value as a byte array (bitset style)
            byte[] tooLargeValue = new byte[Address<T>.ByteSize];
            tooLargeValue[Address<T>.BitDensity / 8] |= (byte)(1 << Address<T>.BitDensity % 8);

            // Now test the overshot value
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                Address<T>.HashToBits(Address<T>.FromByteArray(tooLargeValue))
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
                Assert.AreEqual(text.Length, testAddress.Size, $"The hash for ({typeof(T).Name}){i} is of incorrect length!");

                // Check the text is not a hash collision
                if (!included.Add(text))
                    Assert.Fail($"A hash collision exists at ({typeof(T).Name}){i}. Addresses should not collide.");

            }
        }

        /// <summary>
        /// Runs iterative and max value tests for the expected range of default int16 to int128)
        /// </summary>
        /// <param name="t"></param>
        [TestMethod]
        [DynamicData("TestTypes")]
        public void Generic_TestPrimitiveIntHashing(Type t)
        {

            Assert.IsTrue(t.IsValueType, $"TEST ERROR: The type {t} is not a value type and cannot be used for addressing.");
            // Get the methods
            var m = GetType().GetMethod("TestMaxValue", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(m, "TEST ERROR: Test failed to retrieve internal procedure 'TestMaxValue'");
            m = m.MakeGenericMethod(t); //genericize to the type
            Assert.IsNotNull(m, $"TEST ERROR: Test failed to genericize procecure 'TestMaxValue' to {t}");
            // Now invoke
            m.Invoke(this, null);

            // And again with the iterative test
            m = GetType().GetMethod("TestIteratively", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(m, "TEST ERROR: Test failed to retrieve internal procedure 'TestIteratively'");
            m = m.MakeGenericMethod(t);
            Assert.IsNotNull(m, $"TEST ERROR: Test failed to genericize procecure 'TestIteratively' to {t}");

        }

        /// <summary>
        /// Tests concurrency/thread-safety for the internal get/set allocator by generating a large number of addresses on many threads concurrently
        /// </summary>
        [TestMethod]
        public void Threading_TestConcurrentSafety()
        {
            //create a new provider
            IAddressProvider<long> provider = new AddressProvider<long>();

            // make sure it has no allocations yet
            Assert.AreEqual(provider.GetTotalAllocated(), 0);

            // Configure environment variables
            int iterations = 1_000_000; //loops to run on each thread
            int threads = Environment.ProcessorCount; // saturate
            ManualResetEvent signal = new(false);

            try
            {
                // Cache some stuff
                int byteSize = sizeof(long);
                Assert.AreEqual(
                    byteSize, Address<long>.ByteSize,
                    $"Critical error calculating size of address data! This should DEFINITELY not happen.");

                // Create a list of accumulators
                int[] accumulator = [0];
                Task<Exception?>[] tasks = new Task<Exception?>[threads];

                for (int i = 0; i < threads; i++)
                {
                    tasks[i] = RunConcurrentGenerator(provider, i, iterations, accumulator, signal);
                }
                // All threads ready, allow them to start
                signal.Set();

                // Wait and validate everything
                if (!Task.WaitAll(tasks, TimeSpan.FromSeconds(15)))
                {
                    Assert.Fail("Test Timeout! Potential deadlock or infinite loop.");
                }

                foreach (var t in tasks)
                {
                    if (t.Exception != null || t.Result != null)
                    {
                        var e = t.Result ?? t.Exception;
                        Assert.Fail(
                            $"Thread {Array.IndexOf(tasks, t)} failed with exception: {e?.Message ?? "Unknown error"}."
                        );
                    }
                }

                //Now we can instead just check the total allocation count of the provider
                Assert.AreEqual(accumulator[0], iterations * threads,
                    "TEST ERROR: Incorrect total iterations performed by test configuration");

                //Now we can instead just check the total allocation count of the provider
                Assert.AreEqual(iterations * threads, provider.GetTotalAllocated(),
                    "Mismatched allocation. The same address has been allocated twice.");

            }
            finally
            {
                signal.Dispose();
            }
        }

        /// <summary>
        /// Generates a threaded test task that runs the address generator the given number of times
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="provider">The provider instance being tested</param>
        /// <param name="threadIndex">Index of this generator</param>
        /// <param name="accumulator">Accumulator array to count operations</param>
        /// <param name="signal">Optional signal to synchronize all threads</param>
        /// <returns></returns>
        Task<Exception?> RunConcurrentGenerator<T>(IAddressProvider<T> provider, int threadIndex, int iterations, int[] accumulator, ManualResetEvent? signal = null) where T : struct
        {
            return Task.Run(() =>
            {
                // Wait if requested
                signal?.WaitOne();
                uint lastIndex = 0;
                try
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        var testAddress = provider.Get(out lastIndex);
                        string text = testAddress.Text;
                        // Verify length of address/text form
                        Assert.AreEqual(
                            text.Length, testAddress.Size,
                            $"The hash for {i} produces incorrect output length.");

                        // Verify uniqueness of address
                        Interlocked.Increment(ref accumulator[0]);
                    }
                    return null;
                }
                catch (Exception e)
                {
                    return new Exception($"Failure at index {lastIndex}: {e.Message}", e);
                }
            });
        }

        /// <summary>
        /// Performs a large number of iterations with the given provider
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="provider"></param>
        /// <returns></returns>
        int Iterate<T>(IAddressProvider<T> provider) where T : struct
        {
            for (int j = 0; j < 100_000; ++j) provider.Get();
            return 100_000;
        }

        /// <summary>
        /// Converts a nanosecond time into a nice readable ns/us/ms abbreviation
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        string Unitify(double val)
        {
            string[] nominators = ["ns", "us", "ms"];
            int magnitude = (int)Math.Log10(val) / 3;
            magnitude = int.Min(2, magnitude);
            //logify
            val /= Math.Pow(10, magnitude * 3);
            return $"{Math.Round(val * 100) / 100d}{nominators[magnitude]}";
        }
    }
}
