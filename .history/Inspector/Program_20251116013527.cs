using System.IO;
using System.Linq;
using System.Reflection;

var assemblyPath = "/Users/pannam/.nuget/packages/mupdfcore.mupdfrenderer/2.0.1/lib/netstandard2.0/MuPDFCore.MuPDFRenderer.dll";
var dependentAssemblies = new[]
{
	"/Users/pannam/.nuget/packages/mupdfcore/2.0.1/lib/netstandard2.0/MuPDFCore.dll",
	"/Users/pannam/.nuget/packages/avalonia/11.0.11/lib/net7.0/Avalonia.Base.dll",
	"/Users/pannam/.nuget/packages/avalonia/11.0.11/lib/net7.0/Avalonia.Controls.dll"
};

var coreDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
var runtimeAssemblies = Directory.GetFiles(coreDir, "*.dll");
var resolver = new PathAssemblyResolver(runtimeAssemblies.Concat(dependentAssemblies).Concat(new[] { assemblyPath }));

using var metadataContext = new MetadataLoadContext(resolver);
var assembly = metadataContext.LoadFromAssemblyPath(assemblyPath);
var type = assembly.GetTypes().FirstOrDefault(t => t.Name.Contains("PDFRenderer", StringComparison.OrdinalIgnoreCase));

if (type is null)
{
	Console.WriteLine("Type not found");
	Console.WriteLine("Exported types:");
	foreach (var exportedType in assembly.GetExportedTypes())
	{
		Console.WriteLine(exportedType.FullName);
	}
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
