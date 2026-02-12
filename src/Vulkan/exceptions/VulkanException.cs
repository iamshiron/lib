using Silk.NET.Vulkan;

namespace Shiron.Lib.Vulkan.Exceptions;

public sealed class VulkanException(string message, Result result) : Exception(message) {
    public Result Result { get; } = result;
}
