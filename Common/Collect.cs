namespace Common
{
    public class Collect
    {
        public bool[] Items { get; private set; }
        public dynamic this[int index] => Items[index];

        private Collect(bool[] items) => Items = items;
        public static Collect Of(params bool[] values) => new Collect(values);
        public static Collect None => Of(false);

    }
}