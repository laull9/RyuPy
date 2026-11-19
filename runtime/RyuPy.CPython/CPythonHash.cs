// 跨执行域字典键的哈希合同：精确内建不可变值复用Runtime算法，用户类型保留CPython协议。
using System.Runtime.InteropServices;
using RyuPy.Runtime;

namespace RyuPy.CPython;

public sealed partial class CPythonHost
{
    /// <summary>调用者持有GIL与对象引用；只读取精确内建值，绝不按短类型名猜测布局。</summary>
    internal long HashForRuntime(PythonObject value)
    {
        nint pointer = value.Pointer;
        switch (Native.rp_scalar_kind(pointer))
        {
            case 1: return PyHash.Of(value.ReadScalar("int"));
            case 2: return PyHash.Of(Native.rp_truth(pointer) != 0);
            case 3:
                double number = Native.rp_double(pointer); ThrowIfError();
                // NaN没有跨域值相等合同，保留CPython对象本身的身份哈希。
                if (!double.IsNaN(number)) return PyHash.Of(number);
                break;
            case 4: return PyHash.Of(value.ReadScalar("str"));
            case 5:
                nint bytes = Check(Native.rp_bytes_view(pointer, out long length));
                var contents = new byte[checked((int)length)];
                Marshal.Copy(bytes, contents, 0, contents.Length);
                return PyHash.Of(new PyBytes(contents));
        }
        if (Native.rp_exact_tuple(pointer) != 0)
        {
            int length = checked((int)Native.rp_length(pointer)); ThrowIfError();
            var members = new object?[length];
            // 元素仍使用统一身份代理；PyHash递归回到本合同，不复制或替换可观察对象。
            for (int index = 0; index < length; index++)
                members[index] = FromOwned(Native.rp_op(18, pointer, n: index));
            return PyHash.Of(new PyTuple(members));
        }
        long result = Native.rp_hash(pointer); ThrowIfError(); return result;
    }
}
