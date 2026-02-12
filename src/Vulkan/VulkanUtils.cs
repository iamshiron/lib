
using Silk.NET.Vulkan;

namespace Shiron.Lib.Vulkan;

public static class VulkanUtils {
    public static uint FindMemoryType(in Vk vk, PhysicalDevice device, uint typeFilter, MemoryPropertyFlags properties) {
        vk.GetPhysicalDeviceMemoryProperties(device, out var memProperties);

        for (int i = 0; i < memProperties.MemoryTypeCount; i++) {
            if ((typeFilter & (1 << i)) != 0 &&
                (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties) {
                return (uint) i;
            }
        }

        throw new Exception("Failed to find suitable memory type!");
    }
}
