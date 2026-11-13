// Windows有界词法路径：共享盘符切分，区分os.path原文本操作与pathlib清理，不访问文件系统。
using System;
using System.Linq;

namespace RyuPy.Runtime;

/// <summary>供宿主Path和os.path限定入口使用；仅支持extended盘符绝对路径与完整extended UNC；其他设备命名空间仍拒绝。</summary>
public static class PyWindowsPath
{
    private static bool Separator(char value) => value is '/' or '\\';

    // extended DOS/UNC保留独立drive身份；按ntpath接受两种分隔符，其他设备命名空间不冒充UNC。
    private static (string Drive, string Tail) SplitDrive(string path)
    {
        if (path.Length >= 2 && Separator(path[0]) && Separator(path[1]))
        {
            if (path.Length >= 4 && (path[2] is '?' or '.') && Separator(path[3]))
            {
                // 保留extended身份与尾点/空格；不剥前缀，不将设备路径变成普通文件路径。
                if (path[2] == '?' && path.Length >= 7 && char.IsAsciiLetter(path[4])
                    && path[5] == ':' && Separator(path[6]))
                    return (path[..6], path[6..]);
                if (path[2] == '?' && path.Length >= 8
                    && path.AsSpan(4, 3).Equals("UNC", StringComparison.OrdinalIgnoreCase) && Separator(path[7]))
                {
                    int server = 8;
                    while (server < path.Length && !Separator(path[server])) server++;
                    int share = server + 1;
                    while (share < path.Length && !Separator(path[share])) share++;
                    if (server > 8 && server + 1 < share)
                        return (path[..share], path[share..]);
                }
                throw new PyNotImplementedError("Windows device namespace paths are not supported yet");
            }
            int serverEnd = 2;
            while (serverEnd < path.Length && !Separator(path[serverEnd])) serverEnd++;
            if (serverEnd == path.Length) return (path, "");
            int shareEnd = serverEnd + 1;
            while (shareEnd < path.Length && !Separator(path[shareEnd])) shareEnd++;
            return (path[..shareEnd], path[shareEnd..]);
        }
        // 单个前导分隔符优先表示当前盘根，后随冒号不能被误认成盘符。
        return path.Length >= 2 && !Separator(path[0]) && path[1] == ':' ? (path[..2], path[2..]) : ("", path);
    }

    // pathlib使用同一drive切分，但仅开放完整UNC；不将残缺网络根猜成文件路径。
    private static (string Drive, string Root, string[] Parts) Parse(string path)
    {
        path = path.Replace('/', '\\');
        var (drive, tail) = SplitDrive(path);
        bool extendedDisk = drive.StartsWith("\\\\?\\", StringComparison.Ordinal) && drive.Length == 6 && drive[5] == ':';
        bool unc = drive.StartsWith("\\\\", StringComparison.Ordinal) && !extendedDisk;
        int uncStart = drive.StartsWith("\\\\?\\UNC\\", StringComparison.OrdinalIgnoreCase) ? 8 : 2;
        if (unc && (drive[uncStart..].Split('\\') is not [var server, var share]
            || server.Length == 0 || share.Length == 0))
            throw new PyNotImplementedError("incomplete Windows UNC paths are not supported yet");
        string root = unc || tail.StartsWith('\\') ? "\\" : "";
        return (drive, root, tail.Split('\\').Where(p => p.Length > 0 && p != ".").ToArray());
    }

    private static string Render(string drive, string root, string[] parts)
    {
        string tail = string.Join("\\", parts);
        // 去掉前导.后不能把普通文件名C:foo重新解释为盘符相对路径。
        if (drive.Length == 0 && root.Length == 0 && tail.Length >= 2 && tail[1] == ':')
            tail = ".\\" + tail;
        string value = drive + root + tail;
        return value.Length == 0 ? "." : value;
    }

    /// <summary>pathlib清理保留父目录分量，不查询当前目录或文件系统。</summary>
    internal static string Clean(string path)
    {
        var (drive, root, parts) = Parse(path);
        return Render(drive, root, parts);
    }

    /// <summary>盘符与根组成anchor；完整UNC共享根自带尾分隔符。</summary>
    internal static string Anchor(string path)
    {
        var (drive, root, _) = Parse(path);
        return drive + root;
    }

    /// <summary>pathlib末端名称，根及空路径没有名称。</summary>
    internal static string Name(string path)
    {
        var (_, _, parts) = Parse(path);
        return parts.Length == 0 ? "" : parts[^1];
    }

    /// <summary>词法父目录不解析..，根的父目录保持根。</summary>
    internal static string Parent(string path)
    {
        var (drive, root, parts) = Parse(path);
        return Render(drive, root, parts.Length == 0 ? parts : parts[..^1]);
    }

    /// <summary>anchor作为单个分量；点路径的parts为空。</summary>
    internal static string[] Parts(string path)
    {
        var (drive, root, parts) = Parse(path);
        return drive.Length + root.Length == 0 ? parts : [drive + root, .. parts];
    }

    /// <summary>pathlib必须同时具备盘符与根，根相对路径不是绝对路径。</summary>
    internal static bool IsAbsolute(string path)
    {
        var (drive, root, _) = Parse(path);
        return drive.Length > 0 && root.Length > 0;
    }

    /// <summary>CPython3.12 ntpath.isabs仍接受无盘符根相对路径，与pathlib不同。</summary>
    internal static bool OsIsAbsolute(string path)
    {
        var (_, root, _) = Parse(path);
        return root.Length > 0;
    }

    /// <summary>os.path.basename保留尾分隔符导致的空名称，不作pathlib清理。</summary>
    internal static string BaseName(string path)
    {
        var (_, tail) = SplitDrive(path);
        int start = tail.Length;
        while (start > 0 && !Separator(tail[start - 1])) start--;
        return tail[start..];
    }

    /// <summary>ASCII比较与哈希共用键；Unicode大小写未验，不以CLR比较冒充Python。</summary>
    internal static string ComparisonKey(string path)
    {
        string clean = Clean(path);
        if (clean.Any(c => c > 127))
            throw new PyNotImplementedError("non-ASCII Windows Path equality and hashing are not supported yet");
        return clean.ToLowerInvariant();
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
