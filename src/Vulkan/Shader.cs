using System;
using System.IO;
using System.Text;
using Shiron.Lib.Vulkan.Exceptions;
using Silk.NET.Core.Native;
using Silk.NET.Shaderc;
using Silk.NET.Vulkan;

namespace Shiron.Lib.Vulkan;

public unsafe class Shader : IDisposable {
    private static readonly Shaderc _shaderc = Shaderc.GetApi();

    private readonly Vk _vk;
    private readonly Device _device;

    // We hold the modules so we can destroy them later
    public ShaderModule VertexModule { get; }
    public ShaderModule FragmentModule { get; }

    // Stage Info structs used during Pipeline creation
    private PipelineShaderStageCreateInfo[]? _stageCreateInfos;

    /// <summary>
    /// Loads GLSL source code from files, compiles them to SPIR-V, and creates Vulkan modules.
    /// </summary>
    /// <param name="context">The Vulkan Context.</param>
    /// <param name="vertexPath">Path to the vertex shader file (GLSL).</param>
    /// <param name="fragmentPath">Path to the fragment shader file (GLSL).</param>
    public Shader(Vk vk, Device device, string vertexPath, string fragmentPath) {
        _vk = vk;
        _device = device;
        var vertexSource = File.ReadAllText(vertexPath);
        var fragmentSource = File.ReadAllText(fragmentPath);

        using var compiler = new GlslCompiler();
        var vertSpirv = compiler.Compile(vertexSource, ShaderKind.VertexShader, vertexPath);
        var fragSpirv = compiler.Compile(fragmentSource, ShaderKind.FragmentShader, fragmentPath);

        VertexModule = CreateShaderModule(vertSpirv);
        FragmentModule = CreateShaderModule(fragSpirv);

        CreateStageInfos();
    }

    /// <summary>
    /// Returns the stage create infos for Pipeline creation.
    /// </summary>
    public PipelineShaderStageCreateInfo[] GetPipelineStages() {
        return _stageCreateInfos!;
    }

    private ShaderModule CreateShaderModule(byte[] code) {
        var createInfo = new ShaderModuleCreateInfo {
            SType = StructureType.ShaderModuleCreateInfo,
            CodeSize = (nuint) code.Length
        };

        ShaderModule module;
        fixed (byte* codePtr = code) {
            createInfo.PCode = (uint*) codePtr;

            if (_vk.CreateShaderModule(_device, in createInfo, null, out module) != Result.Success) {
                throw new VulkanException("Failed to create shader module.", Result.ErrorUnknown);
            }
        }

        return module;
    }

    private void CreateStageInfos() {
        // Vertex Stage
        var vertStageInfo = new PipelineShaderStageCreateInfo {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = VertexModule,
            PName = (byte*) SilkMarshal.StringToPtr("main")
        };

        // Fragment Stage
        var fragStageInfo = new PipelineShaderStageCreateInfo {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.FragmentBit,
            Module = FragmentModule,
            PName = (byte*) SilkMarshal.StringToPtr("main")
        };

        _stageCreateInfos = [vertStageInfo, fragStageInfo];
    }

    public void Dispose() {
        // Free the name pointers allocated by StringToPtr
        foreach (var stage in _stageCreateInfos!) {
            SilkMarshal.Free((nint) stage.PName);
        }

        _vk.DestroyShaderModule(_device, FragmentModule, null);
        _vk.DestroyShaderModule(_device, VertexModule, null);
    }

    private class GlslCompiler : IDisposable {
        private readonly Compiler* _compiler;
        private readonly CompileOptions* _options;

        public GlslCompiler() {
            _compiler = _shaderc.CompilerInitialize();
            _options = _shaderc.CompileOptionsInitialize();

            if (_compiler == null || _options == null) {
                throw new Exception("Failed to initialize shaderc compiler or options.");
            }

            _shaderc.CompileOptionsSetOptimizationLevel(_options, OptimizationLevel.Performance);
            _shaderc.CompileOptionsSetTargetEnv(_options, TargetEnv.Vulkan, (uint) EnvVersion.Vulkan13);
        }

        public byte[] Compile(string source, ShaderKind kind, string fileName) {
            var sourceBytes = Encoding.UTF8.GetBytes(source);
            CompilationResult* res;
            var namePtr = (byte*) SilkMarshal.StringToPtr(fileName);
            var entryPtr = (byte*) SilkMarshal.StringToPtr("main");

            try {
                fixed (byte* srcPtr = sourceBytes) {
                    res = _shaderc.CompileIntoSpv(_compiler, srcPtr, (nuint) sourceBytes.Length, kind, namePtr, entryPtr, _options);
                }
            } finally {
                SilkMarshal.Free((nint) namePtr);
                SilkMarshal.Free((nint) entryPtr);
            }

            if (_shaderc.ResultGetCompilationStatus(res) != CompilationStatus.Success) {
                var error = _shaderc.ResultGetErrorMessageS(res);

                _shaderc.ResultRelease(res);
                throw new ShaderCompilationException(error, fileName);
            }

            var length = _shaderc.ResultGetLength(res);
            var spirv = new byte[length];
            fixed (byte* spirvPtr = spirv) {
                System.Buffer.MemoryCopy(_shaderc.ResultGetBytes(res), spirvPtr, length, length);
            }

            _shaderc.ResultRelease(res);
            return spirv;
        }

        public void Dispose() {
            _shaderc.CompilerRelease(_compiler);
            _shaderc.CompileOptionsRelease(_options);
        }
    }
}
