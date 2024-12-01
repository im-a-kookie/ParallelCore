using Containers.Addressing;
using Containers.Models.Attributes;
using Containers.Signals;
using Containers.Threading;
using System.Reflection;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace Containers.Models
{
    [ModelDefinition("Default_Model")]
    public class Model : Addressable
    {
        /// <summary>
        /// The ID for this model
        /// </summary>
        public Address<long> ID { get; private set; } = Address<long>.Zero;
        
        /// <summary>
        /// internal activity flag
        /// </summary>
        private bool _active = false;
        /// <summary>
        /// Whether this model is currently active (aka looping)
        /// </summary>
        public bool IsActive => _active;
        /// <summary>
        /// Whether this model has been correctly constructed
        /// </summary>
        public bool Constructed { get; private set; } = false;

        /// <summary>
        /// The provide that hosts this model
        /// </summary>
        public Provider? Parent;

        /// <summary>
        /// The router instance that handles signals for this model class
        /// </summary>
        public Router? SignalRegistry { get; internal set; } = null;

        /// <summary>
        /// The MessageQueue for this router
        /// </summary>
        public ISignalQueue? MessageQueue { get; internal set; } = null;

        /// <summary>
        /// The expected iteration rate of this model. If this is set to a positive value,
        /// then the model will be updated this many times per second. If set to -1, then the
        /// model will only update when receiving messages.
        /// </summary>
        public double ExpectedIterationsPerSecond = -1;

        /// <summary>
        /// Whether the model notifies the message queue when a message is received. This field is ignored if
        /// <see cref="ExpectedIterationsPerSecond"/> is <= 0, otherwise, the regular iteration is used.
        /// </summary>
        public bool NotifiesOnMessage = true;

        /// <summary>
        /// Event triggered when the thread enters this model.
        /// </summary>
        public event Action? ThreadEnter;

        /// <summary>
        /// Event triggered when a signal is loaded from the queue for processing. Returning
        /// a non-null value from this step, will interrupt the normal processing of the signal.
        /// </summary>
        public event Func<Signal, object?>? ProcessSignal;

        /// <summary>
        /// The container that holds this model
        /// </summary>
        public Container? Container { get; private set;}

        /// <summary>
        /// Creates a new model with the given provider
        /// </summary>
        /// <param name="provider"></param>
        public Model(Provider? provider) : base()
        {
            Parent = provider;
            
            // Set the registry
            if(provider != null)
            {
                // Get ourselves an ID from the global lookup
                if(ModelRegistry.LoadModel(this))
                {   
                    // Get our signal router
                    SignalRegistry = Parent!.ModelRegistry.GetRouterForModel(this);
                    // And get a new message queue
                    MessageQueue = Parent.ProvideSignalQueue();
                    // We are now fully constructed
                    Constructed = true;
                }
            }
            // If constructed was not set to "true" above,
            // Then this model did not construct correctly,
            // and should die
        }

        internal void NotifyContainerReceivedModel(Container container)
        {
            Console.WriteLine($"Model 0x{Address} received by container 0x{container.Address}");
            // We can mark that we are now active
            _active = true;
            this.Container = container;
        }


        public void OnModelEnter(CancellationToken cancellation)
        {
            Console.WriteLine($"Model 0x{Address} Entered!");

            while (MessageQueue?.TryGet(out Signal? signal) ?? false)
            {
                Console.WriteLine($"Model 0x{Address} Received Message...!");

                // Invoke the signal processing event
                var result = ProcessSignal?.Invoke(signal!);
                if (result == null)
                {
                    // Process the signal and call the call thing
                    // Get the message callback from the signal header
                    var wrapper = SignalRegistry!.GetCallback(signal!.Flag);
                    // Yay process it
                    result = wrapper?.Invoke(signal);
                }
                // Now set the signal result
                if (signal!.CompletionSource != null)
                {
                    signal.CompletionSource.SetResult(result);
                }
            }


            //now we have a chance to loop
            ThreadEnter?.Invoke();
        }

        /// <summary>
        /// Called to receive a message into this model
        /// </summary>
        /// <param name="command"></param>
        /// <param name="data"></param>
        /// <param name="lifespan"></param>
        /// <param name="taskCompletionSource"></param>
        /// <returns></returns>
        public bool ReceiveMessage(string command, object? data = null, TimeSpan? lifespan = null, TaskCompletionSource<object?>? taskCompletionSource = null)
        {
            var header = SignalRegistry?.SignalDictionary.GetHeader(command);
            if(header != null)
            {
                return ReceiveMessage(header.Value, data, lifespan, taskCompletionSource);
            }
            return false;
        }

        /// <summary>
        /// Called to receive a message into this model
        /// </summary>
        /// <param name="command"></param>
        /// <param name="data"></param>
        /// <param name="lifespan"></param>
        /// <param name="taskCompletionSource"></param>
        /// <returns></returns>
        public bool ReceiveMessage(Header command, object? data = null, TimeSpan? lifespan = null, TaskCompletionSource<object?>? taskCompletionSource = null)
        {
            Signal s = new Signal()
            {
                Flag = command,
                Data = new Packet(data),
                Receiver = this,
                Expiration = lifespan == null ? DateTime.MaxValue : (DateTime.UtcNow + lifespan.Value),
                CompletionSource = taskCompletionSource
            };


            if(MessageQueue?.Queue(s) ?? false)
            {
                Container?.Notify();
                return true;
            }
            return false;
        }

        public delegate object? SignalDelegate(object? data = null);

        public delegate object? SignalDelegate<T>(T? data = default);


        /// <summary>
        /// Gets a callback that can invoke the given signal
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public IForwardDelegate? GetSignalCaller(string command)
        {
            var header = SignalRegistry?.SignalDictionary.GetHeader(command);
            if (header == null) return null;
            return new ForwardDelegate(this, header.Value);
        }


        public SignalDelegate? GetDelegate(string command)
        {
            var header = SignalRegistry?.SignalDictionary.GetHeader(command);
            if (header == null) return null;
            return (x) => ReceiveMessage(header.Value, x);
        }


        public SignalDelegate<T>? GetDelegate<T>(string command)
        {
            var header = SignalRegistry?.SignalDictionary.GetHeader(command);
            if (header == null) return null;
            return (x) => ReceiveMessage(header.Value, x);
        }


        [Endpoint]
        public void Exit(string? thing)
        {
            Logger.Default.Info("Received Exit Command! Extra: " + (thing ?? "<null>"));
        }


    }
}
