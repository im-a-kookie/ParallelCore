using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Containers.Addressing
{
    internal static class GlobalAddresser
    {

        static AddressProvider<long> GlobalAddresses = new AddressProvider<long>();


        public static Address<long> Get()
        {
            return GlobalAddresses.Get();
        }




    }
}
