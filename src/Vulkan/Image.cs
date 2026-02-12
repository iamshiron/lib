using Silk.NET.Vulkan;

namespace Shiron.Lib.Vulkan;

/// <summary>
/// Encapsulates a Vulkan Image, its memory, and its View.
/// </summary>
public unsafe class VulkanImage : IDisposable {
    private readonly Vk _vk;
    private readonly Device _device;
    private readonly PhysicalDevice _physicalDevice;
    public Image Handle { get; private set; }
    public DeviceMemory Memory { get; private set; }
    public ImageView View { get; private set; }
    public Format Format { get; private set; }
    public Extent2D Extent { get; private set; }
    public uint MipLevels { get; private set; }

    /// <summary>
    /// Indicates whether this instance owns the image and is responsible for disposing it.
    /// </summary>
    private readonly bool _ownsImage;

    public VulkanImage(
        Vk vk,
        Device device,
        PhysicalDevice physicalDevice,
        uint width,
        uint height,
        uint arrayLayers,
        uint mipLevels,
        Format format,
        ImageUsageFlags usage,
        MemoryPropertyFlags properties,
        ImageAspectFlags? aspectFlags = null) {
        _ownsImage = true;

        _vk = vk;
        _device = device;
        _physicalDevice = physicalDevice;
        Format = format;
        Extent = new Extent2D(width, height);
        MipLevels = mipLevels;
        CreateImage(width, height, mipLevels, arrayLayers, format, usage, properties, out var image, out var memory);
        Handle = image;
        Memory = memory;

        var aspect = aspectFlags ?? (format.ToString().Contains("D32") || format.ToString().Contains("D16")
                ? ImageAspectFlags.DepthBit
                : ImageAspectFlags.ColorBit
            );
        var viewType = arrayLayers > 1 ? ImageViewType.Type2DArray : ImageViewType.Type2D;

        View = CreateImageView(Handle, viewType, format, aspect, arrayLayers, mipLevels);
    }

    public static VulkanImage CreateWrapper(Vk vk, Device device, Image existingHandle, Format format, uint width, uint height) {
        return new VulkanImage(vk, device, existingHandle, format, width, height);
    }
    private VulkanImage(Vk vk, Device device, Image existingHandle, Format format, uint width, uint height) {
        _vk = vk;
        _device = device;
        Handle = existingHandle;
        Format = format;
        Extent = new Extent2D(width, height);
        Memory = default; // External images don't have managed memory handles we care about
        _ownsImage = false; // Important: We do NOT own this handle

        // We MUST create a view to render to it
        View = CreateImageView(Handle, ImageViewType.Type2D, format, ImageAspectFlags.ColorBit, 1, 1);
    }

    private void CreateImage(
        uint width,
        uint height,
        uint arrayLayers,
        uint mipLevels,
        Format format,
        ImageUsageFlags usage,
        MemoryPropertyFlags properties,
        out Image image,
        out DeviceMemory memory) {
        var imageInfo = new ImageCreateInfo {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Extent = new Extent3D(width, height, 1),
            MipLevels = mipLevels,
            ArrayLayers = arrayLayers,
            Format = format,
            Tiling = ImageTiling.Optimal,
            InitialLayout = ImageLayout.Undefined,
            Usage = usage,
            Samples = SampleCountFlags.Count1Bit,
            SharingMode = SharingMode.Exclusive
        };
        if (_vk.CreateImage(_device, &imageInfo, null, out image) != Result.Success) {
            throw new Exception("failed to create image!");
        }
        _vk.GetImageMemoryRequirements(_device, image, out var memRequirements);
        var allocInfo = new MemoryAllocateInfo {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties)
        };
        if (_vk.AllocateMemory(_device, &allocInfo, null, out memory) != Result.Success) {
            throw new Exception("failed to allocate image memory!");
        }
        _vk.BindImageMemory(_device, image, memory, 0);
    }

    private ImageView CreateImageView(Image image, ImageViewType viewType, Format format, ImageAspectFlags aspectFlags, uint layerCount, uint mipLevels) {
        var viewInfo = new ImageViewCreateInfo {
            SType = StructureType.ImageViewCreateInfo,
            Image = image,
            ViewType = viewType,
            Format = format,
            SubresourceRange = new ImageSubresourceRange(aspectFlags, 0, mipLevels, 0, layerCount)
        };
        if (_vk.CreateImageView(_device, &viewInfo, null, out var imageView) != Result.Success) {
            throw new Exception("failed to create image view!");
        }
        return imageView;
    }

    private uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties) {
        _vk.GetPhysicalDeviceMemoryProperties(_physicalDevice, out var memProperties);
        for (var i = 0; i < memProperties.MemoryTypeCount; i++) {
            if ((typeFilter & 1 << i) != 0 &&
                (memProperties.MemoryTypes[i].PropertyFlags & properties) == properties) {
                return (uint) i;
            }
        }
        throw new Exception("failed to find suitable memory type!");
    }

    public void Dispose() {
        if (View.Handle != 0) {
            _vk.DestroyImageView(_device, View, null);
        }

        if (_ownsImage) {
            if (Handle.Handle != 0) {
                _vk.DestroyImage(_device, Handle, null);
            }
            if (Memory.Handle != 0) {
                _vk.FreeMemory(_device, Memory, null);
            }
        }
    }
}
