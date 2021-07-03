// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using Asmichi.PlatformAbstraction;

namespace Asmichi.Utilities
{
    /// <summary>
    /// Compare only the name parts of environment variable <see cref="KeyValuePair"/> values.
    /// </summary>
    internal static class EnvironmentVariablePairNameComparer
    {
        private static IComparer<KeyValuePair<string, string>> Ordinal { get; } =
            new ImplByStringComparison(StringComparison.Ordinal);
        private static IComparer<KeyValuePair<string, string>> OrdinalIgnoreCase { get; } =
            new ImplByStringComparison(StringComparison.OrdinalIgnoreCase);
        public static IComparer<KeyValuePair<string, string>> Default { get; } =
            EnvironmentPal.IsEnvironmentVariableNameCaseSensitive ? Ordinal : OrdinalIgnoreCase;
        public static IComparer<KeyValuePair<string, string>> DefaultThenOrdinal { get; } =
            EnvironmentPal.IsEnvironmentVariableNameCaseSensitive ? Ordinal : new ImplOrdinalIgnoreCaseThenOrdinal();

        private sealed class ImplByStringComparison : IComparer<KeyValuePair<string, string>>
        {
            private readonly StringComparison _nameComparison;

            public ImplByStringComparison(StringComparison nameComparison) => _nameComparison = nameComparison;

            public int Compare(KeyValuePair<string, string> x, KeyValuePair<string, string> y) =>
                string.Compare(x.Key, y.Key, _nameComparison);
        }

        private sealed class ImplOrdinalIgnoreCaseThenOrdinal : IComparer<KeyValuePair<string, string>>
        {
            public int Compare(KeyValuePair<string, string> x, KeyValuePair<string, string> y)
            {
                var ignoreCaseResult = string.Compare(x.Key, y.Key, StringComparison.OrdinalIgnoreCase);
                if (ignoreCaseResult != 0)
                {
                    return ignoreCaseResult;
                }

                return string.Compare(x.Key, y.Key, StringComparison.Ordinal);
            }
        }
    }
}
