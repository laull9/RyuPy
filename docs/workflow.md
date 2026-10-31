# 开发工作流

> 工作流的目标是：**每一步都能看到可运行的结果，每一个决定都留下记录，每一个坑都写成测试。**

## 1. 环境准备

| 工具 | 版本 | 用途 |
| --- | --- | --- |
| Rust | stable（见 `rust-toolchain.toml`） | 编译器本体 |
| .NET SDK | 10.x | 构建运行时、运行生成的 `.dll`、Native AOT |
| Python | 3.12+ | e2e 差分测试的「标准答案」 |
| cargo-insta（可选） | 最新 | `cargo install cargo-insta`，审阅快照 |
| ilspycmd（可选） | 最新 | `dotnet tool install -g ilspycmd`，把生成的 `.dll` 反编译成 C# 查看 |

首次进入仓库：

```bash
make runtime   # 构建 C# 运行时
make check     # lint + 单元/快照测试 + e2e，全绿即环境 OK
```

## 2. 常用命令

```bash
make help                                   # 全部命令
make dump FILE=foo.py EMIT=ast|hir|types|mir|cil   # 查看任意阶段的 IR
make run  FILE=foo.py                       # 编译并运行
make test                                   # Python 基线 + 单元 + 快照
make python-baseline                        # 自检含 pending 的 Phase 1–3 Python 用例
make python-extensions                      # 自检 Phase 4，需 NumPy / sqlite3 / zlib
make e2e                                    # 端到端（跳过 pending）
make e2e-all                                # 包括 pending，看整体进度
make snap                                   # 审阅快照变更
make check                                  # 提交前必跑
```

## 3. 开发循环：一次只做一个「竖切」

**曳光弹原则**：先打通一条贯穿所有层的最窄路径（`print(1 + 2)`），再逐个特性加宽。
永远不要「先把 HIR 写完整，再写类型，再写 MIR」——那样要到最后一刻才知道设计行不行。

每个特性按下面的步骤推进（对应 `TODO.md` 里的一个条目）：

Phase 2 起先查看固定项目集的失败原因，再选择能解除阻塞的行为。TODO 中一个里程碑包含多个竖切，标准库以单个 API 或一小组相关行为推进；不把整个模块或整阶段塞进一个实现任务。冷门能力保持明确失败记录，不因为容易展示而插队。

```text
① 选条目    从 TODO.md 当前里程碑里挑一条，标记为进行中
② 写用例    在 tests/e2e/phaseN/ 下写 .py（或复用已有的），开头加 `# ryupy: pending`
            先用 python3 运行确认用例本身正确
③ 逐层实现  syntax → hir → ty → mir → codegen，每层：
              - make dump EMIT=<该层> 看输出是否符合预期
              - 为该层加一个 insta 快照测试，审阅后接受
              - 对错误输入加诊断快照（编译失败的用例）
④ 转正      去掉 `# ryupy: pending`，make e2e 通过
⑤ 收尾      make check 全绿 → 勾掉 TODO.md 条目 → 提交
⑥ 记录      踩到的坑写进 docs/notes/；做了取舍就写 ADR
            更新项目 / 标准库清单，偏差写进 semantics.md §4 与 compatibility.md
```

一个竖切理想情况下 **1~3 天内完成**。超过一周说明切得太大，拆小。

Phase 1 已完成 M1.0–M1.5；旧 M1.6 的 AOT 保持待做并转入 M5.2。后续里程碑不得仅因重新排期就改成已完成。

## 4. 测试怎么选

| 想验证的东西 | 用什么 |
| --- | --- |
| 程序运行结果是否与 Python 一致 | e2e 用例（首选，最有说服力） |
| 某一层的输出结构 | insta 快照（`make dump` 的输出就是快照内容） |
| 某个错误能否正确报出、提示是否友好 | 诊断文本快照（`render(.., color=false)`） |
| 算法本身（并查集、运算规则表、浮点格式化） | 普通单元测试，覆盖边界值 |
| 导入顺序、循环导入、模块共享状态 | M2.0 新增多文件 fixture，M2.7 转正 |
| Python 异常、finally 与清理副作用 | M2.0 扩展错误路径框架，M2.2 起按类型 / 必要消息 / 用户位置比较 |
| 标准库兼容 | stdlib.md 中逐 API 的 CPython 差分与移植上游测试 |
| 项目可运行 | 固定版本、入口、传递依赖和上游测试范围；原生 / 桥接分别统计 |

**发现 bug 时先写一个能复现它的最小 e2e 用例**，再修。用例永远保留。

后面四类测试入口属于后续任务，现有单文件 e2e 不能替代。已接受偏差单独验证并链接编号，不能通过改 CPython 期望值掩盖差异；pending、跳过与桥接结果不能补入原生通过数。

Phase 2–4 的主要 Python 用例已预置。`make check` 会运行独立 CPython 基线，先确认 pending 程序本身成立；Rust e2e 仍跳过 pending。多文件辅助模块不单独发现，预期错误退出由独立 manifest 验证，详见 [tests/README.md](../tests/README.md)。

## 5. 调试手段

| 场景 | 方法 |
| --- | --- |
| 不知道哪一层出错 | 从 `EMIT=ast` 开始逐层 dump，找第一个「不对」的层 |
| 生成的 `.dll` 无法加载 / `InvalidProgramException` | `ilspycmd target/ryupy/x/x.dll` 反编译查看；常见原因是求值栈不平衡或分支目标错误 |
| 想看 RyuJIT 生成的机器码 | `DOTNET_JitDisasm=<方法名> dotnet target/ryupy/x/x.dll`（.NET 7+ 可直接用） |
| 运行结果和 Python 不同 | 查 [semantics.md](semantics.md) 的「语义陷阱」一节，大概率是已知差异 |

## 6. 提交规范

- 使用 Conventional Commits，scope 为 crate 名去掉前缀：

  ```text
  feat(hir): 支持 for-range 去糖
  fix(codegen-cil): 修正 Le 比较的指令序列
  test(e2e): 增加负数 floor div 用例
  docs(adr): ADR-0004 整数溢出策略
  ```

- 一个提交只做一件事；**提交前 `make check` 必须通过**。
- 修改快照时，在提交说明里写清楚快照为什么变了。

## 7. 什么时候写 ADR

满足任一条件就写一篇（模板见 [adr/README.md](adr/README.md)）：

- 有两个以上可行方案，而选择会影响多个 crate；
- 偏离了 roadmap 的原设计；
- 引入新的第三方依赖；
- 半年后的自己可能会问「当时为什么这么做」。

## 8. 学习笔记

`docs/notes/` 用来记录学到的东西（模板见 [notes/README.md](notes/README.md)）。建议每完成一个里程碑写一篇，内容包括：做了什么、遇到什么坑、读了哪些资料、哪里还没想明白。

## 9. 里程碑验收（Definition of Done）

一个里程碑（如 M1.2）只有同时满足以下条件才算完成：

- [ ] 该里程碑所有 e2e 用例已去掉 `pending` 并通过；
- [ ] 每层新增的结构都有快照测试；
- [ ] 至少有一个「错误输入」的诊断快照；
- [ ] `make check` 全绿；
- [ ] `TODO.md` 已更新，`semantics.md` 中对应特性的状态已更新；
- [ ] 涉及模块 / 标准库 / 项目时，传递依赖、API 范围、平台和执行路径已经登记，相关项目测试通过；
- [ ] 偏差有最小示例、影响和替代方式；低影响默认偏差经 ADR 接受，影响常用结果的性能扩展默认关闭；
- [ ] 涉及缓存、容器、模块状态或桥接引用时，说明容量、增长与释放策略，覆盖失效和长期运行；
- [ ] 写了一篇学习笔记。

优化任务还需记录基准输入、工具链、冷启动 / 稳态与内存证据，守卫失败回到正确通用路径。项目覆盖率按 compatibility.md 的冻结分母计算；Phase 3 未达到 80% 时继续处理阻塞，Phase 5 也不能把未支持项写成可用。

## 10. 升级依赖

- **ruff**：修改根 `Cargo.toml` 中三个 ruff 依赖的 `tag`（必须一起改）→ `cargo update -p ruff_python_parser` → `make check`。AST 结构变化只会影响 `ryupy-syntax` 和 `ryupy-hir/src/lower.rs`。
- **.NET**：同时修改 `RyuPy.Runtime.csproj` 的 `TargetFramework`、`runtimeconfig.rs` 的 `TARGET_FRAMEWORK_MAJOR`、`runtime.rs` 的默认路径、CI 中的 `dotnet-version`。
