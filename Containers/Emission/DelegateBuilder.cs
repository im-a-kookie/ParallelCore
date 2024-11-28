using Containers.Models;
using Containers.Signals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using static Containers.Emission.ParameterHelper;

namespace Containers.Emission
{ 


    /// <summary>
    /// Uses ILGenerator to construct delegates that efficiently map method signatures to callbacks
    /// without the need for complicated runtime DynamicInvoke logic.
    /// </summary>
    public class DelegateBuilder
    {

        public class BuilderContext<T>
        {
            /// <summary>
            /// Whether the target method is static
            /// </summary>
            public bool IsStatic { get; private set; }

            /// <summary>
            /// The calling object, or null (expects null if IsStatic)
            /// </summary>
            public Model? Caller {  get; private set; }

            /// <summary>
            /// The return type of the entry delegate
            /// </summary>
            public Type EntryReturn { get; private set; }

            /// <summary>
            /// The return type of the target method
            /// </summary>
            public Type TargetReturn { get; private set; }

            /// <summary>
            /// The parameter types of the entry delegate
            /// </summary>
            public Type[] EntryParams { get; private set; }

            /// <summary>
            /// The parameter types of the target method
            /// </summary>
            public Type[] TargetParams { get; private set; }
            
            /// <summary>
            /// The parameter mappings between entry and target parameters
            /// </summary>
            public List<Mapping> Mappings { get; private set; }

            /// <summary>
            /// The target being invoked
            /// </summary>
            public MethodInfo Target;


            /// <summary>
            /// Creates the builder context from the given arguments
            /// </summary>
            /// <param name="Caller"></param>
            /// <param name="target"></param>
            public BuilderContext(Model? Caller, MethodInfo target)
            {

                if(!typeof(T).IsSubclassOf(typeof(Delegate)))
                {
                    throw new ArgumentException("The provided type must be a Delegate!");
                }

                this.Caller = Caller;
                this.IsStatic = target.IsStatic;
                this.Target = target;

                // Extract the information that we need
                var entrySignature = ParameterHelper.GetDelegateSignature(typeof(T));

                EntryParams = entrySignature.parameterTypes;
                EntryReturn = entrySignature.returnType;

                // and the target information
                TargetParams = target.GetParameters().Select(x => x.ParameterType).ToArray()!;
                TargetReturn = target.ReturnType;

                Mappings = ParameterHelper.MapTypeArrays(EntryParams, TargetParams);

                //validate the target parameters
                ValidateStaticTargetParams();

                //and adjust them for virtual signature changes
                AdjustForVirtualSignature();
            }

            /// <summary>
            /// Gets a qualified target name for error/debugging reasons
            /// </summary>
            /// <returns></returns>
            public string GetQualifiedTargetName()
            {
                StringBuilder sb = new StringBuilder(100);
                sb.Append($"{Target.DeclaringType?.Name ?? "<?>"}.{Target.Name}");
                sb.Append("  ");
                sb.Append($"[{TargetReturn.Name}] ({String.Join(", ", TargetParams.Select(x => x.Name))})");
                return sb.ToString();
            }

            /// <summary>
            ///  Validates static target signatures for total provided information.
            /// </summary>
            private void ValidateStaticTargetParams()
            {
                if (!IsStatic) return;

                // The model and signal can both refer to the model, so either is adequate
                int indexModel = Array.IndexOf(EntryParams, typeof(Model));
                int indexSignal = Array.IndexOf(EntryParams, typeof(Signal));

                // The target may not type them, so we should just see if they were mapped
                // (remember as per implementation, the data fills the first object parameter)
                bool mapsEither = false;
                foreach (var map in Mappings)
                {
                    if (map.src == indexModel || map.src == indexSignal)
                    {
                        mapsEither = true;
                        break;
                    }
                }

                // We did not find a mapping for either important parameter
                if (!mapsEither)
                {
                    // Rather than crashing, we should just give a warning, since this may be intended behaviour
                    Logger.Default.Warn($"Target method provides limited model information. At: {GetQualifiedTargetName()}");
                }
            }

            /// <summary>
            /// Adjusts the parameter arrays for the instance-bound method signature, which injects the instance
            /// reference into the first index of the parameters.
            /// </summary>
            private void AdjustForVirtualSignature()
            {
                if (IsStatic) return;

                var realParams = new Type[EntryParams.Length + 1];
                Array.Copy(EntryParams, 0, realParams, 1, EntryParams.Length);
                realParams[0] = Caller?.GetType() ?? typeof(object);
                EntryParams = realParams;

                // and bump all source mappings forwards
                for (int i = 0; i < Mappings.Count; i++)
                {
                    Mappings[i] = new Mapping(Mappings[i].src + 1, Mappings[i].dst);
                }
            }

            /// <summary>
            /// Creates a stub dynamic method with parameters and returns set according to the
            /// provided caller and target method.
            /// </summary>
            /// <returns></returns>
            public DynamicMethod CreateDynamicMethod()
            {
                return new DynamicMethod(
                name: $"Callback_{Target.Name}",
                returnType: EntryReturn,
                parameterTypes: EntryParams,
                m: Caller?.GetType().Module ?? typeof(Model).Module,
                skipVisibility: true
                );
            }
        }

        /// <summary>
        /// Performs initial input parameter validation
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="target"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        private static void ValidateInputs(Model? caller, MethodInfo target)
        {
            if (target == null)
                throw new ArgumentNullException("Target method must be defined!");

            if (!target.IsStatic && caller == null)
                throw new InvalidOperationException("A host must be provided for non-static methods");

            else if (target.IsStatic && caller != null)
            {
                Logger.Default.Warn($"Model instance {caller.GetType().Name} provided with static method {target.DeclaringType?.Name ?? "<Unknown>"}.{target.Name}");
                Logger.Default.Warn($"As the method is static, the instance will not be used.");
            }
        }

 

        /// <summary>
        /// Generates an EndpointCallback delegate which will invoke the target method
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static Router.EndpointCallback CreateCallbackDelegate(Model? caller, MethodInfo target)
        {

            ValidateInputs(caller, target);

            var context = new BuilderContext<Router.EndpointCallback>(caller, target);
            var dynamicMethod = context.CreateDynamicMethod();

            //now get the il generator
            var il = dynamicMethod.GetILGenerator();

            // If it's a non-static method, load the caller instance as the first argument
            // As we are building the delegate as a child of the caller in this instance
            // (host is now not null)
            if (!target.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, caller!.GetType());

            }

            foreach (var map in context.Mappings)
            {
                // If the parameter is unmapped, then we give it null or 0
                if(map.src < 0)
                {
                    CreateNullOpcode(il, context.TargetParams[map.dst]);
                }
                else
                {
                    // It's mapped, so load it with appropriate casting
                    LoadParameter(il, context.EntryParams[map.src], context.TargetParams[map.dst], map.src);
                }                
            }

            il.EmitCall(target.IsStatic ? OpCodes.Call : OpCodes.Callvirt, target, null);


            if(context.TargetReturn == typeof(void))
            {
                il.Emit(OpCodes.Ldnull);
            }
            else
            {
                il.Emit(OpCodes.Castclass, typeof(object));
            }

            il.Emit(OpCodes.Ret);
            
            //create and return the delegate
            return (Router.EndpointCallback)dynamicMethod.CreateDelegate(typeof(Router.EndpointCallback), caller);

        }

        /// <summary>
        /// Appropriately loads and casts the parameter that was given
        /// </summary>
        /// <param name="il"></param>
        /// <param name="targetTypes"></param>
        /// <param name="srcIndex"></param>
        private static void LoadParameter(ILGenerator il, Type entryType, Type targetType, int srcIndex)
        {

            il.Emit(OpCodes.Ldarg_S, srcIndex);

            // Handle ref types like In and Out
            if (targetType.IsByRef)
            {
                il.Emit(OpCodes.Ldind_Ref); // Dereference the argument (for ref/out parameters)
            }
            else if (targetType.IsValueType) // Handle structs or value types
            {
                HandleValueType(il, targetType, srcIndex);
            }
            else
            {
                // Only bother casting if the target isn't going to easily absorb the input
                // In theory, the only polymorphic type is the Model, and we can validate that the
                // correct model is provided elsewhere
                if (targetType != entryType && targetType != typeof(object))
                {
                    il.Emit(OpCodes.Castclass, targetType);
                }
            }
        }

        /// <summary>
        /// Handle the emission of value type parameters
        /// </summary>
        /// <param name="il"></param>
        /// <param name="paramType"></param>
        /// <param name="srcIndex"></param>
        private static void HandleValueType(ILGenerator il, Type paramType, int srcIndex)
        {
            var underlyingType = Nullable.GetUnderlyingType(paramType);
            if (underlyingType != null)
            {
                HandleNullableType(il, underlyingType, srcIndex);
            }
            else
            {
                il.Emit(OpCodes.Box, paramType); // For normal value types, box them
            }
        }

        /// <summary>
        /// Handles the emission of nullable types. Should only be called for non-null underlying type of a Nullable{}
        /// </summary>
        /// <param name="il"></param>
        /// <param name="underlyingType"></param>
        /// <param name="srcIndex"></param>
        /// <remarks>
        /// Essentially, a nullable value type like int?, when set to null, boxes a 0 value rather than a null value.
        /// This is convenient in C#, but in IL it leads to boxing issues, especially with runtime IL generation.
        /// 
        /// <para>To solve this, we need to use a little bit of a convoluted process to retrieve the correct
        /// zero value for boxing, or otherwise ensure that we don't try to box a null into the value type.</para>
        /// </remarks>
        private static void HandleNullableType(ILGenerator il, Type underlyingType, int srcIndex)
        {
            var labelNotNull = il.DefineLabel();
            var labelDone = il.DefineLabel();

            // Check if we retrieved "null" from the data entry
            il.Emit(OpCodes.Brtrue_S, labelNotNull);

            // Load 0 for the nullable type (null case)
            LoadDefaultValueForValueType(il, underlyingType);

            il.Emit(OpCodes.Br_S, labelDone);

            // Non-null value
            il.MarkLabel(labelNotNull);
            il.Emit(OpCodes.Ldarg_S, srcIndex);

            il.MarkLabel(labelDone);
            il.Emit(OpCodes.Unbox_Any, underlyingType); // Unbox the nullable type
        }

        /// <summary>
        /// Gets the default value for a value type primitive, such that the correct boxing operation can be performend
        /// in <see cref="HandleNullableType(ILGenerator, Type, int)"/>
        /// </summary>
        /// <param name="il"></param>
        /// <param name="type"></param>
        private static void LoadDefaultValueForValueType(ILGenerator il, Type type)
        {
            OpCode code = OpCodes.Ldc_I4_0; // Default for most types (int, bool, etc.)
            if (type == typeof(float))
                code = OpCodes.Ldc_R4; // Default value for float
            else if (type == typeof(double))
                code = OpCodes.Ldc_R8; // Default value for double

            il.Emit(code); // Emit the default value
        }


        public static void CreateNullOpcode(ILGenerator il, Type targetType)
        {
            if (targetType.IsValueType)
            {
                //it's a raw value primitive
                OpCode code = OpCodes.Ldc_I4_0; // Default for most types (int, bool, etc.)
                if (targetType == typeof(float))
                    code = OpCodes.Ldc_R4; // Default value for float
                else if (targetType == typeof(double))
                    code = OpCodes.Ldc_R8; // Default value for double

                il.Emit(code);

                var underlyingType = Nullable.GetUnderlyingType(targetType);
                if (underlyingType != null)
                {
                    il.Emit(OpCodes.Unbox_Any, underlyingType); // Unbox the nullable type
                }

            }
            else
            {
                il.Emit(OpCodes.Ldnull);
            }
        }


    }
}
