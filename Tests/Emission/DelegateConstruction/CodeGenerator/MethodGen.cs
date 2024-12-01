using System.Buffers;
using System.Text;

namespace Tests.Emission.DelegateConstruction.CodeGenerator
{
    internal class MethodGen
    {
        private static string[] parameterTypes = new[]
        {
                "int", "short", "double", "int?", "short?", "double?",
                "SampleDataClass", "SampleDataStruct", "SampleDataStruct?",
                "string", "Model", "Signal", "Int128", "Int128?",
                "string?"
        };
        private static string[] validReturns = new[]
            {   "void", "int", "int?", "short", "short?", "byte", "byte?", "float", "double",
                "double?", "SampleDataClass", "SampleDataClass?",
                "SampleDataStruct", "SampleDataStruct?"
            };

        /// <summary>
        /// Generates random parameter types with the given random
        /// </summary>
        /// <param name="numberOfParams"></param>
        /// <param name="numberOfSignatures"></param>
        /// <returns></returns>
        public static List<string[]> GetParameterTypes(Random randomizer, int numberOfParams, int numberOfSignatures)
        {
            List<string[]> results = new();
            for (int i = 0; i < numberOfSignatures; ++i)
            {
                if (numberOfParams == 0) results.Add([]);
                else
                {
                    results.Add(ArrayPool<string>.Shared.Rent(numberOfParams));

                    for (int j = 0; j < numberOfParams; ++j)
                    {
                        results[i][j] = $"{parameterTypes[randomizer.Next(parameterTypes.Length)]} param{j}";
                    }
                }
            }
            return results;
        }


        public static string[] GenerateMethodSignatures(Random randomizer)
        {
            StringBuilder codeBuilder = new StringBuilder();
            List<string> methods = new();
            int returnPos = 0;
            int counter = 0;
            for (int i = 0; i < 10; i++)
            {
                var paramTypes = GetParameterTypes(randomizer, i, 50);
                for (int pi = 0; pi < paramTypes.Count; ++pi)
                {
                    var signature = string.Join(", ", paramTypes[pi].Where(x => x != null && x.Length > 1));
                    var retVal = validReturns[(returnPos++) % validReturns.Length];
                    if (retVal == "void")
                    {
                        string method = $"public {retVal} Method{counter++}({signature}){{ }}";
                        methods.Add(method);
                    }
                    else
                    {
                        string returner = $"({retVal})1";
                        if (retVal.StartsWith("SampleData"))
                            returner = "new(1)";

                        string method = $"public {retVal} Method{counter++}({signature}){{ return {returner}; }}";
                        methods.Add(method);
                    }


                    ArrayPool<string>.Shared.Return(paramTypes[pi]);

                }
            }

            return methods.ToArray();

        }
    }


}

