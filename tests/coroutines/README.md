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
候选的await/上下文/迭代协议逐切验收；事件循环、异步IO与调度器取消API留待后续竖切。


ADR-0215 继续使用同一个 feature 门，新增 async_with_lifecycle/exceptions/protocol/
cleanup/traceback/traceback_protocol 六组手写输入。它们覆盖真实挂起、清理抑制、
目标绑定失败、取消注入及真实 traceback 身份；三版本 oracle 输出一致。
AOT 门还搬迁 async_with_cleanup.py 与 async_with_traceback_protocol.py。
原生 traceback 头只读，tb_next 可写并拒绝循环；frame/code 字段范围见
[ADR-0215](../../docs/adr/0215-native-async-with-traceback.md)。

ADR-0218新增async_for_protocol/control/exceptions/lifecycle/global_shadow/shared_awaitable
六组手写输入，均以同次固定3.12.13生成预期；本机3.9.25、3.13.3的输出也一致。
它们固定类型槽单次/逐轮查找、描述符失败文案、GET_ANEXT共享协程合同、窄耗尽区域、
跨暂停控制流、闭包与throw/close清理。AOT门新增control/exceptions/lifecycle，
同样真实删除input与整个build目录，再从中文空格搬迁目录运行。
普通类的自定义__getattribute__、动态别名内建异常基类和异步生成器分别保留独立pending
与拒绝门；受信宿主另测特殊槽绕过实例hook，不能借此放开普通类的深属性协议。
范围见[ADR-0218](../../docs/adr/0218-native-async-for.md)。
普通动态while的同根绑定合流修复另由phase3/m3_dynamic_while_binding_join.py保护，
默认关闭候选时也要保持空循环/回边未绑定错误、合法None和finally副作用。
