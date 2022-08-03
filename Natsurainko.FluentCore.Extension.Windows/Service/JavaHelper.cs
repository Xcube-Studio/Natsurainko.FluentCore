using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using Natsurainko.Toolkits.IO;

namespace Natsurainko.FluentCore.Extension.Windows.Service
{
    public class JavaHelper
    {
        public static IEnumerable<string> SearchJavaRuntime()
        {
            #region Cmd

            var process = new Process()
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

            var output = new List<string>();

            process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => output.Add(e.Data);
            process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => output.Add(e.Data);

            process.StandardInput.WriteLine("where javaw");
            process.StandardInput.WriteLine("exit");
            process.WaitForExit();

            for (int i = 0; i < output.Count; i++)
                if (output[i].Contains('>'))
                    output.Remove(output[i]);

            if (output.Count > 0)
                foreach (var item in output.Skip(2))
                    yield return item;

            process.Dispose();

            #endregion

            #region Regedit

            var javaHomePaths = new List<string>();

            List<string> ForRegistryKey(RegistryKey registryKey, string keyName)
            {
                var result = new List<string>();

                foreach (string valueName in registryKey.GetValueNames())
                    if (valueName == keyName)
                        result.Add((string)registryKey.GetValue(valueName));

                foreach (string registrySubKey in registryKey.GetSubKeyNames())
                    ForRegistryKey(registryKey.OpenSubKey(registrySubKey), keyName).ForEach(x => result.Add(x));

                return result;
            };

            using var reg = Registry.LocalMachine.OpenSubKey("SOFTWARE");

            if (reg.GetSubKeyNames().Contains("JavaSoft"))
            {
                using var registryKey = reg.OpenSubKey("JavaSoft");
                ForRegistryKey(registryKey, "JavaHome").ForEach(x => javaHomePaths.Add(x));
            }

            if (reg.GetSubKeyNames().Contains("WOW6432Node"))
            {
                using var registryKey = reg.OpenSubKey("WOW6432Node");
                if (registryKey.GetSubKeyNames().Contains("JavaSoft"))
                {
                    using var registrySubKey = reg.OpenSubKey("JavaSoft");
                    ForRegistryKey(registrySubKey, "JavaHome").ForEach(x => javaHomePaths.Add(x));
                }
            }

            foreach (var item in javaHomePaths)
                if (Directory.Exists(item))
                    yield return new DirectoryInfo(item).Find("javaw.exe").FullName;

            #endregion

            #region Special Folders

            var folders = new List<string>()
            {
                Path.Combine(Environment.GetEnvironmentVariable("APPDATA"),".minecraft\\cache\\java"),
                Environment.GetEnvironmentVariable("JAVA_HOME"),
                "C:\\Program Files\\Java"
            };

            foreach (var item in folders)
                if (Directory.Exists(item))
                    yield return new DirectoryInfo(item).Find("javaw.exe").FullName;

            #endregion
        }
    }
}
