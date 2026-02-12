using Shiron.Lib.Vulkan.Exceptions;
using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Shiron.Lib.Vulkan;

/// <summary>
/// A wrapper around a specific Vulkan CommandBuffer.
/// Handles recording state (Begin/End) and Submission.
/// </summary>
public unsafe class VulkanCommandBuffer : IDisposable {
    private readonly Vk _vk;
    private readonly Device _device;

    private readonly VulkanCommandPool _pool;
    public CommandBuffer Handle;

    public VulkanCommandBuffer(Vk vk, Device device, VulkanCommandPool pool, CommandBufferLevel level) {
        _vk = vk;
        _device = device;
        _pool = pool;

        var allocInfo = new CommandBufferAllocateInfo {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _pool.Handle,
            Level = level,
            CommandBufferCount = 1
        };

        if (_vk.AllocateCommandBuffers(_device, in allocInfo, out Handle) != Result.Success) {
            throw new VulkanException("Failed to allocate command buffer.", Result.ErrorUnknown);
        }
    }

    /// <summary>
    /// Starts recording commands.
    /// </summary>
    public void Begin(CommandBufferUsageFlags flags = 0) {
        var beginInfo = new CommandBufferBeginInfo {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = flags
        };

        if (_vk.BeginCommandBuffer(Handle, in beginInfo) != Result.Success) {
            throw new VulkanException("Failed to begin recording command buffer.", Result.ErrorUnknown);
        }
    }

    /// <summary>
    /// Stops recording commands.
    /// </summary>
    public void End() {
        if (_vk.EndCommandBuffer(Handle) != Result.Success) {
            throw new VulkanException("Failed to end recording command buffer.", Result.ErrorUnknown);
        }
    }

    /// <summary>
    /// Resets the buffer so it can be recorded again.
    /// Requires the pool to have been created with ResetCommandBufferBit.
    /// </summary>
    public void Reset(CommandBufferResetFlags flags = 0) {
        _vk.ResetCommandBuffer(Handle, flags);
    }

    /// <summary>
    /// Helper to submit this buffer to a queue.
    /// </summary>
    /// <param name="queue">The queue to submit to.</param>
    /// <param name="fence">Optional fence to signal when execution completes.</param>
    /// <param name="waitSemaphore">Optional semaphore to wait on before execution.</param>
    /// <param name="signalSemaphore">Optional semaphore to signal after execution.</param>
    /// <param name="waitStage">Pipeline stage to wait at (if waitSemaphore is provided).</param>
    private void Submit(
        Queue queue,
        Fence? fence = null,
        Semaphore? waitSemaphore = null,
        Semaphore? signalSemaphore = null,
        PipelineStageFlags waitStage = PipelineStageFlags.ColorAttachmentOutputBit) {
        var submitInfo = new SubmitInfo {
            SType = StructureType.SubmitInfo
        };

        // Handle Command Buffer
        var bufferHandle = Handle;
        submitInfo.CommandBufferCount = 1;
        submitInfo.PCommandBuffers = &bufferHandle;

        // Handle Wait Semaphore
        var waitDstStageMask = waitStage;
        if (waitSemaphore.HasValue) {
            var waitSem = waitSemaphore.Value;
            submitInfo.WaitSemaphoreCount = 1;
            submitInfo.PWaitSemaphores = &waitSem;
            submitInfo.PWaitDstStageMask = &waitDstStageMask;
        }

        // Handle Signal Semaphore
        if (signalSemaphore.HasValue) {
            var signalSem = signalSemaphore.Value;
            submitInfo.SignalSemaphoreCount = 1;
            submitInfo.PSignalSemaphores = &signalSem;
        }

        // Submit
        // Note: We use the null-coalescing operator for the fence because Silk.NET expects a non-nullable struct if passed directly,
        // but the Fence param here is a valid Vulkan Handle (ulong) which can be 0 (null handle).
        var actualFence = fence ?? default;
        if (_vk.QueueSubmit(queue, 1, in submitInfo, actualFence) != Result.Success) {
            throw new VulkanException("Failed to submit command buffer to queue.", Result.ErrorUnknown);
        }
    }

    /// <summary>
    /// Executes a "One Time Submit" helper.
    /// Begins, runs the action, Ends, Submits, and Waits for idle.
    /// Useful for transferring data (Staging Buffers).
    /// </summary>
    public void ImmediateSubmit(Queue queue, Action<VulkanCommandBuffer> commands) {
        Begin(CommandBufferUsageFlags.OneTimeSubmitBit);
        commands(this);
        End();

        Submit(queue);
        _vk.QueueWaitIdle(queue);
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
        _vk.FreeCommandBuffers(_device, _pool.Handle, 1, in Handle);
    }
}
