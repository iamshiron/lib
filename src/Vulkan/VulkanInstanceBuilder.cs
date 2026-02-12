using System.Runtime.InteropServices;
using Shiron.Lib.Vulkan.Exceptions;
using Shiron.Phonon.Common.Utils;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;

namespace Shiron.Lib.Vulkan;

/// <summary>
/// Builder class for creating a Vulkan instance with customizable configuration.
/// </summary>
public sealed unsafe class VulkanInstanceBuilder {
    private readonly Vk _vk;

    private string _appName = "App";
    private string _engineName = "Engine";
    private SemVer _appVersion = new(1, 0, 0);
    private SemVer _engineVersion = new(1, 0, 0);
    private uint _apiVersion = Vk.Version12;

    private bool _enableValidation;
    private IVulkanErrorCallback? _errorCallback;
    private readonly List<string> _extensions = new();
    private readonly List<string> _layers = new();

    public VulkanInstanceBuilder(Vk vk) {
        _vk = vk ?? throw new ArgumentNullException(nameof(vk));
    }

    public VulkanInstanceBuilder WithApp(string name, SemVer? version = null) {
        _appName = name ?? _appName;
        if (version.HasValue) {
            _appVersion = version.Value;
        }
        return this;
    }

    public VulkanInstanceBuilder WithEngine(string name, SemVer? version = null) {
        _engineName = name ?? _engineName;
        if (version.HasValue) {
            _engineVersion = version.Value;
        }
        return this;
    }

    public VulkanInstanceBuilder WithApiVersion(uint apiVersion) {
        _apiVersion = apiVersion;
        return this;
    }

    /// <summary>
    /// Add required instance extensions (e.g. surface + platform WSI extensions). [web:70]
    /// </summary>
    public VulkanInstanceBuilder AddExtensions(params string[] extensions) {
        foreach (var e in extensions.Where(s => !string.IsNullOrWhiteSpace(s))) {
            if (!_extensions.Contains(e)) {
                _extensions.Add(e);
            }
        }
        return this;
    }

    public VulkanInstanceBuilder EnableValidationLayers(IVulkanErrorCallback? callback = null) {
        _enableValidation = callback != null;
        _errorCallback = callback;
        return this;
    }

    /// <summary>
    /// Override/add instance layers (e.g. "VK_LAYER_KHRONOS_validation").
    /// </summary>
    public VulkanInstanceBuilder AddLayers(params string[] layers) {
        foreach (var l in layers.Where(s => !string.IsNullOrWhiteSpace(s))) {
            if (!_layers.Contains(l)) {
                _layers.Add(l);
            }
        }
        return this;
    }

    /// <summary>
    /// If validation is enabled, also enables VK_EXT_debug_utils and creates a debug messenger. [web:79]
    /// </summary>
    public VulkanInstance Build() {
        if (_enableValidation) {
            if (!_layers.Contains("VK_LAYER_KHRONOS_validation")) {
                _layers.Add("VK_LAYER_KHRONOS_validation"); // standard validation meta-layer name. [web:79]
            }

            if (!_extensions.Contains(ExtDebugUtils.ExtensionName)) {
                _extensions.Add(ExtDebugUtils.ExtensionName); // needed for debug messenger. [web:79]
            }
        }

        var appInfo = new ApplicationInfo {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*) SilkMarshal.StringToPtr(_appName, NativeStringEncoding.UTF8),
            ApplicationVersion = _appVersion,
            PEngineName = (byte*) SilkMarshal.StringToPtr(_engineName, NativeStringEncoding.UTF8),
            EngineVersion = _engineVersion,
            ApiVersion = _apiVersion
        };

        var extPtrs = SilkMarshal.StringArrayToPtr(_extensions.ToArray(), NativeStringEncoding.UTF8);
        var layerPtrs = SilkMarshal.StringArrayToPtr(_layers.ToArray(), NativeStringEncoding.UTF8);

        var createInfo = new InstanceCreateInfo {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo,
            EnabledExtensionCount = (uint) _extensions.Count,
            PpEnabledExtensionNames = (byte**) extPtrs,
            EnabledLayerCount = (uint) _layers.Count,
            PpEnabledLayerNames = (byte**) layerPtrs
        };

        var result = _vk.CreateInstance(&createInfo, null, out var instance);
        SilkMarshal.Free((nint) appInfo.PApplicationName);
        SilkMarshal.Free((nint) appInfo.PEngineName);
        SilkMarshal.Free(extPtrs);
        SilkMarshal.Free(layerPtrs);

        if (result != Result.Success) {
            throw new VulkanException("Failed to create Vulkan instance. " + result, result);
        }

        ExtDebugUtils? debugUtils = null;
        DebugUtilsMessengerEXT debugMessenger = default;
        GCHandle errorCallbackHandle = default;

        if (_enableValidation) {
            debugUtils = new ExtDebugUtils(_vk.Context);
            errorCallbackHandle = GCHandle.Alloc(_errorCallback);

            var messengerCi = new DebugUtilsMessengerCreateInfoEXT {
                SType = StructureType.DebugUtilsMessengerCreateInfoExt,
                MessageSeverity =
                    DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
                    DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                    DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt,
                MessageType =
                    DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                    DebugUtilsMessageTypeFlagsEXT.ValidationBitExt |
                    DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt,
                PfnUserCallback = (PfnDebugUtilsMessengerCallbackEXT) DebugCallback,
                PUserData = (void*) errorCallbackHandle.AddrOfPinnedObject()
            };

            var res = debugUtils.CreateDebugUtilsMessenger(instance, &messengerCi, null, out debugMessenger);
            if (res != Result.Success) {
                throw new VulkanException("Failed to create debug utils messenger.", res);
            }
        }

        return new VulkanInstance(
            _vk,
            instance,
            _enableValidation,
            debugUtils,
            debugMessenger,
            errorCallbackHandle);
    }

    private static uint DebugCallback(
        DebugUtilsMessageSeverityFlagsEXT severity,
        DebugUtilsMessageTypeFlagsEXT types,
        DebugUtilsMessengerCallbackDataEXT* data,
        void* userData) {
        var target = GCHandle.FromIntPtr((IntPtr) userData).Target;

        if (target == null) {
            Console.WriteLine("[ERROR]: Unable to get callback target from handle.");
        } else {
            var callback = (IVulkanErrorCallback) target;
            callback.OnError(severity, types, data);
        }

        return Vk.False;
    }
}
