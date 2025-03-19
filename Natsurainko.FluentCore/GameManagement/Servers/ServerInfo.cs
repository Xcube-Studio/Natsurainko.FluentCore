namespace Nrk.FluentCore.GameManagement.Servers;

public record ServerInfo
{
    public string? Name { get; set; }

    public required string Address { get; set; }

    public string? Icon { get; set; }
}
