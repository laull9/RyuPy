# 纯 CIL core 与真实 CP 函数的双 GC 首证

这是独立嵌入测试，源码只依赖 stock CPython 与 .NET，产品 Runtime 尚未接入。
所有权、后续中性接口与元数据边界见 [ADR-0224](../../../docs/adr/0224-cpython-thin-function-core-handles.md)。

## 重跑

需要 macOS arm64、clang、.NET 10+ SDK（本次使用 11.0.100）、带头文件和共享库的固定
常规 GIL stock CPython。两个版本分别运行：

```sh
python3 tests/libpython_embedding/core_handles/run.py --python "$HOME/.local/share/uv/python/cpython-3.12.13-macos-aarch64-none/bin/python3"
python3 tests/libpython_embedding/core_handles/run.py --python "$HOME/.local/share/uv/python/cpython-3.13.3-macos-aarch64-none/bin/python3"
```

驱动把手写 C# 项目复制到仓库外临时目录，独立构建 shim、JIT 和 NativeAOT，每种模式独立进程。
成功和失败均清理目录；不共享产品 obj/bin/target，不保存生成报告。NativeAOT 发布可需要
恢复 SDK 包。驱动以解释器自身 sysconfig 查询 include、共享库和 home，固定 micro 与 GIL。

## 验证内容与结果

2026-11-17，stock 3.12.13、3.13.3 的 JIT/NativeAOT 四组合均通过。
每组合输出 36 个回收检查点和一个终态：

```text
RESULT cycles=12 functions=48 failed_creations=3 resurrected_functions=1 cil_calls=172 roots=0 handles=0 native_live=0
```

每组合原 48 个函数另加一个复活函数与三处创建失败，GC owner 真实析构共 52 次，52 个 core
弱引用全部失效；成功 F 与 globals 哨兵的真实
weakref 全部失效。每次回收门还断言终结器队列为空，并在 CP 循环 GC 前观察尚存的强环。
字典本身不能 weakref，因此以独占哨兵、真实 F 弱引用、native dealloc 与全根计数联合验证。

- `bridge.c`：固定 factory/f 薄胶水；公开 C API；PyCFunction/GC owner 与强 F；可遍历 CP 强环，原生析构释放 core root。
- `Program.cs`：静态 CIL checked Int64 业务叶子；GCHandle 强 core；弱 H 缓存；owned F 与 GIL drain。
- `Native.cs`：固定 P/Invoke ABI；无反射、任意函数指针封装或 CP 内存偏移。
- `scenarios.py`：测试控制面、同次 oracle 与真实 importlib 成功/失败事务；不进入候选业务执行归属。
- `run.py`：仓库外构建、执行两种模式、清理；不做性能测量。

CP 独持 F、CLR 独持 H、回流 H 重建、F→D 环、删缓存后重导的新旧 D、失败外逃 H 均有断言。
无默认值/无未知业务捕获是首证范围，回流入口拒绝动态新增默认值与替换 code。
F.__code__ 真实来自 `<string>` 胶水，真正加法在 C# 静态 `Core.Leaf` 执行；NativeAOT 模式执行其机器码。

公开 API 面向 CPython 3.9+，本次实际版本仅上述两版。其它版本/平台、单份 abi3、通用 Python
语义、CIL 直达快路、全产品关闭协议和正式性能仍待各自验收。具体元数据漏洞与接入职责见 ADR。

主代理复核后增加参数 `__index__` 重入改写 globals 的回归：转换期间强持 offset，
确保旧值在本次调用结束前保持存活。两版 JIT/AOT 四组合复验通过，执行 stderr 必须为空。

创建阶段通过公开函数/closure API 直接组装，GC owner 强环境建立后恢复 GC 跟踪；
创建期 trace 回归验证半初始化函数不会从 factory return 暴露，正常调用仍可被 trace 观察。
三处创建失败分别覆盖 P 已创建、closure 已创建、F 已取消 GC 跟踪并绑定 closure；
失败销毁前恢复 GC 跟踪，core token 精确释放一次，异常由控制面核验并清理。


最新结构使用共享 `../function_owner.h`：F→closure/cell→P→owner→F 为 CP 可见环，
owner 的纯 native context 只持 core token，无 capsule、weak-F 或内部 watcher。
新增真实 Python finalizer 在 GC 内调用 CIL、保存原 F，再从该 F 回流 H；释放原 weakref
继续保持死亡语义，新弱观察与 owner 析构共同证明最终回收。两版 JIT/AOT 均通过。
参见 ADR-0227 的淘汰方案及 native DESTROY clear 期边界。
