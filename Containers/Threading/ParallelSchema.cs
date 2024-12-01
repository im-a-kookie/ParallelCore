using Containers.Addressing;
using Containers.Models;

namespace Containers.Threading
{
    public abstract class ParallelSchema : Addressable, IDisposable
    {

        public CancellationTokenSource CancellationSource;

        public CancellationToken CancellationToken;

        public ParallelSchema() : base()
        {
            CancellationSource = new();
        }

        public void Dispose()
        {
            CancellationSource.Dispose();
        }


        Lock _lock = new();

        /// <summary>
        /// Gets a boolean value indicating whether this schema has been started
        /// </summary>
        public bool Started { get; private set; }


        /// <summary>
        /// Gets a boolean value indicating whether this schema should continue running
        /// </summary>
        public bool ShouldRun { get; private set; }

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
                    throw new InvalidOperationException($"Parallel schema 0x{Address} has already been started.");
                Logger.Default.Debug($"Provider 0x{Address} started!");

                // Indicate starting information
                ShouldRun = true;
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
