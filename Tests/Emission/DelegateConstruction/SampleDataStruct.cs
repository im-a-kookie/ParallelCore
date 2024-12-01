namespace Tests.Emission.DelegateConstruction
{
    /// <summary>
    /// A sample data struct that can be passed through the delegate
    /// </summary>
    public struct SampleDataStruct
    {
        public int value = 0;
        public SampleDataStruct(int value = 0) { this.value = value; }
        // Explicit conversion operator to int
        public static explicit operator int(SampleDataStruct myClass)
        {
            return myClass.value;
        }
    }
}
