using System;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Shiron.Lib.Vulkan;

/// <summary>
/// Immutable Vulkan swapchain object for presenting rendered images.
/// </summary>
public sealed unsafe class VulkanSwapchain : IDisposable {
    private readonly Vk _vk;
    private readonly Device _device;
    private readonly KhrSwapchain _khrSwapchain;

    public SwapchainKHR Swapchain { get; }
    public VulkanImage[] Images { get; }
    public ImageView[] ImageViews { get; }
    public Format ImageFormat { get; }
    public Extent2D Extent { get; }
    public uint ImageCount => (uint) Images.Length;

    internal VulkanSwapchain(
        Vk vk,
        Device device,
        KhrSwapchain khrSwapchain,
        SwapchainKHR swapchain,
        Image[] images,
        ImageView[] imageViews,
        Format imageFormat,
        Extent2D extent) {
        _vk = vk ?? throw new ArgumentNullException(nameof(vk));
        _device = device;
        _khrSwapchain = khrSwapchain ?? throw new ArgumentNullException(nameof(khrSwapchain));

        Swapchain = swapchain;
        Images = images.Select(i => VulkanImage.CreateWrapper(_vk, _device, i, imageFormat, extent.Width, extent.Height)).ToArray();
        ImageViews = imageViews ?? throw new ArgumentNullException(nameof(imageViews));
        ImageFormat = imageFormat;
        Extent = extent;
    }

    /// <summary>
    /// Acquire the next image from the swapchain.
    /// </summary>
    public Result AcquireNextImage(Semaphore semaphore, Fence fence, out uint imageIndex) {
        imageIndex = 0;
        return _khrSwapchain.AcquireNextImage(_device, Swapchain, ulong.MaxValue, semaphore, fence, ref imageIndex);
    }

    /// <summary>
    /// Present an image to the screen.
    /// </summary>
    public Result Present(Queue presentQueue, uint imageIndex, Semaphore waitSemaphore) {
        var swapchain = Swapchain;
        var presentInfo = new PresentInfoKHR {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &waitSemaphore,
            SwapchainCount = 1,
            PSwapchains = &swapchain,
            PImageIndices = &imageIndex
        };

        return _khrSwapchain.QueuePresent(presentQueue, &presentInfo);
    }

    public void Dispose() {
        // Destroy image views
        foreach (var imageView in ImageViews) {
            if (imageView.Handle != 0) {
                _vk.DestroyImageView(_device, imageView, null);
            }
        }

        // Destroy swapchain
        if (Swapchain.Handle != 0) {
            _khrSwapchain.DestroySwapchain(_device, Swapchain, null);
        }
    }
}
