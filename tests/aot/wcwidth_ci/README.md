# wcwidth 原四调用 NativeAOT 基线

只验证原样 wcwidth 0.2.13 的四调用入口，期望字节为 `3 4 2\n1\n`。
无显式实例模块，使用隔离 `ryupy-driver/ordinary-dynamic-module-pilot` 编译配置，
原入口与上游源不改；当前候选修复通用运行时初值合同，不改默认配置或AOT guards。
各新增门禁范围见下节，不能据此声称20项目AOT、12项API或显式实例AOT通过。

## 发布身份

CI 分支以远端 `6e1b469025ab28b5eec12553e5bc2c2c7d28ddb6` 为父提交，
源码快照等价于已验收 `290905691e204f655dd03a02b194fc226a96322d`，
其源码树为 `afae055788244a6b1307c84aeb371c193f25a329`。首轮另加本目录和新workflow；
当前候选在其上增量修复，实际源码身份以本轮commit/tree及文件hash为准。
这不是将原142个本地提交的身份/历史推送到GitHub；不更改main或现有分支。
新CLI及运行时使用本次构建的SHA256，不冒用历史二进制身份。

## 执行与证据

官方普通 `ubuntu-24.04` hosted runner，禁止PR触发；首轮为单job/55分钟，
当前候选的独立jobs与时限见下节。
固定Rust1.99.1、SDK10.0.403、CPython3.12.13+20260807、wcwidth0.2.13下载和哈希。
NuGet/Cargo仍访问官方注册表；Cargo采用现有lock。Clang/zlib来自hosted镜像并记录身份，
不声称复现旧Debian工具字节或固定整个托管镜像。
本次固定UNICODE_VERSION=latest、LC_ALL=C.UTF-8、TZ=UTC、PYTHONHASHSEED=0，
只记录这四个环境值和实际Python flags，不输出完整环境；属于本次新环境合同。未使用本地受限IPC环境的失败作为CI结果。

流程先检查CPython原输入，然后构建运行时与CLI，执行CLI原生 `--aot` 发布一次，
保留SDK真实双流及Host项目。通过后检查ELF和动态依赖，直接执行，再搬移整个dist、
确认原dist路径消失、哈希不变并再次执行。搬移不等于无系统动态依赖。

证据保存于 `target/wcwidth-aot-evidence`：命令、退出码、原始双流、源与工具身份、
发布项目、ELF/依赖检查、搬移证明、首次原生执行前的dist压缩包。CI上传保留3天。
首次失败停止，不自动改引擎或扩大API/模式；失败原流优先于错误摘要。
命令超时会终止本命令进程组；现有引擎在publish结束后才写SDK双流，
若超时被杀，尚在引擎内存中的SDK双流无法保全，超时记录不冒充完整publish日志。整job超时或host故障时artifact上传仅best-effort。
本地无Rust/.NET工具链，仅能做脚本语法及静态自检；`make check`未运行，不能声称通过。

## 无来源元数据修复候选（ADR0138）

首轮run 42360612957失败及缺ELF归档事实保持不变；本次修改不是改原wcwidth源/入口/golden。
当前候选增加两次Native发布（hello无资源及kind0/kind2聚合模块合同），随后原wcwidth发布，及既有
resource_smoke一次资源发布。每次首次执行前完整dist归档上限128文件/256MiB，记录实际文件数、
字节数及每文件SHA；manifest必须存在。若超量、链接、特殊文件或归档期间变化则失败，不裁剪
证据冒充完整。wcwidth原生失败后也已有完整dist，搬移仍是独立硬门。首轮历史实现只在成功时归档，本次不回写或补认其缺失产物。Native job总上限90分钟。

增加default/pilot两个独立完整make check jobs（各100分钟），官方锁补rustfmt/clippy；
feature用ryupy-driver/ordinary-dynamic-module-pilot，先保存make -n展开再执行，不忽略失败。
项目job80分钟，原20官方sdist恢复到P0目录规范、保留_dist，原源审计由既有门禁独立执行；
runner明确编入ryupy-driver/ordinary-dynamic-module-pilot，Session保持默认JIT；
冻结后显式--runtime运行，核验实际run.json工具hash/完成状态及20份实际模块计划。
原11条自动选择逐项保护（4条compiled、7条未编译），不把规划条目冒称运行成功。
独立job不共用可写构建目录，所有job均为普通ubuntu-24.04 hosted runner。CI证据保留3天。

本地只运行Python语法/证据helper自测、固定CP oracle与make -n；禁止借本地socket权限失败
变更SDK/安全配置。完整make check、C#、实际Native及固定20矩阵必须以本次CI结果为准。
