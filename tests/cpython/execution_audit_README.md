# C0 显式 CPython 执行审计首切

以 `RYUPY_CPYTHON_EXECUTION_AUDIT=1` 开启，以现有
`RYUPY_CPYTHON_REPORT=/仓库外路径/host.json` 收取关闭后的报告。
`0` 或未设置时保留默认宿主行为，其他开关值在初始化前拒绝。
审计仅支持精确 CPython 3.12.13 release/GIL 发行与具有私有帧头文件的同版本 shim；
其他 3.9–3.15 发行保留默认宿主能力，显式开启审计时给出固定诊断。

本切片交付动态执行证据。`execution_audit.complete` 与 `startup_complete`
始终为 `false`；严格全 CIL 产品准入必须拒绝这些报告。

## 观察链与字段

静态 C shim 在 `Py_InitializeFromConfig` 之前注册原生 `PySys_AddAuditHook`。
首个已有 `PyThreadState` 的事件为主解释器安装 PEP 523 eval hook，读取真实
`_PyInterpreterFrame.f_code` 的文件名与函数名。随后普通 Python 函数调用、生成器
恢复、同解释器新线程及原生扩展反向调用都经过该逐解释器入口。观察逻辑不调用
Python helper、不保留 Python 对象引用，原 raised exception 在观测结束时原样恢复。

`startup` 覆盖已观察到的 CPython 初始化及 RyuPy 内部导入钩子安装；
`runtime` 从 `InstallImports` 完成后开始；`shutdown` 从宿主开始释放桥资源前开始，
延续至 `Py_FinalizeEx` 返回。关闭后读取 C 内存快照，不再访问解释器。

宿主报告新增 `execution_audit`：

- `enabled`、`complete`、`startup_complete`、`coverage`：开关与覆盖合同。
- `python_version`、`hook_installed`、`finalized`：选中 ABI、安装及关闭状态。
- `startup_frames`、`runtime_frames`、`shutdown_frames`：真实求值帧进入次数；
  生成器每次恢复也计一次。零次可能是该阶段没有执行 Python。
- `frames`：按 `phase/file/function` 聚合的行，每行含 `calls`。
- `dropped_frames`：元数据复制失败或超过 4096 个聚合行的次数。
- `coverage_failures`：阶段边界或原生审计事件观察到其他解释器/求值 hook 被替换的次数。

禁用模式仅输出前四个覆盖字段，且不调用新增原生 ABI，兼容未开启审计的旧 shim。
新增静态导出为 `rp_execution_audit_start/phase/count/status/row`；UTF-8 行文本由 shim
持有至进程退出，调用方只读，不承担 Python 引用或内存释放。

## 当前覆盖边界

安装之前可能已执行 frozen importlib；实测首个记录为 `<frozen getpath>`，
因此启动完整覆盖尚未建立。直接调用 `_PyEval_EvalFrameDefault` 绕过 PEP 523 的
原生消费者、其他解释器与恶意篡改不在完整覆盖合同内。后续需要在可重建 libpython
的默认求值器入口增加插桩，并把启动、所有解释器与关闭尾链一起纳入验收。
本切片不新增严格执行模式，不变更兼容默认，不替换现有执行入口。

## 定向运行

```sh
python3 -B tests/cpython/execution_audit.py \
  --python /选中发行/bin/python3.12 \
  --unsupported-python /其他发行/bin/python3.13
```

`--unsupported-python` 可选；提供时同时验证版本拒绝和该发行默认宿主仍可运行。
原生宿主驱动支持 macOS/Linux，Windows、NativeAOT 产品与搬迁需单独验收。

驱动即时构建真实产品 shim 与手写 C 宿主，分别用独立进程验证：启动 frozen 帧与
bootstrap helper、直接 C API 对象调用后的嵌套 Python 函数、原生代理反向回调、
异常保持、生成器恢复、后建线程、`atexit` 关闭回调，以及禁用和迟开启负例。
逐行聚合次数必须与阶段总计一致。构建、日志和运行目录全部位于仓库外并自动清理。
