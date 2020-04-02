using System;
using System.Collections.Generic;
using Microsoft.VisualBasic;

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
        
        public static bool Includes<T>(this T[] array, T value)
        {
            return Array.IndexOf(array, value) >= 0;
        }
        
        public static string Spaces(int n) => new string(' ', n * 2);

        public static void Deconstruct<T0>(this object[] items, out T0 t0)
        {
            t0 = items.Length > 0 ? (T0) items[0] : default;
        }

        public static void Deconstruct<T0, T1>(this object[] items, out T0 t0, out T1 t1)
        {
            t0 = items.Length > 0 ? (T0) items[0] : default;
            t1 = items.Length > 1 ? (T1) items[1] : default;
        }
        
        public static void Deconstruct<T0, T1, T2>(this object[] items, out T0 t0, out T1 t1, out T2 t2)
        {
            t0 = items.Length > 0 ? (T0) items[0] : default;
            t1 = items.Length > 1 ? (T1) items[1] : default;
            t2 = items.Length > 2 ? (T2) items[2] : default;
        }

        public static void Deconstruct<T0, T1, T2, T3>(this object[] items, out T0 t0, out T1 t1, out T2 t2, out T3 t3)
        {
            t0 = items.Length > 0 ? (T0) items[0] : default;
            t1 = items.Length > 1 ? (T1) items[1] : default;
            t2 = items.Length > 2 ? (T2) items[2] : default;
            t3 = items.Length > 3 ? (T3) items[3] : default;
        }
        
        public static void Deconstruct<T0, T1, T2, T3, T4>(this object[] items, out T0 t0, out T1 t1, out T2 t2, out T3 t3, out T4 t4)
        {
            t0 = items.Length > 0 ? (T0) items[0] : default;
            t1 = items.Length > 1 ? (T1) items[1] : default;
            t2 = items.Length > 2 ? (T2) items[2] : default;
            t3 = items.Length > 3 ? (T3) items[3] : default;
            t4 = items.Length > 4 ? (T4) items[4] : default;
        }
        
        public static void Deconstruct<T0, T1, T2, T3, T4, T5>(this object[] items, out T0 t0, out T1 t1, out T2 t2, out T3 t3, out T4 t4, out T5 t5)
        {
            t0 = items.Length > 0 ? (T0) items[0] : default;
            t1 = items.Length > 1 ? (T1) items[1] : default;
            t2 = items.Length > 2 ? (T2) items[2] : default;
            t3 = items.Length > 3 ? (T3) items[3] : default;
            t4 = items.Length > 4 ? (T4) items[4] : default;
            t5 = items.Length > 5 ? (T5) items[5] : default;
        }
        
        public static void Deconstruct<T0, T1, T2, T3, T4, T5, T6>(this object[] items, out T0 t0, out T1 t1, out T2 t2, out T3 t3, out T4 t4, out T5 t5, out T6 t6)
        {
            t0 = items.Length > 0 ? (T0) items[0] : default;
            t1 = items.Length > 1 ? (T1) items[1] : default;
            t2 = items.Length > 2 ? (T2) items[2] : default;
            t3 = items.Length > 3 ? (T3) items[3] : default;
            t4 = items.Length > 4 ? (T4) items[4] : default;
            t5 = items.Length > 5 ? (T5) items[5] : default;
            t6 = items.Length > 6 ? (T6) items[6] : default;
        }
        
        public static void Deconstruct<T0, T1, T2, T3, T4, T5, T6, T7>(this object[] items, out T0 t0, out T1 t1, out T2 t2, out T3 t3, out T4 t4, out T5 t5, out T6 t6, out T7 t7)
        {
            t0 = items.Length > 0 ? (T0) items[0] : default;
            t1 = items.Length > 1 ? (T1) items[1] : default;
            t2 = items.Length > 2 ? (T2) items[2] : default;
            t3 = items.Length > 3 ? (T3) items[3] : default;
            t4 = items.Length > 4 ? (T4) items[4] : default;
            t5 = items.Length > 5 ? (T5) items[5] : default;
            t6 = items.Length > 6 ? (T6) items[6] : default;
            t7 = items.Length > 7 ? (T7) items[7] : default;
        }
        
        public static void Deconstruct<T0, T1, T2, T3, T4, T5, T6, T7, T8>(this object[] items, out T0 t0, out T1 t1, out T2 t2, out T3 t3, out T4 t4, out T5 t5, out T6 t6, out T7 t7, out T8 t8)
        {
            t0 = items.Length > 0 ? (T0) items[0] : default;
            t1 = items.Length > 1 ? (T1) items[1] : default;
            t2 = items.Length > 2 ? (T2) items[2] : default;
            t3 = items.Length > 3 ? (T3) items[3] : default;
            t4 = items.Length > 4 ? (T4) items[4] : default;
            t5 = items.Length > 5 ? (T5) items[5] : default;
            t6 = items.Length > 6 ? (T6) items[6] : default;
            t7 = items.Length > 7 ? (T7) items[7] : default;
            t8 = items.Length > 8 ? (T8) items[8] : default;
        }

        public static void Deconstruct<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this object[] items, out T0 t0, out T1 t1, out T2 t2, out T3 t3, out T4 t4, out T5 t5, out T6 t6, out T7 t7, out T8 t8, out T9 t9)
        {
            t0 = items.Length > 0 ? (T0) items[0] : default;
            t1 = items.Length > 1 ? (T1) items[1] : default;
            t2 = items.Length > 2 ? (T2) items[2] : default;
            t3 = items.Length > 3 ? (T3) items[3] : default;
            t4 = items.Length > 4 ? (T4) items[4] : default;
            t5 = items.Length > 5 ? (T5) items[5] : default;
            t6 = items.Length > 6 ? (T6) items[6] : default;
            t7 = items.Length > 7 ? (T7) items[7] : default;
            t8 = items.Length > 8 ? (T8) items[8] : default;
            t9 = items.Length > 9 ? (T9) items[9] : default;
        }
    }
}