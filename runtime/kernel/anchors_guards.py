"""严格内核的拒绝边界：保护无 importlib 的公开/内部入口及自定义求值器旁路。

默认求值器仍统一拥有帧清理；严格模式将 PEP 523 分派送回该拒绝门，避免自定义
eval_frame 绕过统计。依赖 Python importlib 的接口在修改 sys.modules 前明确失败。
"""
from transform import AnchorEdit


def build_guard_anchors() -> list[AnchorEdit]:
    """只替换官方 3.12.13 的精确函数入口，保持普通诊断模式的原分派。"""
    edits = []
    # 各入口的失败值遵循既有 C API，调用方能按正常异常合同处理能力缺口。
    entries = [
        ("magic_number", '    long res;\n    PyInterpreterState *interp = _PyInterpreterState_GET();\n    PyObject *external, *pyc_magic;',
         '    long res;\n    PyInterpreterState *interp = _PyInterpreterState_GET();\n    PyObject *external, *pyc_magic;', "interp", "-1"),
        ("exec_code_pathnames", '    PyObject *nameobj, *pathobj = NULL, *cpathobj = NULL, *external= NULL;',
         '    PyObject *nameobj, *pathobj = NULL, *cpathobj = NULL, *external= NULL;', "_PyInterpreterState_GET()", "NULL"),
        ("exec_code_object", '    PyThreadState *tstate = _PyThreadState_GET();\n    PyObject *d, *external, *res;',
         '    PyThreadState *tstate = _PyThreadState_GET();\n    PyObject *d, *external, *res;', "tstate->interp", "NULL"),
    ]
    for name, old, prefix, interpreter, failure in entries:
        edits.append(AnchorEdit("Python/import.c", "guard_" + name, old,
                                prefix + _guard(interpreter, failure)))
    # 内部消费者也会由扩展/模块 repr 间接到达，不能把 NULL 交给对象协议。
    signatures = [
        ("loader", '_PyImport_GetImportlibLoader(PyInterpreterState *interp,\n                             const char *loader_name)'),
        ("external_loader", '_PyImport_GetImportlibExternalLoader(PyInterpreterState *interp,\n                                     const char *loader_name)'),
        ("bless_loader", '_PyImport_BlessMyLoader(PyInterpreterState *interp, PyObject *module_globals)'),
        ("module_repr", '_PyImport_ImportlibModuleRepr(PyInterpreterState *interp, PyObject *m)'),
    ]
    for name, signature in signatures:
        old = signature + "\n{"
        edits.append(AnchorEdit("Python/import.c", "guard_" + name, old,
                                old + _guard("interp", "NULL")))
    # 此入口由正常初始化调用；独立调用也按同一能力合同拒绝。
    old = "init_importlib_external(PyInterpreterState *interp)\n{"
    edits.append(AnchorEdit("Python/import.c", "guard_init_external", old,
                            old + _guard("interp", "-1")))
    # strict 模式优先，PEP 523 hook 即使先前已经安装也不能抢走本次帧。
    old = "    if (tstate->interp->eval_frame == NULL) {\n        return _PyEval_EvalFrameDefault(tstate, frame, throwflag);"
    new = "    if (rp_kernel_get_strict_reject() || tstate->interp->eval_frame == NULL) {\n        return _PyEval_EvalFrameDefault(tstate, frame, throwflag);"
    edits.append(AnchorEdit("Include/internal/pycore_ceval.h", "guard_custom_eval", old, new))
    old = '#include "pycore_pystate.h"       // _PyThreadState_GET()'
    edits.append(AnchorEdit("Include/internal/pycore_ceval.h", "guard_include_api", old,
                            old + '\n#include "ryupy_kernel.h"'))
    return edits


def _guard(interpreter: str, failure: str) -> str:
    """不获取 Python 引用，且在原函数有任何发布副作用之前返回。"""
    return (f"\n    if (IMPORTLIB({interpreter}) == NULL) {{\n"
            '        PyErr_SetString(PyExc_NotImplementedError,\n'
            '                        "Python importlib API is unavailable in strict RyuPy kernel mode");\n'
            f"        return {failure};\n    }}")
