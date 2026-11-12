// 内部认证的无payload标准库协议根；不更改普通用户类的可变性，也不伪造MI。
namespace RyuPy.Runtime;
public sealed partial class PyClass
{
    internal bool IsFrozenNativeProtocolRoot { get; private set; }
    internal void FreezeNativeProtocolRoot() => IsFrozenNativeProtocolRoot = true;
    private void GuardNativeProtocolMutation()
    {
        if (IsFrozenNativeProtocolRoot) throw new PyTypeError($"cannot set attributes of immutable type '{Name}'");
    }
}
