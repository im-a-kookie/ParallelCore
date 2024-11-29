namespace Containers.Models
{
    public interface IForwardDelegate
    {
        /// <summary>
        /// Invokes the delegate on the bound model
        /// </summary>
        public void Invoke() => Invoke(null);

        /// <summary>
        /// Invokes the delegate on the bound model and providing the given data object
        /// </summary>
        public void Invoke(object? obj);

        /// <summary>
        /// Invokes the delegate on the bound model and providing the given data object
        /// </summary>
        public void Invoke<T>(T obj) => Invoke((object?)obj);

    }
}
