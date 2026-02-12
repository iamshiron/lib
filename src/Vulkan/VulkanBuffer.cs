using System.Numerics;
using Shiron.Lib.Vulkan.Exceptions;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Shiron.Lib.Vulkan;

/// <summary>
/// A generic wrapper for Vulkan Buffers that handles memory allocation and disposal.
/// </summary>
public unsafe class VulkanBuffer : IDisposable {
    protected readonly Vk _vk;
    protected readonly Device _device;

    public Buffer Handle;
    public DeviceMemory Memory;
    public ulong Size { get; }

    public VulkanBuffer(Vk vk, Device device, PhysicalDevice physicalDevice, ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties) {
        _vk = vk;
        _device = device;
        Size = size;

        var bufferInfo = new BufferCreateInfo {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive
        };

        if (vk.CreateBuffer(device, in bufferInfo, null, out Handle) != Result.Success) {
            throw new VulkanException("Failed to create buffer.", Result.ErrorUnknown);
        }

        vk.GetBufferMemoryRequirements(device, Handle, out var memReqs);
        var allocInfo = new MemoryAllocateInfo {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memReqs.Size,
            MemoryTypeIndex = VulkanUtils.FindMemoryType(vk, physicalDevice, memReqs.MemoryTypeBits, properties)
        };

        if (vk.AllocateMemory(device, in allocInfo, null, out Memory) != Result.Success) {
            throw new VulkanException("Failed to allocate buffer memory.", Result.ErrorUnknown);
        }

        // 4. Bind Memory to Buffer
        if (vk.BindBufferMemory(device, Handle, Memory, 0) != Result.Success) {
            throw new VulkanException("Failed to bind buffer memory.", Result.ErrorUnknown);
        }
    }

    /// <summary>
    /// Maps memory and copies data from the CPU to the GPU (Host Visible memory only).
    /// </summary>
    public void SetData<T>(T[] data) where T : unmanaged {
        void* mappedData;
        var res = _vk.MapMemory(_device, Memory, 0, Size, 0, &mappedData);
        if (res != Result.Success) {
            throw new VulkanException("Failed to map memory.", res);
        }

        data.AsSpan().CopyTo(new Span<T>(mappedData, data.Length));
        _vk.UnmapMemory(_device, Memory);
    }

    public virtual void Dispose() {
        _vk.DestroyBuffer(_device, Handle, null);
        _vk.FreeMemory(_device, Memory, null);
    }
}
