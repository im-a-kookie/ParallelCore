using Containers.Models;
using Containers.Models.Signals;
using System.Drawing;
using System.Reflection;

namespace Tests.Emission.DelegateConstruction
{
    /// <summary>
    /// Defines a wide variety of method signatures
    /// </summary>
    public partial class DelegateTest_Signatures
    {

        [BundleDescription("Takes: Non-Nullable Primitive, Class, Struct.\nReturns: Non-Nullable Value Type.\nData Ignored.")]
        public class ValueType_PlainInput_Unconsidered : Bundle
        {

            public override int ExpectedSuccessValue() => 1;

            public int Int32_Empty()
            {
                return 1;
            }

            public byte Int18_Randoms(Model target, long integer, string text, Router router)
            {
                return 1;
            }

            public short Int16_Randoms(Model target, Int128 integer, string text, Router router)
            {
                return 1;
            }

            public int Int32_Random(SampleDataClass data, int integer, string text)
            {
                return 1;
            }

            public long Int64_Random(Model target, int integer, SampleDataStruct data, string command, Signal signal)
            {
                return 1;
            }

            public float FP32_Data_Random(SampleDataClass data, Point integer, Signal signal)
            {
                return 1;
            }

            public double FP64_Data_Random(Model m, SampleDataClass data)
            {
                return 1;
            }

            public SampleDataStruct Struct_Data_Random(SampleDataClass data, string text, Router router)
            {
                return new(1);
            }
        }


        [BundleDescription("Takes: Mixed Class, Primitive, Struct.\nReturns: Non-Nullable Value Type.\nData Ignored.")]
        public class ValueType_MixedInputs_Unconsidered : Bundle
        {

            public override int ExpectedSuccessValue() => 1;

            public int Int32_Empty()
            {
                return 1;
            }

            public byte Int18_Randoms(Model? target, long integer, string text, Router router)
            {
                return 1;
            }

            public short Int16_Randoms(Model target, Int128? integer, string text, Router? router)
            {
                return 1;
            }

            public int Int32_Random(SampleDataClass data, int? integer, string text)
            {
                return 1;
            }

            public long Int64_Random(Model target, int? integer, SampleDataStruct data, string? command, Signal signal)
            {
                return 1;
            }

            public float FP32_Data_Random(SampleDataClass data, Point? point, Signal signal)
            {
                return 1;
            }

            public double FP64_Data_Random(Model? m, SampleDataClass data)
            {
                return 1;
            }
            public SampleDataStruct Struct_Data_Random(SampleDataClass data, string text, Router router)
            {
                return new(1);
            }

        }

        [BundleDescription("Takes: Mixed Class, Primitive, Struct.\nReturns: Non-Nullable Value Type.\nData Ignored.")]
        public class NullableReturn_Mixed_Unconsidered : Bundle
        {

            public override int ExpectedSuccessValue() => 1;

            public int? Int32_Empty()
            {
                return 1;
            }

            public byte? Int18_Randoms(Model? target, long integer, string text, Router router)
            {
                return 1;
            }

            public short? Int16_Randoms(Model target, Int128? integer, string text, Router? router)
            {
                return 1;
            }

            public int? Int32_Random(SampleDataClass data, int? integer, string text)
            {
                return 1;
            }

            public long? Int64_Random(Model target, int? integer, SampleDataStruct data, string? command, Signal signal)
            {
                return 1;
            }

            public float? FP32_Data_Random(SampleDataClass data, Point? point, Signal signal)
            {
                return 1;
            }

            public double? FP64_Data_Random(Model? m, SampleDataClass data)
            {
                return 1;
            }
            public SampleDataStruct? Struct_Data_Random(SampleDataClass data, string text, Router router)
            {
                return new(1);
            }
        }








        /// <summary>
        /// A simple description annotation
        /// </summary>
        internal class BundleDescription : Attribute
        {
            public string Text { get; set; }
            public BundleDescription(string text)
            {
                this.Text = text;
            }
        }

        /// <summary>
        /// A simple Bundle class that makes it a little easier to get all of the methods out
        /// </summary>
        public abstract class Bundle
        {
            /// <summary>
            /// An Integer value that represents the output of every test. All results provided must provide
            /// Object.Equals(ExpectedSuccessValue) for successful condition.
            /// </summary>
            /// <returns></returns>
            public abstract int ExpectedSuccessValue();

            public MethodInfo[] GetVirtualMethods()
            {
                return GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }

            public MethodInfo[] GetTypeStaticMethods()
            {
                return GetType().GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }

        }

    }
}
