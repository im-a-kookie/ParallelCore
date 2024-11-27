using Containers.Signals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Containers.Emission
{
    /// <summary>
    /// Explores the given class and detects members that match the conditions
    /// </summary>
    public class Explorer
    {

        /// <summary>
        /// Maps parameters from input to output for delegate construction
        /// </summary>
        public struct Mapping
        {
            int src;
            int dst;
            public Mapping(int src, int dst)
            {
                this.src = src; this.dst = dst;
            }
        }

        /// <summary>
        /// A bundle that describes how to map <see cref="Signals.Router.EndpointCallback"/> to the given method
        /// </summary>
        class DelegateBundle
        {
            /// <summary>
            /// The entry point delegate
            /// </summary>
            public Type EntryPoint;

            /// <summary>
            /// The method that will be called
            /// </summary>
            public MethodInfo Target;
            /// <summary>
            /// The mapping of parameters from <see cref="Signals.Router.EndpointCallback"/> to <see cref="Target"/>
            /// </summary>
            public List<Mapping> ParameterMappings = new();

            /// <summary>
            /// The callback function that invokes <see cref="Target"/> and matches the signature of <see cref="Signals.Router.EndpointCallback"/>
            /// </summary>
            public Delegate? Callback;

            /// <summary>
            /// Prepares a delegate bundle with the given entry point and target method.
            /// </summary>
            /// <param name="entrypoint"></param>
            /// <param name="target"></param>
            public DelegateBundle(Type entrypoint, MethodInfo target)
            {
                this.EntryPoint = entrypoint;
                this.Target = target;
            }

            /// <summary>
            /// Generates the callback within this bundle
            /// </summary>
            public Delegate? GenerateCallback()
            {
                //1. Compute the parameter mappings
                ParameterMappings = GenerateSignatureMappings(EntryPoint, Target);

                // now refer to the delegate builder to construct the IL method calls

                return null;
            }

        }

        /// <summary>
        /// Encapsulation context for mapping parameters in the parameter mapping algorithm. Provides some useful helpers and validation.
        /// </summary>
        public class MappingContext
        {
            /// <summary>
            /// The input parameters provided to the mapping algorithm
            /// </summary>
            public Type[] InputParameters { get; set; }

            /// <summary>
            /// The output parameters expected to be mapped from the inputs
            /// </summary>
            public Type[] OutputParameters { get; set; }

            /// <summary>
            /// Dictionary mapping <see cref="OutputParameters"/> index to <see cref="InputParameters"/> index (or -1 for null/default).
            /// </summary>
            public Dictionary<int, int> Mappings { get; set; }

            /// <summary>
            /// A flag array indicating the consumption state of input parameters
            /// </summary>
            public bool[] SolvedInputs { get; set; }

            /// <summary>
            /// A flag array indicating the consumption state of output parameters
            /// </summary>
            public bool[] SolvedOutputs { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="MappingContext"/> class.
            /// </summary>
            /// <param name="inputParameters">The input parameters.</param>
            /// <param name="outputParameters">The output parameters.</param>
            public MappingContext(Type[] inputParameters, Type[] outputParameters)
            {
                InputParameters = inputParameters;
                OutputParameters = outputParameters;
                Mappings = new Dictionary<int, int>(); // Initialize the dictionary to hold mappings
                SolvedInputs = new bool[inputParameters.Length]; // Initialize input flags array
                SolvedOutputs = new bool[outputParameters.Length]; // Initialize output flags array
            }

            /// <summary>
            /// Maps the output index to the input index and consumes both parameters
            /// </summary>
            /// <param name="o"></param>
            /// <param name="i"></param>
            /// <exception cref="ArgumentOutOfRangeException"></exception>
            public void Map(int o, int i)
            {
                // Validate the indices
                // Remmeber that -1 is permitted to indicate nullification
                if (i >= InputParameters.Length || (i < 0 && i != -1))
                    throw new ArgumentOutOfRangeException(
                        "The index of the input is not valid for the parameter array");

                if(o < 0 || o >= OutputParameters.Length)
                    throw new ArgumentOutOfRangeException(
                        "The output index must fall within the output parameter array!");

                if (SolvedInputs[o])
                    throw new InvalidOperationException(
                        $"The output parameter at index {o} is already provided");

                // Now we can fill it
                Mappings.TryAdd(o, i);
                SolvedInputs[i] = true;
                SolvedOutputs[o] = true;
            }

            /// <summary>
            /// Computes this context into a list of mappings, ordered by output parameter index.
            /// </summary>
            /// <returns>A sorted result of mapping structs</returns>
            public List<Mapping> ComputeSortedMapping(bool nullifyEmptyOutputs = true)
            {
                for(int o = 0; o < OutputParameters.Length; ++o)
                {
                    if (!SolvedOutputs[o] && nullifyEmptyOutputs)
                    {
                        FillUnmappedOutputs(this);
                        break;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Parameter could not be located for output parameter {o} ({OutputParameters[o]}" +
                            $". Mapping cannot be generated.");
                    }
                }

                // Now just map and done
                return Mappings
                .OrderBy(x => x.Key) // Sort by Value in ascending order
                .Select(x => new Mapping(x.Value, x.Key))
                .ToList();
            }

        }

        /// <summary>
        /// Gets the signature of a delegate from the given type
        /// </summary>
        /// <param name="delegateType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static (Type returnType, Type[] parameterTypes) GetDelegateSignature(Type delegateType)
        {
            if (!typeof(Delegate).IsAssignableFrom(delegateType))
                throw new ArgumentException("Type must be a delegate.", nameof(delegateType));

            // Get the Invoke method of the delegate
            var invokeMethod = delegateType.GetMethod("Invoke");

            if (invokeMethod == null)
                throw new InvalidOperationException("Delegate type is invalid.");

            // Get the return type
            var returnType = invokeMethod.ReturnType;

            // Get the parameter types
            var parameterTypes = invokeMethod.GetParameters()
                                             .Select(p => p.ParameterType)
                                             .ToArray();

            return (returnType, parameterTypes);
        }


        /// <summary>
        /// The return type of the delegate
        /// </summary>
        static Type? _returnType;
        /// <summary>
        /// The input parameters of the delegate
        /// </summary>
        static List<Type>? _cachedTypes;

        /// <summary>
        /// Gets the input types for the delegate signature that we need to map
        /// </summary>
        /// <returns></returns>
        public static Type[] GetDelegateInputs()
        {
            // Validate cache
            if(_cachedTypes == null || _returnType == null)
            {
                var types = GetDelegateSignature(typeof(Router.EndpointCallback));
                // And store
                _returnType = types.returnType;
                _cachedTypes = new List<Type>(types.parameterTypes);
            }
            return _cachedTypes!.ToArray();
        }

        /// <summary>
        /// Gets the return type for the delegate signature that we need to map
        /// </summary>
        /// <returns></returns>
        public static Type GetDelegateReturn()
        {            
            // Validate cache
            if (_cachedTypes == null || _returnType == null)
            {
                var types = GetDelegateSignature(typeof(Router.EndpointCallback));
                // And store
                _returnType = types.returnType;
                _cachedTypes = new List<Type>(types.parameterTypes);
            }
            return _returnType;

        }


        /// <summary>
        /// Generates a list of mappings that relate the parameters of the input delegate, to an array of parameters
        /// that can be provided directly to a method or function delegate.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        /// <remarks>
        /// Algorithm details are provided in <see cref="MapTypeArrays(Type[], Type[])"/>
        /// </remarks>
        public static List<Mapping> GenerateSignatureMappings(Type entryPoint, MethodInfo target)
        {
            // get the types from the input things
            var inputParams = GetDelegateSignature(entryPoint).parameterTypes;
            var outputParams = target.GetParameters().Select(x => x.ParameterType).ToArray(); 
            //and bonk
            return MapTypeArrays(inputParams, outputParams);
        }

        /// <summary>
        /// Generates a list of parameter mappings, ordered according to output parameter order (and ldArg.S stack preparation for Call opcode).
        /// 
        /// <para>
        /// This function fills aggressively from the first parameter. Design assumes no more than
        /// one untyped (object) parameter in <paramref name="inputParameters"/>, and <see cref="ArgumentException"/>
        /// is thrown if more are provided.
        /// </para>
        /// 
        /// <para>
        /// In case of unconsumed untyped output paramters, input parameters are consumed in declaration order.
        /// <list type="number">
        /// <item>Object typed input parameters</item>
        /// <item>The next unconsumed input parameter</item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="inputParameters">The parameters given by the entry point</param>
        /// <param name="outputParameters">The parameters expected by the Call target</param>
        /// <returns>An ordered list of [src, dst] mappings, where src = -1 indicates null/default, 
        /// sorted by output parameter order</returns>
        public static List<Mapping> MapTypeArrays(Type[] inputParameters, Type[] outputParameters)
        {

            // Create the mapping context
            MappingContext context = new MappingContext(inputParameters, outputParameters);

            // Solve direct assignable mappings
            SolveDirectMappings(context);

            // Solve wildcard object? assignments
            SolveWildcardMappings(context);

            // Handle unmapped outputs
            FillUnmappedOutputs(context);

            // Debug output
            foreach (var k in context.Mappings)
            {
                string str = k.Value < 0 ? "Null" : inputParameters[k.Value].ToString();
                Logger.Default.Info($"Map: {k.Key}:{k.Value},   {str}:{outputParameters[k.Key]}");
            }

            // Build the resulting mapping
            return context.ComputeSortedMapping();
        }

        /// <summary>
        /// Solves immediately assignable parameter mappings in the given context. This method maps
        /// like-for-like, but will attempt to fill input wildcards against unmatched output types.
        /// </summary>
        /// <param name="context"></param>
        private static void SolveDirectMappings(MappingContext context)
        {
            for (int i = 0; i < context.InputParameters.Length; i++)
            {
                if (context.SolvedInputs[i]) continue; //skip consumed
                var it = context.InputParameters[i]; //cache

                for (int o = 0; o < context.OutputParameters.Length; o++)
                {
                    if (context.SolvedOutputs[o] || context.SolvedInputs[i]) continue;

                    // This is solved on the first pass when
                    // 1. The output type is not object?
                    // 2. and [it] can theoretically be provided to [ot]

                    // However
                    // If [it] is an object, the assignability becomes vague
                    // So we will only try to match this to a non-object output type if that type
                    // meets certain conditions (such as not being directly assignable to any other parameters)
                    if (it == typeof(object))
                    {
                        if (SolveObjectType(i, o, context)) break;
                    }
                    // Otherwise, check assignability and fill
                    else if (context.OutputParameters[o].IsAssignableTo(it))
                    {
                        // Direct assignment possible
                        context.Map(o, i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Attempt to consume the given input index parameter into an output parameter, where the input is
        /// untyped/object, while ensuring reservation of output for any matchning typed input parameters.
        /// </summary>
        /// <param name="i">The input parameter index (must be an object)</param>
        /// <param name="o">The output parameter index attempting to be filled</param>
        /// <param name="context"></param>
        /// <returns>True if the output accepted the input and the input was consumed.</returns>
        /// <exception cref="ArgumentException"></exception>
        private static bool SolveObjectType(int i, int o, MappingContext context)
        {
            // Don't allow mismatching of non-object input[i] parameter
            if (context.InputParameters[i] != typeof(object))
                throw new ArgumentException($"The parameter at i ({i}) must be System.Object");

            if (context.OutputParameters[o] == typeof(object))
            {
                // Both are object,so direct assignment is simple
                context.Map(o, i);
                return true;
            }

            // Determine whether output parameter should be reserved for unresolved inputs
            bool isAssignable = false;
            for (int t = 0; t < context.InputParameters.Length; t++)
            {
                if (context.SolvedInputs[t] || t == i) continue;
                if (context.OutputParameters[o].IsAssignableTo(context.InputParameters[t]))
                {
                    isAssignable = true;
                    break;
                }
            }

            if (!isAssignable)
            {
                // No other index can take [ot]
                // and [it] is assumed
                context.Map(o, i);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Fill wildcard mappings by consuming input paramters in declaration order
        /// </summary>
        /// <param name="context"></param>
        private static void SolveWildcardMappings(MappingContext context)
        {
            for (int o = 0; o < context.OutputParameters.Length; o++)
            {
                // Skip indices that have been consumed, or that are not wildcard friendly
                if (context.SolvedOutputs[o] || context.OutputParameters[o] != typeof(object)) continue;

                // Now look to consume a remaining input parameter
                for (int i = 0; i < context.InputParameters.Length; i++)
                {
                    // Skip consumed
                    if (context.SolvedInputs[i]) continue;
                    // Or Fill it
                    context.Map(o, i);
                    break;
                }
            }
        }

        /// <summary>
        /// Fills unmapped output parameters with -1 indicating null/0
        /// </summary>
        /// <param name="context"></param>
        private static void FillUnmappedOutputs(MappingContext context)
        {
            for (int o = 0; o < context.OutputParameters.Length; o++)
            {
                if (!context.SolvedOutputs[o])
                {
                    // The output parameter was not mapped, so a null/default will be calculated
                    // We can find the correct nullref/0 parameter during IL gen
                    context.Mappings.Add(o, -1);
                }
            }
        }


    }
}
