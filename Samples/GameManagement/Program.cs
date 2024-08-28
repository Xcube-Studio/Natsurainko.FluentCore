using Nrk.FluentCore.GameManagement.Instances;

var mcFolderPath = @"D:\Minecraft\.minecraft";
var minecraftInstanceParser = new MinecraftInstanceParser(mcFolderPath);
var instances = minecraftInstanceParser.ParseAllInstances();

foreach (var instance in instances)
{
    var libs = instance.GetRequiredLibraries();

	foreach (var item in libs.Libraries)
        Console.WriteLine($"{item.GetType()}, {item.MavenName}");

    foreach (var item in libs.NativeLibraries)
        Console.WriteLine($"{item.GetType()}, Native,{item.MavenName}");

    Console.WriteLine($"\r\n{instance.InstanceId} {instance.GetType()}");

    Console.ReadKey();
    Console.WriteLine("\x1b[3J");

    Console.Clear();
}

Console.WriteLine();
