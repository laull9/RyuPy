# 用户catalog与通用C ABI的AOT候选整合

独立分支 `integration/dev-catalog-aot-20261113`；不修改dev/main，不强推。

## 合并边界

先以用户dev `3237be71` 为第一父合入已验AOT候选 `1dddbb2c`，发布前再次发现用户新增通用ABI `34924e16` / 验收 `979a7f3c`，进一步非破坏性合入。保留用户来源catalog、静态probe、分支别名候选、多签名C ABI、缓冲借用、schema7/8与原测试；没有复制另造一套ABI实现。

唯一两处文本冲突为CLI Probe邻接Build注释及driver build_configured注释，均保留两方功能。自动合并另核实际职责：来源报告不授予编译权限；queue仍detected_only；来源Token绑定真实内嵌字节和部署路径；固定绑定仍隔离默认源码选择。资源请求仍校验owned output_roots后合成，默认优化不变。Probe不接收项目metadata/resources，不冒充最终部署完整性证明。

用户ADR0173（通用C ABI）与0174（版本化catalog）编号及正文保留，我方旧0173 Windows词法重编号0187、旧0174资源树重编号0186，引用同步。没有恢复生成IR快照。

## 失败与修正留痕

- 首轮本地check：我方资源拒绝测试旧期望仅build-error.log，用户新catalog会在语义/发射前落source-plan。改为精确允许两文件并核compiler_verified=false，继续保证未发射、SDK不启动、失败保旧dist全字节。不是放宽生产路径。
- 首轮CI43377249978 Mac：默认temp的/var祖先是链接，被安全规则拒绝，source_probe两失败/JIT一失败；真实colorsys AOT搬迁55行通过。改验收步骤专属TMPDIR/TMP/TEMP至固定SDK根内真实目录，不改产品链接规则。该调整也避免另装SDK被意外选择。
- 同轮Windows：first/colorsys.py混合分隔符期望与实际first\\colorsys.py不同；仅把测试期望改为逐级Path.join，保first provider选择和session还原断言。JIT/AOT各55行本来已过。
- 本地额外真实ILC试跑遇MSB4216：沙箱MSBuild无法启动/连接NET task host，保失败日志；不计AOT通过，以GitHub实际宿主原生回归另证。

## 验收范围与未覆盖

用户colorsys原测试覆盖六API共55行CP差分，显式OrdinaryDynamicPilot，不改变默认准入。成功测试会自行删除临时树，证据为未修改测试的命令/断言日志，不声称独立保全该成功原生包。旧C ABI多导出及新通用ABI分别验收，不互相替代。

最终三平台门包括源码probe、catalog JIT/真ILC搬迁、CLI诊断、直接/薄入口、v1/v2多导出、资源5/6、as_file、v3 ABI7/资源8、坏声明与坏archive、源/build移走后空工具PATH搬迁。受限PATH仍是相同安装工具的宿主环境，不是无SDK全新机器证明。仅有限API/项目验收，不证明Nuitka全部兼容或整体性能。

先前性能对照仍严格属于提交1f524989及CI43372683973；本次新增catalog/ABI未重新做性能A/B，不把此前12%左右构建收益伪作新dev测量。

## 最终已验证结果（2026-11-13）

代码提交 `1920f5b1e9ca987379b78a5fab687d72ada6e054`，CI [43379355977](https://github.com/laull9/RyuPy/actions/runs/43379355977) 三平台全部成功，六份原件各自SHA/长度及内容已核验。全部原件身份见[机器收据](catalog-aot-integration-20261113.json)。最终本地完整 `make check` 退出0：C#3116通过/6跳过，driver库256通过/4忽略，编译e2e458通过/286既有忽略，未删除失败用例或改pending。

每个平台source_probe10项、catalog JIT/AOT两项（各55行CP差分）、CLI诊断6项通过；旧入口协议各12次，原多导出及资源5/6、as_file继续通过。Windows路径120项也通过。

每个平台新v3验收37步骤、24导出、14项无效声明、103行固定CP原流与JIT/Native/搬迁/失败后旧包逐字节一致；三个loader负例通过。JIT/Native schema7及资源Native schema8的交付文件全集size/SHA已核，真实缺符号archive失败不破坏旧包。Windows源fixture仅精确LF→CRLF检出差异，未改源语义或原输出。

同一代码的前序CI43378769925 Linux/Mac已通过，Windows仅尚未修的路径期望失败；最终run重新完整验证三平台，不拼接前序通过冒充全绿。最终文档提交仅记录结果，代码与上述已验证提交一致。dev发布前仍为用户 `979a7f3c`，本次未修改。
