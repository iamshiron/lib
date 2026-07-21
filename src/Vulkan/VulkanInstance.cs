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
    private readonly GCHandle? _errorCallbackHandle;

    public Instance Instance { get; }

    internal VulkanInstance(
        Vk vk,
        Instance instance,
        bool hasValidation,
        ExtDebugUtils? debugUtils,
        DebugUtilsMessengerEXT debugMessenger,
        GCHandle? errorCallbackHandle) {
        _vk = vk ?? throw new ArgumentNullException(nameof(vk));
        Instance = instance;
        _hasValidation = hasValidation;
        _debugUtils = debugUtils;
        _debugMessenger = debugMessenger;
        _errorCallbackHandle = errorCallbackHandle;
    }

    private bool _messengerDestroyed;

    /// <summary>
    /// Destroys the debug-utils messenger and releases the pinned error callback. Idempotent.
    /// </summary>
    /// <remarks>
    /// This must run while the logical device is still alive. The validation layer unregisters the
    /// messenger from its per-device state during destruction; if the device dispatch table has
    /// already been torn down (<c>vkDestroyDevice</c>), the layer fails the lookup and aborts the
    /// process ("VkDevice dispatch handle was not found"). Callers therefore destroy the messenger
    /// before the device, not as part of <see cref="Dispose"/>.
    /// </remarks>
    public void DestroyDebugMessenger() {
        if (_messengerDestroyed || !_hasValidation) {
            return;
        }
        _messengerDestroyed = true;

        _debugUtils?.DestroyDebugUtilsMessenger(Instance, _debugMessenger, null);

        if (_errorCallbackHandle != null && _errorCallbackHandle.Value.IsAllocated) {
            _errorCallbackHandle.Value.Free();
        }
    }

    public void Dispose() {
        DestroyDebugMessenger();

        if (Instance.Handle != 0) {
            _vk.DestroyInstance(Instance, null);
        }
    }
}
