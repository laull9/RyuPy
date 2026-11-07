# wcwidth 原四调用 NativeAOT 基线

只验证原样 wcwidth 0.2.13 的四调用入口，期望字节为 `3 4 2\n1\n`。
无显式实例模块，使用隔离 `ryupy-driver/ordinary-dynamic-module-pilot` 编译配置，
不改引擎、默认配置或 AOT guards。不能据此声称20项目、12项API或显式实例AOT通过。

## 发布身份

CI 分支以远端 `6e1b469025ab28b5eec12553e5bc2c2c7d28ddb6` 为父提交，
源码快照等价于已验收 `290905691e204f655dd03a02b194fc226a96322d`，
其源码树为 `afae055788244a6b1307c84aeb371c193f25a329`。另加本目录和新workflow。
这不是将原142个本地提交的身份/历史推送到GitHub；不更改main或现有分支。
新CLI及运行时使用本次构建的SHA256，不冒用历史二进制身份。

## 执行与证据

官方普通 `ubuntu-24.04` hosted runner、单job、55分钟总上限，禁止PR触发。
固定Rust1.99.1、SDK10.0.403、CPython3.12.13+20260807、wcwidth0.2.13下载和哈希。
NuGet/Cargo仍访问官方注册表；Cargo采用现有lock。Clang/zlib来自hosted镜像并记录身份，
不声称复现旧Debian工具字节或固定整个托管镜像。
本次固定UNICODE_VERSION=latest、LC_ALL=C.UTF-8、TZ=UTC、PYTHONHASHSEED=0，
只记录这四个环境值和实际Python flags，不输出完整环境；属于本次新环境合同。未使用本地受限IPC环境的失败作为CI结果。

流程先检查CPython原输入，然后构建运行时与CLI，执行CLI原生 `--aot` 发布一次，
保留SDK真实双流及Host项目。通过后检查ELF和动态依赖，直接执行，再搬移整个dist、
确认原dist路径消失、哈希不变并再次执行。搬移不等于无系统动态依赖。

证据保存于 `target/wcwidth-aot-evidence`：命令、退出码、原始双流、源与工具身份、
发布项目、ELF/依赖检查、搬移证明、成功时的dist压缩包。CI上传保留3天。
首次失败停止，不自动改引擎或扩大API/模式；失败原流优先于错误摘要。
命令超时会终止本命令进程组；现有引擎在publish结束后才写SDK双流，
若超时被杀，尚在引擎内存中的SDK双流无法保全，超时记录不冒充完整publish日志。整job超时或host故障时artifact上传仅best-effort。
本地无Rust/.NET工具链，仅能做脚本语法及静态自检；`make check`未运行，不能声称通过。
