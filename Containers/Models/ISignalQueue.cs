﻿using Containers.Signals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Containers.Models
{
    /// <summary>
    /// Represents a queue for managing the signals provided to the Model. Implementation must be threadsafe.
    /// </summary>
    public interface ISignalQueue
    {

        /// <summary>
        /// Attempts to retrieve a signal from the queue within a specified timeout period.
        /// </summary>
        /// <param name="timeout">The maximum duration to wait for a signal.</param>
        /// <param name="result">The retrieved signal, if available, or null if the timeout is reached.</param>
        /// <returns>True if a signal was successfully retrieved; otherwise, false.</returns>
        public bool TryGet(int timeout, out Signal? result);

        /// <summary>
        /// Attempts to retrieve a signal from the queue within a specified timeout period.
        /// </summary>
        /// <param name="timeout">The maximum duration to wait for a signal.</param>
        /// <param name="result">The retrieved signal, if available, or null if the timeout is reached.</param>
        /// <returns>True if a signal was successfully retrieved; otherwise, false.</returns>
        public bool TryGet(TimeSpan timeout, out Signal? result);

        /// <summary>
        /// Attempts to retrieve a signal from the queue without specifying a timeout.
        /// </summary>
        /// <param name="result">The retrieved signal, if available, or null if the queue is empty.</param>
        /// <returns>True if a signal was successfully retrieved; otherwise, false.</returns>
        public bool TryGet(out Signal? result);

        /// <summary>
        /// Adds a signal to the queue.
        /// </summary>
        /// <param name="signal">The signal to be queued.</param>
        /// <returns>True if the signal was successfully added to the queue; otherwise, false.</returns>
        public bool Queue(Signal signal);

        /// <summary>
        /// Sets the maximum size for the queue (overflow items trigger <see cref="Queue(Signal)"/> to return false.
        /// </summary>
        /// <param name="size"></param>
        public void SetMaxSize(int size);

        /// <summary>
        /// Gets the current size of the signal queue
        /// </summary>
        /// <returns></returns>
        public int Count();

        /// <summary>
        /// Locks this queue from access by any other thread
        /// </summary>
        /// <returns></returns>
        public bool Lock(int timeoutMs);

        /// <summary>
        /// Allows access to this queue from all threads
        /// </summary>
        /// <returns></returns>
        public void Unlock();


    }
}