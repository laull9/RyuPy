// textwrap测试的固定解释器入口：原输入矩阵在手写Python文件中，当次输出只存在内存。
using System.Diagnostics;
using System.Text.Json;

namespace RyuPy.Runtime.Tests;

internal static class TextWrapOracle
{
    public static JsonDocument Read(string mode)
    {
        string python = Environment.GetEnvironmentVariable("PYTHON")
            ?? throw new InvalidOperationException("请通过PYTHON指定固定CPython 3.12.13绝对路径");
        if (!Path.IsPathFullyQualified(python))
            throw new InvalidOperationException("PYTHON必须是固定CPython 3.12.13绝对路径");
        DirectoryInfo? source = new(AppContext.BaseDirectory);
        while (source is not null && !File.Exists(Path.Combine(source.FullName, "tests/textwrap_oracle.py")))
            source = source.Parent;
        if (source is null) throw new InvalidOperationException("找不到手写tests/textwrap_oracle.py输入");

        string root = Path.Combine(Path.GetTempPath(), "ryupy-textwrap-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var start = new ProcessStartInfo(python)
            {
                WorkingDirectory = root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            foreach (string argument in new[] { "-I", "-B", "-S", Path.Combine(source.FullName, "tests/textwrap_oracle.py"), mode })
                start.ArgumentList.Add(argument);
            start.Environment.Remove("PYTHONHOME");
            start.Environment.Remove("PYTHONPATH");
            start.Environment["LC_ALL"] = "C";
            start.Environment["TZ"] = "UTC";
            using var process = Process.Start(start) ?? throw new InvalidOperationException("固定CPython启动失败");
            var output = process.StandardOutput.ReadToEndAsync();
            var errors = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(60000))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
                throw new TimeoutException("textwrap固定CPython预期生成超时");
            }
            string stderr = errors.GetAwaiter().GetResult();
            if (process.ExitCode != 0 || stderr.Length != 0)
                throw new InvalidOperationException($"textwrap固定CPython失败，退出码{process.ExitCode}：{stderr}");
            return JsonDocument.Parse(output.GetAwaiter().GetResult());
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
