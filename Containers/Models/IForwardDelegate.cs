using Containers.Signals;

namespace Containers.Models
{
    public interface IForwardDelegate
    {

        /// <summary>
        /// Gets the model that will invoke this delegate
        /// </summary>
        /// <returns></returns>
        public Model GetCaller();

        /// <summary>
        /// Gets the endpoint for this forward delegate
        /// </summary>
        /// <returns></returns>
        public Header GetEndpoint();

        /// <summary>
        /// Invokes the delegate on the bound model
        /// </summary>
        public void Invoke() => Invoke(null);

        /// <summary>
        /// Invokes the delegate on the bound model and providing the given data object
        /// </summary>
        public void Invoke(object? obj = null, TaskCompletionSource<object?>? notifier = null, TimeSpan? timeout = null)
        {
            GetCaller().ReceiveMessage(GetEndpoint(), obj, timeout, notifier);
        }

        /// <summary>
        /// Invokes the delegate on the bound model and providing the given data object
        /// </summary>
        public void Invoke<T>(T obj) => Invoke((object?)obj);

        /// <summary>
        /// Invokes this call and returns an async task that can be awaited, producing a result
        /// from the return of the endpoint. 
        /// </summary>
        /// <typeparam name="T">The expected type</typeparam>
        /// <param name="obj">The data object.</param>
        /// <returns>The return value, or null if the endpoint returns null, or a value of an incompatible type.</returns>
        public Task<T?> InvokeAsync<T>(object? obj) 
        {
            return Task.Run<T?>(async () =>
            {
                var t = InvokeAsync(obj);
                await t; //await the result
                if(t != null && t is T tt)
                {
                    return tt;
                }
                // bonk
                return default;
            });
        }

        /// <summary>
        /// Invokes this call and returns an async task that can be awaited, producing a result
        /// from the return of the endpoint.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>The return value, or null if the endpoint returns null</returns>
        public Task<object?> InvokeAsync(object? obj)
        {
            TaskCompletionSource<object?> tcs = new TaskCompletionSource<object?>();
            Invoke(obj, tcs, null);
            return tcs.Task;
        }

    }
}
