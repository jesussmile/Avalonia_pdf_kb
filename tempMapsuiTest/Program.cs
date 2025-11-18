using System;
using System.Linq;
using System.Reflection;

var tilingAssembly = Assembly.Load("Mapsui.Tiling");
var tilingTypes = tilingAssembly.GetTypes().OrderBy(t => t.FullName).ToList();

Console.WriteLine($"Tiling type count: {tilingTypes.Count}");
foreach (var type in tilingTypes.Take(40))
{
    Console.WriteLine(type.FullName);
}
