// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: CLSCompliant(false)]
[assembly: InternalsVisibleTo("Asmichi.Utilities.ChildProcess.Test, PublicKey=0024000004800000940000000602000000240000525341310004000001000100f9668e0c5bb910623df15dc298b71a5e8a57bcc946964c15217646a95b8c7ff18f904e94a96b14fe03317c72dcd3f12c761092ae7268e23b6c7dbbb4f4b555a31fcfb4363b780d251ce00acaaa17ca59d2031d2d9ea10c4236d5ea7e7931631f2da07b337cc86b50c755e64adf5e42b629837509b437780b29798c5835991abd")]

#if DEBUG || ADD_IMPORT_SEARCH_PATH_ASSEMBLY_DIRECTORY
// Workaround for https://github.com/dotnet/sdk/issues/1088.
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32 | DllImportSearchPath.AssemblyDirectory)]
#else
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
#endif
