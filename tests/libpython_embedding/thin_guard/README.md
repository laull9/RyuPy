# 薄函数 closure/GC owner 元数据探针

配套 [ADR-0227](../../../docs/adr/0227-stock-thin-function-metadata-probe.md)。
固定普通 `f(x)` 的有限合同；产品候选默认关闭。

## 重跑

```sh
python3 tests/libpython_embedding/thin_guard/run.py \
  --python "$HOME/.local/share/uv/python/cpython-3.12.13-macos-aarch64-none/bin/python3"
python3 tests/libpython_embedding/thin_guard/run.py \
  --python "$HOME/.local/share/uv/python/cpython-3.13.3-macos-aarch64-none/bin/python3"
```

macOS arm64、常规 GIL；查询指定 micro 自身头文件/共享库，仓外 C11/O2/Wall/Wextra/Werror
构建。查询、编译、执行退出码与完整 stderr 均校验，成功失败均清理临时目录。
内部 owner 不使用 weakref/watcher；独立 native DESTROY 消费者对照使用 3.12+ watcher。

## 结构与结果

A 的真实 closure 持 P，P self 为 GC heap owner，owner 强 F，纯 native context 仅存
登记链与 core；owner traverse F/type，dealloc 先摘链再清引用。实例 P 不进入 code 常量。
B/C 常量方案因 stock GIL code 缺 GC traversal 退出；raw witness 因 weak callback 分配
失败可缺席而退出。源码仅保留最终结构，历史失败与功能诊断日志保存在仓外。

两版最终均：`failed=0 finalize=0 live=0 destroyed=42 calls=16 blocked=6`。
覆盖 CP binder/错误供参/身份、活元数据、defaults 合法写后调用拒绝、code setter audit、
trace/profile、三处失败注入、三十轮字典强环、P/owner/cell 保活与释放，以及 __del__ 在 GC
内即时调用和复活。cell 写 abs 保留 CP 行为，恢复原 P 后 native 调用恢复。

clear 期原生 DESTROY 复活的 stock 普通 closure 环与候选均观察字段 `(1,0,0)`：
code 仍在，globals/closure 已清。仅使用公开 getter 观察及回收，禁止调用这种不完整对象。
产品业务来源、原生消费者、线程、任意签名、动态默认值、性能与全应用闭包按后续合同推进。
