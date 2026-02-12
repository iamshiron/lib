
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Shiron.Lib.Vulkan.Exceptions;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Shiron.Lib.Vulkan;

/// <summary>
/// Utility class for creating a Vulkan logical device with specified queues and features.
/// </summary>
public sealed unsafe class LogicalDeviceBuilder : IDisposable {
    private readonly Vk _vk;
    private readonly PhysicalDevice _physicalDevice;
    private readonly QueueFamilyIndices _queueFamilyIndices;

    private readonly List<string> _extensions = new();
    private readonly Dictionary<uint, List<float>> _queuePriorities = new();
    private PhysicalDeviceFeatures _enabledFeatures;
    private bool _enableAllAvailableFeatures;

    private Device _device;
    private readonly Dictionary<uint, Queue[]> _queues = new();

    private bool _built;

    public LogicalDeviceBuilder(Vk vk, PhysicalDevice physicalDevice, QueueFamilyIndices queueFamilyIndices) {
        _vk = vk ?? throw new ArgumentNullException(nameof(vk));
        if (physicalDevice.Handle == 0)
            throw new ArgumentException("Invalid physical device.", nameof(physicalDevice));
        _physicalDevice = physicalDevice;
        _queueFamilyIndices = queueFamilyIndices;
    }

    /// <summary>
    /// Convenience constructor that takes a PhysicalDeviceSelector directly.
    /// </summary>
    public LogicalDeviceBuilder(Vk vk, PhysicalDeviceSelector selector)
        : this(vk, selector.PhysicalDevice, selector.QueueFamilies) { }

    public Device Device => _device;

    /// <summary>
    /// Add device extensions (e.g. VK_KHR_swapchain).
    /// </summary>
    public LogicalDeviceBuilder AddExtensions(params string[] extensions) {
        foreach (var e in extensions.Where(s => !string.IsNullOrWhiteSpace(s)))
            if (!_extensions.Contains(e)) _extensions.Add(e);
        return this;
    }

    /// <summary>
    /// Add a queue from the graphics family with specified priority (0.0 - 1.0).
    /// </summary>
    public LogicalDeviceBuilder AddGraphicsQueue(float priority = 1.0f) {
        if (_queueFamilyIndices.GraphicsFamily.HasValue)
            AddQueue(_queueFamilyIndices.GraphicsFamily.Value, priority);
        return this;
    }

    /// <summary>
    /// Add a queue from the compute family with specified priority (0.0 - 1.0).
    /// </summary>
    public LogicalDeviceBuilder AddComputeQueue(float priority = 1.0f) {
        if (_queueFamilyIndices.ComputeFamily.HasValue)
            AddQueue(_queueFamilyIndices.ComputeFamily.Value, priority);
        return this;
    }

    /// <summary>
    /// Add a queue from the transfer family with specified priority (0.0 - 1.0).
    /// </summary>
    public LogicalDeviceBuilder AddTransferQueue(float priority = 1.0f) {
        if (_queueFamilyIndices.TransferFamily.HasValue)
            AddQueue(_queueFamilyIndices.TransferFamily.Value, priority);
        return this;
    }

    /// <summary>
    /// Add a queue from the present family with specified priority (0.0 - 1.0).
    /// </summary>
    public LogicalDeviceBuilder AddPresentQueue(float priority = 1.0f) {
        if (_queueFamilyIndices.PresentFamily.HasValue)
            AddQueue(_queueFamilyIndices.PresentFamily.Value, priority);
        return this;
    }

    /// <summary>
    /// Add a queue from a specific queue family index with specified priority.
    /// </summary>
    public LogicalDeviceBuilder AddQueue(uint queueFamilyIndex, float priority = 1.0f) {
        priority = Math.Clamp(priority, 0.0f, 1.0f);
        if (!_queuePriorities.ContainsKey(queueFamilyIndex))
            _queuePriorities[queueFamilyIndex] = new List<float>();
        _queuePriorities[queueFamilyIndex].Add(priority);
        return this;
    }

    /// <summary>
    /// Set specific device features to enable.
    /// </summary>
    public LogicalDeviceBuilder WithFeatures(PhysicalDeviceFeatures features) {
        _enabledFeatures = features;
        return this;
    }

    /// <summary>
    /// Configure device features using a callback.
    /// </summary>
    public LogicalDeviceBuilder ConfigureFeatures(Action<FeatureConfigurator> configure) {
        var configurator = new FeatureConfigurator(_enabledFeatures);
        configure(configurator);
        _enabledFeatures = configurator._features;
        return this;
    }

    /// <summary>
    /// Enable all features supported by the physical device.
    /// </summary>
    public LogicalDeviceBuilder EnableAllAvailableFeatures() {
        _enableAllAvailableFeatures = true;
        return this;
    }

    /// <summary>
    /// Build the logical device.
    /// </summary>
    public Device Build() {
        if (_built) throw new InvalidOperationException("LogicalDeviceBuilder.Build() can only be called once.");
        _built = true;

        // Ensure at least one queue is requested
        if (_queuePriorities.Count == 0) {
            // Default: add one graphics queue if available
            if (_queueFamilyIndices.GraphicsFamily.HasValue)
                AddGraphicsQueue();
            else
                throw new InvalidOperationException("No queues specified and no graphics queue family available.");
        }

        // Get max queue count per family
        uint queueFamilyCount = 0;
        _vk.GetPhysicalDeviceQueueFamilyProperties(_physicalDevice, &queueFamilyCount, null);
        var queueFamilyProps = new QueueFamilyProperties[queueFamilyCount];
        fixed (QueueFamilyProperties* pProps = queueFamilyProps) {
            _vk.GetPhysicalDeviceQueueFamilyProperties(_physicalDevice, &queueFamilyCount, pProps);
        }

        // Build queue create infos
        var queueCreateInfos = new List<DeviceQueueCreateInfo>();
        var priorityHandles = new List<GCHandle>();

        foreach (var (familyIndex, priorities) in _queuePriorities) {
            // Clamp to max available queues in this family
            var maxQueues = (int) queueFamilyProps[familyIndex].QueueCount;
            var actualPriorities = priorities.Take(maxQueues).ToArray();

            var handle = GCHandle.Alloc(actualPriorities, GCHandleType.Pinned);
            priorityHandles.Add(handle);

            queueCreateInfos.Add(new DeviceQueueCreateInfo {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = familyIndex,
                QueueCount = (uint) actualPriorities.Length,
                PQueuePriorities = (float*) handle.AddrOfPinnedObject()
            });
        }

        // Get enabled features
        var enabledFeatures = _enabledFeatures;
        if (_enableAllAvailableFeatures) {
            _vk.GetPhysicalDeviceFeatures(_physicalDevice, out enabledFeatures);
        }

        // Prepare extensions
        var extPtrs = SilkMarshal.StringArrayToPtr(_extensions.ToArray(), NativeStringEncoding.UTF8);

        // Create device
        fixed (DeviceQueueCreateInfo* pQueueCreateInfos = queueCreateInfos.ToArray()) {
            var vulkan13Features = new PhysicalDeviceVulkan13Features {
                SType = StructureType.PhysicalDeviceVulkan13Features,
                Synchronization2 = true,
                DynamicRendering = true
            };

            var createInfo = new DeviceCreateInfo {
                SType = StructureType.DeviceCreateInfo,
                PNext = &vulkan13Features,

                QueueCreateInfoCount = (uint) queueCreateInfos.Count,
                PQueueCreateInfos = pQueueCreateInfos,
                EnabledExtensionCount = (uint) _extensions.Count,
                PpEnabledExtensionNames = (byte**) extPtrs,
                PEnabledFeatures = &enabledFeatures
            };

            var result = _vk.CreateDevice(_physicalDevice, &createInfo, null, out _device);

            // Cleanup
            SilkMarshal.Free(extPtrs);
            foreach (var handle in priorityHandles) handle.Free();

            if (result != Result.Success) {
                throw new VulkanException("Failed to create logical device. " + result, result);
            }
        }

        // Retrieve created queues
        foreach (var (familyIndex, priorities) in _queuePriorities) {
            var maxQueues = (int) queueFamilyProps[familyIndex].QueueCount;
            var actualCount = Math.Min(priorities.Count, maxQueues);
            var queues = new Queue[actualCount];

            for (uint i = 0; i < actualCount; i++) {
                _vk.GetDeviceQueue(_device, familyIndex, i, out queues[i]);
            }

            _queues[familyIndex] = queues;
        }

        return _device;
    }

    /// <summary>
    /// Get the graphics queue (first queue from graphics family).
    /// </summary>
    public Queue GetGraphicsQueue(uint index = 0) {
        return GetQueue(_queueFamilyIndices.GraphicsFamily, index);
    }

    /// <summary>
    /// Get the compute queue (first queue from compute family).
    /// </summary>
    public Queue GetComputeQueue(uint index = 0) {
        return GetQueue(_queueFamilyIndices.ComputeFamily, index);
    }

    /// <summary>
    /// Get the transfer queue (first queue from transfer family).
    /// </summary>
    public Queue GetTransferQueue(uint index = 0) {
        return GetQueue(_queueFamilyIndices.TransferFamily, index);
    }

    /// <summary>
    /// Get the present queue (first queue from present family).
    /// </summary>
    public Queue GetPresentQueue(uint index = 0) {
        return GetQueue(_queueFamilyIndices.PresentFamily, index);
    }

    /// <summary>
    /// Get a queue from a specific family by index.
    /// </summary>
    public Queue GetQueue(uint? familyIndex, uint queueIndex = 0) {
        if (!_built)
            throw new InvalidOperationException("Device not built. Call Build() first.");

        if (!familyIndex.HasValue)
            throw new InvalidOperationException("Queue family not available.");

        if (!_queues.TryGetValue(familyIndex.Value, out var queues))
            throw new InvalidOperationException($"No queues created for family {familyIndex.Value}.");

        if (queueIndex >= queues.Length)
            throw new ArgumentOutOfRangeException(nameof(queueIndex),
                $"Queue index {queueIndex} out of range. Only {queues.Length} queue(s) available.");

        return queues[queueIndex];
    }

    public void Dispose() {
        if (_built && _device.Handle != 0) {
            _vk.DestroyDevice(_device, null);
            _device = default;
        }
    }
}

/// <summary>
/// Helper class for configuring physical device features in a fluent manner.
/// </summary>
public class FeatureConfigurator {
    internal PhysicalDeviceFeatures _features;

    internal FeatureConfigurator(PhysicalDeviceFeatures initial) {
        _features = initial;
    }

    public FeatureConfigurator SamplerAnisotropy(bool enable = true) {
        _features.SamplerAnisotropy = enable;
        return this;
    }

    public FeatureConfigurator GeometryShader(bool enable = true) {
        _features.GeometryShader = enable;
        return this;
    }

    public FeatureConfigurator TessellationShader(bool enable = true) {
        _features.TessellationShader = enable;
        return this;
    }

    public FeatureConfigurator MultiDrawIndirect(bool enable = true) {
        _features.MultiDrawIndirect = enable;
        return this;
    }

    public FeatureConfigurator FillModeNonSolid(bool enable = true) {
        _features.FillModeNonSolid = enable;
        return this;
    }

    public FeatureConfigurator WideLines(bool enable = true) {
        _features.WideLines = enable;
        return this;
    }

    public FeatureConfigurator LargePoints(bool enable = true) {
        _features.LargePoints = enable;
        return this;
    }

    public FeatureConfigurator MultiViewport(bool enable = true) {
        _features.MultiViewport = enable;
        return this;
    }

    public FeatureConfigurator ShaderFloat64(bool enable = true) {
        _features.ShaderFloat64 = enable;
        return this;
    }

    public FeatureConfigurator ShaderInt64(bool enable = true) {
        _features.ShaderInt64 = enable;
        return this;
    }

    public FeatureConfigurator ShaderInt16(bool enable = true) {
        _features.ShaderInt16 = enable;
        return this;
    }

    public FeatureConfigurator DepthClamp(bool enable = true) {
        _features.DepthClamp = enable;
        return this;
    }

    public FeatureConfigurator DepthBiasClamp(bool enable = true) {
        _features.DepthBiasClamp = enable;
        return this;
    }

    public FeatureConfigurator DepthBounds(bool enable = true) {
        _features.DepthBounds = enable;
        return this;
    }

    public FeatureConfigurator AlphaToOne(bool enable = true) {
        _features.AlphaToOne = enable;
        return this;
    }

    public FeatureConfigurator DualSrcBlend(bool enable = true) {
        _features.DualSrcBlend = enable;
        return this;
    }

    public FeatureConfigurator LogicOp(bool enable = true) {
        _features.LogicOp = enable;
        return this;
    }

    public FeatureConfigurator ImageCubeArray(bool enable = true) {
        _features.ImageCubeArray = enable;
        return this;
    }

    public FeatureConfigurator IndependentBlend(bool enable = true) {
        _features.IndependentBlend = enable;
        return this;
    }

    public FeatureConfigurator RobustBufferAccess(bool enable = true) {
        _features.RobustBufferAccess = enable;
        return this;
    }

    public FeatureConfigurator FullDrawIndexUint32(bool enable = true) {
        _features.FullDrawIndexUint32 = enable;
        return this;
    }

    public FeatureConfigurator TextureCompressionBC(bool enable = true) {
        _features.TextureCompressionBC = enable;
        return this;
    }

    public FeatureConfigurator TextureCompressionETC2(bool enable = true) {
        _features.TextureCompressionEtc2 = enable;
        return this;
    }

    public FeatureConfigurator TextureCompressionASTC_LDR(bool enable = true) {
        _features.TextureCompressionAstcLdr = enable;
        return this;
    }

    public FeatureConfigurator ShaderStorageImageExtendedFormats(bool enable = true) {
        _features.ShaderStorageImageExtendedFormats = enable;
        return this;
    }

    public FeatureConfigurator FragmentStoresAndAtomics(bool enable = true) {
        _features.FragmentStoresAndAtomics = enable;
        return this;
    }

    public FeatureConfigurator VertexPipelineStoresAndAtomics(bool enable = true) {
        _features.VertexPipelineStoresAndAtomics = enable;
        return this;
    }
}
