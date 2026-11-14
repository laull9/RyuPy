# 历史性能实验记录

此目录保留历史实验的源码、构建脚本、源码补丁、环境/版本记录、测量数据与结论。
其中由编译器和 JIT 生成的整份 IR、机器码转储及机器码差分不再纳入 Git：

- `*.mir`、`*.cil`、`*.types`：`ryupy dump <输入.py> --emit mir|cil|types` 的输出或方法摘录。
- `*.asm`：使用 `DOTNET_JitDisasm`、`DOTNET_JitStdOutFile` 独立采集的 JIT 转储或摘录。
- `*.asm.diff`：上述转储之间的文本差分。
- `m3_10/union-prototype/jit-arithmetic.txt` 和 `jit-calls.txt`：原型的 JIT 转储。

`.gitignore` 仅按上述目录和类型忽略生成物，不忽略实验源码、性能测量摘要、
CPython golden 或 `crates/ryupy-driver/tests/snapshots/*.snap`。后者是 `insta`
实际读取的已审阅测试期望，缺失时不能用当次输出自动接受来代替回归检查。

## 查看历史证据

旧 README、清单与摘要中的转储路径是当次实验记录，当前工作树可能不再包含该文件。
本次清理前的完整版本为 `0f8454112b1d59a1d47e0de9d29cc46d9fa330bd`。
需要查看原文时，从 Git 历史读取指定路径，不要重新加入索引：

```bash
git show 0f8454112b1d59a1d47e0de9d29cc46d9fa330bd:<仓库内转储路径>
```

这次只从当前版本移除生成物，不改写历史；已有提交中的证据仍可读取。

## 重新生成

新实验优先将输出写到已忽略的 `target/` 子目录。先核对对应实验 README、
基准输入、源码版本/补丁、工具链、JIT 分层设置，再运行原有采集脚本。
IR 可用同一源码版本的 `ryupy dump` 重建；JIT 诊断需单独进程执行，不能混入
正式计时。`m3_10/bounds-check/candidate/collect_bounds_jit.py`、
`m3_9/tuple-baseline/allocation.py` 及 `m3_10/union-prototype/README.md`
保留具体采集方式。脚本要求的程序集/测量程序须先用对应源码版本构建。

当前编译器或其它主机重新生成的内容不保证与旧转储逐字节一致，不能冒作历史测量。
