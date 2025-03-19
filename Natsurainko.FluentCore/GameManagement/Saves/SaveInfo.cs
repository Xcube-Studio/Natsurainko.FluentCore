using System;

namespace Nrk.FluentCore.GameManagement.Saves;

public record SaveInfo
{
    public required string Folder { get; set; }

    public required string FolderName { get; set; }

    public required string LevelName { get; set; }

    public required string Version { get; set; }

    public bool AllowCommands { get; set; }

    public DateTime LastPlayed { get; set; }

    public long? Seed { get; set; }

    public string? IconFilePath { get; set; }

    public int GameType { get; set; }
}


