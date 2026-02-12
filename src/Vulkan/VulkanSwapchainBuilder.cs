using System;
using System.Linq;
using Shiron.Lib.Vulkan.Exceptions;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Shiron.Lib.Vulkan;

/// <summary>
/// Builder class for creating a Vulkan swapchain with customizable configuration.
/// </summary>
public sealed unsafe class VulkanSwapchainBuilder {
    private readonly Vk _vk;
    private readonly Device _device;
    private readonly PhysicalDevice _physicalDevice;
    private readonly SurfaceKHR _surface;
    private readonly KhrSurface _khrSurface;
    private readonly KhrSwapchain _khrSwapchain;

    // Configuration
    private Vector2D<uint>? _desiredExtent;
    private Format _desiredFormat = Format.B8G8R8A8Srgb;
    private ColorSpaceKHR _desiredColorSpace = ColorSpaceKHR.SpaceSrgbNonlinearKhr;
    private PresentModeKHR _desiredPresentMode = PresentModeKHR.MailboxKhr;
    private uint _desiredImageCount = 3; // Triple buffering
    private ImageUsageFlags _imageUsage = ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferDstBit;
    private CompositeAlphaFlagsKHR _compositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr;
    private bool _clipped = true;
    private SwapchainKHR _oldSwapchain;
    private readonly uint _graphicsQueueFamilyIndex;
    private readonly uint _presentQueueFamilyIndex;

    // Cached surface info
    private SurfaceCapabilitiesKHR _surfaceCapabilities;
    private SurfaceFormatKHR[] _surfaceFormats = [];
    private PresentModeKHR[] _presentModes = [];

    public VulkanSwapchainBuilder(
        Vk vk,
        Device device,
        PhysicalDevice physicalDevice,
        SurfaceKHR surface,
        KhrSurface khrSurface,
        uint graphicsQueueFamilyIndex,
        uint presentQueueFamilyIndex) {
        _vk = vk ?? throw new ArgumentNullException(nameof(vk));
        if (device.Handle == 0) {
            throw new ArgumentException("Invalid device.", nameof(device));
        }
        if (physicalDevice.Handle == 0) {
            throw new ArgumentException("Invalid physical device.", nameof(physicalDevice));
        }
        if (surface.Handle == 0) {
            throw new ArgumentException("Invalid surface.", nameof(surface));
        }

        _device = device;
        _physicalDevice = physicalDevice;
        _surface = surface;
        _khrSurface = khrSurface ?? throw new ArgumentNullException(nameof(khrSurface));
        _graphicsQueueFamilyIndex = graphicsQueueFamilyIndex;
        _presentQueueFamilyIndex = presentQueueFamilyIndex;

        if (!_vk.TryGetDeviceExtension(_vk.CurrentInstance!.Value, _device, out _khrSwapchain)) {
            throw new VulkanException("Failed to get KHR_swapchain extension.", Result.ErrorExtensionNotPresent);
        }

        QuerySurfaceSupport();
    }

    // Expose surface info for external use
    public SurfaceCapabilitiesKHR SurfaceCapabilities => _surfaceCapabilities;
    public SurfaceFormatKHR[] SurfaceFormats => _surfaceFormats;
    public PresentModeKHR[] PresentModes => _presentModes;

    /// <summary>
    /// Set the desired swapchain extent. If not set, uses the surface's current extent.
    /// </summary>
    public VulkanSwapchainBuilder WithExtent(uint width, uint height) {
        _desiredExtent = new Vector2D<uint>(width, height);
        return this;
    }

    /// <summary>
    /// Set the desired swapchain extent from window size.
    /// </summary>
    public VulkanSwapchainBuilder WithExtent(Vector2D<int> windowSize) {
        _desiredExtent = new Vector2D<uint>((uint) windowSize.X, (uint) windowSize.Y);
        return this;
    }

    /// <summary>
    /// Set the desired surface format and color space.
    /// </summary>
    public VulkanSwapchainBuilder WithFormat(Format format, ColorSpaceKHR colorSpace = ColorSpaceKHR.SpaceSrgbNonlinearKhr) {
        _desiredFormat = format;
        _desiredColorSpace = colorSpace;
        return this;
    }

    /// <summary>
    /// Set the desired present mode. Defaults to Mailbox (triple buffering) if available.
    /// </summary>
    public VulkanSwapchainBuilder WithPresentMode(PresentModeKHR presentMode) {
        _desiredPresentMode = presentMode;
        return this;
    }

    /// <summary>
    /// Set the desired number of swapchain images. Defaults to 3 (triple buffering).
    /// </summary>
    public VulkanSwapchainBuilder WithImageCount(uint count) {
        _desiredImageCount = count;
        return this;
    }

    /// <summary>
    /// Set image usage flags. Defaults to ColorAttachment.
    /// </summary>
    public VulkanSwapchainBuilder WithImageUsage(ImageUsageFlags usage) {
        _imageUsage = usage;
        return this;
    }

    /// <summary>
    /// Set composite alpha mode. Defaults to Opaque.
    /// </summary>
    public VulkanSwapchainBuilder WithCompositeAlpha(CompositeAlphaFlagsKHR compositeAlpha) {
        _compositeAlpha = compositeAlpha;
        return this;
    }

    /// <summary>
    /// Set whether pixels obscured by other windows can be discarded. Defaults to true.
    /// </summary>
    public VulkanSwapchainBuilder WithClipping(bool clipped) {
        _clipped = clipped;
        return this;
    }

    /// <summary>
    /// Set the old swapchain for recreation (allows for smoother transitions).
    /// </summary>
    public VulkanSwapchainBuilder WithOldSwapchain(SwapchainKHR oldSwapchain) {
        _oldSwapchain = oldSwapchain;
        return this;
    }

    /// <summary>
    /// Build the swapchain with the specified configuration.
    /// Chooses format, present mode, extent, and image count based on device capabilities.
    /// </summary>
    public VulkanSwapchain Build() {
        // Re-query surface capabilities (may have changed since construction)
        QuerySurfaceSupport();

        // Choose best surface format
        var surfaceFormat = ChooseSurfaceFormat();
        var swapchainFormat = surfaceFormat.Format;

        // Choose best present mode
        var presentMode = ChoosePresentMode();

        // Choose extent
        var swapchainExtent = ChooseExtent();

        // Determine image count
        var imageCount = _desiredImageCount;
        if (imageCount < _surfaceCapabilities.MinImageCount) {
            imageCount = _surfaceCapabilities.MinImageCount;
        }
        if (_surfaceCapabilities.MaxImageCount > 0 && imageCount > _surfaceCapabilities.MaxImageCount) {
            imageCount = _surfaceCapabilities.MaxImageCount;
        }

        // Create swapchain
        var createInfo = new SwapchainCreateInfoKHR {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = _surface,
            MinImageCount = imageCount,
            ImageFormat = swapchainFormat,
            ImageColorSpace = surfaceFormat.ColorSpace,
            ImageExtent = swapchainExtent,
            ImageArrayLayers = 1,
            ImageUsage = _imageUsage,
            PreTransform = _surfaceCapabilities.CurrentTransform,
            CompositeAlpha = _compositeAlpha,
            PresentMode = presentMode,
            Clipped = _clipped,
            OldSwapchain = _oldSwapchain
        };

        if (_graphicsQueueFamilyIndex != _presentQueueFamilyIndex) {
            var queueFamilyIndices = stackalloc uint[] { _graphicsQueueFamilyIndex, _presentQueueFamilyIndex };
            createInfo.ImageSharingMode = SharingMode.Concurrent;
            createInfo.QueueFamilyIndexCount = 2;
            createInfo.PQueueFamilyIndices = queueFamilyIndices;
        } else {
            createInfo.ImageSharingMode = SharingMode.Exclusive;
            createInfo.QueueFamilyIndexCount = 0;
            createInfo.PQueueFamilyIndices = null;
        }

        var result = _khrSwapchain.CreateSwapchain(_device, &createInfo, null, out var swapchain);
        if (result != Result.Success) {
            throw new VulkanException("Failed to create swapchain. " + result, result);
        }

        // Retrieve swapchain images
        var swapchainImages = RetrieveSwapchainImages(swapchain);

        // Create image views
        var swapchainImageViews = CreateImageViews(swapchainImages, swapchainFormat);

        return new VulkanSwapchain(
            _vk,
            _device,
            _khrSwapchain,
            swapchain,
            swapchainImages,
            swapchainImageViews,
            swapchainFormat,
            swapchainExtent);
    }

    private void QuerySurfaceSupport() {
        // Get surface capabilities
        _khrSurface.GetPhysicalDeviceSurfaceCapabilities(_physicalDevice, _surface, out _surfaceCapabilities);

        // Get surface formats
        uint formatCount = 0;
        _khrSurface.GetPhysicalDeviceSurfaceFormats(_physicalDevice, _surface, &formatCount, null);
        if (formatCount > 0) {
            _surfaceFormats = new SurfaceFormatKHR[formatCount];
            fixed (SurfaceFormatKHR* pFormats = _surfaceFormats) {
                _khrSurface.GetPhysicalDeviceSurfaceFormats(_physicalDevice, _surface, &formatCount, pFormats);
            }
        }

        // Get present modes
        uint presentModeCount = 0;
        _khrSurface.GetPhysicalDeviceSurfacePresentModes(_physicalDevice, _surface, &presentModeCount, null);
        if (presentModeCount > 0) {
            _presentModes = new PresentModeKHR[presentModeCount];
            fixed (PresentModeKHR* pModes = _presentModes) {
                _khrSurface.GetPhysicalDeviceSurfacePresentModes(_physicalDevice, _surface, &presentModeCount, pModes);
            }
        }
    }

    private SurfaceFormatKHR ChooseSurfaceFormat() {
        // Look for desired format
        foreach (var format in _surfaceFormats) {
            if (format.Format == _desiredFormat && format.ColorSpace == _desiredColorSpace) {
                return format;
            }
        }

        // Look for any SRGB format
        foreach (var format in _surfaceFormats) {
            if (format.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr) {
                return format;
            }
        }

        // Return first available
        return _surfaceFormats[0];
    }

    private PresentModeKHR ChoosePresentMode() {
        // Look for desired present mode
        if (_presentModes.Contains(_desiredPresentMode)) {
            return _desiredPresentMode;
        }

        // Mailbox (triple buffering) is preferred
        if (_presentModes.Contains(PresentModeKHR.MailboxKhr)) {
            return PresentModeKHR.MailboxKhr;
        }

        // FIFO is guaranteed to be available
        return PresentModeKHR.FifoKhr;
    }

    private Extent2D ChooseExtent() {
        // If the surface extent is defined (not uint.MaxValue), use it
        if (_surfaceCapabilities.CurrentExtent.Width != uint.MaxValue) {
            return _surfaceCapabilities.CurrentExtent;
        }

        // Otherwise, use desired extent clamped to surface limits
        if (_desiredExtent.HasValue) {
            return new Extent2D {
                Width = Math.Clamp(_desiredExtent.Value.X,
                    _surfaceCapabilities.MinImageExtent.Width,
                    _surfaceCapabilities.MaxImageExtent.Width),
                Height = Math.Clamp(_desiredExtent.Value.Y,
                    _surfaceCapabilities.MinImageExtent.Height,
                    _surfaceCapabilities.MaxImageExtent.Height)
            };
        }

        // Fallback to min extent
        return _surfaceCapabilities.MinImageExtent;
    }

    private Image[] RetrieveSwapchainImages(SwapchainKHR swapchain) {
        uint imageCount = 0;
        _khrSwapchain.GetSwapchainImages(_device, swapchain, &imageCount, null);

        var swapchainImages = new Image[imageCount];
        fixed (Image* pImages = swapchainImages) {
            _khrSwapchain.GetSwapchainImages(_device, swapchain, &imageCount, pImages);
        }

        return swapchainImages;
    }

    private ImageView[] CreateImageViews(Image[] swapchainImages, Format swapchainFormat) {
        var swapchainImageViews = new ImageView[swapchainImages.Length];

        for (var i = 0; i < swapchainImages.Length; i++) {
            var createInfo = new ImageViewCreateInfo {
                SType = StructureType.ImageViewCreateInfo,
                Image = swapchainImages[i],
                ViewType = ImageViewType.Type2D,
                Format = swapchainFormat,
                Components = new ComponentMapping {
                    R = ComponentSwizzle.Identity,
                    G = ComponentSwizzle.Identity,
                    B = ComponentSwizzle.Identity,
                    A = ComponentSwizzle.Identity
                },
                SubresourceRange = new ImageSubresourceRange {
                    AspectMask = ImageAspectFlags.ColorBit,
                    BaseMipLevel = 0,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1
                }
            };

            var result = _vk.CreateImageView(_device, &createInfo, null, out swapchainImageViews[i]);
            if (result != Result.Success) {
                throw new VulkanException($"Failed to create image view {i}. " + result, result);
            }
        }

        return swapchainImageViews;
    }

    /// <summary>
    /// Get a description of the chosen present mode.
    /// </summary>
    public static string GetPresentModeDescription(PresentModeKHR mode) {
        return mode switch {
            PresentModeKHR.ImmediateKhr => "Immediate (no vsync, may tear)",
            PresentModeKHR.MailboxKhr => "Mailbox (triple buffering, no tearing)",
            PresentModeKHR.FifoKhr => "FIFO (vsync, double buffering)",
            PresentModeKHR.FifoRelaxedKhr => "FIFO Relaxed (vsync with late frame allowance)",
            PresentModeKHR.SharedDemandRefreshKhr => "Shared Demand Refresh",
            PresentModeKHR.SharedContinuousRefreshKhr => "Shared Continuous Refresh",
            _ => mode.ToString()
        };
    }
}
