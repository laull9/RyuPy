"""CPython 3.12.13 清空 frozen/deepfreeze 载荷与构建规则的锚点集合。

本模块定义针对官方 CPython 3.12.13 源码树的后半组结构变换：
1. `Python/frozen.c`：清空 `_PyImport_FrozenBootstrap` / `Stdlib` / `Test` / `Aliases`
   表为空哨兵，彻底去除内置字节码数组与 deepfreeze 符号引用。
2. `Modules/getpath.c`：移除 `frozen_modules/getpath.h`，并在需要计算路径配置或调用
   `_Py_Get_Getpath_CodeObject` 时显式报错拒绝。
3. `Makefile.pre.in`：将 `$(DEEPFREEZE_C)` 替换为仅导出空初始化/销毁函数的 C stub，并切断
   `Modules/getpath.o` 与 `Python/frozen.o` 对 `frozen_modules/*.h` 的构建依赖。
"""
from __future__ import annotations

from transform import AnchorEdit


FROZEN_EMPTY_BODY = """\
#include "Python.h"
#include "pycore_import.h"

#include <stdbool.h>

/* RyuPy 严格零字节码内核原型：清空所有内置 frozen 与 deepfreeze 表，
 * 仅保留上游 ABI 符号以维持链接兼容并阻止任何内置字节码启动。 */
static const struct _frozen bootstrap_modules[] = {
    {0, 0, 0} /* bootstrap sentinel */
};
static const struct _frozen stdlib_modules[] = {
    {0, 0, 0} /* stdlib sentinel */
};
static const struct _frozen test_modules[] = {
    {0, 0, 0} /* test sentinel */
};
const struct _frozen *_PyImport_FrozenBootstrap = bootstrap_modules;
const struct _frozen *_PyImport_FrozenStdlib = stdlib_modules;
const struct _frozen *_PyImport_FrozenTest = test_modules;

static const struct _module_alias aliases[] = {
    {0, 0} /* aliases sentinel */
};
const struct _module_alias *_PyImport_FrozenAliases = aliases;

const struct _frozen *PyImport_FrozenModules = NULL;"""


def build_frozen_build_anchors() -> list[AnchorEdit]:
    """返回剥离 frozen/deepfreeze 载荷与切断冻结头文件构建依赖的精确锚点。"""
    return [
        AnchorEdit(
            rel_path="Python/frozen.c",
            name="frozen_empty_tables",
            old=(
                '#include "Python.h"\n'
                '#include "pycore_import.h"\n'
                "\n"
                "#include <stdbool.h>\n"
                "\n"
                "/* Includes for frozen modules: */\n"
                '#include "frozen_modules/importlib._bootstrap.h"\n'
                '#include "frozen_modules/importlib._bootstrap_external.h"\n'
                '#include "frozen_modules/zipimport.h"\n'
                '#include "frozen_modules/abc.h"\n'
                '#include "frozen_modules/codecs.h"\n'
                '#include "frozen_modules/io.h"\n'
                '#include "frozen_modules/_collections_abc.h"\n'
                '#include "frozen_modules/_sitebuiltins.h"\n'
                '#include "frozen_modules/genericpath.h"\n'
                '#include "frozen_modules/ntpath.h"\n'
                '#include "frozen_modules/posixpath.h"\n'
                '#include "frozen_modules/os.h"\n'
                '#include "frozen_modules/site.h"\n'
                '#include "frozen_modules/stat.h"\n'
                '#include "frozen_modules/importlib.util.h"\n'
                '#include "frozen_modules/importlib.machinery.h"\n'
                '#include "frozen_modules/runpy.h"\n'
                '#include "frozen_modules/__hello__.h"\n'
                '#include "frozen_modules/__phello__.h"\n'
                '#include "frozen_modules/__phello__.ham.h"\n'
                '#include "frozen_modules/__phello__.ham.eggs.h"\n'
                '#include "frozen_modules/__phello__.spam.h"\n'
                '#include "frozen_modules/frozen_only.h"\n'
                "/* End includes */\n"
                "\n"
                "#define GET_CODE(name) _Py_get_##name##_toplevel\n"
                "\n"
                "/* Start extern declarations */\n"
                "extern PyObject *_Py_get_importlib__bootstrap_toplevel(void);\n"
                "extern PyObject *_Py_get_importlib__bootstrap_external_toplevel(void);\n"
                "extern PyObject *_Py_get_zipimport_toplevel(void);\n"
                "extern PyObject *_Py_get_abc_toplevel(void);\n"
                "extern PyObject *_Py_get_codecs_toplevel(void);\n"
                "extern PyObject *_Py_get_io_toplevel(void);\n"
                "extern PyObject *_Py_get__collections_abc_toplevel(void);\n"
                "extern PyObject *_Py_get__sitebuiltins_toplevel(void);\n"
                "extern PyObject *_Py_get_genericpath_toplevel(void);\n"
                "extern PyObject *_Py_get_ntpath_toplevel(void);\n"
                "extern PyObject *_Py_get_posixpath_toplevel(void);\n"
                "extern PyObject *_Py_get_os_toplevel(void);\n"
                "extern PyObject *_Py_get_site_toplevel(void);\n"
                "extern PyObject *_Py_get_stat_toplevel(void);\n"
                "extern PyObject *_Py_get_importlib_util_toplevel(void);\n"
                "extern PyObject *_Py_get_importlib_machinery_toplevel(void);\n"
                "extern PyObject *_Py_get_runpy_toplevel(void);\n"
                "extern PyObject *_Py_get___hello___toplevel(void);\n"
                "extern PyObject *_Py_get___phello___toplevel(void);\n"
                "extern PyObject *_Py_get___phello___ham_toplevel(void);\n"
                "extern PyObject *_Py_get___phello___ham_eggs_toplevel(void);\n"
                "extern PyObject *_Py_get___phello___spam_toplevel(void);\n"
                "extern PyObject *_Py_get_frozen_only_toplevel(void);\n"
                "/* End extern declarations */\n"
                "\n"
                "static const struct _frozen bootstrap_modules[] = {\n"
                '    {"_frozen_importlib", _Py_M__importlib__bootstrap, (int)sizeof(_Py_M__importlib__bootstrap), false, GET_CODE(importlib__bootstrap)},\n'
                '    {"_frozen_importlib_external", _Py_M__importlib__bootstrap_external, (int)sizeof(_Py_M__importlib__bootstrap_external), false, GET_CODE(importlib__bootstrap_external)},\n'
                '    {"zipimport", _Py_M__zipimport, (int)sizeof(_Py_M__zipimport), false, GET_CODE(zipimport)},\n'
                "    {0, 0, 0} /* bootstrap sentinel */\n"
                "};\n"
                "static const struct _frozen stdlib_modules[] = {\n"
                "    /* stdlib - startup, without site (python -S) */\n"
                '    {"abc", _Py_M__abc, (int)sizeof(_Py_M__abc), false, GET_CODE(abc)},\n'
                '    {"codecs", _Py_M__codecs, (int)sizeof(_Py_M__codecs), false, GET_CODE(codecs)},\n'
                '    {"io", _Py_M__io, (int)sizeof(_Py_M__io), false, GET_CODE(io)},\n'
                "\n"
                "    /* stdlib - startup, with site */\n"
                '    {"_collections_abc", _Py_M___collections_abc, (int)sizeof(_Py_M___collections_abc), false, GET_CODE(_collections_abc)},\n'
                '    {"_sitebuiltins", _Py_M___sitebuiltins, (int)sizeof(_Py_M___sitebuiltins), false, GET_CODE(_sitebuiltins)},\n'
                '    {"genericpath", _Py_M__genericpath, (int)sizeof(_Py_M__genericpath), false, GET_CODE(genericpath)},\n'
                '    {"ntpath", _Py_M__ntpath, (int)sizeof(_Py_M__ntpath), false, GET_CODE(ntpath)},\n'
                '    {"posixpath", _Py_M__posixpath, (int)sizeof(_Py_M__posixpath), false, GET_CODE(posixpath)},\n'
                '    {"os.path", _Py_M__posixpath, (int)sizeof(_Py_M__posixpath), false, GET_CODE(posixpath)},\n'
                '    {"os", _Py_M__os, (int)sizeof(_Py_M__os), false, GET_CODE(os)},\n'
                '    {"site", _Py_M__site, (int)sizeof(_Py_M__site), false, GET_CODE(site)},\n'
                '    {"stat", _Py_M__stat, (int)sizeof(_Py_M__stat), false, GET_CODE(stat)},\n'
                "\n"
                "    /* runpy - run module with -m */\n"
                '    {"importlib.util", _Py_M__importlib_util, (int)sizeof(_Py_M__importlib_util), false, GET_CODE(importlib_util)},\n'
                '    {"importlib.machinery", _Py_M__importlib_machinery, (int)sizeof(_Py_M__importlib_machinery), false, GET_CODE(importlib_machinery)},\n'
                '    {"runpy", _Py_M__runpy, (int)sizeof(_Py_M__runpy), false, GET_CODE(runpy)},\n'
                "    {0, 0, 0} /* stdlib sentinel */\n"
                "};\n"
                "static const struct _frozen test_modules[] = {\n"
                '    {"__hello__", _Py_M____hello__, (int)sizeof(_Py_M____hello__), false, GET_CODE(__hello__)},\n'
                '    {"__hello_alias__", _Py_M____hello__, (int)sizeof(_Py_M____hello__), false, GET_CODE(__hello__)},\n'
                '    {"__phello_alias__", _Py_M____hello__, (int)sizeof(_Py_M____hello__), true, GET_CODE(__hello__)},\n'
                '    {"__phello_alias__.spam", _Py_M____hello__, (int)sizeof(_Py_M____hello__), false, GET_CODE(__hello__)},\n'
                '    {"__phello__", _Py_M____phello__, (int)sizeof(_Py_M____phello__), true, GET_CODE(__phello__)},\n'
                '    {"__phello__.__init__", _Py_M____phello__, (int)sizeof(_Py_M____phello__), false, GET_CODE(__phello__)},\n'
                '    {"__phello__.ham", _Py_M____phello___ham, (int)sizeof(_Py_M____phello___ham), true, GET_CODE(__phello___ham)},\n'
                '    {"__phello__.ham.__init__", _Py_M____phello___ham, (int)sizeof(_Py_M____phello___ham), false, GET_CODE(__phello___ham)},\n'
                '    {"__phello__.ham.eggs", _Py_M____phello___ham_eggs, (int)sizeof(_Py_M____phello___ham_eggs), false, GET_CODE(__phello___ham_eggs)},\n'
                '    {"__phello__.spam", _Py_M____phello___spam, (int)sizeof(_Py_M____phello___spam), false, GET_CODE(__phello___spam)},\n'
                '    {"__hello_only__", _Py_M__frozen_only, (int)sizeof(_Py_M__frozen_only), false, GET_CODE(frozen_only)},\n'
                "    {0, 0, 0} /* test sentinel */\n"
                "};\n"
                "const struct _frozen *_PyImport_FrozenBootstrap = bootstrap_modules;\n"
                "const struct _frozen *_PyImport_FrozenStdlib = stdlib_modules;\n"
                "const struct _frozen *_PyImport_FrozenTest = test_modules;\n"
                "\n"
                "static const struct _module_alias aliases[] = {\n"
                '    {"_frozen_importlib", "importlib._bootstrap"},\n'
                '    {"_frozen_importlib_external", "importlib._bootstrap_external"},\n'
                '    {"os.path", "posixpath"},\n'
                '    {"__hello_alias__", "__hello__"},\n'
                '    {"__phello_alias__", "__hello__"},\n'
                '    {"__phello_alias__.spam", "__hello__"},\n'
                '    {"__phello__.__init__", "<__phello__"},\n'
                '    {"__phello__.ham.__init__", "<__phello__.ham"},\n'
                '    {"__hello_only__", NULL},\n'
                "    {0, 0} /* aliases sentinel */\n"
                "};\n"
                "const struct _module_alias *_PyImport_FrozenAliases = aliases;\n"
                "\n"
                "\n"
                "/* Embedding apps may change this pointer to point to their favorite\n"
                "   collection of frozen modules: */\n"
                "\n"
                "const struct _frozen *PyImport_FrozenModules = NULL;"
            ),
            new=FROZEN_EMPTY_BODY,
        ),
        AnchorEdit(
            rel_path="Modules/getpath.c",
            name="getpath_remove_frozen_header",
            old=(
                "/* Reference the precompiled getpath.py */\n"
                '#include "../Python/frozen_modules/getpath.h"'
            ),
            new="/* RyuPy 严格零字节码内核：已移除预编译 getpath.py 字节码头文件。 */",
        ),
        AnchorEdit(
            rel_path="Modules/getpath.c",
            name="getpath_disable_codeobject",
            old=(
                "PyObject *\n"
                "_Py_Get_Getpath_CodeObject(void)\n"
                "{\n"
                "    return PyMarshal_ReadObjectFromString(\n"
                "        (const char*)_Py_M__getpath, sizeof(_Py_M__getpath));\n"
                "}"
            ),
            new=(
                "PyObject *\n"
                "_Py_Get_Getpath_CodeObject(void)\n"
                "{\n"
                "    PyErr_SetString(PyExc_RuntimeError,\n"
                '                    "frozen getpath.py is disabled in RyuPy minimal kernel");\n'
                "    return NULL;\n"
                "}"
            ),
        ),
        AnchorEdit(
            rel_path="Modules/getpath.c",
            name="getpath_reject_compute_path_config",
            old=(
                "    if (_PyStatus_EXCEPTION(status) || !compute_path_config) {\n"
                "        return status;\n"
                "    }\n"
                "\n"
                "    if (!_PyThreadState_UncheckedGet()) {"
            ),
            new=(
                "    if (_PyStatus_EXCEPTION(status) || !compute_path_config) {\n"
                "        return status;\n"
                "    }\n"
                "\n"
                '    return PyStatus_Error("frozen getpath.py path computation is disabled in RyuPy minimal kernel");\n'
                "\n"
                "    if (!_PyThreadState_UncheckedGet()) {"
            ),
        ),
        AnchorEdit(
            rel_path="Makefile.pre.in",
            name="makefile_stub_deepfreeze",
            old=(
                "# BEGIN: deepfreeze modules\n"
                "$(DEEPFREEZE_C): $(DEEPFREEZE_DEPS)\n"
                "\t$(PYTHON_FOR_FREEZE) $(srcdir)/Tools/build/deepfreeze.py \\\n"
                "\tPython/frozen_modules/importlib._bootstrap.h:importlib._bootstrap \\\n"
                "\tPython/frozen_modules/importlib._bootstrap_external.h:importlib._bootstrap_external \\\n"
                "\tPython/frozen_modules/zipimport.h:zipimport \\\n"
                "\tPython/frozen_modules/abc.h:abc \\\n"
                "\tPython/frozen_modules/codecs.h:codecs \\\n"
                "\tPython/frozen_modules/io.h:io \\\n"
                "\tPython/frozen_modules/_collections_abc.h:_collections_abc \\\n"
                "\tPython/frozen_modules/_sitebuiltins.h:_sitebuiltins \\\n"
                "\tPython/frozen_modules/genericpath.h:genericpath \\\n"
                "\tPython/frozen_modules/ntpath.h:ntpath \\\n"
                "\tPython/frozen_modules/posixpath.h:posixpath \\\n"
                "\tPython/frozen_modules/os.h:os \\\n"
                "\tPython/frozen_modules/site.h:site \\\n"
                "\tPython/frozen_modules/stat.h:stat \\\n"
                "\tPython/frozen_modules/importlib.util.h:importlib.util \\\n"
                "\tPython/frozen_modules/importlib.machinery.h:importlib.machinery \\\n"
                "\tPython/frozen_modules/runpy.h:runpy \\\n"
                "\tPython/frozen_modules/__hello__.h:__hello__ \\\n"
                "\tPython/frozen_modules/__phello__.h:__phello__ \\\n"
                "\tPython/frozen_modules/__phello__.ham.h:__phello__.ham \\\n"
                "\tPython/frozen_modules/__phello__.ham.eggs.h:__phello__.ham.eggs \\\n"
                "\tPython/frozen_modules/__phello__.spam.h:__phello__.spam \\\n"
                "\tPython/frozen_modules/frozen_only.h:frozen_only \\\n"
                "\t-o Python/deepfreeze/deepfreeze.c\n"
                "# END: deepfreeze modules"
            ),
            new=(
                "# BEGIN: deepfreeze modules (RyuPy no-bytecode empty stub)\n"
                "$(DEEPFREEZE_C):\n"
                "\t@mkdir -p Python/deepfreeze\n"
                # 上游 deepfreeze 同时定义函数版本计数器；空载荷仍保留此 ABI 状态，
                # 从首个有效版本开始，供普通 PyFunction 创建与特化失效使用。
                "\t@printf '#include <stdint.h>\\nuint32_t _Py_next_func_version = 1;\\nint _Py_Deepfreeze_Init(void) { return 0; }\\nvoid _Py_Deepfreeze_Fini(void) {}\\n' > $(DEEPFREEZE_C)\n"
                "# END: deepfreeze modules"
            ),
        ),
        AnchorEdit(
            rel_path="Makefile.pre.in",
            name="makefile_getpath_drop_frozen_dep",
            old="Modules/getpath.o: $(srcdir)/Modules/getpath.c Python/frozen_modules/getpath.h Makefile $(PYTHON_HEADERS)",
            new="Modules/getpath.o: $(srcdir)/Modules/getpath.c Makefile $(PYTHON_HEADERS)",
        ),
        AnchorEdit(
            rel_path="Makefile.pre.in",
            name="makefile_frozen_drop_headers_dep",
            old="Python/frozen.o: $(FROZEN_FILES_OUT)",
            new="Python/frozen.o: $(srcdir)/Python/frozen.c Makefile $(PYTHON_HEADERS)",
        ),
    ]
