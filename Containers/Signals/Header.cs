namespace Containers.Signals
{
    public struct Header
    {

        public static int Max_Value => short.MaxValue;


        /// <summary>
        /// The header value (references the packet identifier
        /// </summary>
        private ushort id;

        /// <summary>
        /// Gets the header ID out of this struct
        /// </summary>
        public ushort ID
        {
            get => (ushort)(id & 0x7F);
            set
            {
                id = value;
            }
        }

        /// <summary>
        /// Gets the data flag out of this struct
        /// </summary>
        public bool DataFlag
        {
            get => (id & 0x80) != 0;
            set => id = (ushort)(ID | (value ? 0x80 : 0));
        }

        /// <summary>
        /// Create a new header with the given id
        /// </summary>
        /// <param name="id"></param>
        public Header(ushort id)
        {
            this.id = id;
        }

        /// <summary>
        /// Creates a new header with the given value and flag
        /// </summary>
        /// <param name="value"></param>
        /// <param name="flag"></param>
        public Header(ushort value, bool flag)
        {
            this.id = value;
            DataFlag = flag;
        }

        /// <summary>
        /// Creates a new header with the given value and flag. Expects positive header id/index.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="flag"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Header(short value, bool flag = false)
        {
            if (value < 0) throw new ArgumentOutOfRangeException($"Header ID value cannot be negative! Given: ({value})");
            value = short.Abs(value);
            this.id = (ushort)value;
            DataFlag = flag;
        }

    }
}
