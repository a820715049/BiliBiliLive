namespace BitConverter
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit)]
    internal struct SingleConverter
    {
        [FieldOffset(0)]
        private int intValue;

        [FieldOffset(0)]
        private float floatValue;

        internal SingleConverter(int intValue)
        {
            this.floatValue = 0;
            this.intValue = intValue;
        }

        internal SingleConverter(float floatValue)
        {
            this.intValue = 0;
            this.floatValue = floatValue;
        }

        internal int GetIntValue() => intValue;

        internal float GetFloatValue() => floatValue;
    }
}