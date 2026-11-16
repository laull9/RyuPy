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
本机额外使用 3.9.25 与 3.13.3 跑过相同手写源码，三组输出均与 3.12.13 一致。
候选只提供原生协程和 await 协议；事件循环、IO、取消和异步上下文管理进入后续竖切。
