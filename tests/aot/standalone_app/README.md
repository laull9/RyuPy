# 真实 standalone app 合同：pyfiglet 1.0.2

状态：两个完整 CPython 3.12.13 入口已通过；产品 managed/Native、搬迁和性能尚待实际验收。不以枚举 metadata、导入模块或截取渲染行数冒充 app 通过。

## 锁定输入与准备

`inputs.json` 锁定当前官方 1.0.2 源码树所有 Python 文件、原 `pyfiglet/fonts` 完整 553 个数据文件（5,990,660 字节） 及同一包完整 `pyfiglet.egg-info` 的逐文件 SHA256。逐文件显式枚举所有字体数据，不复制宿主 site-packages。`prepare_cp.py OUT PYTHON` 验证哈希并在仓库外新 OUT 中物化，写出完整命令、stdout/stderr/退出码和解释器身份；输入错或已有输出目录拒绝覆盖。

两个入口各运行 `-f standard hi` 与 `-f slant hi`：

1. 原样 `package/pyfiglet/__main__.py`，执行原 `sys.exit(main())`。
2. `console_entry.py` 从真实 `console_scripts` 发现唯一 pyfiglet 项，核 `pyfiglet:main`，实际 `.load()` 并执行返回的 main。

两条 CP 流程都执行上游 optparse、Figlet、资源读取、`sys.stdout.detach()`、bytes write，未猴补 stdout 或上游 main。最终 CP 使用正常 site 的 `-B`、禁 user site、清除宿主 PYTHON 配置，仅物化包 PYTHONPATH，空外部 cwd，固定非 -O；早期 -S 仅隔离探针，不代表默认产品语义。完整 stdout 在 `stdout.txt`，SHA256 `e01c7b04d9bcccb597250161c6083f5ab5fce6ae22c4a0c386b6a051da23b270`，两入口 exit 0/stderr 空。

初始 CP 探索错误把 EntryPoints 当可整数索引序列，CP3.12 拒绝 KeyError(0)；原失败在外部 cp001 保留，最终改为 list(entries)[0]，未改上游包。标准字体初稿成功证据在外部 cp002，保留不覆盖；完整字体包四个成功运行在外部 cp003-full-fonts。

## 现有公开构建接口与范围

- `--resources resources.json`：schema_version=1，modules 中 module=`pyfiglet.fonts`，files 显式列出完整 553 数据文件，不用 glob；约 6 MB 在既有预算内。
- `--distribution-metadata metadata-request.json`：schema_version=1、mode=`closed_snapshot`、roots 为 id/path；准备器写相对 path=`package`，里面有真实 egg-info。
- distribution metadata 请求不改变 sys.path，也不自动保证 entry.load 目标已编译；目标 pyfiglet 必须按现有源码发现/显式包含能力供给。编译器/driver 代理负责准确调用方式及实际产物，不在这里发明新 CLI 参数。
- `--aot` 的既有发布能力不等于本 app 通过。main 还依赖 optparse、io/TextIOWrapper.detach、bytes 写流、sys.argv、importlib.resources、metadata 延迟加载等；任何缺口必须报告真实失败链，不把入口改成 renderText 或只 import。

## standalone/搬迁验收

两入口均须先检查 managed 与 Native 原始 stdout、stderr、exit 和上述 CP 完整相等；只统计真实执行。记录冻结 CLI/runtime/CP、源输入、资源、metadata payload 和 dist 全文件 hash，保所有失败原流，不提交生成 IR。

发布后整体复制或搬迁 dist 到新空目录，冻结 before/after 文件身份，确认原 dist 不存在；使用空 cwd、清除 PYTHONPATH/PYTHONHOME 等源码通路，禁止从开发源码/原 egg-info/原字体取回数据。Native 测试环境不得靠 Python/.NET 解释器或 SDK 启动；可通过独立无工具容器/宿主证明，不能单凭 ELF 头宣称 standalone。

移动后两个完整入口仍须各 exit0/完整输出匹配；metadata发现与load仍执行实际 main；缺字体验证不得偷偷回退开发字体；metadata快照内容和console目标须可审计。上述失败不得以仅标记资源存在代替。

Nuitka 同机同源同参数、同字体/metadata及完整输出门由 `benchmark/` 独立执行器负责；环境或版本不能锁定则明确未计时，不拿局部单测比较速度。

## 完整字体资源范围

当前 resources 完整性仅指锁定原包 fonts 数据树 553 个文件全部声明，不表示任意包数据/动态依赖完整或 pyfiglet 所有功能通过。Nuitka 必须使用同一完整字体集合及同源 egg-info。slant 完整 stdout 独立保存为 slant.stdout.txt，standard 黄金不变。

原 CLI --list_fonts 的 549 个字体名排序确定。普通非 -O CPython 默认加载 site，两个真实入口均完整输出并 exit0；standard/slant 原黄金不变。最终六个正常模式原流在外部 cp005-normal，环境证据核唯一 pyfiglet1.0.2 分发、no_site=0、optimize=0、no_user_site=1。
早期隔离 -S 探针在打印后调用 exit(0) 出现 NameError，cp004-full-fonts 保留；这是禁用 site 的探针条件差异，不能称上游默认 CLI 失败。若产品缺 site 提供的 exit，须作为真实 CLI 模式缺口报告，不缩为打印549行即成功。Nuitka 与 CP 必须按普通默认 site 语义对齐。

## schema4 组合清单消费

`tests/aot/standalone_manifest.py` 提供专用组合验证入口；旧 default_source_aot.validate_schema4 继续要求空资源、declared_resources_complete=false。组合入口保全原始 schema4 和字段集合，要求自动位置完整、显式声明完整、resources_included 与集合真值一致；复用 source/锚点、延期锚碰撞、artifact 哈希/大小、consumer emitted-ID、预算和便携路径校验。显式空请求可 declared=true/resources=false，无请求不等价。

调用者须独立提供锁定请求的 expected_resources，不能复制 manifest 自己的资源列表冒充预期；须由 resources.json、inputs.json 字节和实际独立 PE source 报告的 emitted-ID/逻辑锚推导。candidate 原载荷及 metadata_report 仍须对独立 PE 原件核对，实际磁盘文件使用既有 check_artifacts 验 hash。仅结构自洽不证明资源请求无遗漏、ILC 保根或 standalone 运行完成。
