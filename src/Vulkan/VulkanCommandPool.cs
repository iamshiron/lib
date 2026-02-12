using Shiron.Lib.Vulkan.Exceptions;
using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Shiron.Lib.Vulkan;

/// <summary>
/// Manages the memory pool from which CommandBuffers are allocated.
/// </summary>
public unsafe class VulkanCommandPool : IDisposable {
    private readonly Vk _vk;
    private readonly Device _device;

    public CommandPool Handle;

    public VulkanCommandPool(Vk vk, Device device, uint queueFamilyIndex, CommandPoolCreateFlags flags = CommandPoolCreateFlags.ResetCommandBufferBit) {
        _vk = vk;
        _device = device;

        var poolInfo = new CommandPoolCreateInfo {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = queueFamilyIndex,
            Flags = flags
        };

        if (_vk.CreateCommandPool(_device, in poolInfo, null, out Handle) != Result.Success) {
            throw new VulkanException("Failed to create command pool.", Result.ErrorUnknown);
        }
    }

    /// <summary>
    /// Factory method to create a single Primary CommandBuffer from this pool.
    /// </summary>
    public VulkanCommandBuffer AllocateCommandBuffer(CommandBufferLevel level = CommandBufferLevel.Primary) {
        return new VulkanCommandBuffer(_vk, _device, this, level);
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
        _vk.DestroyCommandPool(_device, Handle, null);
    }
}
