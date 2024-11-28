using Containers.Models;
using Containers.Signals;
using Containers.Threading;
using Containers.Threading.Pool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Containers
{
    public class Provider
    {


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

        public Provider(ParallelSchema? parallelSchema = null)
        {
            _parallelProvider = parallelSchema ?? new ParallelPoolSchema();
            //_parallelProvider.Provider = this;
            _modelRegistry = new ModelRegistry();
        }

        public ISignalQueue ProvideSignalQueue()
        {
            return new SignalQueue();
        }


        public void StartModel(Model model)
        {
            // Configure the internals of the model
            model.MessageQueue = ProvideSignalQueue();
            model.SignalRegistry = _modelRegistry.GetRouterForModel(model);

            // TODO: give it to the parallel schema provider

        }



    }
}
