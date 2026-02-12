using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;

namespace Shiron.Lib.Vulkan;

/// <summary>
/// Immutable Vulkan instance object.
/// </summary>
public sealed unsafe class VulkanInstance : IDisposable {
    private readonly Vk _vk;
    private readonly bool _hasValidation;
    private readonly ExtDebugUtils? _debugUtils;
    private readonly DebugUtilsMessengerEXT _debugMessenger;
    private readonly GCHandle _errorCallbackHandle;

    public Instance Instance { get; }

    internal VulkanInstance(
        Vk vk,
        Instance instance,
        bool hasValidation,
        ExtDebugUtils? debugUtils,
        DebugUtilsMessengerEXT debugMessenger,
        GCHandle errorCallbackHandle) {
        _vk = vk ?? throw new ArgumentNullException(nameof(vk));
        Instance = instance;
        _hasValidation = hasValidation;
        _debugUtils = debugUtils;
        _debugMessenger = debugMessenger;
        _errorCallbackHandle = errorCallbackHandle;
    }

    public void Dispose() {
        if (_hasValidation) {
            _debugUtils?.DestroyDebugUtilsMessenger(Instance, _debugMessenger, null);
            if (_errorCallbackHandle.IsAllocated) {
                _errorCallbackHandle.Free();
            }
        }

        if (Instance.Handle != 0) {
            _vk.DestroyInstance(Instance, null);
        }
    }
}
