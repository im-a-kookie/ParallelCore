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
