using Containers.Emission;
using Containers.Models;
using Containers.Signals;
using Microsoft.Testing.Platform.Extensions.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using static Tests.Emission.TestSignatureMapping;

namespace Tests.Emission
{
    [TestClass]
    public class TestSignatureMapping
    {

        delegate int SampleDelegate(double first, StringBuilder second);

        /// <summary>
        /// Tests <see cref="Explorer.GetDelegateSignature(Type)"/>
        /// </summary>
        [TestMethod]
        public void Test_GetDelegateSignature()
        {
            var t = typeof(SampleDelegate);

            var result = Explorer.GetDelegateSignature(t);

            Assert.AreEqual(result.returnType, typeof(int), "The return parameter was not correctly retrieved");
            Assert.AreEqual(result.parameterTypes[0], typeof(double), "The method signature was not correctly retrieved");
            Assert.AreEqual(result.parameterTypes[1], typeof(StringBuilder), "The method signature was not correctly retrieved");

        }


        /// <summary>
        /// Validates <see cref="Explorer.MapTypeArrays(Type[], Type[])"/> for basic input arrays.
        /// </summary>
        [TestMethod]
        public void Test_AlikeTypeMatching()
        {
            var inputs = new Type[] { typeof(SampleClass), typeof(int), typeof(object) };
            var outputs = new Type[] { typeof(int), typeof(object), typeof(SampleClass) };
            var mappings = Explorer.MapTypeArrays(inputs, outputs);
            foreach(var m in mappings)
            {
                Assert.AreEqual(inputs[m.src], outputs[m.dst],
                    $"Type Mapping failed to match parameters for {m.src}=>{m.dst}");
            }
        }


        /// <summary>
        /// Validates <see cref="Explorer.MapTypeArrays(Type[], Type[])"/> for basic input arrays.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Test_InvalidSignature()
        {
            var inputs = new Type[] { typeof(SampleClass), typeof(int), typeof(object), typeof(object) };
            var outputs = new Type[] { typeof(int), typeof(object), typeof(object), typeof(SampleClass) };
            var mappings = Explorer.MapTypeArrays(inputs, outputs);
        }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

        /// <summary> Ensures exceptions for null output array </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_NullInput()
        {
            var mappings = Explorer.MapTypeArrays(null, []);
        }

        /// <summary> Ensures exceptions for null output array </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_NullOutput()
        {
            var mappings = Explorer.MapTypeArrays([], null);
        }

        /// <summary> Ensures exceptions for null input parameter </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_InputContainsNull()
        {
            var inputs = new Type[] { typeof(SampleClass), null, typeof(object) };
            var mappings = Explorer.MapTypeArrays(inputs, []);
        }

        /// <summary> Ensures exceptions for null output parameter </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test_OutputsContainsNull()
        {
            var outputs = new Type[] { typeof(SampleClass), null, typeof(object) };
            var mappings = Explorer.MapTypeArrays([], outputs);
        }

#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.


        /// <summary>
        /// Internal collection of test parameters
        /// </summary>
        static partial class TestParameters
        {
            public static TestItem Empty = new([]);
            public static TestItem DataOnly = new(typeof(SampleClass)) { Wildcards = 1 };
            public static TestItem SignalData = new(typeof(Signal), typeof(SampleClass)) { Wildcards = 1 };
            // this test consumes signal #1, and then the wildcard
            public static TestItem TwoSignals = new(typeof(Signal), typeof(Signal)) { NullEntries = 0, Wildcards = 1 };
            // this test consumes signal #1, and then the wildcard
            public static TestItem ThreeSignals = new(typeof(Signal), typeof(Signal), typeof(Signal)) { NullEntries = 1, Wildcards = 1 };
            public static TestItem TwoDatas = new(typeof(Router), typeof(object), typeof(Model), typeof(Signal), typeof(SampleClass))
            {
                Wildcards = 1,
                NullEntries = 1
            };
            // Here, the first object consumes the wildcard
            // Then we are left with two unfillable parameters
            public static TestItem MisplacedWildcards = new(typeof(object), typeof(object), typeof(object), typeof(Signal), typeof(SampleClass), typeof(SampleClass))
            {
                Wildcards = 4,
                NullEntries = 2
            };

            // Here, the wildcard SampleClass is placed correctly and will absorb the wildcard from the entry delegate
            public static TestItem CorrectWildcards = new(typeof(SampleClass), typeof(object), typeof(object), typeof(Signal), typeof(object), typeof(SampleClass))
            {
                Wildcards = 4,
                NullEntries = 1
            };
        }


        /// <summary>
        /// Tests <see cref="Explorer.MapTypeArrays(Type[], Type[])"/> for core delegate <see cref="Router.EndpointCallback"/>
        /// </summary>
        [TestMethod]
        public void Test_MapEndpointCallback()
        {
            // Use the actual router callback delegate
            var inputTypes = Explorer.GetDelegateSignature(typeof(Router.EndpointCallback)).parameterTypes;

            var tests = TestParameters.GetItems();
            foreach (var testItem in tests)
            {
                var mappings = Explorer.MapTypeArrays(inputTypes, testItem.Values);

                // ensure correct count
                Assert.AreEqual(mappings.Count, testItem.Values.Length, 
                    $"Mapping for {testItem.Name} provides incorrect output arguments!");
                
                // Ensure progressive sorting
                for(int i = 0; i < mappings.Count; ++i)
                {
                    Assert.AreEqual(i, mappings[i].dst,
                        $"Mapping for {testItem.Name} is incorrectly sorted. " +
                        $"Indices must be progressive.");
                }

                // Ensure type compatibility
                int nulls = 0;
                for (int i = 0; i < mappings.Count; ++i)
                {
                    // count nulls and skip to avoid index checks
                    if (mappings[i].src == -1)
                    {
                        ++nulls;
                        continue;
                    }

                    Assert.IsTrue(
                        // Either the types match (e.g Entry is Model and Target inherits Model)
                        // Or the target is an object parameter
                        Explorer.CheckTypeCompabitility(inputTypes[mappings[i].src], testItem.Values[mappings[i].dst]),
                        // .
                        $"Mapping for {testItem.Name} provides incompatible types for argument {i}. " +
                        $"Expect: <{inputTypes[mappings[i].src]}>, Got: <{testItem.Values[mappings[i].dst]}>. " +
                        $"Mapping: {mappings[i].src}=>{mappings[i].dst}");
                }

                // validate null counting from the loop above
                Assert.AreEqual(nulls, testItem.NullEntries, 
                    $"Mapping for {testItem.Name} provides unexpected null entry count! " +
                    $"This can be implementation error, or test configuration error.");

            }
        }

        /// <summary>
        /// An empty class to validate wildcards on guaranteed-unknown types
        /// </summary>
        class SampleClass
        {

        }

        /// <summary>
        /// Helper class for defining test items for this test item
        /// </summary>
        public class TestItem : IEnumerable<Type>
        {
            public string Name = "";
            public int Wildcards = 0;
            public int NullEntries = 0;
            public Type[] Values;
            public TestItem(IList<Type> types)
            {
                Values = types.ToArray();
            }

            public TestItem(params Type[] types)
            {
                Values = types;
            }

            public IEnumerator<Type> GetEnumerator()
            {
                return (IEnumerator<Type>)Values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return Values.GetEnumerator();
            }
        }

        /// <summary>
        /// Provides a GetItems implementation. Implementation moved to bottom of file to reduce clutter
        /// </summary>
        static partial class TestParameters
        {
            /// <summary>
            /// Gets the list of test items out of this arameter class
            /// </summary>
            /// <returns></returns>
            public static List<TestItem> GetItems()
            {
                var testclass = typeof(TestParameters);
                var fields = testclass.GetFields().Where(x => x.FieldType == typeof(TestItem));
                // Add the fields iteratively
                List<TestItem> tests = new();
                foreach (var f in fields)
                {
                    var item = f.GetValue(null);
                    if (item != null && item is TestItem testItem)
                    {
                        tests.Add(testItem);
                        testItem.Name = f.Name;
                    }
                }
                return tests;
            }
        }

    }
}
