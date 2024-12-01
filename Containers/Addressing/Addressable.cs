using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Containers.Addressing
{
    public class Addressable
    {
        public Address<long> Address;

        /// <summary>
        /// Creates the new addressable
        /// </summary>
        public Addressable()
        {
            Address = GlobalAddresser.Get();
        }

    }
}
