namespace Shiron.Lib.DockerUtils.Model;

/// <summary>
/// Port forwarding configuration parsed from a Docker Compose service.
/// </summary>
public sealed class PortForward {
    /// <summary>The container-side port exactly as specified in the compose file, without protocol.</summary>
    public required string ContainerPort { get; init; }

    /// <summary>The host-side (exposed) port exactly as specified in the compose file.</summary>
    public required string HostPort { get; init; }

    /// <summary>Optional host IP/hostname the published port is bound to.</summary>
    public string? HostAddress { get; init; }

    /// <summary>Optional container IP/hostname the port forwards to.</summary>
    public string? ContainerAddress { get; init; }

    /// <summary>Optional protocol (<c>tcp</c> or <c>udp</c>); <c>null</c> when omitted from the compose file.</summary>
    public string? Protocol { get; init; }
}
