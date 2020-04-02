using System;
using System.Collections.Generic;

namespace Common
{
    public static class Util
    {
        public static TU TryGetValueOrDefault<T, TU>(this Dictionary<T, TU> dictionary, T key, TU defaultValue = default)
        {
            if (dictionary == null) throw new ArgumentNullException();
            if (key == null) throw new ArgumentNullException();

            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }
    }
}