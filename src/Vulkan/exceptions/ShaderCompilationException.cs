namespace Shiron.Lib.Vulkan.Exceptions;

public class ShaderCompilationException(string message, string file) : Exception(message) {
    public readonly string File = file;

    public override string ToString() {
        return $"ShaderCompilationException: {Message}\nFile: {File}";
    }
}
