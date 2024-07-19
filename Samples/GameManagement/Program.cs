using Nrk.FluentCore.Experimental.GameManagement.Instances;

var mcFolderPath = @"C:\Users\jinch\Saved Games\Minecraft\.minecraft";
var minecraftInstanceParser = new MinecraftInstanceParser(mcFolderPath);
var instances = minecraftInstanceParser.ParseAllInstances();
Console.WriteLine();
