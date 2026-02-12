using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Shiron.Lib.Vulkan;

public class VulkanVertexBuffer<T> : VulkanBuffer where T : unmanaged {
    public unsafe VulkanVertexBuffer(Vk vk, Device device, PhysicalDevice physicalDevice, T[] vertices)
        : base(vk, device, physicalDevice, (ulong) (vertices.Length * sizeof(T)), BufferUsageFlags.VertexBufferBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit) {

        SetData(vertices);
    }

    // Bind requires a CommandBuffer to record the action into
    public unsafe void Bind(CommandBuffer cmd) {
        var buffers = new[] { Handle };
        ulong[] offsets = [0];

        fixed (ulong* offsetsPtr = offsets)
        fixed (Buffer* bufPtr = buffers) {
            _vk.CmdBindVertexBuffers(cmd, 0, 1, bufPtr, offsetsPtr);
        }
    }
}
