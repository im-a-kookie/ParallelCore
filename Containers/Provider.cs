using Containers.Models;
using Containers.Signals;
using Containers.Threading;
using Containers.Threading.Pool;

namespace Containers
{
    public class Provider
    {

        /// <summary>
        /// The schema for parallelization within this provider
        /// </summary>
        ParallelSchema _parallelProvider;
        /// <summary>
        /// Gets the Parallel Scheme provider for this provider
        /// </summary>
        public ParallelSchema ParallelProvider => _parallelProvider;

        ModelRegistry _modelRegistry;
        /// <summary>
        /// Gets the model registry for this provider
        /// </summary>
        public ModelRegistry ModelRegistry => _modelRegistry;

        /// <summary>
        /// Creates a new provider given the optional parallel schema (defaults to <see cref="ParallelPoolSchema"/> if
        /// not provided.
        /// </summary>
        /// <param name="parallelSchema"></param>
        public Provider(ParallelSchema? parallelSchema = null)
        {
            _parallelProvider = parallelSchema ?? new ParallelPoolSchema();
            //_parallelProvider.Provider = this;
            _modelRegistry = new ModelRegistry();
        }

        /// <summary>
        /// Provides a signal queue to the model
        /// </summary>
        /// <returns></returns>
        internal ISignalQueue ProvideSignalQueue()
        {
            return new SignalQueue();
        }

        /// <summary>
        /// Starts running this provider
        /// </summary>
        public void StartProvider()
        {
            // Start the parallel schema
            _parallelProvider._Start(this);
        }


        public void StartModel(Model model)
        {
            // Configure the internals of the model
            model.MessageQueue = ProvideSignalQueue();
            model.SignalRegistry = _modelRegistry.GetRouterForModel(model);
            _parallelProvider.RunModel(model);
            // now we have to go to the parallel provider, make a container
            // put this into the container
            // and put the container to the thread
            // uuuugh

        }



    }
}
