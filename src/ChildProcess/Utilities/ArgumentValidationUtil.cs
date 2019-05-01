// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Threading;

namespace Asmichi.Utilities.Utilities
{
    internal static class ArgumentValidationUtil
    {
        public static void CheckTimeOutRange(int millisecondsTimeout)
        {
            if (millisecondsTimeout < Timeout.Infinite)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(millisecondsTimeout), "Timeout must be Timeout.Infinite or a non-negative integer.");
            }
        }
    }
}
