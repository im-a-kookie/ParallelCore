﻿using System.Runtime.InteropServices;

namespace Containers.Addressing
{

    public class AddressProvider<T> : IAddressProvider<T> where T : struct
    {
        /// <summary>
        /// Internal counter representing the total number of instances allocated in this lifetime
        /// </summary>
        private int _lifetimeAllocation = 0;

        public Address<T> FromValue(T value)
        {
            return new(Address<T>.HashToBits(value));
        }

        public Address<T> Get()
        {
            //increment the counter
            uint counter = (uint)Interlocked.Increment(ref _lifetimeAllocation);

            //get a correctly lengthed value
            byte[] data = new byte[Marshal.SizeOf<T>()];
            if (!BitConverter.TryWriteBytes(data, counter))
            {
                BitConverter.TryWriteBytes(data, (ushort)counter);
            }

            //and return the hashed type
            return new(Address<T>.HashToBits(Address<T>.FromByteArray(data)));
        }

        public Address<T> Get(out uint index)
        {
            //increment the counter
            uint counter = (uint)Interlocked.Increment(ref _lifetimeAllocation);

            //get a correctly lengthed value
            byte[] data = new byte[Marshal.SizeOf<T>()];
            if (!BitConverter.TryWriteBytes(data, counter))
            {
                BitConverter.TryWriteBytes(data, (ushort)counter);
            }

            index = counter;

            //and return the hashed type
            return new(Address<T>.HashToBits(Address<T>.FromByteArray(data)));
        }

        public int GetTotalAllocated()
        {
            return _lifetimeAllocation;
        }
    }


}
