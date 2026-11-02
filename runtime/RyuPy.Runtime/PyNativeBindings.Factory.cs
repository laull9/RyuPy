// 固定绑定的专用factory：仅参数绑定错误在这里准备Python报告；body有自己的窄抛出点。
using System;

namespace RyuPy.Runtime;

public static partial class PyNativeBindings
{
    /// <summary>构造真实一等PyFunction，签名一次创建，两个参数均位置专用且必填。</summary>
    public static PyFunction CreateI32Binary(string name, Func<object?[], object?> body)
    {
        PySignature signature = PySignature.Create(name, ["left", "right"], ["posonly", "posonly"], ["0", "0"]);
        return new PyFunction(name, signature, (self, args, keywords, values) =>
        {
            object?[] bound;
            try
            {
                bound = PyBinder.Bind(signature, args, keywords, values, self.Defaults);
            }
            catch (PyBaseException error)
            {
                // 只能捕Bind；用户body/参数校验/原生调用的异常不得在这里宽捕或重写。
                PyErrors.PrepareRaise(error);
                throw;
            }
            return body(bound) ?? PyNone.Instance;
        }, [null, null], false);
    }
}
