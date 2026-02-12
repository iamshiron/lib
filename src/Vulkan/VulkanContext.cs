using System.Runtime.InteropServices;
using Shiron.Phonon.Common.Utils;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;

namespace Shiron.Lib.Vulkan;

public class VulkanContextOptions {
    public required string AppName { get; init; }
    public required string EngineName { get; init; }
    public required SemVer AppVersion { get; init; }
    public required SemVer EngineVersion { get; init; }
    public required IVulkanErrorCallback? ErrorCallback { get; init; }
    public required uint FramesInFlight { get; init; }

    public required bool ComputeFamilyRequired { get; init; }
    public required bool TransferFamilyRequired { get; init; }

    public Action<VulkanSwapchainBuilder>? SwapchainBuilder { get; init; } = null;
    public Action<PhysicalDeviceSelector>? DeviceSelector { get; init; } = null;
    public Action<LogicalDeviceBuilder>? LogicalDeviceBuilder { get; init; } = null;
    public required Action OnSwapchainRecreated { get; init; }
}

public class VulkanContext {
    private KhrSurface _khrSurface;

    public Vk Vk { get; }
    public VulkanInstance Instance { get; }
    public VulkanSwapchain Swapchain { get; private set; }
    public Device Device { get; }
    public PhysicalDevice PhysicalDevice { get; }
    public SurfaceKHR Surface { get; }
    public KhrSurface KhrSurface => _khrSurface;

    public uint GraphicsQueueFamilyIndex { get; }
    public uint PresentQueueFamilyIndex { get; }
    public uint? ComputeQueueFamilyIndex { get; }
    public uint? TransferQueueFamilyIndex { get; }

    public Queue GraphicsQueue { get; }
    public Queue PresentQueue { get; }
    public Queue? ComputeQueue { get; }
    public Queue? TransferQueue { get; }

    public PhysicalDeviceProperties PhysicalDeviceProperties { get; }

    private readonly VulkanContextOptions _options;

    public unsafe VulkanContext(Vk vk, IWindow window, VulkanContextOptions options) {
        _options = options;
        Vk = vk;

        // Build the vulkan instance
        var instanceBuilder = new VulkanInstanceBuilder(vk)
            .WithApp(_options.AppName, _options.AppVersion)
            .WithEngine(_options.EngineName, _options.EngineVersion)
            .WithApiVersion(Vk.Version13)
            .AddExtensions(GetRequiredWindowExtensions(window));

        if (_options.ErrorCallback != null) {
            instanceBuilder.AddExtensions(ExtDebugUtils.ExtensionName)
                .EnableValidationLayers(_options.ErrorCallback);
        }

        Instance = instanceBuilder.Build();
        Surface = window.VkSurface!.Create<AllocationCallbacks>(Instance.Instance.ToHandle(), null).ToSurface();
        if (!vk.TryGetInstanceExtension(Instance.Instance, out _khrSurface)) {
            throw new Exception("Failed to create Vulkan surface.");
        }

        // Select the physical device
        var physicalDeviceSelector = new PhysicalDeviceSelector(Vk, Instance.Instance)
            .PreferDeviceType(PhysicalDeviceType.DiscreteGpu)
            .AddRequiredExtensions(KhrSwapchain.ExtensionName)
            .RequireGraphicsQueue()
            .RequirePresentQueue(Surface, GetPresentSupportFunc(KhrSurface, Surface));

        _options.DeviceSelector?.Invoke(physicalDeviceSelector);
        PhysicalDevice = physicalDeviceSelector.Select();

        // Create the logical device
        var logicalDeviceBuilder = new LogicalDeviceBuilder(vk, physicalDeviceSelector)
            .AddExtensions(KhrSwapchain.ExtensionName)
            .AddGraphicsQueue()
            .AddPresentQueue();

        if (_options.ComputeFamilyRequired) {
            logicalDeviceBuilder.AddComputeQueue();
        }
        if (_options.TransferFamilyRequired) {
            logicalDeviceBuilder.AddTransferQueue();
        }

        _options.LogicalDeviceBuilder?.Invoke(logicalDeviceBuilder);
        Device = logicalDeviceBuilder.Build();
        GraphicsQueue = logicalDeviceBuilder.GetGraphicsQueue();
        PresentQueue = logicalDeviceBuilder.GetPresentQueue();

        if (_options.ComputeFamilyRequired) {
            ComputeQueue = logicalDeviceBuilder.GetComputeQueue();
        }
        if (_options.TransferFamilyRequired) {
            TransferQueue = logicalDeviceBuilder.GetTransferQueue();
        }

        var families = physicalDeviceSelector.QueueFamilies;

        // Fundamental families for rendering
        GraphicsQueueFamilyIndex = families.GraphicsFamily ?? throw new Exception("Graphics queue family not available!");
        PresentQueueFamilyIndex = families.PresentFamily ?? throw new Exception("Present queue family not available!");

        if (!families.ComputeFamily.HasValue && _options.ComputeFamilyRequired) {
            throw new Exception("Compute queue family not available!");
        }
        ComputeQueueFamilyIndex = families.ComputeFamily;

        if (!families.TransferFamily.HasValue && _options.TransferFamilyRequired) {
            throw new Exception("Transfer queue family not available!");
        }
        TransferQueueFamilyIndex = families.TransferFamily;

        PhysicalDeviceProperties = physicalDeviceSelector.Properties;

        Swapchain = CreateSwapchain(window.Size, null);
    }

    private static unsafe string[] GetRequiredWindowExtensions(IWindow window) {
        if (window.VkSurface is null) {
            throw new Exception("Vulkan surface extension not available from window.");
        }

        var windowExtensions = window.VkSurface.GetRequiredExtensions(out var count);
        var extensions = new string[count];
        for (var i = 0; i < count; i++) extensions[i] = Marshal.PtrToStringAnsi((nint) windowExtensions[i]) ?? "";
        return extensions;
    }
    private static Func<PhysicalDevice, uint, bool> GetPresentSupportFunc(KhrSurface khrSurface, SurfaceKHR surface) {
        return (device, queueFamilyIndex) => {
            var res = khrSurface.GetPhysicalDeviceSurfaceSupport(device, queueFamilyIndex, surface, out var supported);
            return res != Result.Success ? throw new Exception("Failed to query physical device surface support.") : (bool) supported;
        };
    }

    private VulkanSwapchain CreateSwapchain(Vector2D<int> size, VulkanSwapchain? oldSwapchain) {
        var builder = new VulkanSwapchainBuilder(Vk, Device, PhysicalDevice, Surface, KhrSurface, GraphicsQueueFamilyIndex, PresentQueueFamilyIndex)
            .WithExtent(size)
            .WithFormat(Format.B8G8R8A8Srgb)
            .WithPresentMode(PresentModeKHR.MailboxKhr)
            .WithImageCount(_options.FramesInFlight);

        if (oldSwapchain != null) {
            builder.WithOldSwapchain(oldSwapchain.Swapchain);
        }

        return builder.Build();
    }

    public void RecreateSwapchain(Vector2D<int> size) {
        Swapchain = CreateSwapchain(size, Swapchain);
        _options.OnSwapchainRecreated.Invoke();
    }
}
