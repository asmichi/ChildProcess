// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.Threading.Tasks;

namespace Asmichi.Utilities.Utilities
{
    // Cached completed Task<bool>
    internal static class CompletedBoolTask
    {
        public static readonly Task<bool> True = Task.FromResult(true);
    }
}
