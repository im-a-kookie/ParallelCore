using Containers.Emission;
using Containers.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Containers.Signals
{
    public abstract class Wrapper
    {
        /// <summary>
        /// Invoke this wrapper to get a generic object
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public abstract object? Invoke(Signal signal);

        /// <summary>
        /// Invokes this wrapper to get a return of type T, or null otherwise
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="receiver"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public abstract T? Invoke<T>(Signal signal) where T : notnull;



    }

    public class Wrapper<Data, Return> : Wrapper
    {

        public int index = -1;

        public Type? DataType { get; private set; }
        public Type? ReturnType { get; private set; }

        public object? DefaultValue = null;


        public Router.EndpointCallback Callback;

        public List<string> Names = [];

        public Wrapper(Router.EndpointCallback callback)
        {
            Callback = callback;
            DataType = typeof(Data);
            ReturnType = typeof(Return);
        }

        public override object? Invoke(Signal signal)
        {
            // Build out the parameters
            var data = signal.Data?.GetData();
            var receiver = signal.Receiver;
            Console.WriteLine(data + ", " + typeof(Data));
            if (data is Data d)
            {
                return Callback(d, receiver, receiver?.Parent, receiver?.Parent?.ModelRegistry, receiver?.SignalRegistry);
            }
            else
            {
                return Callback(default(Data), receiver, receiver?.Parent, receiver?.Parent?.ModelRegistry, receiver?.SignalRegistry);
            }
        }

        public override T? Invoke<T>(Signal signal) where T : default
        {
            var result = Invoke(signal);
            if (result is T t) return t;
            return default;
        }

        public Header GetHeader()
        {
            if(index < 0)
            {
                throw new InvalidOperationException(
                    $"Error creating header. The index is not initialized. Expect: 0<index<{Header.Max_Value}, Index: {index}");

            }

            if (index > Header.Max_Value)
            {
                throw new InvalidOperationException(
                    $"Error creating header. The max value has been exceeded. Expect: 0<index<{Header.Max_Value}, Index: {index}");
            }
            return new Header((ushort)index);
        }


    }
}
