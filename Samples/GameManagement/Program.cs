using Nrk.FluentCore.GameManagement;
using System.Collections.Generic;
using System.Text.Json;

var mcFolderPath = @"C:\Users\jinch\Saved Games\Minecraft\.minecraft";
var minecraftInstanceParser = new MinecraftInstanceParser(mcFolderPath);
var instances = minecraftInstanceParser.ParseAllInstances();
Console.WriteLine();
