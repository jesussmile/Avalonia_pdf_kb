using System.Linq;
using System.Reflection;

var assemblyPath = "/Users/pannam/.nuget/packages/mupdfcore.mupdfrenderer/2.0.1/lib/netstandard2.0/MuPDFCore.MuPDFRenderer.dll";
var assembly = Assembly.LoadFrom(assemblyPath);
var type = assembly.GetType("MuPDFCore.MuPDFRenderer.PDFRenderer");

if (type is null)
{
	Console.WriteLine("Type not found");
	return;
}

Console.WriteLine($"Type: {type.FullName}");

Console.WriteLine("Properties:");
foreach (var property in type.GetProperties())
{
	Console.WriteLine($" - {property.PropertyType.Name} {property.Name} (Writable: {property.CanWrite})");
}

Console.WriteLine("Methods:");
foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
{
	var parameters = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
	Console.WriteLine($" - {method.ReturnType.Name} {method.Name}({parameters})");
}
