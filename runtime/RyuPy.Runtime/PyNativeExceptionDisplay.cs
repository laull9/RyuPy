// 原生模块异常的终端限定名；实例repr与类型短名不受影响。
namespace RyuPy.Runtime;

internal static class PyNativeExceptionDisplay
{
    internal static string Name(PyBaseException error) => error is PyGetoptError
        ? "getopt." + error.PyTypeName : PyOptParseExceptions.DisplayName(error);
}
