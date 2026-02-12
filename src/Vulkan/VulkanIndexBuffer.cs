using System.Numerics;
using Silk.NET.Vulkan;

namespace Shiron.Lib.Vulkan;

public class VulkanIndexBuffer<T> : VulkanBuffer where T : unmanaged, INumber<T> {
    public IndexType IndexType { get; }

    public unsafe VulkanIndexBuffer(Vk vk, Device device, PhysicalDevice physicalDevice, T[] indices)
        : base(vk, device, physicalDevice, (ulong) (indices.Length * sizeof(T)), BufferUsageFlags.IndexBufferBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit) {

        IndexType = sizeof(T) switch {
            2 => IndexType.Uint16,
            4 => IndexType.Uint32,
            _ => throw new NotSupportedException($"Index type {typeof(T)} is not supported.")
        };

        SetData(indices);
    }

    public void Bind(CommandBuffer cmd) {
        _vk.CmdBindIndexBuffer(cmd, Handle, 0, IndexType);
    }
}
