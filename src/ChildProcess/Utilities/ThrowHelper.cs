// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Asmichi.Utilities.ProcessManagement;

namespace Asmichi.Utilities.Utilities
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        public static void ThrowExecutableNotFoundException(string fileName, ChildProcessFlags flags, Exception? innerException = null)
        {
            bool ignoreSearchPath = flags.HasIgnoreSearchPath();
            var format = ignoreSearchPath ? "Executable not found: {0}" : "Executable not found on the search path: {0}";
            throw new FileNotFoundException(string.Format(CultureInfo.InvariantCulture, format, fileName), fileName, innerException);
        }
    }
}
