# 可重建严格 libpython 内核原型

本目录实施 [ADR-0208](../../docs/adr/0208-cil-execution-libpython-kernel.md) 的 C4 前置内核。
由官方 CPython 3.12.13 源码构建，移除 frozen/deepfreeze 执行载荷，以纯 C 导入与 `_io`
完成启动和标准流。现有 auto provider 的 Python 3.9–3.15 产品合同继续保留。

| 维度 | 本目录严格内核 | 现有兼容产品 |
| --- | --- | --- |
| 版本 | 首切 CPython 3.12.13 release/GIL | Python 3.9–3.15 release/GIL |
| 来源 | 官方源码与版本专属精确锚点 | 已有自动探测、PBS 分发与许可闭包 |
| 平台 | macOS arm64 已实测；Linux/macOS x64 待验；Windows 构建待实现 | 保持已有三平台回归 |
| 产品接入 | 独立 C 探针，尚未接入 RyuPy.CPython/CLI | 既有 JIT/standalone/onefile |

## 源码变换与构建

`build_kernel.py` 从 python.org 获取源码，或使用显式 `--source-tarball` 外部副本。
`transform.py` 对全部版本与结构锚点先做唯一匹配预检，失配时在写入前失败。
没有普通文件摘要校验，也不生成 patch/diff 文件。

- `anchors_eval_import.py`：固定发布版本、默认求值器计数与拒绝、内建导入。
- `anchors_frozen_build.py`：清空 frozen/deepfreeze 表及其构建依赖，跳过 getpath 字节码。
- `anchors_guards.py`：无 importlib 的明确失败边界、PEP 523 严格分派保护。
- `anchors_stdio_codecs.py`：纯 C UTF-8 codec 与 `_io` 标准流启动链。
- `ryupy_utf8_codec.h` / `ryupy_utf8_incremental.h`：C codec 注册、增量编码与解码类型。

上游源码、构建树、前缀和日志全部位于仓库外；默认结束后清理，显式 `--keep` 才保留。
交付前缀包含动态库、头文件与 Python LICENSE，不包含 `.py/.pyc`。
内建 `math` 经 Setup.local 静态编入；浮点和整数运算均调用真实 C 模块。

```bash
python3 -B runtime/kernel/test_transform.py
python3 -B runtime/kernel/build_kernel.py --transform-only
python3 -B runtime/kernel/build_kernel.py
# 使用仓库外已有官方源码包；调试保留需显式 --keep
python3 -B runtime/kernel/build_kernel.py --source-tarball /外部路径/Python-3.12.13.tar.xz
```

## 严格求值与三阶段统计

`Include/ryupy_kernel.h` 导出：

- `rp_kernel_version()`：静态版本字符串 `ryupy-cpython-3.12.13-kernel-proto-v1`。
- `rp_kernel_set_phase(int)` / `rp_kernel_get_phase()`：startup=0、runtime=1、shutdown=2。
- `rp_kernel_set_strict_reject(int)` / `rp_kernel_get_strict_reject()`。
- `rp_kernel_reset_eval_stats()`。
- `rp_kernel_eval_entry_count(phase)`、`rp_kernel_eval_executed_count(phase)`、
  `rp_kernel_eval_rejected_count(phase)`、`rp_kernel_eval_total_entries()`、
  `rp_kernel_eval_total_rejected()`。

严格模式在默认求值器执行字节码前记录拒绝并抛 RuntimeError，沿 CPython 原帧清理路径
安全退栈。分派优先送入该门，预先安装的 PEP 523 求值器也无法绕过；真实生成器 throw
被拒绝时，其注入异常保留为拒绝异常的 `__context__`。

嵌入宿主须在开始求值前设置严格模式。诊断关闭模式统计默认求值器入口；CPython 内联
子帧不会逐帧重入该入口。阶段切换和计数清零在无活动求值的边界执行。
零字节码准入要求严格模式覆盖整个启动/运行/关闭链。

## 纯 C UTF-8 标准流

在 `_install_importlib=0` 下注册 C UTF-8 CodecInfo 和增量编解码器，复用 C 错误处理器；
不导入 `encodings` Python 包。仅支持 UTF-8 及上游同义名，其它编码明确失败。
标准流直接使用 `_io.FileIO`、BufferedReader/Writer 和 TextIOWrapper，保留：

- `sys.std*` 与 `sys.__std*__` 的真实身份、编码、errors、buffer、mode/name 等字段。
- 中文 print/input、提示刷新、EOF、文本/字节缓冲、增量字符跨 chunk、换行处理。
- strict/replace/surrogateescape/backslashreplace 与自定义 C 错误处理器。
- fileno/isatty/readable/writable、flush、close、关闭后错误及 finalize 自动刷新。
- `closefd=False`：关闭 Python 流或解释器后，宿主 0/1/2 描述符继续可用。
- 坏 fd 对应流为 None；非 UTF-8 配置、目录 stdin 与错误处理配置按真实阶段失败。

增量编码器的 final 参数不求真值；解码器支持标准二元状态、reset，以及不可变 bytes
状态的身份保留。CodecInfo 的 streamreader/streamwriter 为 None；完整 `codecs` Python
模块、流编解码器类及任意自定义状态/反射属性属于后续闭包编译合同。

## 已运行的验证

2026-11-16 macOS arm64 完整源码构建与十五个独立探针模式通过：

| 探针 | 覆盖 |
| --- | --- |
| kernel_probe 四模式 | 零帧启动/内建 math/关闭；EvalCode 拒绝；诊断阶段计数 (1,2,1)；importlib 初始化拒绝 |
| import_guard_probe 六模式 | GetMagicNumber、ExecCode 两入口、模块 repr、自定义 evaluator、生成器 throw 上下文 |
| stdio_codecs_probe 两模式 | UTF-8/增量/错误处理/状态；标准流、输入输出、缓冲与 finalize |
| stdio_edge_probe 三模式 | closefd、坏 fd、无缓冲与错误初始化 |

五个 stdio/codecs 模式在同次未修改 CPython 3.12.13 上编译运行同一 C 输入并通过。
标准流和 codec 成功路径的严格内核 startup/runtime/shutdown entries/executed/rejected
均为零；拒绝探针按预期记录入口和拒绝，执行数为零。

原公开 importlib API 的三个空指针崩溃已实测复现并修复；内部 loader 三个非导出入口
完成精确源码守卫，尚待后续真实消费者验证。所有未测平台继续列为待验。

## 后续

当前导入只支持 sys.modules 与 PyImport_Inittab 内建模块。动态 `.so/.pyd`、包 fromlist、
相对导入、CIL 反向导入和 reload 尚未接入；缺口返回明确异常。模块 repr 当前明确拒绝，
完整原生元数据后续另切。Windows 探针需替换 POSIX pipe/fork 层并实现 MSVC/DLL 构建。
完整 Python 依赖闭包、全部受支持 ABI、产品接入及三平台验收仍由 C4/C5 推进。
