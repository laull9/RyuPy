# binaryornot 普通文件分类 consumer 的 Native 部署验收

此目录使用未修改的 binaryornot 0.4.4 和 chardet 5.2.0 官方锁定源码及 egg-info。`scanner.py` 是本项目编写的 `binaryornot.check.is_binary` 普通 consumer，**不是上游 CLI**。项目仅声明一个 vendor 搜索根和两包 metadata，不显式 includes、不声明资源，不新增产品依赖预检功能。

## 合同与执行

`inputs.json` 锁定官方归档、包文件及独立预期 47 来源：consumer 1、binaryornot 3、chardet 43。`contract.json` 锁定 consumer 和四种外部用户输入：ASCII、UTF-8、UTF-16 BOM、含 NUL 的二进制。固定 CPython 3.12.13 分别返回 False、False、False、True。数据文件不是产物资源。

公开命令为 `ryupy build --project PROJECT/ryupy.json --target scan -o OUT`。构建成功后才运行独立发射前 oracle，复用已有 schema4、实际 PE 冷注册/来源、metadata 载荷及完整产物审计；精确要求 47 来源、空资源、空显式 includes 和 binaryornot/chardet 两分发，不从 manifest 反造预期。

产物原位与复制搬迁后，各运行四个外部输入。隐藏项目源码，输入和 dist 均不允许改变。固定 `runtime.Dockerfile` 镜像须实际证明无 Python/.NET，然后在禁网、只读根、去 capabilities、no-new-privileges、非 root 用户环境中仅挂只读 dist 和输入，再跑两位置八次。结果保留全部 argv、退出码与 stdout/stderr 原流、工具与输入摘要。

## 已验证结果（2026-11-12）

修复候选 `3a512ee928806c8554c4ed4a012f242f386c6f3e`（树 `b3269bda04972f5c747ffc1076d3f72815d59801`）在 [CI 43314021657](https://github.com/laull9/RyuPy/actions/runs/43314021657) 完整通过。原位与搬迁各四输入，Linux宿主8/8和无Python/.NET禁网容器8/8均exit 0、stderr空、stdout逐字节等于CP合同；实际PE/47来源/两分发metadata/零资源及工具摘要审计通过。官方ZIP `13986868626` 的SHA256为 `ee19c82c31de9e9ef68d57d5909721d6bf7b1056ffc26889f45c7a0cfce75607`，下载后再次独立检查16条原流、上游源码锁和搬迁字节一致。本地直接执行同一官方Native二进制的四输入也通过。

同树本地完整 `make check` 退出0，C# 3050通过/5跳过，e2e 452通过/282忽略。首轮旧decode根方法禁用断言已由新的四路正负合同替代；第二轮磁盘不足后仅清理可再生debug缓存，并以关闭调试信息和增量缓存的配置完成第三轮，未改变产品优化或功能。此前失败日志仍保留，不将失败轮改写成成功。

验证只覆盖本文Linux普通consumer四输入，不代表上游CLI、完整编码探测或任意文件兼容；无性能测量，不更新固定20项目分子或Nuitka比较。dev/main未因本次验证而移动。

## 状态边界

首轮 hosted Native CI `43310884534` 实际构建与结构审计成功，ASCII 宿主和无 Python/.NET 容器各通过一次，但 UTF-8 运行因 `bytes.decode(encoding=...)` 被错误拒绝而进入上游 Python 2 的 `unicode` 回退，最终失败。该轮不是通过，搬迁门尚未执行。后续候选修复通用 bytes/bytearray decode 绑定与严格 UTF-16 解码，不改写上游源码；本地重新编译的普通 JIT consumer 四输入已与 CP 一致，但不能替代新的完整 hosted Native 门。此前本地发布的 MSB4216/4027 原始失败也保留，不改写历史。缺 chardet 的本地实验也仅在发布失败前观察到四来源和一分发，不能宣称产品已有依赖缺失预检。这些历史原件保留，不改写成成功。

不修改固定 20 项目的原生通过分子、旧基线或 Nuitka 数字，不产生性能结论；仅以本门真实结果报告这一 consumer 的交付能力，不扩张到完整编码探测、任意文件或全库兼容。

## 无条件依赖预检候选

本门现显式请求 `dependency_check.mode=unconditional_closure`，根为 `binaryornot==0.4.4`。冻结egg-info的 `requires.txt` 声明 `chardet>=3.0.2`，正例编译期收据须绑定请求/载荷摘要以及恰好这两个分发和两条检查；原metadata payload、上游源码和四输入合同不变。

新增公开CLI负例门使用独立项目副本与无效Python入口、假SDK哨兵：缺chardet、版本不足、markers/extras/URL及基础条件段必须先报告依赖预检失败；关闭配置则仍按旧流程抵达源码语法错误。完整原流/耗时/工具和项目摘要保留，不以exit非0单独作为通过。正例仍要求后续原Native部署门全部执行。

此节为新候选，不能复用上一节旧CI当成新预检已通过Native的证明。只验证无条件声明闭包，不验证Requires-Python、ABI或动态导入；详见[ADR-0176](../../../docs/adr/0176-explicit-dependency-preflight.md)。
