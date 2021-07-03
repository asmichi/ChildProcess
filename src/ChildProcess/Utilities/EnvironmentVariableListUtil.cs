// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Asmichi.PlatformAbstraction;

namespace Asmichi.Utilities
{
    /// <summary>
    /// Utiliities for manipulating a list of environment variables.
    /// </summary>
    internal static class EnvironmentVariableListUtil
    {
        public static ArraySegment<KeyValuePair<string, string>> ToSortedDistinctKeyValuePairs(IDictionary envVarDictionary)
        {
            var nameComparison = EnvironmentPal.EnvironmentVariableNameComparison;
            var envVars = ToSortedKeyValuePairs(envVarDictionary);

            // Remove duplicates in place.
            int lastStoredIndex = 0;
            for (int i = 1; i < envVars.Length; i++)
            {
                if (!string.Equals(envVars[lastStoredIndex].Key, envVars[i].Key, nameComparison))
                {
                    lastStoredIndex++;
                    envVars[lastStoredIndex] = envVars[i];
                }
            }

            return new ArraySegment<KeyValuePair<string, string>>(envVars, 0, lastStoredIndex + 1);
        }

        public static KeyValuePair<string, string>[] ToSortedKeyValuePairs(IDictionary envVarDictionary)
        {
            var array = new KeyValuePair<string, string>[envVarDictionary.Count];
            ToSortedKeyValuePairs(envVarDictionary, array);
            return array;
        }

        public static void ToSortedKeyValuePairs(IDictionary envVarDictionary, KeyValuePair<string, string>[] buffer)
        {
            int count = envVarDictionary.Count;

            if (buffer.Length < count)
            {
                throw new ArgumentException("Buffer too small.", nameof(buffer));
            }

            int i = 0;
#pragma warning disable CS8605 // Unboxing a possibly null value.
            foreach (DictionaryEntry de in envVarDictionary)
#pragma warning restore CS8605 // Unboxing a possibly null value.
            {
                var name = (string)de.Key;
                var value = (string)de.Value!;
                buffer[i++] = new KeyValuePair<string, string>(name, value);
            }

            Debug.Assert(i == count);

            Array.Sort(buffer, 0, count, EnvironmentVariablePairNameComparer.DefaultThenOrdinal);
        }
    }
}
