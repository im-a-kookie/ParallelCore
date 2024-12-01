namespace Tests.Emission.DelegateConstruction
{
    public class SampleDataClass
    {
        public int value = 0;
        public SampleDataClass(int value = 0) { this.value = value; }
        // Explicit conversion operator to int
        public static explicit operator int(SampleDataClass myClass)
        {
            return myClass.value;
        }
    }
}
