// Windows dirname/join 的有限词法实现：保留原分隔符与文本，不调用文件系统或将路径规范化为绝对路径。
using System;

namespace RyuPy.Runtime;

/// <summary>仅供 os.path 的两个已验入口及跨平台纯算法测试；不是完整 ntpath 模块。</summary>
public static class PyWindowsPath
{
    private static bool Separator(char value) => value is '/' or '\\';

    // 盘符、UNC share 和剩余路径分开保存。设备命名空间没有在本切冒充普通 UNC。
    private static (string Drive, string Tail) SplitDrive(string path)
    {
        if (path.Length >= 2 && Separator(path[0]) && Separator(path[1]))
        {
            if (path.Length >= 4 && (path[2] is '?' or '.') && Separator(path[3]))
                throw new PyNotImplementedError("Windows device namespace paths are not supported yet");
            int serverEnd = 2;
            while (serverEnd < path.Length && !Separator(path[serverEnd])) serverEnd++;
            if (serverEnd == path.Length) return (path, "");
            int shareEnd = serverEnd + 1;
            while (shareEnd < path.Length && !Separator(path[shareEnd])) shareEnd++;
            return (path[..shareEnd], path[shareEnd..]);
        }
        return path.Length >= 2 && path[1] == ':' ? (path[..2], path[2..]) : ("", path);
    }

    /// <summary>保留盘符/UNC 根及输入分隔符；普通末端名称移除，不消除点分量。</summary>
    public static string DirName(string path)
    {
        var (drive, tail) = SplitDrive(path);
        int end = tail.Length;
        while (end > 0 && !Separator(tail[end - 1])) end--;
        int trimmed = end;
        while (trimmed > 0 && Separator(tail[trimmed - 1])) trimmed--;
        // 全部由分隔符构成的 head 是根，不能裁为空串。
        return drive + tail[..(trimmed == 0 ? end : trimmed)];
    }

    private static bool SameDrive(string left, string right)
    {
        if (left == right) return true;
        if (left.Length == 0 || right.Length == 0) return false;
        // CP 的 Unicode lower 不能用 CLR 文化比较替代；尾部 Unicode 不参与此限制。
        foreach (char c in left)
            if (c > 127) throw new PyNotImplementedError("non-ASCII Windows drive comparison is not supported yet");
        foreach (char c in right)
            if (c > 127) throw new PyNotImplementedError("non-ASCII Windows drive comparison is not supported yet");
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>按原顺序转换每个 PathLike；根片段重置尾部，不错误丢掉已有盘符。</summary>
    public static string Join(object?[] parts)
    {
        if (parts.Length == 0) throw new PyTypeError("join() missing required argument: 'a'");
        var (drive, tail) = SplitDrive(PyOsPath.Text(parts[0]));
        for (int index = 1; index < parts.Length; index++)
        {
            var (nextDrive, nextTail) = SplitDrive(PyOsPath.Text(parts[index]));
            if (nextTail.Length > 0 && Separator(nextTail[0]))
            {
                if (nextDrive.Length > 0 || drive.Length == 0) drive = nextDrive;
                tail = nextTail;
                continue;
            }
            if (nextDrive.Length > 0 && nextDrive != drive)
            {
                if (!SameDrive(drive, nextDrive))
                {
                    drive = nextDrive; tail = nextTail; continue;
                }
                drive = nextDrive;
            }
            if (tail.Length > 0 && !Separator(tail[^1])) tail += "\\";
            tail += nextTail;
        }
        // UNC share 与相对尾部间需要分隔符；C: 相对路径则不能误变成 C:\ 根路径。
        if (drive.Length > 0 && drive[^1] != ':' && tail.Length > 0 && !Separator(tail[0]))
            return drive + "\\" + tail;
        return drive + tail;
    }
}
