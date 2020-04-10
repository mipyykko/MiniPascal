namespace Common
{
    public class Parameters
    {
        public bool[] Items { get; private set; }
        public dynamic this[int index] => Items[index];

        private Parameters(bool[] items) => Items = items;
        public static Parameters Of(params bool[] values) => new Parameters(values);
        public static Parameters NoParameters => Parameters.Of();

    }
}