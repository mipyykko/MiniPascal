namespace Common
{
    public class Production
    {
        public dynamic[] Items { get; private set; }

        public dynamic this[int index] => Items[index];

        private Production(dynamic[] items)
        {
            Items = items;
        }

        public static Production Of(params dynamic[] values)
        {
            return new Production(values);
        }

        public const char Epsilon = 'ε';
        public static readonly Production EpsilonProduction = Of(Epsilon);
    }
}