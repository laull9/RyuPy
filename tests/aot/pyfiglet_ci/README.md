# 原 pyfiglet 1.0.2 显式部署配方

当前增加JIT空资源子包发现候选，原入口、上游源码及golden不改。
新候选尚待完整实际验收；旧CI58为默认10/8/2，不提前登记pyfiglet晋升。

## 准入与固定输入

沿既有专用 `wcwidth-legacy-aot.yml` 的独立 `pyfiglet-explicit-deployment` job，
使用官方GitHub hosted Ubuntu 24.04；与既有baseline、双full-check、Rust、S2和20项门并行且互不替代。
本轮编写不等于发布或运行授权。实际commit/push/CI等待单独准入，禁止本地调用 `run.py`。

执行合同v3 SHA256 `2f31844adfd98f5df5999a647a48c877fd98c4521c1749bfa7442dc829ef039a`。
v3只明确允许hosted复用既有 `docker_image.prepare_image` 一次准备固定官方镜像，
不改变v2其余预算、输入与隔离要求。禁止本机Docker socket、提权、私有registry或改镜像。

准备工具复用 `wcwidth_ci/prepare.py` 与原 `downloads.json`：
CPython build-standalone 3.12.13，SDK10.0.403/host10.0.14，固定官方Rust。
每个工具下载身份仍由原准备器核验；新产物保留download-identity。
原包仅从现有source-recovery中pyfiglet记录下载，不安装包、不执行setup.py。
归档SHA为 `758788018ab8faaddc0984e1ea05ff330d3c64be663c513cc1f105f6a3066dab`。
入口与golden原件分别在 `tests/projects/entries/pyfiglet_sample.py`、
`tests/projects/baselines/pyfiglet/`，资源JSON为此目录冻结原件。
fonts是真实1字节普通空包；只部署28504字节standard.flf，不部署源码、standard.tlf或全字体。

## 有界命令与证据

hosted调用顺序：纯合成unittest → 固定工具准备 → `"$PYTHON" -B tests/aot/pyfiglet_ci/run.py`。
原流在每条命令启动前创建，保留完整stdout/stderr/exit、argv、cwd和无秘密环境字段。
工具构建仅一次 `make runtime` 与一次带 `ryupy-driver/ordinary-dynamic-module-pilot` 的CLI cargo build；
目标预算为：

1. CP原入口 `-B -S` 正测一次，与固定golden逐字对三流。
2. 同CLI不include默认臂：一次build、一次JIT；必须逐字对CP/golden成功，图中fonts带自动发现理由。
3. 同CLI仅增加 `--include-module pyfiglet.fonts`：一次build、一次JIT；逐字对CP/golden。
4. explicit全入口通过后唯一 `--aot --resources <本目录JSON>` publish。
5. 原位置/完整复制搬迁/无SDK无源码隔离容器/独立缺字体副本各运行一次。

每build明确设置RYUPY_RUNTIME_DIR、DOTNET、DOTNET_ROOT，两臂同用只读diagnostic-stages，
读取实际module-environment，不用feature-default冒充control。不猜产物路径，解析CLI报告并核独占输出、
真实PE/Runtime、AOT清单exact-set及ELF。manifest sources实际模块名与编译源SHA逐项绑定。
所有失败停止，不补API、不重试、不换字体/工具/镜像；环境准备失败不是目标产品失败。

每次目标run及build新空cwd，检查宿主shared目录、源码tlf/flf、入口遮蔽及CP系统/user/venv site、
标准库可见根（含zip）的目标安装污染。发现污染或不可读先保存逐项观测/失败相位再停报，不删除宿主字体或卸载包。
隔离仅挂已核dist到 `/app`，使用准备记录中的不可变image ID，`--pull=never --network none --read-only`；
容器内另查无dotnet/python/python3/python3.12、空/tmp及shared字体，无源码/IL/SDK挂载。

## 交付与缺件负域

首次Native执行前归档完整dist，并安全枚举实际文件与manifest角色/哈希逐项相符。
本窗口只允许唯一ELF64/x64 native、已核冻结字体、至多一个顶层ELF64/x64 `.dbg` 调试sidecar。
拒绝Python源码/缓存、DLL、runtimeconfig/deps、改名PE及其他未知sidecar布局。
这是固定声明域无另附Python/managed输入的检查，不声称检查任意ELF内嵌内容。全部构建树、SDK原流及原始产物位于
`target/pyfiglet-deployment-evidence`，Actions始终收件完整目录，无论成功或首次失败。
保留同轮manifest、PE、ELF、Runtime、包成员、source/golden/resource与工具哈希。
只新增本轮pyfiglet产物，不重复复制旧baseline大包。

部署时完整解包已验 `native-dist.tar.gz`，保留manifest及所有declared_resource相对位置，
从真实manifest唯一native_executable启动。字体是dist外置资源，不是嵌入；
module_locations的虚拟__init__.py只是metadata锚点，不应手工创建或打包。
真实资源位置只能由resources record确定，与artifacts角色/大小/SHA/consumer交叉核验。

负测仅用新独立完整副本；逐级拒绝symlink/硬链接/路径越界，按record只删除一个字体。
删除前后核源/原dist不变，删除后lstat证明不存在，原manifest不改。
先调用现有verify_manifest得到check_artifacts唯一missing拒绝并单独记录，再直接启动缺件native一次。
第二oracle只接受正常应用失败exit=1且没有成功glyph，保留实际完整异常，不匹配异常类型/消息。
信号或崩溃样退出标unexpected-crash，其他非1退出标unexpected-exit，均停止且不通过。
Popen启动失败先保存launch-failed/environment状态和原流文件，再重抛；未启动不算应用拒绝。
不宣称CP缺字体异常类型/消息等价。
末尾再次核源、工具、入口、golden、原dist与搬迁dist未改。

## 本地允许的轻量自测

`python3 -B -m unittest discover -s tests/aot/pyfiglet_ci -p 'test_*.py'`

测试只使用合成临时文件和subprocess mock，不运行目标CP、产品、Docker、Rust或.NET构建。
完整make check与真实hosted部署在后续单独准入；脚本通过不等于Native验证通过。
