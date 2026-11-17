// 字面量**的mapping-only合并：复用逐项keys/getitem合同，原普通dict构造/update路径保持独立。
namespace RyuPy.Runtime;

public static partial class PyOps
{
    /// <summary>只接收mapping的字典字面量合并；有限标量键保证不触发未闭合的CLR hash/eq重入。</summary>
    public static void DictUnpackMapping(object target, object? source)
    {
        if (target is not PyDict dictionary)
            throw DictUpdateError(new PyTypeError("dict unpack target must be a dict"));
        // 新**形状在读取源前拒绝目标已有自定义hash键；检测本身绝不调用用户hash/eq。
        new DictUpdateTargetGuard(dictionary).Check();
        PyDict? raw = source as PyDict ?? (source as IPyNativeMapping)?.Dictionary;
        if (source is PyDictInstance wrapper)
        {
            wrapper.Class.RequireDictReady();
            if (!PyDictSubclassDispatch.HasCustomIterator(wrapper)) raw = wrapper.Payload;
        }
        if (raw is not null)
        {
            // 当前存储没有公开已存hash；拒绝这类新复制形状，避免重复执行用户__hash__。
            for (int slot = 0; slot < raw.SlotCount; slot++)
                if (raw.TryKeyAt(slot, out object? key)) DictUpdateRequireKey(key);
            if (!ReferenceEquals(raw, dictionary))
                foreach (var (key, value) in raw.Entries()) dictionary.Set(key, value);
            return;
        }
        if (source is PyInstanceIterator adapter) source = adapter.Instance;
        // 普通属性协议允许实例keys覆盖；foreign对象经中性属性/迭代/GetItem ABI参与同一流程。
        if (!DictUpdateKeys(source, out object? keysMethod))
            throw DictUpdateError(new PyTypeError($"'{PyRepr.TypeName(source)}' object is not a mapping"));
        DictUpdateMapping(dictionary, source, keysMethod, literal: true);
    }
}
