using Containers.Models;

namespace Containers.Threading
{
    public abstract class ParallelSchema
    {



        Lock _lock = new();

        /// <summary>
        /// Gets a boolean value indicating whether this schema has been started
        /// </summary>
        public bool Started { get; private set; }

        /// <summary>
        /// The provider that is operating with this schema
        /// </summary>
        public Provider? Provider { get; private set; }

        /// <summary>
        /// Internal call that starts the provider
        /// </summary>
        /// <param name="provider"></param>
        /// <exception cref="InvalidOperationException"></exception>
        internal virtual void _Start(Provider provider)
        {
            lock (_lock)
            {
                if (Started)
                    throw new InvalidOperationException($"This parallel scheme has already been started.");
                Started = true;
                Provider = provider;
                OnStart();
            }
        }

        /// <summary>
        /// Called after the parallel schema has been started, and should provide core behaviour to initialize
        /// threads and pools and so on.
        /// </summary>
        public abstract void OnStart();

        /// <summary>
        /// Starts running the given model on this parallel scheme
        /// </summary>
        /// <param name="model"></param>
        public abstract void RunModel(Model model);


    }
}
