using Silk.NET.Vulkan;

namespace Shiron.Lib.Vulkan;

public unsafe class GraphicsPipelineBuilder(Vk vk, Device device) {
    private readonly PipelineInputAssemblyStateCreateInfo _inputAssembly = new() {
        SType = StructureType.PipelineInputAssemblyStateCreateInfo,
        Topology = PrimitiveTopology.TriangleList,
        PrimitiveRestartEnable = false
    };
    private PipelineRasterizationStateCreateInfo _rasterizer = new() {
        SType = StructureType.PipelineRasterizationStateCreateInfo,
        DepthClampEnable = false,
        RasterizerDiscardEnable = false,
        PolygonMode = PolygonMode.Fill,
        LineWidth = 1.0f,
        CullMode = CullModeFlags.BackBit,
        FrontFace = FrontFace.CounterClockwise,
        DepthBiasEnable = false
    };
    private readonly PipelineMultisampleStateCreateInfo _multisample = new() {
        SType = StructureType.PipelineMultisampleStateCreateInfo,
        SampleShadingEnable = false,
        RasterizationSamples = SampleCountFlags.Count1Bit
    };
    // One blend state per color attachment; sized to the color-format count in Build.
    private PipelineColorBlendAttachmentState[] _colorBlendAttachments = [DefaultBlendAttachment()];
    private readonly PipelineColorBlendStateCreateInfo _colorBlend = new() {
        SType = StructureType.PipelineColorBlendStateCreateInfo,
        LogicOpEnable = false
    };
    private PipelineDepthStencilStateCreateInfo _depthStencil = new() {
        SType = StructureType.PipelineDepthStencilStateCreateInfo,
        DepthTestEnable = false,
        DepthWriteEnable = false,
        DepthCompareOp = CompareOp.Less,
        DepthBoundsTestEnable = false,
        StencilTestEnable = false
    };
    private readonly PipelineViewportStateCreateInfo _viewportState = new() {
        SType = StructureType.PipelineViewportStateCreateInfo,
        ViewportCount = 1,
        ScissorCount = 1
    };
    private readonly PipelineDynamicStateCreateInfo _dynamicState = new() {
        SType = StructureType.PipelineDynamicStateCreateInfo
    };

    private Shader? _shader;
    private Format[] _colorAttachmentFormats = [];
    private Format _depthAttachmentFormat = Format.Undefined;
    private DescriptorSetLayout _descriptorSetLayout;
    private PushConstantRange _pushConstantRange;
    private VertexInputBindingDescription _bindingDescription;
    private VertexInputAttributeDescription[] _attributeDescriptions = [];
    private bool _noVertexInput;

    private static PipelineColorBlendAttachmentState DefaultBlendAttachment() => new() {
        ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
        BlendEnable = false
    };

    public GraphicsPipelineBuilder SetShader(Shader shader) {
        _shader = shader;
        return this;
    }

    public GraphicsPipelineBuilder SetVertexInput(VertexInputBindingDescription binding, VertexInputAttributeDescription[] attributes) {
        _bindingDescription = binding;
        _attributeDescriptions = attributes;
        _noVertexInput = false;
        return this;
    }

    /// <summary>
    /// Configures the pipeline with no vertex input (no bindings, no attributes) — for passes that
    /// generate geometry from <c>gl_VertexIndex</c> (e.g. a fullscreen triangle).
    /// </summary>
    public GraphicsPipelineBuilder SetNoVertexInput() {
        _noVertexInput = true;
        _attributeDescriptions = [];
        return this;
    }

    /// <summary>
    /// Enables standard alpha blending on the first color attachment (for UI/overlay passes).
    /// </summary>
    /// <param name="premultiplied">
    /// When true, expects premultiplied-alpha source (src factor <c>One</c>); when false, straight
    /// alpha (src factor <c>SrcAlpha</c>). Destination factor is always <c>OneMinusSrcAlpha</c>.
    /// </param>
    public GraphicsPipelineBuilder EnableAlphaBlend(bool premultiplied = false) {
        _colorBlendAttachments = [
            new PipelineColorBlendAttachmentState {
                ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
                BlendEnable = true,
                SrcColorBlendFactor = premultiplied ? BlendFactor.One : BlendFactor.SrcAlpha,
                DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                ColorBlendOp = BlendOp.Add,
                SrcAlphaBlendFactor = BlendFactor.One,
                DstAlphaBlendFactor = BlendFactor.OneMinusSrcAlpha,
                AlphaBlendOp = BlendOp.Add
            }
        ];
        return this;
    }

    /// <summary>Configures dynamic rendering with a single color attachment.</summary>
    public GraphicsPipelineBuilder SetDynamicRendering(Format colorFormat, Format depthFormat = Format.Undefined) {
        _colorAttachmentFormats = [colorFormat];
        _depthAttachmentFormat = depthFormat;
        return this;
    }

    /// <summary>
    /// Configures dynamic rendering with multiple color attachments (MRT) — e.g. a deferred G-buffer.
    /// Pass an empty array for a depth-only pass (a shadow map).
    /// </summary>
    public GraphicsPipelineBuilder SetDynamicRendering(Format[] colorFormats, Format depthFormat = Format.Undefined) {
        _colorAttachmentFormats = colorFormats;
        _depthAttachmentFormat = depthFormat;
        return this;
    }

    public GraphicsPipelineBuilder SetLayout(DescriptorSetLayout descriptorLayout, PushConstantRange pushRange) {
        _descriptorSetLayout = descriptorLayout;
        _pushConstantRange = pushRange;
        return this;
    }

    public GraphicsPipelineBuilder SetRasterizer(PolygonMode mode, CullModeFlags cullMode) {
        _rasterizer.PolygonMode = mode;
        _rasterizer.CullMode = cullMode;
        return this;
    }

    /// <summary>Enables a constant + slope-scaled depth bias (for shadow-map rendering to reduce acne).</summary>
    public GraphicsPipelineBuilder SetDepthBias(float constantFactor, float slopeFactor) {
        _rasterizer.DepthBiasEnable = true;
        _rasterizer.DepthBiasConstantFactor = constantFactor;
        _rasterizer.DepthBiasSlopeFactor = slopeFactor;
        return this;
    }

    public GraphicsPipelineBuilder SetDepthStencil(bool depthTestEnable, bool depthWriteEnable, CompareOp compareOp = CompareOp.Less) {
        _depthStencil.DepthTestEnable = depthTestEnable;
        _depthStencil.DepthWriteEnable = depthWriteEnable;
        _depthStencil.DepthCompareOp = compareOp;
        return this;
    }

    public Pipeline Build(out PipelineLayout pipelineLayout) {
        if (_shader == null) {
            throw new InvalidOperationException("Cannot build pipeline without a shader.");
        }

        var descriptorLayout = _descriptorSetLayout;
        var pushRange = _pushConstantRange;
        var bindingDesc = _bindingDescription;
        var inputAssembly = _inputAssembly;
        var viewportState = _viewportState;
        var rasterizer = _rasterizer;
        var multisample = _multisample;
        var depthStencil = _depthStencil;
        var colorBlend = _colorBlend;
        var dynamicState = _dynamicState;

        var stages = _shader.GetPipelineStages();

        var colorCount = _colorAttachmentFormats.Length;
        var colorFormats = stackalloc Format[colorCount == 0 ? 1 : colorCount];
        for (var i = 0; i < colorCount; i++) colorFormats[i] = _colorAttachmentFormats[i];

        var pipelineRenderingInfo = new PipelineRenderingCreateInfo {
            SType = StructureType.PipelineRenderingCreateInfo,
            ColorAttachmentCount = (uint) colorCount,
            PColorAttachmentFormats = colorCount == 0 ? null : colorFormats,
            DepthAttachmentFormat = _depthAttachmentFormat
        };

        // Vulkan requires one blend state per color attachment (attachmentCount == colorAttachmentCount).
        var blendStates = new PipelineColorBlendAttachmentState[colorCount];
        for (var i = 0; i < colorCount; i++)
            blendStates[i] = i < _colorBlendAttachments.Length ? _colorBlendAttachments[i] : DefaultBlendAttachment();

        var layoutInfo = new PipelineLayoutCreateInfo {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 1,
            PSetLayouts = &descriptorLayout,
            PushConstantRangeCount = 1,
            PPushConstantRanges = &pushRange
        };

        if (vk.CreatePipelineLayout(device, &layoutInfo, null, out pipelineLayout) != Result.Success) {
            throw new Exception("Failed to create pipeline layout!");
        }

        fixed (VertexInputAttributeDescription* attrsPtr = _attributeDescriptions)
        fixed (PipelineShaderStageCreateInfo* stagesPtr = stages)
        fixed (PipelineColorBlendAttachmentState* blendPtr = blendStates) {
            var vertexInput = new PipelineVertexInputStateCreateInfo {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexBindingDescriptionCount = _noVertexInput ? 0u : 1u,
                PVertexBindingDescriptions = _noVertexInput ? null : &bindingDesc,
                VertexAttributeDescriptionCount = (uint) _attributeDescriptions.Length,
                PVertexAttributeDescriptions = _noVertexInput ? null : attrsPtr
            };

            colorBlend.AttachmentCount = (uint) colorCount;
            colorBlend.PAttachments = colorCount == 0 ? null : blendPtr;

            var dynamicStates = stackalloc[] { DynamicState.Viewport, DynamicState.Scissor };
            dynamicState.SType = StructureType.PipelineDynamicStateCreateInfo;
            dynamicState.DynamicStateCount = 2;
            dynamicState.PDynamicStates = dynamicStates;

            var pipelineInfo = new GraphicsPipelineCreateInfo {
                SType = StructureType.GraphicsPipelineCreateInfo,
                PNext = &pipelineRenderingInfo,
                StageCount = (uint) stages.Length,
                PStages = stagesPtr,
                PVertexInputState = &vertexInput,
                PInputAssemblyState = &inputAssembly,
                PViewportState = &viewportState,
                PRasterizationState = &rasterizer,
                PMultisampleState = &multisample,
                PDepthStencilState = &depthStencil,
                PColorBlendState = &colorBlend,
                PDynamicState = &dynamicState,
                Layout = pipelineLayout,
                RenderPass = default,
                Subpass = 0
            };

            return vk.CreateGraphicsPipelines(device, default, 1, &pipelineInfo, null, out var pipeline) != Result.Success
                ? throw new Exception("Failed to create graphics pipeline!")
                : pipeline;
        }
    }
}
