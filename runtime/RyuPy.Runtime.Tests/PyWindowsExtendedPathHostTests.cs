// 仅真实Windows执行：公开Path读取与NativeAOT模块资源锚点保留extended身份。
using System;
using System.IO;
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;
[Collection("生成器帧栈")]
public class PyWindowsExtendedPathHostTests
{
    [WindowsPathFact]
    public void PublicPathReadsFromExtendedModuleAnchorWithoutPrefixStripping()
    {
        string root = Path.Combine(Path.GetTempPath(), "ryupy-ext-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string ordinary = Path.GetFullPath(root);
            string extended = ordinary.StartsWith(@"\\", StringComparison.Ordinal)
                ? @"\\?\UNC\" + ordinary[2..] : @"\\?\" + ordinary;
            string location = PyModuleLocations.ResolveForPlatform(2,
                "_internal/resources/v1/entry/demo.py", false, extended + @"\app.exe");
            Assert.StartsWith(@"\\?\", location);
            string directory = Path.GetDirectoryName(location)!;
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "data.txt"), "native-resource\n");
            var path = new PyPath(location);
            var parent = Assert.IsType<PyPath>(path.GetAttr("parent"));
            var data = Assert.IsType<PyPath>(PyOps.CallValue(parent.GetAttr("joinpath"), ["data.txt"], [], []));
            Assert.StartsWith(@"\\?\", data.Value);
            Assert.Equal("native-resource\n", PyOps.CallValue(data.GetAttr("read_text"), [], [], []));
            var ordinaryData = new PyPath(Path.Combine(ordinary, "_internal/resources/v1/entry/data.txt"));
            Assert.False(PyHash.Equal(data, ordinaryData));
            Assert.Equal(File.ReadAllText(ordinaryData.Value), PyOps.CallValue(data.GetAttr("read_text"), [], [], []));
            File.Delete(data.Value);
            Assert.Throws<PyFileNotFoundError>(() => PyOps.CallValue(data.GetAttr("read_text"), [], [], []));
        }
        finally { Directory.Delete(root, true); }
    }
}
