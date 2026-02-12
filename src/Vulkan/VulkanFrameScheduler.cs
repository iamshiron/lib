using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Shiron.Lib.Vulkan;

public unsafe class VulkanFrameScheduler : IDisposable {
    private readonly Vk _vk;
    private readonly Device _device;
    private readonly VulkanCommandBuffer[] _commandBuffers;
    private readonly VulkanSwapchain _swapchain;

    private readonly Semaphore[] _imageAvailableSemaphores;
    private readonly Semaphore[] _renderFinishedSemaphores;
    private readonly Fence[] _inFlightFences;
    private readonly int _framesInFlight;

    // State
    private uint _currentImageIndex;
    private int _currentFrame = 0;

    private readonly Queue _presentQueue;
    private readonly Queue _graphicsQueue;

    public VulkanCommandBuffer CurrentCommandBuffer => _commandBuffers[_currentFrame];
    public int CurrentFrameIndex => _currentFrame;
    public uint CurrentImageIndex => _currentImageIndex;

    public VulkanFrameScheduler(
        Vk vk,
        Device device,
        VulkanCommandBuffer[] commandBuffers,
        VulkanSwapchain swapchain,
        Queue presentQueue,
        Queue graphicsQueue,
        uint framesInFlight) {
        _vk = vk;
        _commandBuffers = commandBuffers;
        _device = device;
        _swapchain = swapchain;
        _presentQueue = presentQueue;
        _graphicsQueue = graphicsQueue;
        _framesInFlight = (int) framesInFlight;

        _imageAvailableSemaphores = new Semaphore[_framesInFlight];
        _inFlightFences = new Fence[_framesInFlight];

        _renderFinishedSemaphores = new Semaphore[_swapchain.ImageCount];

        var semaphoreInfo = new SemaphoreCreateInfo { SType = StructureType.SemaphoreCreateInfo };
        var fenceInfo = new FenceCreateInfo { SType = StructureType.FenceCreateInfo, Flags = FenceCreateFlags.SignaledBit };

        for (var i = 0; i < _framesInFlight; i++) {
            if (vk.CreateSemaphore(device, in semaphoreInfo, null, out _imageAvailableSemaphores[i]) != Result.Success ||
                vk.CreateFence(device, in fenceInfo, null, out _inFlightFences[i]) != Result.Success) {
                throw new Exception("Failed to create synchronization objects (frames in flight)!");
            }
        }

        for (var i = 0; i < _framesInFlight; i++) {
            if (vk.CreateSemaphore(device, in semaphoreInfo, null, out _renderFinishedSemaphores[i]) != Result.Success) {
                throw new Exception("Failed to create synchronization objects (swapchain images)!");
            }
        }
    }

    /// <summary>
    /// Waits for the previous frame, Resets Fences, and Acquires the new Swapchain Image.
    /// Returns true if successful, false if Swapchain is out of date (resize needed).
    /// </summary>
    public bool BeginFrame() {
        _vk.WaitForFences(_device, 1, in _inFlightFences[_currentFrame], true, ulong.MaxValue);

        var result = _swapchain.AcquireNextImage(
            _imageAvailableSemaphores[_currentFrame],
            default,
            out _currentImageIndex
        );

        if (result == Result.ErrorOutOfDateKhr) {
            return false;
        }
        if (result != Result.Success && result != Result.SuboptimalKhr) {
            throw new Exception("Failed to acquire swapchain image.");
        }

        _vk.ResetFences(_device, 1, in _inFlightFences[_currentFrame]);

        CurrentCommandBuffer.Reset(CommandBufferResetFlags.None);
        CurrentCommandBuffer.Begin(CommandBufferUsageFlags.OneTimeSubmitBit);

        return true;
    }

    /// <summary>
    /// Submits the command buffer (filled by RenderGraph) and Presents.
    /// </summary>
    public void EndFrame() {
        var cmd = CurrentCommandBuffer;

        cmd.End();
        var waitSems = stackalloc[] { _imageAvailableSemaphores[_currentFrame] };
        var waitStages = stackalloc[] { PipelineStageFlags.ColorAttachmentOutputBit };
        var signalSems = stackalloc[] { _renderFinishedSemaphores[_currentImageIndex] };

        fixed (CommandBuffer* handle = &cmd.Handle) {
            var submitInfo = new SubmitInfo {
                SType = StructureType.SubmitInfo,
                WaitSemaphoreCount = 1,
                PWaitSemaphores = waitSems,
                PWaitDstStageMask = waitStages,
                CommandBufferCount = 1,
                PCommandBuffers = handle,
                SignalSemaphoreCount = 1,
                PSignalSemaphores = signalSems
            };

            if (_vk.QueueSubmit(_graphicsQueue, 1, &submitInfo, _inFlightFences[_currentFrame]) != Result.Success) {
                throw new Exception("Failed to submit draw command buffer!");
            }
        }

        _swapchain.Present(_presentQueue, _currentImageIndex, _renderFinishedSemaphores[_currentImageIndex]);
        _currentFrame = (_currentFrame + 1) % _framesInFlight;
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
        _vk.DeviceWaitIdle(_device);

        for (var i = 0; i < _framesInFlight; i++) {
            _vk.DestroySemaphore(_device, _imageAvailableSemaphores[i], null);
            _vk.DestroyFence(_device, _inFlightFences[i], null);
        }

        foreach (var sem in _renderFinishedSemaphores) {
            _vk.DestroySemaphore(_device, sem, null);
        }
    }
}
