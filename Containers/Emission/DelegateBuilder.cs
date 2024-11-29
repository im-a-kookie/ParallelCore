using Containers.Models;
using Containers.Signals;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using static Containers.Emission.ParameterHelper;

namespace Containers.Emission
{
    /// <summary>
    /// Uses ILGenerator to construct delegates that efficiently map method signatures to callbacks
    /// without the need for complicated runtime DynamicInvoke logic.
    /// </summary>
    public class DelegateBuilder
    {

        public static void Callout(int x, object? t)
        {
            Console.WriteLine(x + ": " + t?.GetType().AssemblyQualifiedName ?? "<null>");
            return;
        }

        public class BuilderContext<T>
        {
            /// <summary>
            /// Whether the target method is static
            /// </summary>
            public bool IsStatic { get; private set; }

            /// <summary>
            /// The calling object, or null (expects null if IsStatic)
            /// </summary>
            public Model? Caller { get; private set; }

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
            /// <param name="caller"></param>
            /// <param name="target"></param>
            public BuilderContext(Model? caller, MethodInfo target)
            {

                if (!typeof(T).IsSubclassOf(typeof(Delegate)))
                {
                    throw new ArgumentException("The provided type must be a Delegate!");
                }

                this.Caller = caller;
                this.IsStatic = target.IsStatic;
                this.Target = target;

                if (IsStatic) this.Caller = null;

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
                // virtual methods already provide the model inherently as (this),
                // so the validation here is only relevant to static methods
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
                // we only need to do this for instance calls
                if (IsStatic) return;

                // Instance calls provide "this" invisibly as the first parameter to all methods
                // Which must now be accounted for by shifting all of the parameters down one space
                var realParams = new Type[EntryParams.Length + 1];
                Array.Copy(EntryParams, 0, realParams, 1, EntryParams.Length);
                // now we can just set it to the caller or otherwise an object, it's always a ref type so it's not a huge deal
                realParams[0] = Caller?.GetType() ?? typeof(object);
                // And switch it into the EntryParams
                EntryParams = realParams;

                // Next, bump the mapping sources forwards where they aren't -1 (null)
                for (int i = 0; i < Mappings.Count; i++)
                {
                    if (Mappings[i].src >= 0)
                    {
                        Mappings[i] = new Mapping(Mappings[i].src + 1, Mappings[i].dst);
                    }
                }
            }

            /// <summary>
            /// Creates a stub dynamic method with parameters and returns set according to the
            /// provided caller and target method.
            /// </summary>
            /// <returns>An empty <see cref="DynamicMethod"/> with parameters configured according to this context.</returns>
            /// <exception cref="InvalidOperationException"></exception>
            public DynamicMethod CreateDynamicMethod()
            {
                try
                {
                    return new DynamicMethod(
                        name: $"Callback_{Target.Name}",
                        returnType: EntryReturn,
                        parameterTypes: EntryParams,
                        m: Caller?.GetType().Module ?? typeof(Model).Module,
                        skipVisibility: true
                        );
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"An error {e} was thrown during dynamic method creation!", e);
                }

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

            // static method with an instance given, not a fatal error but not great either
            else if (target.IsStatic && caller != null)
            {
                // Spit out a warning
                // Since this *may* be intended behaviour, but could also lead to unintended behaviour
                Logger.Default.WriteBlock(
                    $"Static method declaration provided with instance reference",
                    $"Method:    {target.DeclaringType?.Name ?? "<Unknown>"}.{target.Name}\n" +
                    $"Instance:  {caller.GetType().Name}\n" +
                    $"The instance reference will be discarded.",
                    level: LogLevel.Warn);
            }
        }



        /// <summary>
        /// Generates an EndpointCallback delegate which will invoke the target method
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static Router.EndpointCallback CreateCallbackDelegate(Model? caller, MethodInfo target)
        {
            // first, ensure input validity
            ValidateInputs(caller, target);
            // Build the context for the creation
            var context = new BuilderContext<Router.EndpointCallback>(caller, target);
            var dynamicMethod = context.CreateDynamicMethod(); // makes it easy to get the dynamic method

            //now get the il generator
            var il = dynamicMethod.GetILGenerator();

            // Little debugging thing
            //var callout = typeof(DelegateBuilder).GetMethod("Callout", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
            //for (int i = 0; i < context.EntryParams.Length; ++i)
            //{
            //    il.Emit(OpCodes.Ldc_I4, i);
            //    il.Emit(OpCodes.Ldarg, i);
            //    il.EmitCall(OpCodes.Call, callout, null);
            //}

            // If it's a non-static method, load the caller instance as the first argument
            // As we are building the delegate as a child of the caller in this instance
            // (host is now not null)
            if (!target.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, context.Caller!.GetType());
            }

            foreach (var map in context.Mappings)
            {

                // If the parameter is unmapped, then we give it null or 0
                if (map.src < 0)
                {
                    EmitNullOrDefault(il, context.TargetParams[map.dst]);
                }
                else
                {
                    // It's mapped, so load it with appropriate casting
                    LoadParameter(il, context.EntryParams[map.src], context.TargetParams[map.dst], map.src);
                }
            }

            // whewwww now we can make the call
            il.EmitCall(context.IsStatic ? OpCodes.Call : OpCodes.Callvirt, context.Target, null);
            EmitReturnType(il, context);

            // -- We have fully generated the IL for the dynamic method --

            // So create the delegate and send it back
            return (Router.EndpointCallback)dynamicMethod.CreateDelegate(typeof(Router.EndpointCallback), context.Caller);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="il"></param>
        /// <param name="context"></param>
        private static void EmitReturnType<T>(ILGenerator il, BuilderContext<T> context)
        {
            // If we do not have a return type from the delegate
            // Then we need to clear the stack from any target calls
            if (context.EntryReturn == typeof(void))
            {
                if (context.TargetReturn != typeof(void))
                {
                    il.Emit(OpCodes.Pop); //bonk
                }
                il.Emit(OpCodes.Ret); //and return
                return;
            }

            // So we do have a return, we need to figure out what it is
            if (context.TargetReturn == typeof(void))
            {
                // Null is null
                il.Emit(OpCodes.Ldnull);
            }
            else if (context.TargetReturn.IsValueType)
            {
                // value types, we can box into an object
                il.Emit(OpCodes.Box, context.TargetReturn);
            }
            else
            {
                // ref types, we can cast into objects
                il.Emit(OpCodes.Castclass, typeof(object));
            }

            il.Emit(OpCodes.Ret);

        }

        /// <summary>
        /// Loads a parameter to the stack from the given index in the entry delegate, and matching it to the Target type
        /// based on the given EntryType.
        /// </summary>
        /// <param name="il"></param>
        /// <param name="targetTypes"></param>
        /// <param name="srcIndex"></param>
        private static void LoadParameter(ILGenerator il, Type entryType, Type targetType, int srcIndex)
        {

            il.Emit(OpCodes.Ldarg_S, srcIndex); // load

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
                // In theory, the only polymorphic types are model and and data
                // So we can just validate their typing elsewhere
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
                HandleNullableValueType(il, underlyingType, srcIndex);
            }
            else
            {
                il.Emit(OpCodes.Unbox_Any, paramType);

            }
        }


        /// <summary>
        /// Handles the emission of nullable types. Should only be called for non-null underlying type of a Nullable{}
        /// </summary>
        /// <param name="il"></param>
        /// <param name="underlyingType"></param>
        /// <param name="srcIndex"></param>
        /// <remarks>
        /// Non-primitive only require us to box whatever we get into a nullable of the type,
        /// however primitives require null to be converted into a boxed 0 value;
        /// <code>
        /// if (pop == null)
        ///     [if type is value]
        ///         push 0        //or 0.0 for fp
        ///     [else]            //[type is ref]
        ///         push nullref
        /// else: reload parameter
        /// then: unbox           //unbox the nullable
        /// </code>
        /// </remarks>
        private static void HandleNullableValueType(ILGenerator il, Type underlyingType, int srcIndex)
        {

            if (!underlyingType.IsPrimitive)
            {
                // This case is simple
                var nullableType = typeof(Nullable<>).MakeGenericType(underlyingType);
                il.Emit(OpCodes.Unbox_Any, nullableType);
            }
            else
            {
                var labelNotNull = il.DefineLabel();
                var labelDone = il.DefineLabel();

                // Check if we retrieved "null" from the data entry
                il.Emit(OpCodes.Brtrue_S, labelNotNull); //>---------┐
                /*                                                   |
                // This means the argument given was null            |
                *///so we need a zero                                |
                var code = GetZeroForPrimitive(underlyingType);//    |
                il.Emit(code);//                                     |
                // now jump to boxing                                |
                il.Emit(OpCodes.Br_S, labelDone); //>----------┐     |
                //                                             |     |
                // Non-null value                              |     |
                il.MarkLabel(labelNotNull);         // <-------)-----┘    
                il.Emit(OpCodes.Ldarg_S, srcIndex); //         |
                //                                             |
                il.MarkLabel(labelDone); //<-------------------┘
                il.Emit(OpCodes.Unbox_Any, underlyingType); // Unbox the nullable type
            }


        }

        /// <summary>
        /// Gets an opcode for a zero value for a primitive type 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static OpCode GetZeroForPrimitive(Type type)
        {

            OpCode code = OpCodes.Ldc_I4_0; // Default for most types (int, bool, etc.)
            if (type == typeof(float))
                code = OpCodes.Ldc_R4; // Default value for float
            else if (type == typeof(double))
                code = OpCodes.Ldc_R8; // Default value for double
            return code;
        }


        /// <summary>
        /// Creates a null or zero/default OpCode for the given target type.
        /// </summary>
        /// <param name="il"></param>
        /// <param name="targetType"></param>
        public static void EmitNullOrDefault(ILGenerator il, Type targetType)
        {
            //If this is a value type, then the logic becomes... annoying
            if (targetType.IsValueType)
            {
                // If it's nullable, then even more annoying
                var underlyingType = Nullable.GetUnderlyingType(targetType);

                // Primitives can simply emit a zero value and box it for nulls
                if (targetType.IsPrimitive)
                {
                    il.Emit(GetZeroForPrimitive(targetType));
                    if (underlyingType != null) il.Emit(OpCodes.Unbox_Any, underlyingType); // Emit the unboxing

                }
                else
                {
                    // In this case it's a struct, so we need to make a new copy
                    // Luckily structs have default constructors, so we can just call new() into a local
                    var local = il.DeclareLocal(targetType);
                    il.Emit(OpCodes.Ldloca, local);
                    il.Emit(OpCodes.Initobj, targetType);
                    il.Emit(OpCodes.Ldloc, local);

                    // Nullable requires the struct to be boxed into a Nullable<T>
                    if (underlyingType != null)
                    {
                        // So we should get the constructor for Nullable<T> and instantiate it yay
                        var nullableType = typeof(Nullable<>).MakeGenericType(underlyingType);
                        var nullConstructor = nullableType.GetConstructor([underlyingType])!;
                        il.Emit(OpCodes.Newobj, nullConstructor); // And make it

                    }
                }
            }
            else
            {
                // It's just a boring ref type so emit a nullref
                il.Emit(OpCodes.Ldnull);
            }
        }


    }
}
