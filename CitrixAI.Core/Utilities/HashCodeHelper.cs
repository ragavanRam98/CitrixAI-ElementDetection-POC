using System.Collections.Generic;

namespace CitrixAI.Core.Utilities
{
    /// <summary>
    /// Helper class for generating hash codes in .NET Framework 4.8.
    /// Provides functionality similar to .NET Core's HashCode struct.
    /// </summary>
    public static class HashCodeHelper
    {
        /// <summary>
        /// Combines multiple values into a single hash code.
        /// </summary>
        /// <param name="values">Values to combine.</param>
        /// <returns>Combined hash code.</returns>
        public static int Combine(params object[] values)
        {
            if (values == null || values.Length == 0)
                return 0;

            unchecked
            {
                int hash = 17;
                foreach (var value in values)
                {
                    hash = hash * 23 + (value?.GetHashCode() ?? 0);
                }
                return hash;
            }
        }

        /// <summary>
        /// Combines two values into a single hash code.
        /// </summary>
        /// <typeparam name="T1">Type of first value.</typeparam>
        /// <typeparam name="T2">Type of second value.</typeparam>
        /// <param name="value1">First value.</param>
        /// <param name="value2">Second value.</param>
        /// <returns>Combined hash code.</returns>
        public static int Combine<T1, T2>(T1 value1, T2 value2)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (value1?.GetHashCode() ?? 0);
                hash = hash * 23 + (value2?.GetHashCode() ?? 0);
                return hash;
            }
        }

        /// <summary>
        /// Combines three values into a single hash code.
        /// </summary>
        /// <typeparam name="T1">Type of first value.</typeparam>
        /// <typeparam name="T2">Type of second value.</typeparam>
        /// <typeparam name="T3">Type of third value.</typeparam>
        /// <param name="value1">First value.</param>
        /// <param name="value2">Second value.</param>
        /// <param name="value3">Third value.</param>
        /// <returns>Combined hash code.</returns>
        public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (value1?.GetHashCode() ?? 0);
                hash = hash * 23 + (value2?.GetHashCode() ?? 0);
                hash = hash * 23 + (value3?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}