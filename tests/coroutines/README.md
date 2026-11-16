# 原生协程显式候选

本目录只保留手写输入；通过 `native-coroutines` 特性专用集成测试发现。
普通 e2e 不自动启用候选。用例最初以 pending 登记，贯通后转入本目录，
预期由每次固定 CPython 即时生成，产物在仓库外临时目录并清理。
范围和取舍见 [ADR-0210](../../docs/adr/0210-native-coroutine-state-machine.md)。

```bash
make runtime
PYTHON=/绝对路径/CPython3.12.13/bin/python3.12 \
  cargo test -p ryupy-e2e --features native-coroutines --test native_coroutines -- --test-threads=1

# 同时执行真实 NativeAOT、删除输入/构建目录和中文空格搬迁门。
PYTHON=/绝对路径/CPython3.12.13/bin/python3.12 \
  cargo test -p ryupy-e2e --features native-coroutines --test native_coroutines -- --include-ignored --test-threads=1
```

3.12.13 是本测试的固定 oracle，产品和 Cargo 特性没有 Python 版本锁。
本机额外使用 3.9.25 与 3.13.3 跑过相同手写源码，四组正例输出均与 3.12.13 一致。
call_preparation.py 验证方法绑定和混合位置组的星号实参在 await 前固定；
唯一星号源后仅有关键字时，展开保留到关键字 await 恢复后，并核对源表达式、变异和迭代的副作用顺序。
reject_await_kwargs.py 验证位置/关键字参数中包含 await 的双星号调用明确拒绝。
候选只提供原生协程和 await 协议；事件循环、IO、取消和异步上下文管理进入后续竖切。


ADR-0215 继续使用同一个 feature 门，新增 async_with_lifecycle/exceptions/protocol/
cleanup/traceback/traceback_protocol 六组手写输入。它们覆盖真实挂起、清理抑制、
目标绑定失败、取消注入及真实 traceback 身份；三版本 oracle 输出一致。
AOT 门还搬迁 async_with_cleanup.py 与 async_with_traceback_protocol.py。
原生 traceback 头只读，tb_next 可写并拒绝循环；frame/code 字段范围见
[ADR-0215](../../docs/adr/0215-native-async-with-traceback.md)。
