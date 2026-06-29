using Shiron.Lib.DockerUtils.Model;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Shiron.Lib.DockerUtils;

/// <inheritdoc/>
public sealed class ComposeReader : IComposeReader {
    /// <inheritdoc/>
    public IReadOnlyList<Service> Read(string composeYaml) {
        ArgumentNullException.ThrowIfNull(composeYaml);
        if (string.IsNullOrWhiteSpace(composeYaml)) return [];

        var stream = new YamlStream();
        try {
            stream.Load(new StringReader(composeYaml));
        } catch (YamlException ex) {
            throw new ComposeReadException($"Invalid Docker Compose YAML: {ex.Message}", ex);
        }

        if (stream.Documents.Count == 0) return [];

        var result = new List<Service>();
        foreach (var document in stream.Documents) {
            if (document.RootNode is not YamlMappingNode root) continue;
            if (!root.TryGetChild("services", out var servicesNode)) continue;

            if (servicesNode is not YamlMappingNode services)
                throw new ComposeReadException("Top-level 'services' key must be a mapping of service definitions.");

            foreach (var (keyNode, valueNode) in services.Children) {
                if (keyNode is not YamlScalarNode keyScalar) continue;
                if (valueNode is not YamlMappingNode serviceMapping)
                    throw new ComposeReadException($"Service '{keyScalar.Value}': expected a mapping, got {valueNode.GetType().Name}.");
                result.Add(MapService(keyScalar.Value, serviceMapping));
            }
        }

        return result;
    }

    private static Service MapService(string name, YamlMappingNode mapping) {
        var image = mapping.GetScalar("image");
        if (image is null)
            throw new ComposeReadException($"Service '{name}': missing required 'image' field.");

        return new Service {
            Name = name,
            Image = image,
            ContainerName = mapping.GetScalar("container_name"),
            Restart = ParseRestart(name, mapping.GetScalar("restart")),
            Ports = ParsePorts(name, mapping),
            Volumes = ParseVolumes(name, mapping),
            Environment = ParseEnvironment(name, mapping),
            Networks = ParseNetworks(name, mapping),
        };
    }

    private static RestartAction? ParseRestart(string serviceName, string? value) {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var policy = value.Trim();
        var colon = policy.IndexOf(':');
        if (colon >= 0) policy = policy[..colon];

        return policy switch {
            "always" => RestartAction.Always,
            "on-failure" => RestartAction.OnFailure,
            "unless-stopped" => RestartAction.UnlessStopped,
            "no" => null,
            _ => throw new ComposeReadException($"Service '{serviceName}': unknown restart policy '{value}'."),
        };
    }

    private static PortForward[] ParsePorts(string serviceName, YamlMappingNode mapping) {
        if (!mapping.TryGetChild("ports", out var node)) return [];
        if (node is not YamlSequenceNode sequence)
            throw new ComposeReadException($"Service '{serviceName}': 'ports' must be a sequence.");

        var ports = new PortForward[sequence.Children.Count];
        for (var i = 0; i < sequence.Children.Count; i++)
            ports[i] = ParsePort(serviceName, i, sequence.Children[i]);
        return ports;
    }

    private static PortForward ParsePort(string serviceName, int index, YamlNode item) {
        return item switch {
            YamlScalarNode scalar => ParseShortPort(serviceName, index, scalar.Value),
            YamlMappingNode map => ParseLongPort(serviceName, index, map),
            _ => throw new ComposeReadException($"Service '{serviceName}': port #{index} has unsupported shape ({item.GetType().Name})."),
        };
    }

    private static PortForward ParseShortPort(string serviceName, int index, string value) {
        var raw = value.Trim();
        if (raw.Length == 0)
            throw new ComposeReadException($"Service '{serviceName}': port #{index} is empty.");

        string? protocol = null;
        var slash = raw.LastIndexOf('/');
        if (slash >= 0) {
            protocol = raw[(slash + 1)..];
            raw = raw[..slash];
        }

        var parts = raw.Split(':', StringSplitOptions.RemoveEmptyEntries);
        string containerPort, hostPort;
        string? hostAddress = null;

        switch (parts.Length) {
            case 1:
                containerPort = parts[0];
                hostPort = parts[0];
                break;
            case 2:
                hostPort = parts[0];
                containerPort = parts[1];
                break;
            case 3:
                hostAddress = parts[0];
                hostPort = parts[1];
                containerPort = parts[2];
                break;
            default:
                throw new ComposeReadException($"Service '{serviceName}': port #{index} '{value}' has an unexpected format.");
        }

        return new PortForward {
            ContainerPort = containerPort,
            HostPort = hostPort,
            HostAddress = hostAddress,
            Protocol = protocol,
        };
    }

    private static PortForward ParseLongPort(string serviceName, int index, YamlMappingNode map) {
        var target = map.GetScalar("target");
        if (string.IsNullOrEmpty(target))
            throw new ComposeReadException($"Service '{serviceName}': port #{index} long form is missing 'target'.");

        var published = map.GetScalar("published");
        var protocol = map.GetScalar("protocol");
        var hostIp = map.GetScalar("host_ip");

        return new PortForward {
            ContainerPort = target,
            HostPort = published ?? target,
            HostAddress = hostIp,
            Protocol = protocol,
        };
    }

    private static string[] ParseVolumes(string serviceName, YamlMappingNode mapping) {
        if (!mapping.TryGetChild("volumes", out var node)) return [];
        if (node is not YamlSequenceNode sequence)
            throw new ComposeReadException($"Service '{serviceName}': 'volumes' must be a sequence.");

        var volumes = new string[sequence.Children.Count];
        for (var i = 0; i < sequence.Children.Count; i++)
            volumes[i] = sequence.Children[i] switch {
                YamlScalarNode scalar => scalar.Value,
                var other => throw new ComposeReadException($"Service '{serviceName}': volume #{i} has unsupported shape ({other.GetType().Name}); long-form volume objects are not yet supported."),
            };
        return volumes;
    }

    private static Dictionary<string, string?> ParseEnvironment(string serviceName, YamlMappingNode mapping) {
        if (!mapping.TryGetChild("environment", out var node)) return [];

        switch (node) {
            case YamlSequenceNode sequence: {
                    var env = new Dictionary<string, string?>(sequence.Children.Count);
                    var i = 0;
                    foreach (var item in sequence.Children) {
                        if (item is not YamlScalarNode scalar)
                            throw new ComposeReadException($"Service '{serviceName}': environment #{i} must be a scalar string ({item.GetType().Name}).");
                        var (key, value) = ParseEnvEntry(serviceName, i, scalar.Value);
                        env[key] = value;
                        i++;
                    }
                    return env;
                }
            case YamlMappingNode map: {
                    var env = new Dictionary<string, string?>(map.Children.Count);
                    foreach (var (key, val) in map.Children) {
                        if (key is not YamlScalarNode keyScalar)
                            throw new ComposeReadException($"Service '{serviceName}': environment keys must be scalar.");
                        env[keyScalar.Value] = val switch {
                            null => null,
                            YamlScalarNode valueScalar => valueScalar.Value,
                            _ => throw new ComposeReadException($"Service '{serviceName}': environment value for '{keyScalar.Value}' must be a scalar."),
                        };
                    }
                    return env;
                }
            default:
                throw new ComposeReadException($"Service '{serviceName}': 'environment' must be a sequence or a mapping.");
        }
    }

    private static (string Key, string? Value) ParseEnvEntry(string serviceName, int index, string entry) {
        if (entry.Length == 0)
            throw new ComposeReadException($"Service '{serviceName}': environment #{index} is empty.");
        var eq = entry.IndexOf('=');
        if (eq < 0) return (entry, null);
        if (eq == 0)
            throw new ComposeReadException($"Service '{serviceName}': environment #{index} '{entry}' is missing a key.");
        return (entry[..eq], entry[(eq + 1)..]);
    }

    private static string[] ParseNetworks(string serviceName, YamlMappingNode mapping) {
        if (!mapping.TryGetChild("networks", out var node)) return [];

        switch (node) {
            case YamlSequenceNode sequence: {
                    var networks = new string[sequence.Children.Count];
                    for (var i = 0; i < sequence.Children.Count; i++)
                        networks[i] = sequence.Children[i] switch {
                            YamlScalarNode scalar => scalar.Value,
                            var other => throw new ComposeReadException($"Service '{serviceName}': network #{i} has unsupported shape ({other.GetType().Name})."),
                        };
                    return networks;
                }
            case YamlMappingNode map: {
                    var networks = new string[map.Children.Count];
                    var i = 0;
                    foreach (var (key, _) in map.Children) {
                        if (key is not YamlScalarNode keyScalar)
                            throw new ComposeReadException($"Service '{serviceName}': network #{i} must be a scalar.");
                        networks[i++] = keyScalar.Value;
                    }
                    return networks;
                }
            default:
                throw new ComposeReadException($"Service '{serviceName}': 'networks' must be a sequence or a mapping.");
        }
    }

}
