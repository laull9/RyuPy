using System; using System.Reflection;
Console.WriteLine(typeof(object).Assembly.FullName);
Console.WriteLine(typeof(System.Runtime.Intrinsics.Vector128).Assembly.FullName);
foreach (var file in args) Console.WriteLine(AssemblyName.GetAssemblyName(file).FullName);
