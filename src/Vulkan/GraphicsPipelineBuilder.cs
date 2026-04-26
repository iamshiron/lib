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
    private readonly PipelineColorBlendAttachmentState _colorBlendAttachment = new() {
        ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
        BlendEnable = false
    };
    private readonly PipelineColorBlendStateCreateInfo _colorBlend = new() {
        SType = StructureType.PipelineColorBlendStateCreateInfo,
        LogicOpEnable = false,
        AttachmentCount = 1
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
    private Format _colorAttachmentFormat;
    private Format _depthAttachmentFormat = Format.Undefined;
    private DescriptorSetLayout _descriptorSetLayout;
    private PushConstantRange _pushConstantRange;
    private VertexInputBindingDescription _bindingDescription;
    private VertexInputAttributeDescription[] _attributeDescriptions = [];

    public GraphicsPipelineBuilder SetShader(Shader shader) {
        _shader = shader;
        return this;
    }

    public GraphicsPipelineBuilder SetVertexInput(VertexInputBindingDescription binding, VertexInputAttributeDescription[] attributes) {
        _bindingDescription = binding;
        _attributeDescriptions = attributes;
        return this;
    }

    public GraphicsPipelineBuilder SetDynamicRendering(Format colorFormat, Format depthFormat = Format.Undefined) {
        _colorAttachmentFormat = colorFormat;
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
        var colorFormats = stackalloc[] { _colorAttachmentFormat };

        var pipelineRenderingInfo = new PipelineRenderingCreateInfo {
            SType = StructureType.PipelineRenderingCreateInfo,
            ColorAttachmentCount = 1,
            PColorAttachmentFormats = colorFormats,
            DepthAttachmentFormat = _depthAttachmentFormat
        };

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
        fixed (PipelineShaderStageCreateInfo* stagesPtr = stages) {
            var vertexInput = new PipelineVertexInputStateCreateInfo {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexBindingDescriptionCount = 1,
                PVertexBindingDescriptions = &bindingDesc,
                VertexAttributeDescriptionCount = (uint) _attributeDescriptions.Length,
                PVertexAttributeDescriptions = attrsPtr
            };

            var blendAttachment = _colorBlendAttachment;
            colorBlend.PAttachments = &blendAttachment;

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

    public GraphicsPipelineBuilder SetDepthStencil(bool depthTestEnable, bool depthWriteEnable, CompareOp compareOp = CompareOp.Less) {
        _depthStencil.DepthTestEnable = depthTestEnable;
        _depthStencil.DepthWriteEnable = depthWriteEnable;
        _depthStencil.DepthCompareOp = compareOp;
        return this;
    }
}
