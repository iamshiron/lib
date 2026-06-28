using System.Diagnostics.CodeAnalysis;
using YamlDotNet.RepresentationModel;

namespace Shiron.Lib.DockerUtils;

internal static class YamlMappingExtensions {
    public static bool TryGetChild(
        this YamlMappingNode mapping,
        string key,
        [NotNullWhen(true)] out YamlNode? child
    ) {
        foreach (var (k, v) in mapping.Children) {
            if (k is YamlScalarNode s && s.Value == key) {
                child = v;
                return true;
            }
        }
        child = null;
        return false;
    }

    public static string? GetScalar(this YamlMappingNode mapping, string key) {
        if (!mapping.TryGetChild(key, out var child) || child is not YamlScalarNode scalar) return null;
        return scalar.Value;
    }
}
