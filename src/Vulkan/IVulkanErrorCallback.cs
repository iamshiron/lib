using Silk.NET.Vulkan;

namespace Shiron.Lib.Vulkan;

public unsafe interface IVulkanErrorCallback {
    void OnError(DebugUtilsMessageSeverityFlagsEXT severity, DebugUtilsMessageTypeFlagsEXT types, DebugUtilsMessengerCallbackDataEXT* data);
}
