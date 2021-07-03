// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;

namespace Asmichi.Utilities
{
    /// <summary>
    /// Inspects environment variables.
    /// </summary>
    internal static class EnvironmentVariableUtil
    {
        public static void ValidateNameAndValue(string name, string value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (name.Length == 0)
            {
                throw new ArgumentException("Environment variable name must not be empty.", nameof(name));
            }
            if (name.Contains('\0', StringComparison.Ordinal))
            {
                throw new ArgumentException("Environment variable name must not contain '\0'.", nameof(name));
            }
            if (name.Contains('=', StringComparison.Ordinal))
            {
                throw new ArgumentException("Environment variable name must not contain '='.", nameof(name));
            }

            if (!string.IsNullOrEmpty(value))
            {
                if (value.Contains('\0', StringComparison.Ordinal))
                {
                    throw new ArgumentException("Environment variable value must not contain '\0'.", nameof(value));
                }
            }
            else
            {
                // null or empty indicates that this variable should be removed.
            }
        }
    }
}
