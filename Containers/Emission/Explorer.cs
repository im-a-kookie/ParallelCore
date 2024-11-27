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
        /// Maps parameters from entry->target for delegate construction
        /// </summary>
        public struct Mapping
        {
            public readonly int src;
            public readonly int dst;
            public Mapping(int entry, int target)
            {
                this.src = entry; this.dst = target;
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
        /// Encapsulation context for mapping entry->target parameter signatures
        /// </summary>
        public class MappingContext
        {
            /// <summary>
            /// The entry parameters provided to the mapping algorithm
            /// </summary>
            public Type[] EntryParameters { get; set; }

            /// <summary>
            /// The target parameters expected to be mapped from the entry
            /// </summary>
            public Type[] TargetParameters { get; set; }

            /// <summary>
            /// Dictionary mapping <see cref="TargetParameters"/> index to <see cref="EntryParameters"/> index (or -1 for null/default).
            /// </summary>
            public Dictionary<int, int> Mappings { get; set; }

            /// <summary>
            /// A flag array indicating the consumption state of entry parameters
            /// </summary>
            public bool[] SolvedEntries { get; set; }

            /// <summary>
            /// A flag array indicating the consumption state of target parameters
            /// </summary>
            public bool[] SolvedTargets { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="MappingContext"/> class.
            /// </summary>
            /// <param name="entryParameters">The parameters for the entry call.</param>
            /// <param name="targetParameters">The parameters for the target call.</param>
            public MappingContext(Type[] entryParameters, Type[] targetParameters)
            {

                if (entryParameters == null || entryParameters.Contains(null))
                    throw new ArgumentNullException($"{nameof(entryParameters)} contains null type. This is not valid.");

                if (targetParameters == null || targetParameters.Contains(null))
                    throw new ArgumentNullException($"{nameof(targetParameters)} contains null type. This is not valid.");


                EntryParameters = entryParameters;
                TargetParameters = targetParameters;
                Mappings = new Dictionary<int, int>(); // Initialize the dictionary to hold mappings
                SolvedEntries = new bool[entryParameters.Length]; // Initialize input flags array
                SolvedTargets = new bool[targetParameters.Length]; // Initialize output flags array
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
                if (i >= EntryParameters.Length || (i < 0 && i != -1))
                    throw new ArgumentOutOfRangeException(
                        "The index of the input is not valid for the parameter array");

                if(o < 0 || o >= TargetParameters.Length)
                    throw new ArgumentOutOfRangeException(
                        "The output index must fall within the output parameter array!");

                if (SolvedTargets[o])
                    throw new InvalidOperationException(
                        $"The output parameter at index {o} is already provided");

                // Now we can fill it
                Mappings.TryAdd(o, i);
                if(i >= 0) SolvedEntries[i] = true; //already checked i>len
                SolvedTargets[o] = true;
            }

            /// <summary>
            /// Computes this context into a list of mappings, ordered by output parameter index.
            /// </summary>
            /// <returns>A sorted result of mapping structs</returns>
            public List<Mapping> ComputeSortedMapping(bool nullifyEmptyOutputs = true)
            {
                for(int o = 0; o < TargetParameters.Length; ++o)
                {
                    if (!SolvedTargets[o])
                    {
                        if (nullifyEmptyOutputs)
                        {
                            FillUnmappedOutputs(this);
                            break;
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                $"Parameter could not be located for output parameter {o} ({TargetParameters[o]}" +
                                $". Mapping cannot be generated.");
                        }
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
        /// Checks type assignability of the given types.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="target"></param>
        /// <returns>A boolean indicating the compabitility of the given types</returns>
        /// <remarks>Note that inheritance hierarchy is inverted. For example, if the entry is Model, the target must inherit Model.</remarks>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool CheckTypeCompabitility(Type entry, Type target)
        {
            if (entry == null)
                throw new ArgumentNullException("Cannot match null entry type!");

            if (target == null)
                throw new ArgumentNullException("Cannot match null target type!");


            return target.IsAssignableTo(entry) || target == typeof(object);
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
        /// Generates a list of mappings that relate the parameters of the entry delegate, to an array of parameters
        /// that can be provided directly to a method or function delegate (target)
        /// </summary>
        /// <param name="entryPoint"></param>
        /// <param name="target"></param>
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
        /// Generates a list of parameter mappings, ordered according to target parameter order (for LdArgS->Call).
        /// 
        /// <para>
        /// This function fills aggressively from the first parameter. Design assumes no more than
        /// one untyped (object) parameter in <paramref name="entryParameters"/>, and <see cref="ArgumentException"/>
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
        /// <param name="entryParameters">The parameters given by the entry point</param>
        /// <param name="targetParameters">The parameters expected by the Call target</param>
        /// <returns>An ordered list of [src, dst] mappings, where src = -1 indicates null/default, 
        /// sorted by output parameter order</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static List<Mapping> MapTypeArrays(Type[] entryParameters, Type[] targetParameters)
        {
            // Validate typing of inputs
            if (entryParameters.Where(x => x == typeof(object)).Count() > 1)
                throw new ArgumentException("Entry parameters should contain no more than one untyped argument!");

            // Create the mapping context
            MappingContext context = new(entryParameters, targetParameters);

            // Solve like-like mappings
            SolveDirectMappings(context);

            // Solve wildcard object? assignments
            SolveWildcardMappings(context);

            // Handle unmapped outputs
            FillUnmappedOutputs(context);

            // Debug output
            foreach (var k in context.Mappings)
            {
                string str = k.Value < 0 ? "Null" : entryParameters[k.Value].ToString();
                Logger.Default.Info($"Map: {k.Key}:{k.Value},   {str}:{targetParameters[k.Key]}");
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
            for (int i = 0; i < context.EntryParameters.Length; i++)
            {
                if (context.SolvedEntries[i]) continue; //skip consumed
                var it = context.EntryParameters[i]; //cache

                for (int o = 0; o < context.TargetParameters.Length; o++)
                {
                    if (context.SolvedTargets[o] || context.SolvedEntries[i]) continue;

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
                    else if (context.TargetParameters[o].IsAssignableTo(it))
                    {
                        // Direct assignment possible
                        context.Map(o, i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Attempt to consume the given input (entry) index parameter into an output (target) parameter, where the input is
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
            if (context.EntryParameters[i] != typeof(object))
                throw new ArgumentException($"The parameter at i ({i}) must be System.Object");

            if (context.TargetParameters[o] == typeof(object))
            {
                // Both are object,so direct assignment is simple
                context.Map(o, i);
                return true;
            }

            // Determine whether output parameter should be reserved for unresolved inputs
            bool isAssignable = false;
            for (int t = 0; t < context.EntryParameters.Length; t++)
            {
                if (context.SolvedEntries[t] || t == i) continue;
                if (context.TargetParameters[o].IsAssignableTo(context.EntryParameters[t]))
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
            for (int o = 0; o < context.TargetParameters.Length; o++)
            {
                // Skip indices that have been consumed, or that are not wildcard friendly
                if (context.SolvedTargets[o] || context.TargetParameters[o] != typeof(object)) continue;

                // Now look to consume a remaining input parameter
                for (int i = 0; i < context.EntryParameters.Length; i++)
                {
                    // Skip consumed
                    if (context.SolvedEntries[i]) continue;
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
            for (int o = 0; o < context.TargetParameters.Length; o++)
            {
                if (!context.SolvedTargets[o])
                {
                    // The output parameter was not mapped, so a null/default will be calculated
                    // We can find the correct nullref/0 parameter during IL gen
                    context.Map(o, -1);
                }
            }
        }


    }
}
