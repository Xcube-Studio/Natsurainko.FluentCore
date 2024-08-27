using Nrk.FluentCore.Authentication;
using Nrk.FluentCore.Environment;
using Nrk.FluentCore.Experimental.GameManagement.Instances;
using Nrk.FluentCore.Experimental.Launch;
using Nrk.FluentCore.Utils;

var offlineAuthenticator = new OfflineAuthenticator();
var account = offlineAuthenticator.Login("Steve");

var java = JavaUtils.SearchJava()
    .Select(JavaUtils.GetJavaInfo)
    .MaxBy(x => x.Version);

var mcFolderPath = @"D:\Minecraft\Test\.minecraft";
var minecraftInstanceParser = new MinecraftInstanceParser(mcFolderPath);
var instances = minecraftInstanceParser.ParseAllInstances();

foreach (var instance in instances)
{
    var errorDatas = new List<string>();

    void Process_ErrorDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
            errorDatas.Add(e.Data);
    }

    var libs = instance.GetRequiredLibraries();

    Console.WriteLine($"\r\n{instance.InstanceId} {instance.GetType()}");

    using var process = new MinecraftProcessBuilder(instance)
        .SetJavaSettings(java!.FilePath, 1024, 1024)
        .SetAccountSettings(account, false)
        .Build();

    UnzipUtils.BatchUnzip(
        Path.Combine(instance.MinecraftFolderPath, "versions", instance.InstanceId, "natives"),
        process.Natives.Select(x => x.FullPath));

    process.ErrorDataReceived += Process_ErrorDataReceived;
    process.Start();
    process.Process.WaitForInputIdle(TimeSpan.FromSeconds(5));

    await Task.Delay(10000);
    process.Process.CloseMainWindow();
    process.Process.WaitForExit();

    if (process.Process.ExitCode != 0)
    {
        errorDatas.ForEach(Console.WriteLine);
        Console.ReadKey();
    }

    Console.WriteLine("\x1b[3J");
    Console.Clear();
}



Console.WriteLine();

