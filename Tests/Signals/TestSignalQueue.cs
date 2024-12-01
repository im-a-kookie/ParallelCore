using Containers.Models;
using Containers.Models.Signals;

namespace Containers.Tests
{
    [TestClass]
    public class SignalQueueTests
    {
        [TestMethod]
        public void Queue_AddsSignalWhenCalled()
        {

            using var queue = new SignalQueue();
            var signal = new Signal();

            var result = queue.Queue(signal);
            Assert.AreEqual(1, queue.Count(), "Queue does not contain items after addition");
            Assert.IsTrue(result, "Queue addition flag returns false!");

        }

        [TestMethod]
        public void Queue_MaxSizeRespected()
        {
            // Make new queue and set size
            using var queue = new SignalQueue();
            queue.SetMaxSize(2);

            queue.Queue(new Signal()); // Add first signal
            queue.Queue(new Signal()); // Add second signal
            // Attempt to add third signal
            var result = queue.Queue(new Signal());

            // Assert
            Assert.AreEqual(2, queue.Count(), $"The queue contains {queue.Count()} where 2 is expected!");
            Assert.IsFalse(result, "Queue addition flag returns true!");

        }

        [TestMethod]
        public void TryGet_ReturnsSignalWhenAvailable()
        {
            using var queue = new SignalQueue();
            var signal = new Signal();
            queue.Queue(signal);
            var success = queue.TryGet(TimeSpan.FromMilliseconds(100), out var result);
            Assert.AreEqual(signal, result, "Queue did not provide correct instance!");
            Assert.IsTrue(success, "Queue retrieval flag is not set!");

        }

        [TestMethod]
        public void TryGet_FailsTimeoutCorrectly()
        {
            using var queue = new SignalQueue();
            var success = queue.TryGet(TimeSpan.FromMilliseconds(100), out var result);
            Assert.IsFalse(success, "Empty queue did not reject after timeout!");
            Assert.IsNull(result, "Empty queue returned an item!");
        }


        [TestMethod]
        public void Lock_QueueProvidesLock()
        {
            using var queue = new SignalQueue();
            var lockAcquired = queue.Lock(100);
            Assert.IsTrue(lockAcquired, "Failed to aquire write lock!");
            queue.Unlock();
        }

        [TestMethod]
        public void Lock_BlocksAdditionCorrectly()
        {
            using var queue = new SignalQueue();

            //lock
            queue.Lock(100);
            //a signal
            bool result = false;

            //use Thread to absolutely guarantee running in new context
            Thread thread = new Thread(() =>
            {
                result = queue.Queue(new Signal(), 0);

            });
            thread.Start();
            // Now wait for it to be completed
            if (!thread.Join(TimeSpan.FromSeconds(3)))
            {
                Assert.Fail("The queue does not respect immediate timeout!");
            }
            queue.Unlock();

            Assert.AreEqual(queue.Count(), 0, "Item was inserted while queue was in locked state!");
            Assert.IsFalse(result, "Flag indicates item was added during queue addition!");

        }


        [TestMethod]
        public void Lock_QueueRespectsTimeout()
        {
            using var queue = new SignalQueue();

            //lock
            queue.Lock(100);
            bool result = false;

            // Use Thread to absolutely guarantee running in new context
            Thread thread = new Thread(() =>
            {
                result = queue.Queue(new Signal(), -1);

            });
            thread.Start();
            // Now wait for it to be completed
            if (thread.Join(TimeSpan.FromMilliseconds(300)))
            {
                Assert.Fail("The queue does not respect indefinite timeout!");
            }
            // Unlock and donk
            queue.Unlock();
            if (!thread.Join(TimeSpan.FromMilliseconds(300)))
            {
                Assert.Fail("The thread does not unlock correctly!");
            }


            Assert.AreEqual(queue.Count(), 1, "Item was not correctly inserted!");
            Assert.IsTrue(result, "Flag does not match insertion state!");

        }

    }
}
