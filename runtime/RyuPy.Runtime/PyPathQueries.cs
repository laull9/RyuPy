// pathlib 查询保留权限异常；不能直接复用会吞掉所有 IO 错误的 File.Exists。
using System;
using System.IO;

namespace RyuPy.Runtime;

/// <summary>路径查询的属性探针，缺失/非目录返回空，权限等其它错误保留 Python 异常。</summary>
public static class PyPathQueries
{
    /// <summary>查询最终目标的属性。reader 可注入后端错误以验证 root 环境下的权限语义。</summary>
    public static FileAttributes? Probe(string path, Func<string, FileAttributes>? reader = null)
    {
        if (path.Contains('\0')) return null;
        try { return (reader ?? ReadFollowingLinks)(path); }
        catch (Exception error) when (error is FileNotFoundException or DirectoryNotFoundException) { return null; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { throw PyIO.FileSystemError(error); }
    }

    private static FileAttributes ReadFollowingLinks(string path)
    {
        FileAttributes attributes = File.GetAttributes(path);
        if ((attributes & FileAttributes.ReparsePoint) == 0) return attributes;
        FileSystemInfo info = (attributes & FileAttributes.Directory) != 0
            ? new DirectoryInfo(path) : new FileInfo(path);
        FileSystemInfo? target = info.ResolveLinkTarget(true);
        return target is null ? attributes : File.GetAttributes(target.FullName);
    }
}
