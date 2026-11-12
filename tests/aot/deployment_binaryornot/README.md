# binaryornot 普通文件分类 consumer 的 Native 部署验收

此目录使用未修改的 binaryornot 0.4.4 和 chardet 5.2.0 官方锁定源码及 egg-info。`scanner.py` 是本项目编写的 `binaryornot.check.is_binary` 普通 consumer，**不是上游 CLI**。项目仅声明一个 vendor 搜索根和两包 metadata，不显式 includes、不声明资源，不新增产品依赖预检功能。

## 合同与执行

`inputs.json` 锁定官方归档、包文件及独立预期 47 来源：consumer 1、binaryornot 3、chardet 43。`contract.json` 锁定 consumer 和四种外部用户输入：ASCII、UTF-8、UTF-16 BOM、含 NUL 的二进制。固定 CPython 3.12.13 分别返回 False、False、False、True。数据文件不是产物资源。

公开命令为 `ryupy build --project PROJECT/ryupy.json --target scan -o OUT`。构建成功后才运行独立发射前 oracle，复用已有 schema4、实际 PE 冷注册/来源、metadata 载荷及完整产物审计；精确要求 47 来源、空资源、空显式 includes 和 binaryornot/chardet 两分发，不从 manifest 反造预期。

产物原位与复制搬迁后，各运行四个外部输入。隐藏项目源码，输入和 dist 均不允许改变。固定 `runtime.Dockerfile` 镜像须实际证明无 Python/.NET，然后在禁网、只读根、去 capabilities、no-new-privileges、非 root 用户环境中仅挂只读 dist 和输入，再跑两位置八次。结果保留全部 argv、退出码与 stdout/stderr 原流、工具与输入摘要。

## 状态边界

当前为未完成 hosted Native 验收的候选。此前本地已发射 47 来源和两分发，但发布受 MSB4216/4027 阻塞；执行 managed 发射件不等于 Native 验收。缺 chardet 的本地实验也仅在发布失败前观察到四来源和一分发，不能宣称产品已有依赖缺失预检。这些历史原件保留，不改写成成功。

不修改固定 20 项目的原生通过分子、旧基线或 Nuitka 数字，不产生性能结论；仅以本门真实结果报告这一 consumer 的交付能力，不扩张到完整编码探测、任意文件或全库兼容。
