// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.Globalization;
using System.IO;

namespace Asmichi.Utilities.PlatformAbstraction.Utilities
{
    internal static class NamedPipeUtil
    {
        // NOTE: There may be multiple instances of this assembly (AppDomain or AssemblyLoadContext).
        //       Therefore embedding pid only does not provide uniqueness.
        public static string MakePipePathPrefix(string pathPrefix, uint pid) =>
            Path.Combine(
                pathPrefix,
                string.Format(CultureInfo.InvariantCulture, "Asmichi.ChildProcess.{0:D5}.{1}.", pid, Path.GetRandomFileName()));
    }
}
