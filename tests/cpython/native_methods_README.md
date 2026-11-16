# C1 原生方法描述符候选

原生 super 转发已移除两个 Python helper，调用真实 CPython `super` 类型与属性 API。
方法包装候选由 `RYUPY_CPYTHON_NATIVE_METHODS=1` 开启；默认保留旧 PyFunction 包装器。
决策与候选能力范围见 [ADR-0211](../../docs/adr/0211-native-method-descriptor-candidate.md)。

## 多版本真实原生门

```sh
python3 -B tests/cpython/native_methods.py \
  --python /3.9发行/bin/python3.9 \
  --python /3.12发行/bin/python3.12 \
  --python /3.13发行/bin/python3.13
```

每个选中发行独立运行当次 oracle、构建真实 shim、小宿主和描述符消费者。
检查普通绑定、classmethod/staticmethod/property、参数和异常、动态名字/限定名、
元数据字典、弱引用和自环 GC 回收；精确 CodeType 查询按候选合同明确报错。
输出在仓库外自动清理。当前驱动支持 macOS/Linux，Windows 产品单独验收。

## 真实托管类桥

先将 shim 构建到仓库外，显式传给测试，避免读取旧预构建 shim：

```sh
sh runtime/RyuPy.CPython/native/build.sh /选中3.12.13发行 /仓库外/shim 3.12
RYUPY_CPYTHON_NATIVE_METHODS=1 \
RYUPY_CPYTHON_TEST_SHIM=/仓库外/shim/libryupy_cpython.dylib \
RYUPY_CPYTHON_EXECUTION_AUDIT=1 \
RYUPY_CPYTHON_REPORT=/仓库外/candidate-audit.json \
dotnet test runtime/RyuPy.CPython.Tests/RyuPy.CPython.Tests.csproj \
  --filter FullyQualifiedName~CPythonClassBridgeTests
```

Linux 将 shim 文件名改为 `libryupy_cpython.so`。把候选开关改为 `0` 后用独立进程
重复同一八项测试，可保护默认包装器。原生审计仍仅适用具有私有帧头文件的3.12.13；
多版本小宿主门直接使用对应公开/版本适配 ABI，不依赖该审计。

## 产品输入

`cases/native_super.py` 为单继承 super 独立输入；
`cases/native_method_descriptors.py` 把编译类交给真实 CPython 消费者
`bridge_fixture.method_descriptor_contract.verify`，检查外部真实描述符而非托管镜像。
产品驱动必须在实际应用进程环境内显式加入候选变量；共用 `support.environment()`
会清理继承的 `RYUPY_*` 变量，不能仅在外层 shell 设置后假定候选已启用。

产品审计应确认 `_rp_wrap_method`、`_rp_method`、`_rp_super_getattr`、
`_rp_super_create` 四个名字的执行帧总和为零。完整启动/关闭零执行仍依照 C0 报告
覆盖字段判断；本方法切片保留类构建 Python helper 与外部 Python 依赖。
