using Containers.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Containers.Signals
{
    public class Signal
    {
        public Header Flag;

        public Model? Sender;

        public Model? Receiver;

        public Packet? Data;


    }
}
