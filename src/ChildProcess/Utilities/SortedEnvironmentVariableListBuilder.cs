// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Asmichi.PlatformAbstraction;

namespace Asmichi.Utilities
{
    /// <summary>
    /// (Non-copyable) Builds a sorted distinct list of environment variables.
    /// </summary>
    // NOTE: Not a requirement, but normally entries in an environment block are sorted.
    internal struct SortedEnvironmentVariableListBuilder
    {
        private readonly KeyValuePair<string, string>[] _array;
        private int _count;

        private SortedEnvironmentVariableListBuilder(int capacity)
        {
            _array = capacity == 0 ? Array.Empty<KeyValuePair<string, string>>() : new KeyValuePair<string, string>[capacity];
            _count = 0;
        }

        public static SortedEnvironmentVariableListBuilder Create(int capacity) =>
            new SortedEnvironmentVariableListBuilder(capacity);

        public static SortedEnvironmentVariableListBuilder CreateFromContext(
            int capacity,
            ReadOnlyMemory<KeyValuePair<string, string>> contextEnvVars)
        {
            var value = new SortedEnvironmentVariableListBuilder(capacity);
            contextEnvVars.CopyTo(value._array.AsMemory());
            value._count = contextEnvVars.Length;
            return value;
        }

        public static SortedEnvironmentVariableListBuilder CreateFromProcess(
            int capacity,
            IDictionary processEnvVars)
        {
            // NOTE: On Windows, the environment block may contain names that differ only in cases
            //       (due to broken programs generating such entries, for example, Cygwin).
            //       Preserve such names by not removing duplicates in order to:
            //
            //       - match the behavior of CreateProcess with lpEnvironment == null
            //       - match the `extraEnvVars.Count == 0` case
            //
            //       At the same time, try not to introduce new instances of such broken entries
            //       by removing future duplicates.
            var value = new SortedEnvironmentVariableListBuilder(capacity);
            EnvironmentVariableListUtil.ToSortedKeyValuePairs(processEnvVars, value._array);
            value._count += processEnvVars.Count;
            return value;
        }

        public ReadOnlyMemory<KeyValuePair<string, string>> Build() => _array.AsMemory(0, _count);

        public void InsertOrRemoveRange(IReadOnlyCollection<KeyValuePair<string, string>> extraEnvVars)
        {
            foreach (var (name, value) in extraEnvVars)
            {
                InsertOrRemove(name, value);
            }
        }

        /// <summary>
        /// If <paramref name="value"/> is <see langword="null"/> or empty, removes the entry with <paramref name="name"/> (if any).
        /// Otherwise, removes existing entries with <paramref name="name"/> and inserts an new entry.
        /// </summary>
        // Insertion sort should be sufficient. Generally we do not have tens of extra environment variables.
        public void InsertOrRemove(string name, string value)
        {
            EnvironmentVariableUtil.ValidateNameAndValue(name, value);

            var (start, end) = SearchMatchingElements(_array.AsSpan(0, _count), name);

            Debug.Assert(end >= start);

            if (string.IsNullOrEmpty(value))
            {
                // Remove
                if (start == end)
                {
                    // _array does not have any element with the name. Do nothing.
                }
                else
                {
                    // Remove the matching elements.
                    Array.Copy(_array, end, _array, start, _count - end);
                    _count -= end - start;
                }
            }
            else
            {
                // Insert
                if (start == end)
                {
                    // _array does not have any element with the name. Insert the new element.
                    Array.Copy(_array, end, _array, end + 1, _count - end);
                    _array[start] = new(name, value);
                    _count++;
                }
                else if (end == start + 1)
                {
                    // _array has exactly one element with the name. Just overwrite it.
                    _array[start] = new(name, value);
                }
                else
                {
                    // _array has multiple elements with the name. Overwrite the first one and remove the rest.
                    _array[start] = new(name, value);
                    Array.Copy(_array, end, _array, start + 1, _count - end);
                    _count -= end - start - 1;
                }
            }
        }

        private static (int start, int end) SearchMatchingElements(
            Span<KeyValuePair<string, string>> elements,
            string name)
        {
            var nameComparison = EnvironmentPal.EnvironmentVariableNameComparison;
            var index = elements.BinarySearch(new(name, ""), EnvironmentVariablePairNameComparer.Default);
            if (index < 0)
            {
                return (~index, ~index);
            }
            else
            {
                // Search for adjuscent matching elements.
                var start = index - 1;
                for (; start >= 0; start--)
                {
                    if (!string.Equals(name, elements[start].Key, nameComparison))
                    {
                        break;
                    }
                }
                start++;

                var end = index + 1;
                for (; end < elements.Length; end++)
                {
                    if (!string.Equals(name, elements[end].Key, nameComparison))
                    {
                        break;
                    }
                }

                return (start, end);
            }
        }
    }
}
