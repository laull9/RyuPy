"""CPython 3.12.13 纯 C UTF-8 编解码器与 _io 标准流锚点集合。

本模块定义针对官方 CPython 3.12.13 源码树的 stdio / codecs 竖切变换，严禁修改
`anchors_eval_import.py` 的求值器与导入路由逻辑，亦不依赖任何 frozen `.py`/`.pyc`：
1. 注入 `Python/ryupy_utf8_codec.h`：实现纯 C UTF-8 `CodecInfo`、`Utf8IncrementalEncoder`
   与 `Utf8IncrementalDecoder`，复用内建 C 错误处理器，仅匹配 UTF-8 及其标准别名。
2. `Python/codecs.c`：在 `_install_importlib=0` 时跳过 `import encodings`，改为向
   `codec_search_path` 注册纯 C UTF-8 搜索函数并标记 `codecs_initialized = 1`。
3. `Python/pylifecycle.c`：在 `init_interp_main` 的 `!config->_install_importlib` 分支中
   初始化 `_PyUnicode_InitEncodings`、强制校验 UTF-8 配置、调用 `init_sys_streams`（底层
   直接导入内建 `_io` 模块以实例化真实 `FileIO(closefd=False)` / `BufferedReader` /
   `BufferedWriter` / `TextIOWrapper`）、设置 `builtins.open = _io.open` 与 `__main__`。
"""
from __future__ import annotations

from pathlib import Path

from transform import AnchorEdit, NewHeaderFile


_CODEC_HEADER_TEXT = Path(__file__).with_name("ryupy_utf8_codec.h").read_text(encoding="utf-8")
_INCREMENTAL_HEADER_TEXT = Path(__file__).with_name("ryupy_utf8_incremental.h").read_text(
    encoding="utf-8"
)

RYUPY_UTF8_CODEC_HEADER = NewHeaderFile(
    rel_path="Python/ryupy_utf8_codec.h",
    content=_CODEC_HEADER_TEXT,
)

RYUPY_UTF8_INCREMENTAL_HEADER = NewHeaderFile(
    rel_path="Python/ryupy_utf8_incremental.h",
    content=_INCREMENTAL_HEADER_TEXT,
)


def build_stdio_codecs_anchors() -> list[AnchorEdit]:
    """返回纯 C UTF-8 编解码注册与 _io 标准流初始化所需的精确源码锚点。"""
    return [
        AnchorEdit(
            rel_path="Python/codecs.c",
            name="codecs_include_ryupy_utf8_codec",
            old="extern int _Py_normalize_encoding(const char *, char *, size_t);",
            new=(
                "extern int _Py_normalize_encoding(const char *, char *, size_t);\n"
                '#include "ryupy_utf8_codec.h"'
            ),
        ),
        AnchorEdit(
            rel_path="Python/codecs.c",
            name="codecs_registry_init_without_encodings",
            old=(
                '    mod = PyImport_ImportModule("encodings");\n'
                "    if (mod == NULL) {\n"
                "        return -1;\n"
                "    }\n"
                "    Py_DECREF(mod);\n"
                "    interp->codecs_initialized = 1;\n"
                "    return 0;"
            ),
            new=(
                "    if (!_PyInterpreterState_GetConfig(interp)->_install_importlib) {\n"
                "        if (rp_kernel_register_utf8_codec(interp) < 0) {\n"
                "            return -1;\n"
                "        }\n"
                "        interp->codecs_initialized = 1;\n"
                "        return 0;\n"
                "    }\n"
                "\n"
                '    mod = PyImport_ImportModule("encodings");\n'
                "    if (mod == NULL) {\n"
                "        return -1;\n"
                "    }\n"
                "    Py_DECREF(mod);\n"
                "    interp->codecs_initialized = 1;\n"
                "    return 0;"
            ),
        ),
        AnchorEdit(
            rel_path="Python/pylifecycle.c",
            name="pylifecycle_init_interp_main_stdio_codecs",
            old=(
                "    if (!config->_install_importlib) {\n"
                "        /* Special mode for freeze_importlib: run with no import system\n"
                "         *\n"
                "         * This means anything which needs support from extension modules\n"
                "         * or pure Python code in the standard library won't work.\n"
                "         */\n"
                "        if (is_main_interp) {\n"
                "            interp->runtime->initialized = 1;\n"
                "        }\n"
                "        return _PyStatus_OK();\n"
                "    }"
            ),
            new=(
                "    if (!config->_install_importlib) {\n"
                "        /* RyuPy 严格零字节码内核：在 _install_importlib=0 下启用纯 C UTF-8 编解码\n"
                "         * 与真实内建 _io 标准流，同时维持零 frozen importlib / 零 site 边界。 */\n"
                "        status = _PyUnicode_InitEncodings(tstate);\n"
                "        if (_PyStatus_EXCEPTION(status)) {\n"
                "            return status;\n"
                "        }\n"
                "        if (!interp->unicode.fs_codec.utf8 ||\n"
                "            config->stdio_encoding == NULL ||\n"
                '            wcscmp(config->stdio_encoding, L"utf-8") != 0)\n'
                "        {\n"
                '            return _PyStatus_ERR("RyuPy strict kernel requires UTF-8 stdio and filesystem encodings");\n'
                "        }\n"
                "        status = init_sys_streams(tstate);\n"
                "        if (_PyStatus_EXCEPTION(status)) {\n"
                "            return status;\n"
                "        }\n"
                "        status = init_set_builtins_open();\n"
                "        if (_PyStatus_EXCEPTION(status)) {\n"
                "            return status;\n"
                "        }\n"
                "        status = add_main_module(interp);\n"
                "        if (_PyStatus_EXCEPTION(status)) {\n"
                "            return status;\n"
                "        }\n"
                "        if (is_main_interp) {\n"
                "            interp->runtime->initialized = 1;\n"
                "        }\n"
                "        return _PyStatus_OK();\n"
                "    }"
            ),
        ),
        AnchorEdit(
            rel_path="Python/pylifecycle.c",
            name="pylifecycle_builtins_open_use_builtin_io",
            old='    if (!(wrapper = _PyImport_GetModuleAttrString("io", "open"))) {',
            new=(
                '    const char *io_mod_name = _PyInterpreterState_GetConfig(\n'
                '        _PyInterpreterState_GET())->_install_importlib ? "io" : "_io";\n'
                '    if (!(wrapper = _PyImport_GetModuleAttrString(io_mod_name, "open"))) {'
            ),
        ),
        AnchorEdit(
            rel_path="Python/pylifecycle.c",
            name="pylifecycle_init_sys_streams_use_builtin_io",
            old='    if (!(iomod = PyImport_ImportModule("io"))) {',
            new=(
                '    const char *io_mod_name = config->_install_importlib ? "io" : "_io";\n'
                "    if (!(iomod = PyImport_ImportModule(io_mod_name))) {"
            ),
        ),
        AnchorEdit(
            rel_path="Python/pylifecycle.c",
            name="pylifecycle_add_main_module_skip_builtin_importer",
            old=(
                "    loader = _PyDict_GetItemStringWithError(d, \"__loader__\");\n"
                "    if (loader == NULL || loader == Py_None) {\n"
                "        if (PyErr_Occurred()) {\n"
                '            return _PyStatus_ERR("Failed to test __main__.__loader__");\n'
                "        }\n"
                "        PyObject *loader = _PyImport_GetImportlibLoader(interp,\n"
                '                                                        "BuiltinImporter");\n'
                "        if (loader == NULL) {\n"
                '            return _PyStatus_ERR("Failed to retrieve BuiltinImporter");\n'
                "        }\n"
                '        if (PyDict_SetItemString(d, "__loader__", loader) < 0) {\n'
                '            return _PyStatus_ERR("Failed to initialize __main__.__loader__");\n'
                "        }\n"
                "        Py_DECREF(loader);\n"
                "    }"
            ),
            new=(
                '    loader = _PyDict_GetItemStringWithError(d, "__loader__");\n'
                "    if ((loader == NULL || loader == Py_None) &&\n"
                "        _PyInterpreterState_GetConfig(interp)->_install_importlib)\n"
                "    {\n"
                "        if (PyErr_Occurred()) {\n"
                '            return _PyStatus_ERR("Failed to test __main__.__loader__");\n'
                "        }\n"
                "        PyObject *loader = _PyImport_GetImportlibLoader(interp,\n"
                '                                                        "BuiltinImporter");\n'
                "        if (loader == NULL) {\n"
                '            return _PyStatus_ERR("Failed to retrieve BuiltinImporter");\n'
                "        }\n"
                '        if (PyDict_SetItemString(d, "__loader__", loader) < 0) {\n'
                '            return _PyStatus_ERR("Failed to initialize __main__.__loader__");\n'
                "        }\n"
                "        Py_DECREF(loader);\n"
                "    }\n"
                "    else if (PyErr_Occurred()) {\n"
                '        return _PyStatus_ERR("Failed to test __main__.__loader__");\n'
                "    }"
            ),
        ),
    ]
