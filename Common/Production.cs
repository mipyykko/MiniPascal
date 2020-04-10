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

        public static Production Of(params dynamic[] values) => new Production(values);
        public const char Epsilon =  'ε';
        public static Production EpsilonProduction = Production.Of(Epsilon);
    }
}