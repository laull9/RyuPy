// 只进入托管预门：真实PE、自测、加载字节身份；Native配置从Compile集合排除此文件。
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using RyuPy.Runtime;

internal static class ManagedInputChecks
{
    internal static void Run(string[] args)
    {
        var identity = args.Skip(2).Select(path => new {
            Path = path, Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) });
        File.WriteAllText(Path.Combine(args[1], "identity.json"), JsonSerializer.Serialize(identity));
        GeneratedPeChecksTests.Run(Path.Combine(args[1], "generated-pe-selftests.json"));
        GeneratedPeChecks.Inspect(args[3], Path.Combine(args[1], "generated-pe.json"), NativeChecks.IsNative(args[0]));
        InspectPe(args[2], true, true);
        InspectRuntime(args[2], true);
        InspectRuntime(args[4], false);
        InspectPe(args[3], true, false);
        InspectPe(args[4], false, true);
        InspectPe(args[5], false, false);
        ExecutionChecks.Require(File.ReadAllBytes(typeof(PyBaseException).Assembly.Location).SequenceEqual(File.ReadAllBytes(args[2])), "实际加载runtime不是本轮probe字节");
        ExecutionChecks.Require(File.ReadAllBytes(typeof(RyuPy.Generated.Module).Assembly.Location).SequenceEqual(File.ReadAllBytes(args[3])), "实际加载模块不是本轮生成字节");
    }

    private static void InspectPe(string path, bool probe, bool runtime)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        bool adapter = runtime
            ? metadata.TypeDefinitions.Any(h => metadata.GetString(metadata.GetTypeDefinition(h).Name) == "PyTraceAdapter")
            : metadata.TypeReferences.Any(h => metadata.GetString(metadata.GetTypeReference(h).Name) == "PyTraceAdapter");
        ExecutionChecks.Require(adapter == probe, "实际PE的probe标志/引用与隔离合同不符: " + path);

    }

    // PE静态存在性与下面实际事件身份联合验证，不把方法名存在当成已执行。
    internal static void InspectRuntime(string path, bool probe)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        foreach (var name in new[] { "CreateLocalsPositionalError", "CaptureNativeCaller", "ForwardNative" })
        {
            var methods = metadata.MethodDefinitions
                .Where(h => metadata.GetString(metadata.GetMethodDefinition(h).Name) == name).ToArray();
            if (methods.Length != (probe ? 1 : 0))
                throw new InvalidOperationException("native方法隔离不符: " + name);
            if (probe && pe.GetMethodBody(metadata.GetMethodDefinition(methods[0]).RelativeVirtualAddress).GetILBytes() is not { Length: > 0 })
                throw new InvalidOperationException("native真实方法体缺失: " + name);
        }
    }

}
