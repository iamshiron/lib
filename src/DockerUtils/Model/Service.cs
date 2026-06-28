namespace Shiron.Lib.DockerUtils.Model;

public class Service {
    public required string Image { get; init; }
    public required string Name { get; init; }
    public required string? ContainerName { get; init; }
    public required RestartAction? Restart { get; init; }
    public required PortForward[] Ports { get; init; } = [];
    public required string[] Volumes { get; init; } = [];
    public required Dictionary<string, string?> Environment { get; init; } = [];
    public required string[] Networks { get; init; } = [];
}
