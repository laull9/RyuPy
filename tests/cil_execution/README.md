# 全 CIL 执行准入

按 [ADR-0223](../../docs/adr/0223-stock-libpython-bounded-glue.md)，本目录的零字节码门
保留为独立严格研究合同。stock 主线允许登记的必要胶水，另行验收执行清单与成本；
这里的完整覆盖字段和拒绝条件保持原义。

本目录保存 ADR-0208 的严格生命周期门；手写测试构造的是门禁输入，实际产品证据
由 `RyuPy.CPython` 原生求值观察生成，报告写仓库外并在验收后清理。

## 当前观测合同

固定 CPython 3.12.13 release/GIL 可设置：

```bash
RYUPY_CPYTHON_EXECUTION_AUDIT=1 \
RYUPY_CPYTHON_REPORT=/absolute/external/path/host.json /path/to/product
python3 -B tests/cil_execution/check_audit.py /absolute/external/path/host.json
```

报告包括 startup/runtime/shutdown 的真实帧次数、来源明细、丢帧、求值器覆盖变化及
关闭状态。C0 首切在最早获得线程状态的原生审计事件安装 PEP 523 hook，初始化前段
与直接调用默认求值器仍有盲区，报告固定 `complete=false`、`startup_complete=false`。
这个阶段的报告会被严格门拒绝；后续完整内核插桩完成后才可验证零计数准入。

门要求宿主已关闭、完整覆盖、三个阶段计数与明细一致、无丢帧和覆盖失败，而且没有
任何 Python 帧执行。缺失报告、禁用观测、空明细但非零总数和覆盖不足的零计数都失败。
产品载荷中的源码/字节码、嵌入 helper、扩展绕过求值器、其他平台及 NativeAOT 搬迁仍按
[实施路线](../../docs/cil-execution-roadmap.md) 的独立门验收。

## 自检与真实观测

```bash
python3 -B -m unittest discover -s tests/cil_execution -p 'test_*.py'
python3 -B tests/cpython/execution_audit.py \
  --python /path/to/cpython-3.12.13/bin/python3.12 \
  --unsupported-python /path/to/cpython-3.13/bin/python3.13
```

自检已接入 `make python-baseline` / `make check`。真实原生探针需安装指定发行的头文件、
共享库和 C 编译器，覆盖递归、原生回调、新线程、atexit 及不支持版本拒绝；其运行产物
由驱动放在仓库外并自动清理。
