# schema9 联合分发与公开离线验包：三平台整合验收

## 身份与结论

独立分支 `integration/dev-schema9-verify-20261113` 的实际受测代码提交为
`a9c2027f239b088298c8fb4ca8ee5b25aff6f4e8`，树为
`3864b6da7e6125860a9eb501ba93d86e4d25381b`。真实双亲分别为用户 dev
`a851eab14c9c38a37ee0e510e8ca728ea77786a5` 与已验离线核验候选
`0ad73ec6a6112b522dd91f4c0e4aee20df58fd67`。未覆盖或更新 dev/main，未强推。
本报告收尾只改验证文档，不改变受测实现、测试和 workflow。

保留用户标准库源码、C ABI、资源联合分发语义、完整新增测试及
[ADR0175](../adr/0175-stdlib-binding-resource-distribution.md) 原文；我方同号文档移至
[ADR0189](../adr/0189-resource-output-preflight.md)。统一资源组合保持用户 `declared`、
消费者投影与完整旧状态恢复，不重复投影。用户原11步 runner 和 fixture 未编辑。

本地完整回归、两组三平台 CI、27份归档的 size/SHA 与原件独审全部通过。
这证明本提交在所列环境完成实际 schema9 联合交付及公开文件完整性核验，
不是完整 schema 语义、全部 Python/Nuitka 兼容性或新的性能收益证明。

## 可复核证据

- [联合分发、ABI/catalog 和入口回归 43389138507](https://github.com/laull9/RyuPy/actions/runs/43389138507)
- [两真实应用公开验包 43389138578](https://github.com/laull9/RyuPy/actions/runs/43389138578)
- [逐平台独审、归档身份与本地日志摘要](schema9-verify-integration-20261113.json)

两轮均为上述同一代码 SHA；Linux x64、macOS arm64、Windows x64 共6个 job 全成功。
固定 Rust 1.99.1、CPython 3.12.13、.NET SDK 10.0.403。归档保留期7天，CI产物并非永久存档；
审计摘要及小原流另行保留。清理本地 ZIP 缓存前已核远端同ID、size、SHA且未过期。

## 已实际验证

每平台的 schema9 合同均完成原11步，包括真实 CP/JIT/Native 对照、缺资源构建失败
保旧包、删除源码/build/JIT后 Unicode/空格/`&` 路径搬迁运行及最终缺资源运行失败。
联合包包含 `colorsys` 的 `cpython_source`、`native_probe` 的静态 `c_abi_binding`、
显式 `pkgdata` 资源、自动模块位置及两项完整性范围标志。绑定联合构建使用用户现有
`ordinary-dynamic-module-pilot` 功能，不能冒称默认构建已开放该兼容范围；公开 verify
始终使用同提交冻结的默认 CLI，并核实际工具字节未变化。

专用观察器在原真实成功构建/搬迁命令返回后同步验包，原参数、环境和 CompletedProcess
保持不变；观察异常直接失败。原 runner 最后会删除资源，故先保存当时完整包，
不事后填回资源冒称原件。三平台共6次正验包、18次拒绝：实际最终缺资源包及5种独立副本
（同尺寸资源篡改、缺文件、多文件、未知版本、自洽非ASCII资源名）。最后一种同步更新
文件及两处清单路径、保持hash/闭集自洽，并核明确ASCII资源诊断，防止只放宽版本号而漏校验。
实际原流、清单闭集、源/资源SHA、前后字节与模式快照、SDK/ILC输入均独审。

旧兼容门在同提交重新执行：每平台 direct入口12次、v1/v2多导出与schema4/5/6资源、
`as_file`、通用ABI37步/24导出/14类坏声明及schema7/8受限环境搬迁。catalog source-probe
现为11项（包含用户新增扫描测试），colorsys2项各55行，CLI诊断6项。
Windows路径120项全过；Linux/macOS各116过、4个真实Windows宿主专属项明确跳过。

两真实应用 `pyfiglet`（553字体和metadata）及 `binaryornot/chardet` 每平台公开构建后
在隐藏输入/build的条件下搬迁，合计21组 CP/Native 原流一致。合计90次验包：30成功、
60拒绝；包含各13种独立副本中的3个明确允许情况。schema9与两应用验包均无skip。
空工具PATH、失败DOTNET/PYTHON哨兵及验包前后树不变已核；这些不是系统调用沙箱。

## 本地回归与保留失败

`make check` 第005轮完整退出0：C#3116通过/6跳过、driver277通过/4ignored、
CP参考740通过、RyuPy e2e458通过/286ignored；追加 fmt、clippy 均退出0。
6项观察器轻测通过。CP参考通过不计RyuPy新增兼容，ignored也不算通过。

保留失败而不隐藏：001为机械合并残留字段/未用函数编译问题，已修；002为未改C#零分配
测试一次失败，同代码定向重测通过且最终全套通过，未改阈值或排除；003/004因磁盘不足
失败。核可重取原件后清理缓存，再清8.5GiB可重建Cargo缓存完成005。
005在C#阶段另发现未知版本负例仍为9，已改10后才编译Rust测试，最终补fmt/clippy覆盖。
不以重试记录产生新性能结论。

## 边界

verify只承诺文件闭集、受限相对路径、size/SHA及唯一native角色的内容自洽。
`authenticity`、`runtime_compatibility`、`schema_semantics`仍为`not_checked`；自洽改写
文件与清单可被接受，不是签名认证、执行权限、完整原生依赖闭包或目标机资格认证。
同步观察不是原子快照或恶意并发替换防护。GitHub ZIP不独立证明现场权限/链接，
以现场前后快照限定；同宿主受限PATH搬迁不等于无SDK干净机。本轮没有性能测量或收益外推。
历史[1–8验包报告](distribution-verify-20261113.md)保留原提交与原范围，本轮新增schema9实测证据。
