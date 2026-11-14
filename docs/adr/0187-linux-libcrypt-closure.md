# ADR-0187：Linux libcrypt ABI 的构建期系统包冻结

- 日期：2026-11-14
- 状态：实现；macOS 定向控制流验证通过，真实 Linux 产品与搬迁等待 CI

## 决定

PBS CPython 3.12.13 的 `_crypt` 扩展依赖 `libcrypt.so.1`。该库属于产品闭包，继续使用
ADR-0186 的核心系统库精确白名单，禁止以添加 libcrypt 白名单消除构建失败。

Linux 每条非核心 ELF 边先在冻结产品载荷中唯一解析。同一真实文件的符号链接别名可合并，
不同真实文件的同名候选拒绝。所有原生输入静态验证 ELF64、小端、x86-64，并要求 readelf
成功读取头部、DT_NEEDED、SONAME 和符号版本；版本需求必须由选定库的定义满足。
含路径的 DT_NEEDED 拒绝，现有载荷无法满足时不会回退替换它。

本次系统收集明确限于 `libcrypt.so.1`，作为 `_crypt` 的必要系统 ABI。仅从本机
`/sbin/ldconfig -p` 的 x86-64 记录解析，真实来源必须唯一；通过 dpkg-query 的文件归属、
已安装状态、amd64 架构、精确包名及版本核验后复制到 `python/lib/libcrypt.so.1`。
缓存路径与解析后的真实路径一并冻结。未授权的其他边要求显式 payload；未安装 dpkg
或缺少来源、包版本、许可证均构建失败。构建不加载依赖库、不运行 ldd、不联网、不扫描复制
系统库目录，不对普通文件做摘要校验。

Debian 包登记的 copyright 必须存在并包含 License 字段；原文和引用的 common-licenses
文本复制到 `python/licenses/native/<包名>/`，引用文本缺失即失败。来源报告写入
`python/native-sources.json`，包含 SONAME、来源/真实路径、包名/版本/架构、解析器、
相对目标与许可证列表，随既有 python 文件清单一起发布。后续 provider 报告可以引用此文件。

新收集库进入同一增长队列递归审计。完整闭包确定后，逐映像将 rpath 重写为 `$ORIGIN`
及各被依赖目录的相对路径，并读取实际 rpath 复核。运行期消费随包文件，不查构建主机缓存。

## 取舍与验收

选择窄范围 ABI 策略，使本次 Linux 独立发布所需的 libcrypt 来源明确；其他发行版包管理器
和通用系统库采集留待独立设计。若未来开放额外系统来源，应在 provider 冻结计划中声明
允许的 SONAME/包管理器，沿现有部署计划传递，禁止用环境变量形成隐式运行期来源。

定向入口：`cargo test -p ryupy-driver --test cpython_linux_closure linux_`。
macOS 执行缓存/版本/歧义/许可拒绝测试及子进程 mock readelf/patchelf 控制流，覆盖
正常相对 rpath、缺库、错架构、同名歧义、符号版本缺失。mock 输入是手写 ELF 头，
用途为构建控制流；Linux 原生执行验收由 CI 完成。
Linux 条件测试使用 cc 构建真实依赖 libcrypt 的共享对象，走真实缓存、dpkg、许可复制与
patchelf，并验证冻结库、来源、许可、相对 rpath 及错架构拒绝。CI 必须运行该专用入口，
再运行 standalone 的 JIT/NativeAOT 产品构建、搬迁和缺库负例，验证本次真实阻断已解除。
所有测试临时输入/产物在仓库外创建并由作用域结束清理。
