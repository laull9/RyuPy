# AOT 上下文协议交叉评审修订验收

2026-11-13，dev 整合树，macOS arm64，固定 CPython 3.12.13。

修复两项 P2：with 通过类型绑定 enter/exit，忽略实例覆盖；ContextDecorator
每次经普通属性协议调用公开 `_recreate_cm`，执行返回上下文的进入、退出与异常清理。
新增 `resources_as_file/protocol` 差分覆盖显式属性调用、原生/普通类、property 绑定顺序、
重建副作用、错误和异常身份；已有 with 用例继续验证 return/break/continue 清理。

## 实际验证

- `make runtime` 通过；资源/ABI C# 专项 18 项通过。
- `resources_as_file` 编译差分 3 项通过、2 项 pending 保留。
- 真实 `tests/aot/binding_resources/run.py` 23 步全部符合预期，见 result.json。
  v1/v2 发布后整树搬迁，隐藏原源码、原生库和构建路径，换 cwd 执行与 CP 双流一致；
  v2 在实例覆盖和公开重建替换下读取包数据、调用两个 C 符号。
  缺资源副本报 FileNotFoundError；四类构建失败保留旧 dist。
- 最终 `make check` exit 0：fmt、严格 clippy、Rust workspace 通过；C# 3009 通过、
  2 跳过；CPython 基线 721、目录自检 724、编译 e2e 444 通过、284 ignored。
- `with` 的 HIR/MIR/CIL 快照更新为类型绑定协议；引用表拆分后最终门禁再次通过。

复现命令：

```sh
make runtime
CARGO_TARGET_DIR="$PWD/target" RUSTC_WRAPPER= cargo build -p ryupy-cli
"$(uv python find "$(cat tests/pinned-python.txt)")" -B \
  tests/aot/binding_resources/run.py --ryupy target/debug/ryupy \
  --out target/context-binding-resources
```

现场与日志：`target/context-binding-resources/binding-resources-q0f_qm9b/`、
`target/context-binding-resources.log`、`target/context-final-check.log`。
SDK 仍安装于宿主；Linux/Windows、无 SDK 目标环境继续单独验收。
任意自定义描述符与类级反射沿现有待实现边界推进。
