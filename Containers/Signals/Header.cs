using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Containers.Signals
{
    public struct Header
    {
        /// <summary>
        /// The header value (references the packet identifier
        /// </summary>
        public short id;

        /// <summary>
        /// Indicates whether the parent contains data
        /// </summary>
        public bool dataflag;

        /// <summary>
        /// Create a new header with the given id
        /// </summary>
        /// <param name="id"></param>
        public Header(short id)
        {
            this.id = id;
            dataflag = id < 0;
        }

    }
}
