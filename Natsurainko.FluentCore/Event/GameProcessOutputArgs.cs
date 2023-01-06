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
            GameProcessOutputLevel.Info => ConsoleColor.White,
            GameProcessOutputLevel.Warn => ConsoleColor.Yellow,
            GameProcessOutputLevel.Error => ConsoleColor.Red,
            GameProcessOutputLevel.Fatal => ConsoleColor.DarkRed,
            GameProcessOutputLevel.Debug => ConsoleColor.DarkGray,
            _ => ConsoleColor.Gray
        });
    }
}
