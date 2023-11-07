using Microsoft.Win32;
using Nrk.FluentCore.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

namespace Nrk.FluentCore.Environment;

[SupportedOSPlatform("windows")]
public static class JavaUtils
{
    /// <summary>
    /// Search for all installed Java versions
    /// </summary>
    /// <param name="otherPaths">Paths to search other than default locations</param>
    /// <returns>A list of paths of javaw.exe</returns>
    public static IEnumerable<string> SearchJava(IEnumerable<string>? otherPaths = null)
    {
        var result = new List<string>();

        #region Cmd: Find Java by running "where javaw" command in cmd.exe

        using var process = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = "cmd",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true,
        };

        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        var output = new List<string?>();

        process.OutputDataReceived += (sender, e) => output.Add(e.Data);
        process.ErrorDataReceived += (sender, e) => output.Add(e.Data);

        process.StandardInput.WriteLine("where javaw");
        process.StandardInput.WriteLine("exit");
        process.WaitForExit();

        IEnumerable<string> javaPaths = output.Where(
            x => !string.IsNullOrEmpty(x) && x.EndsWith("javaw.exe") && File.Exists(x)
        )!; // null checked in the where clause
        result.AddRange(javaPaths);

        #endregion

        #region Registry: Find Java by searching the registry

        var javaHomePaths = new List<string>();

        // Local function: recursively search for the keyName in the registry
        List<string> ForRegistryKey(RegistryKey registryKey, string keyName)
        {
            var result = new List<string>();

            foreach (string valueName in registryKey.GetValueNames())
            {
                if (valueName == keyName) // Check that the valueName exists
                    result.Add((string)registryKey.GetValue(valueName)!);
            }

            foreach (string registrySubKey in registryKey.GetSubKeyNames())
            {
                using var subKey = registryKey.OpenSubKey(registrySubKey);
                if (subKey is not null) // Check that the registrySubKey exists
                    result.AddRange(ForRegistryKey(subKey, keyName));
            }

            return result;
        };

        using var reg = Registry.LocalMachine.OpenSubKey("SOFTWARE");

        if (reg is not null && reg.GetSubKeyNames().Contains("JavaSoft"))
        {
            using var registryKey = reg.OpenSubKey("JavaSoft");
            if (registryKey is not null)
                javaHomePaths.AddRange(ForRegistryKey(registryKey, "JavaHome"));
        }

        if (reg is not null && reg.GetSubKeyNames().Contains("WOW6432Node"))
        {
            using var registryKey = reg.OpenSubKey("WOW6432Node");
            if (registryKey is not null && registryKey.GetSubKeyNames().Contains("JavaSoft"))
            {
                using var registrySubKey = reg.OpenSubKey("JavaSoft");
                if (registrySubKey is not null)
                    ForRegistryKey(registrySubKey, "JavaHome").ForEach(x => javaHomePaths.Add(x));
            }
        }

        foreach (var item in javaHomePaths)
            if (Directory.Exists(item))
                result.AddRange(new DirectoryInfo(item).FindAll("javaw.exe").Select(x => x.FullName));

        #endregion

        #region Special Folders

        var folders = new List<string>();

        // %APPDATA%\.minecraft\cache\java
        string? appDataPath = System.Environment.GetEnvironmentVariable("APPDATA");
        if (appDataPath is not null)
            folders.Add(Path.Combine(appDataPath, ".minecraft\\cache\\java")); // Use \ as this is Windows only

        // %JAVA_HOME%
        string? javaHomePath = System.Environment.GetEnvironmentVariable("JAVA_HOME");
        if (javaHomePath is not null)
            folders.Add(javaHomePath);

        // C:\Program Files\Java
        folders.Add("C:\\Program Files\\Java"); // Use \ as this is Windows only

        // Check Java for each folder
        foreach (var folder in folders)
            if (Directory.Exists(folder))
                result.AddRange(new DirectoryInfo(folder).FindAll("javaw.exe").Select(x => x.FullName));

        if (otherPaths is not null)
            foreach (string path in otherPaths)
                if (Directory.Exists(path))
                    result.AddRange(new DirectoryInfo(path).FindAll("javaw.exe").Select(x => x.FullName));

        #endregion

        return result.Distinct();
    }

    /// <summary>
    /// Get the JavaInfo of a javaw.exe
    /// </summary>
    /// <param name="file">Path of javaw.exe</param>
    /// <returns>A JavaInfo object representing the javaw.exe</returns>
    public static JavaInfo GetJavaInfo(string file)
    {
        var fileVersionInfo = FileVersionInfo.GetVersionInfo(file);

        string name = "";
        if (!string.IsNullOrWhiteSpace(fileVersionInfo.ProductName))
            name = fileVersionInfo.ProductName.Trim().Split(" ")[0]; // Guarantees that the split will have at least one element
        else
            name = $"Java {fileVersionInfo.ProductMajorPart}"; // Use the major version if the product name is not available

        // Process the name to make it more user friendly
        if (name.StartsWith("Java(TM)"))
            name = $"Java {fileVersionInfo.ProductMajorPart}";
        else if (name.StartsWith("OpenJDK"))
            name = $"OpenJDK {fileVersionInfo.ProductMajorPart}";

        var runtimeInfo = new JavaInfo
        {
            Name = name,
            ProductName = fileVersionInfo.ProductName,
            Company = fileVersionInfo.CompanyName,
            Version = new Version(
                fileVersionInfo.ProductMajorPart,
                fileVersionInfo.ProductMinorPart,
                fileVersionInfo.ProductBuildPart,
                fileVersionInfo.ProductPrivatePart
            ),
            Architecture = GetPeArchitecture(file) switch
            {
                523 => "x64",
                267 => "x86",
                _ => "unknown"
            }
        };

        return runtimeInfo;
    }

    public static ushort GetPeArchitecture(string filePath)
    {
        ushort architecture = 0;

        try
        {
            using var fStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var bReader = new BinaryReader(fStream);

            if (bReader.ReadUInt16() == 23117)
            {
                fStream.Seek(0x3A, SeekOrigin.Current);
                fStream.Seek(bReader.ReadUInt32(), SeekOrigin.Begin);

                if (bReader.ReadUInt32() == 17744)
                {
                    fStream.Seek(20, SeekOrigin.Current);
                    architecture = bReader.ReadUInt16();
                }
            }
        }
        catch { }

        return architecture;
    }
}
