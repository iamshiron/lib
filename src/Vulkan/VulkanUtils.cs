using Silk.NET.Vulkan;

namespace Shiron.Lib.Vulkan;

public static class VulkanUtils {
    public static uint FindMemoryType(in Vk vk, PhysicalDevice device, uint typeFilter, MemoryPropertyFlags properties) {
        vk.GetPhysicalDeviceMemoryProperties(device, out var memProperties);

        for (var i = 0; i < memProperties.MemoryTypeCount; i++) {
            if ((typeFilter & 1 << i) != 0 &&
                (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties) {
                return (uint) i;
            }
        }

        throw new Exception("Failed to find suitable memory type!");
    }

    public static Format FindSupportedFormat(Vk vk, PhysicalDevice physicalDevice, Format[] candidates, ImageTiling tiling, FormatFeatureFlags features) {
        foreach (var format in candidates) {
            vk.GetPhysicalDeviceFormatProperties(physicalDevice, format, out var props);

            if (tiling == ImageTiling.Linear && (props.LinearTilingFeatures & features) == features) {
                return format;
            }
            if (tiling == ImageTiling.Optimal && (props.OptimalTilingFeatures & features) == features) {
                return format;
            }
        }
        throw new Exception("Failed to find supported format!");
    }

    public static Format FindDepthFormat(Vk vk, PhysicalDevice physicalDevice) {
        Format[] candidates = {
            Format.D32Sfloat,
            Format.D32SfloatS8Uint,
            Format.D24UnormS8Uint
        };

        return FindSupportedFormat(
            vk, physicalDevice,
            candidates,
            ImageTiling.Optimal,
            FormatFeatureFlags.DepthStencilAttachmentBit
        );
    }
}
