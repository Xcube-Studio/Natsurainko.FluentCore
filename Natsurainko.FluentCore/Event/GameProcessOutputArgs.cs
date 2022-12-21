using Natsurainko.FluentCore.Model.Launch;
using System;

namespace Natsurainko.FluentCore.Event;

public class GameProcessOutputArgs : EventArgs
{
    public GameProcessOutput GameProcessOutput { get; private set; }

    public bool IsErrorOutputData { get; private set; }

    public GameProcessOutputArgs(GameProcessOutput output, bool isErrorOutputData)
    {
        GameProcessOutput = output;
        IsErrorOutputData = isErrorOutputData;
    }

    public void Print()
    {
        static void ColorText(string text, ConsoleColor consoleColor = ConsoleColor.Gray)
        {
            Console.ForegroundColor = consoleColor;
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        ColorText(GameProcessOutput.FullData, GameProcessOutput.Level switch
        {
            GameProcessOutput.GameProcessOutputLevel.Info => ConsoleColor.White,
            GameProcessOutput.GameProcessOutputLevel.Warn => ConsoleColor.Yellow,
            GameProcessOutput.GameProcessOutputLevel.Error => ConsoleColor.Red,
            GameProcessOutput.GameProcessOutputLevel.Fatal => ConsoleColor.DarkRed,
            GameProcessOutput.GameProcessOutputLevel.Debug => ConsoleColor.DarkGray,
            _ => ConsoleColor.Gray
        });
    }
}
