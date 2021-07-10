// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Threading;

namespace Asmichi.Utilities
{
    internal static class ArgumentValidationUtil
    {
        public static int ValidateTimeoutRange(int millisecondsTimeout)
        {
            if (millisecondsTimeout < Timeout.Infinite)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(millisecondsTimeout), "Timeout must be Timeout.Infinite or a non-negative integer.");
            }

            return millisecondsTimeout;
        }

        public static TimeSpan ValidateTimeoutRange(TimeSpan timeout)
        {
            if (!(timeout.Ticks >= 0 || timeout == Timeout.InfiniteTimeSpan))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeout), "Timeout must be Timeout.InfiniteTimeSpan or a non-negative TimeSpan.");
            }

            return timeout;
        }
    }
}
