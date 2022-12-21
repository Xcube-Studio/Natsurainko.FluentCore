using System;
using System.Text.RegularExpressions;

namespace Natsurainko.FluentCore.Model.Launch;

public class GameProcessOutput
{
    public enum GameProcessOutputLevel
    {
        Info = 0,
        Warn = 1,
        Error = 2,
        Fatal = 3,
        Debug = 4
    }

    public GameProcessOutputLevel Level { get; private set; }

    public string Thread { get; private set; }

    public string Text { get; private set; }

    public string FullData { get; private set; }

    public DateTime DateTime { get; private set; }

    public static GameProcessOutput Parse(string data)
    {
        var timeRegex = new Regex("([01]?[0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]").Match(data).Value;
        var regex = new Regex(@"\[[\w/\s-]+\]").Match(data).Value.TrimStart('[').TrimEnd(']');

        var processOutput = new GameProcessOutput()
        {
            FullData = data,
            Text = data.Contains(": ") ? data.Substring(data.IndexOf(": ") + 2) : data,
            DateTime = string.IsNullOrEmpty(timeRegex) ? DateTime.Now : DateTime.Parse(timeRegex),
            Level = GameProcessOutputLevel.Info,
        };

        if (regex.Contains("/"))
        {
            processOutput.Level = regex.Split('/')[1].ToLower() switch
            {
                "info" => GameProcessOutputLevel.Info,
                "warn" => GameProcessOutputLevel.Warn,
                "error" => GameProcessOutputLevel.Error,
                "datal" => GameProcessOutputLevel.Fatal,
                "debug" => GameProcessOutputLevel.Debug,
                _ => GameProcessOutputLevel.Info,
            };
            processOutput.Thread = regex.Split('/')[0];
        }

        if (data.StartsWith("\tat") || (data.Contains(": ") && data.Split(':')[0].EndsWith("Exception")))
            processOutput.Level = GameProcessOutputLevel.Error;

        return processOutput;
    }

    public static GameProcessOutput Parse(string data, bool error)
    {
        var processOutput = Parse(data);

        if (error)
            processOutput.Level = GameProcessOutputLevel.Error;
        return processOutput;
    }
}
