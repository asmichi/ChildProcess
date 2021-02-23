// Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "This is not production code.")]
[assembly: SuppressMessage("Naming", "CA1707: Identifiers should not contain underscores", Justification = "This is test code.")]
[assembly: SuppressMessage("Reliability", "CA2007:Do not directly await a Task", Justification = "This is application-level code.")]
