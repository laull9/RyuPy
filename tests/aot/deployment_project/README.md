# 普通项目部署入口合同

本目录测试统一 `ryupy.json` 项目入口，不改变已通过的 standalone_app benchmark。
配置路径必须显式给出文件，不做目录自动发现；首版仅 `mode: "aot"`。

## 普通项目准备

测试环境先恢复锁定 pyfiglet 1.0.2 包，再执行：

```
python3 tests/aot/deployment_project/prepare.py /work/pyfiglet-project
```

这是测试输入复制，不是产品构建工具。生成目录自含原包源码、553 字体数据、完整 egg-info、原 CLI、已有 metadata 入口、resources.json、metadata-request.json 和 ryupy.json。不用下载脚本、prepare 或 oracle 即可直接公开构建：

```
ryupy build --project /work/pyfiglet-project/ryupy.json --target cli
ryupy build --project /work/pyfiglet-project/ryupy.json --target metadata
```

新生成的 `resources.json` 使用显式 `pyfiglet.fonts` 目录 `"."` 与 `.flf` / `.flc` 后缀，不再手写 553 文件名。轻量测试独立对照旧 `standalone_app/resources.json` 与锁定输入，要求集合精确一致；旧 benchmark 请求与结果保持不变。新选择器仍需新的真实 Native 验收，下文历史 CI 只覆盖当时的手列声明。

默认输出分别位于项目 `build/cli`、`build/metadata`；`-o` 覆盖仍相对于调用者 cwd。项目配置的 search_paths、入口及请求文件相对配置文件父目录；metadata 请求内部 roots 继续相对于该请求文件自身，不重写语义。

## 验证层次

`test_project.py` 是轻量配置与真实公开 argv 合同，不等于 Native 成功。
`public_build.py --cli ... --runtime ... --project ... --output ...` 从无关 cwd 对两个入口实际公开 AOT 构建；然后整体搬迁项目再构建。每个产物核 schema4/清单文件和真实 ELF，并移出项目、隔离源码后执行 standard、slant、list_fonts 三模式。四个独立尝试保留失败原流，任意失败使整门失败。

该探针不运行独立 oracle，也不将普通产物清单自检称为独立深度验收。结果明确标记 `deep_receipt: not-performed`、`clean_container: false`，不生成任何性能比。独立源/PE/metadata 审计及无 Python/.NET 容器验证由下述 hosted 门另行完成。旧候选已成功的产物不算本项目入口的新验收。

## 独立 hosted 门

`.github/workflows/deployment-project-native.yml` 仅跑项目部署验收，不安装 Nuitka 或重测旧性能。默认 Release CLI 一次构建，两个入口分别在原项目位置和整体搬迁后调用公开命令，共四个真实 AOT 发布。每次产品构建成功后才调用独立 exact oracle，复用实际 PE/metadata/资源 receipt 深验；oracle不是产品构建所需输入。每份 bundle 在隔离源码后跑三模式，再在固定无 Python/.NET 镜像禁网、仅挂只读 dist 执行，共12次容器运行。任何一臂失败则整体失败，无性能数据或速度结论。

本地新CLI对应最终driver源码重建后，真实公开命令已越过解析/资源/metadata并完成restore，但SDK task host出现MSB4216/4027，无ELF；该结果不是Native通过。该本地失败保留，不覆盖下述新的 hosted 候选验收。

搬迁重建前，将原项目 `build` 整目录移入证据区 `original-project-build-preserved`，完整保留原始owner和产物，再搬迁自足输入项目。新位置默认输出为空，正常重新构建；不删除旧证据、不放宽绝对source owner，也不声称旧编译缓存可以随项目搬迁复用。

## 已验证的有限结果

[CI 43255885614](https://github.com/laull9/RyuPy/actions/runs/43255885614)，候选 `2556fea1`，使用无 feature Release CLI，已通过 Linux x64 的 pyfiglet 1.0.2 两入口合同：原 CLI 和真实 console_scripts metadata 加载同一 main。原位置及源码项目搬迁后共四次公开 `build --project FILE --target NAME` 成功，四份独立源/PE/metadata/完整字体资源 receipt 通过；standard、slant、list_fonts 三模式共12次隔离源码宿主运行、12次禁网无 Python/.NET 容器运行通过。

此结果只证明上述锁定项目、入口与模式。源码项目搬迁后是新位置重新构建，旧 build 完整保留，不是编译缓存搬迁复用；不扩展为跨平台或任意依赖支持，不产生新的性能结论，也不修改旧 benchmark 数字。

## macOS / Windows 当前候选专项门

`deployment-project-platforms.yml` 用同候选默认 Release CLI 和 Release runtime，固定 Rust 1.99.1、CP 3.12.13、.NET SDK 10.0.403；运行时再核平台/架构/版本和工具摘要。macOS 仅 arm64，Windows 仅 x64 MSVC。每平台两个公开项目target，独立同RID schema4 receipt，原位及搬迁各三模式，源目录隔离；Mach-O/PE格式和otool/dumpbin依赖审计不接受Linux ELF或托管PE替代。平台CP原流按真实上游协议：standard/slant在detach后直接写bytes，保留LF；fonts走文本print，才使用平台换行。Native逐字匹配该CP，不改输出忽略差异。

这些平台在装有SDK的GitHub宿主上以精简PATH运行，**不是无Python/.NET干净容器证明**，也不生成性能测量。旧Linux容器链不改变。初始候选曾缺 Windows 分发 metadata 末端 nofollow；后续已补实现，并继续要求三项指定安全测试实际通过，不删请求或跳过 metadata 入口凑成功。最新结果见下文，历史失败不改写为成功。

首轮平台CI 43261914690未获得Native证据：macOS14标签已退役、未分配runner；Windows先被测试自身默认编码及斜杠断言阻断，尚未触发产品metadata读取。第二候选改用官方仍支持的macos-15 arm64，保实际架构断言；所有本门文本读取显式UTF-8，配置与黄金binary流不额外归一化。Windows新nofollow合同须真实执行三项指定Rust测试（链接/目录等创建失败也不能skip），然后完整两入口项目验收，不能将Linux单测当Windows安全证明。

## 2026-11-12 跨平台复核断点

[CI 43285836945](https://github.com/laull9/RyuPy/actions/runs/43285836945)，候选 `17df93ba`：macOS 15.8.2 arm64 两入口 Native 构建、2份独立receipt、原位/搬迁共12模式运行通过，并复核原件。Windows x64 完成 nofollow 3/3、两份 Native 构建和receipt；但两个入口首次正常执行仍抛 `NotImplementedError: Windows lexical paths are not implemented yet`，尚无应用成功结果。不能将构建成功算作可用发布。

本轮把平台前提与产品语义分开：Windows 的 HOME/USERPROFILE/APPDATA/LOCALAPPDATA/TEMP/TMP 位于私有空目录，仅保留必要系统目录变量，PATH 为空。CPython 与 Native 使用完全相同环境；CP仅通过显式源路径加 runpy 启动两个锁定原入口，不把此 wrapper 声称为任意程序的通用等价启动器。源隐藏前 CP 六模式通过；CP 与 Native 各两次缺 APPDATA 负控均得到上游声明的 KeyError。随后 Native 的 Windows Path 拒绝才是已隔离出的产品缺口。

固定 PE 读取器使用精确 SHA；`.gitattributes` 只固定该文件 LF，实际 Git autocrlf 回归同时拒绝 CRLF 及其它字节篡改，不做归一化后放行。产物ZIP及原始输出在验收证据区保存，生成 IR/编译物不进入源码仓库。

## 显式资源目录选择验收（2026-11-12）

新compact声明独立通过 [Linux CI 43296651927](https://github.com/laull9/RyuPy/actions/runs/43296651927)，候选 `f1765bb4`：四次公开AOT、四份独立receipt各553资源、12次宿主和12次禁网只读无Python/.NET容器运行。旧手列清单仍独立推导资源预期，未改benchmark合同。

[同树平台CI 43298130779](https://github.com/laull9/RyuPy/actions/runs/43298130779)，触发提交 `c16be5c4`（两候选树均 `c42b182b`）：macOS arm64与Windows x64各两入口、两份553资源receipt、原位/产物搬迁共12次运行，与各平台CP原流精确一致。Windows路径宿主3/3及metadata nofollow3/3实际通过。ZIP摘要与原流已复核；这些平台宿主仍装有SDK，不等于无Python/.NET干净机，不给新性能数字。

## 输出相交预检门

平台门另执行资源/输出边界与事务分组：Windows 3+2项，macOS 3+4项，名称及计数均须匹配，忽略/零测试不得通过。覆盖实际生成目录相交、合法兄弟、原产物不变及POSIX输出别名/fake SDK；之后继续原两入口真实Native流程。边界测试不是Native成功的替代证据。
