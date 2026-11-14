# ADR-0188：复用完整 standalone 树的 NativeAOT 单文件

状态：实现；2026-11-14 macOS arm64 本机真实单文件通过，Linux x64 / Windows x64 待真实单文件验收。

## 问题与决定

standalone 已冻结 NativeAOT 主程序、libpython、shim、标准库、原生扩展、资源和许可证的相对布局。
单文件在此完整交付树外增加 NativeAOT 薄启动器，不重新收集依赖、不修改 provider 的加载路径。
`onefile::package(dist_root, native_executable, output) -> anyhow::Result<PathBuf>` 是独立 driver API；
输出必须尚不存在且位于载荷树外，由高层布局事务负责最终命名及替换。建议 CLI 的 `--onefile`
隐含 `--aot`，先完成 standalone 候选，再封装；调用方继续保留失败 attempt 及清单所有权。

Rust 写入私有二进制归档，SDK 构建期托管压缩器流式 Brotli 压缩，再以 EmbeddedResource 纳入
NativeAOT 启动器。源码通过 `include_str!` 嵌入 driver，安装后的编译器可直接生成项目，无需定位源码仓库。
SDK 构建只发生于发布机；目标机按原生可执行文件运行，无需 .NET、Python、Rust、解压命令或联网。
SDK 日志保留于仓库外独占目录，成功即清理，失败诊断明确提供目录位置。无需新增 Rust 依赖。

## 解包与进程合同

每次运行创建独占随机临时目录。POSIX 使用 0700；Windows 以当前用户 FullControl 和受保护
ACL 创建目录，禁止继承临时父目录宽泛权限。无共享可写缓存。所有文件写完后才启动内层程序。
归档路径使用 UTF-8 与 `/`，拒绝绝对路径、`.` / `..`、空组件、Windows 保留名、ADS、反斜线、
控制字符及尾随点/空格。重复路径按宿主平台语义拒绝（Linux区分大小写，macOS/Windows拒绝大小写别名）；普通文件使用 CreateNew。输入链接、reparse
和特殊文件在构建期拒绝；归档无链接类型。路径最多 4096 字节、128 层、条目最多 100000、单文件最多
4 GiB、总解包数据最多 16 GiB；类型、权限与目录零长度同样验证。流式解析使用有界缓冲区，
Brotli 必须显式解码完成，压缩/解包尾随数据、结束标记损坏与任意截断均拒绝。普通文件不添加 hash。

保留目录层级、空目录和 POSIX 0777 范围内权限；执行位完整保留，setuid/setgid/sticky 不进入载荷。
内层主程序必须属于载荷且在 POSIX 有执行权限。以 ProcessStartInfo.ArgumentList 逐个传参，继承
cwd、stdin/stdout/stderr，返回内层退出码。正常退出、启动/解包异常均执行清理；POSIX 转发
SIGINT/SIGTERM/SIGHUP 后等待内层退出再清理，Windows Ctrl+C 终止子进程树后清理。清理拒绝跟随
子程序新建的链接，支持移除只读文件/目录。SIGKILL、Windows强制结束进程及掉电无法执行用户态
finally，操作系统临时目录维护负责其遗留目录；本实现没有常驻守护进程。

## 取舍

保留完整 standalone 布局带来每次解包开销和双层原生文件体积；这是维护成熟 CPython 相对加载合同
与并发隔离的明确选择。后续若增加缓存，需另行制定只读缓存、所有者验证及原子发布协议。
单独设置 PublishSingleFile 无法吸收外部 CPython 依赖；shell 自解压依赖目标机工具且参数/信号
语义复杂，因此采用 SDK NativeAOT 启动器。格式用于结构安全和完整读取，发布程序签名沿平台发行
流程处理；不为普通文件额外引入内容摘要。

## 验收入口

接线后执行 `cargo test -p ryupy-driver --lib -- onefile`，包括非法路径、链接拒绝与 .NET 解包器
源码负例；`cargo test -p ryupy-driver --lib onefile_native_process -- --ignored` 显式执行双层
NativeAOT 发布、删除 dist 和编译输入、空 PATH、中文/空格/空串/换行参数、三流、资源、非零退出码
及四进程并发清理。生成输入和结果均在仓库外，失败保留路径供定位。


## 本机实测记录（2026-11-14）

macOS arm64、.NET SDK 10.0.403。共享 driver 接线由主代理负责，实施时以仓库外 Cargo
小宿主直接引入本模块和 anyhow，执行 `cargo test --offline`（3 通过、1 显式忽略）、
`cargo clippy --all-targets -- -D warnings` 以及 `onefile_native_process -- --ignored`（1 通过）。
后者真实发布内层与启动器、删除源码/dist，验证空 PATH、资源、参数、三流、退出37和四并发。
另外使用 clang 手写进程 fixture 验证六并发与 SIGINT/SIGTERM/SIGHUP，退出码分别为130/143/129，
每次子程序报告的独占解包目录在启动器退出后均已删除。

复用 `tests/cpython/standalone_run.py` 源码在仓库外临时驱动，将完整候选封装后物理删除其
standalone 目录及构建输入，以单文件替换运行入口；固定 CPython3.12.13 同次 oracle 的
sqlite3、ssl、中文默认 IO、第三方包 metadata/资源、stdout/stderr/退出码与文件副作用全部一致。
该轮使用本机缓存的固定 PBS20260623 发行与官方元数据，运行臂 PATH 为空且 Python 路径受污染。
源码解析器负例包含路径逃逸、重复路径、大小/计数越界、非法类型、损坏结束标记、解包及压缩帧
每个字节位置的截断和压缩尾随数据；成功现场自动清理，失败现场仅在仓库外保留。

高层 CLI 接线、三平台 onefile CI 和 Windows 中断的真实行为由各自后续验收负责。

## CLI 与重复发布事务

`ryupy build app.py --onefile` 隐含 `--aot`，发布至输出根的 `app.onefile/app`
（Windows 为 `app.exe`）。该目录仅含一个原生文件；`app.build`、`app.dist` 与
`app.onefile.build` 为构建输入及诊断，搬走单文件后可以移除。完整 standalone 清单随载荷内嵌。

同名发布通过独占锁串行化；新封装完成后才替换旧目录，提交失败恢复旧版本。所有权记录放在
独立 onefile.build 中，依据来源、文件名、大小、修改时间及权限核对已拥有的输出，拒绝覆盖
未拥有文件或被外部修改的发布目录。正常成功清理本轮候选及备份，失败保留仓库外所选输出根中的诊断。

主代理正式 CLI 接线复验已完成：`--onefile` 不传 `--aot` 的原生构建、唯一文件目录、空 PATH 执行、
同名源码更新后重建、语法错误后旧版保留、删除全部输入后搬迁执行均通过。另以同次固定
CPython3.12.13 oracle 执行完整 CPython 载荷的 CLI 单文件发布，sqlite3/ssl/默认中文 IO、
第三方包 metadata/资源、stdout/stderr/退出码与文件副作用比较通过。该 CLI 单文件运行臂
验证唯一文件隔离执行；缺失内部组件负例由 standalone 套件、外层归档结构负例由解析器套件分别覆盖。

首轮CI：macOS完整单文件通过。Linux的官方terminfo含hp2621A/hp2621a等大小写不同的真实条目，
归档与解包改按Linux区分大小写语义保留全部文件，并加入写入器和解包器回归；Windows路径测试
区分原生反斜杠分隔符与归档文本，目录构建器移除非Unix上的unused_mut。MSBuild中间目录
改由命令行提前设定，消除MSB3539；CONIN$/CONOUT$双端拒绝已补齐。修正版继续实机验收。

第二轮 CI：macOS/Linux 单文件正常运行、三流/副作用、退出42、并发、中断清理与损坏头负例通过；
Linux 无Python/.NET/SDK容器亦通过。Windows正常运行、并发和CTRL_BREAK清理通过，OS对损坏头
返回ERROR_FILE_CORRUPT(1392)，驱动补齐该明确拒绝状态，继续复跑。
