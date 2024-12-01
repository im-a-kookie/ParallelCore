using Containers.Addressing;
using Containers.Models;
using Containers.Models.Abstractions;
using Containers.Signals;

namespace Containers.Threading
{
    public abstract class ParallelSchema : Addressable, IDisposable
    {

        /// <summary>
        /// A lock to help this schema do its thing
        /// </summary>
        private Lock _lock = new();

        /// <summary>
        /// The cancellation source for this schema, allowing a cancellation signal through the stack.
        /// </summary>
        public CancellationTokenSource CancellationSource;

        /// <summary>
        /// The cancellation token for <see cref="CancellationSource"/>. Monitor to receive cancellation signal.
        /// </summary>
        public CancellationToken CancellationToken;

        /// <summary>
        /// Gets the model registry for this provider
        /// </summary>
        public ModelRegistry ModelRegistry { get; private set; }


        /// <summary>
        /// Gets a boolean value indicating whether this schema has been started
        /// </summary>
        public bool Started { get; private set; }


        /// <summary>
        /// Gets a boolean value indicating whether this schema should continue running
        /// </summary>
        public bool ShouldRun { get; private set; }



        /// <summary>
        /// Creates a new parallel schema
        /// </summary>
        public ParallelSchema() : base()
        {
            CancellationSource = new();
            ModelRegistry = new ModelRegistry();
            StartProvider();
        }

        /// <summary>
        /// Cleanup some stuffffff
        /// </summary>
        public void Dispose()
        {
            CancellationSource.Cancel();
            CancellationSource.Dispose();
        }

        /// <summary>
        /// Provides the signal queue instance to the models
        /// </summary>
        /// <returns></returns>
        public virtual ISignalQueue ProvideSignalQueue()
        {
            return new SignalQueue();
        }

        /// <summary>
        /// Starts running a model on this schema
        /// </summary>
        /// <param name="model"></param>
        public void StartModel(Model model)
        {
            // Configure the internals of the model
            model.MessageQueue = ProvideSignalQueue();
            model.SignalRegistry = ModelRegistry.GetRouterForModel(model);
            ProvideModelToThreads(model);
        }

        /// <summary>
        /// Internal call that starts the provider
        /// </summary>
        /// <param name="provider"></param>
        /// <exception cref="InvalidOperationException"></exception>
        internal virtual void StartProvider()
        {
            lock (_lock)
            {
                if (Started)
                    throw new InvalidOperationException($"Parallel schema 0x{Address} has already been started.");
                Logger.Default.Debug($"Provider 0x{Address} started!");

                // Indicate starting information
                ShouldRun = true;
                Started = true;

                NotifySchemaStarted();
            }
        }

        /// <summary>
        /// Called when this schema is started, and should be used to start running the threads
        /// </summary>
        internal abstract void NotifySchemaStarted();

        /// <summary>
        /// Provides this model to the threads within this schema
        /// </summary>
        /// <param name="model"></param>
        internal abstract void ProvideModelToThreads(Model model);


    }
}
