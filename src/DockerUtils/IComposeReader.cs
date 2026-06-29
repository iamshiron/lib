using Shiron.Lib.DockerUtils.Model;

namespace Shiron.Lib.DockerUtils;

/// <summary>
/// Reads Docker Compose files and extracts service definitions into DTOs.
/// </summary>
public interface IComposeReader {
    /// <summary>
    /// Parses a Docker Compose YAML string and returns the extracted services.
    /// </summary>
    /// <param name="composeYaml">The raw Docker Compose YAML content.</param>
    /// <returns>The services defined under the top-level <c>services</c> key, flattened into a uniform shape.</returns>
    /// <exception cref="ComposeReadException">Thrown when the YAML is invalid or contains an unsupported field shape.</exception>
    IReadOnlyList<Service> Read(string composeYaml);
}
