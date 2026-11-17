# ADR-0240：按冻结目标RID收敛原生Mach-O架构

- 日期：2026-11-17。
- 状态：macOS arm64、CPython 3.12.13结构门与六产品搬迁门已通过；启动收益待正式测量。
- 接续：[ADR-0239](0239-canonical-internal-application-layout.md)；签名保留候选见[ADR-0241](0241-minimal-macho-relocation-signature.md)。

## 问题与决定

真实OneFile输入闭包约1.39GB，其中大型原生文件包含arm64与x86_64两个slice。
构建已经冻结单一目标RID，部署这些原生映像的其它架构增加复制、压缩和私有解包字节数。
本决定让部署副本与该目标保持一致，继续使用原闭包及原生加载协议。
实现依据Mach-O头、目标CPU/subtype和原重定位拥有集合，没有第三方名称分支。

仅处理既有原生重定位器已经拥有的loadable映像。收敛发生在读取最终加载身份与
重定位之前，导出接口目录与最终文件库存读取转换后的部署副本。源安装文件、wheel的
WHEEL/RECORD、原发行标签、来源URL与源码归属保留原始事实。

首切目标为`osx-arm64`：必须有且只有一个`CPU_TYPE_ARM64`、
`CPU_SUBTYPE_ARM64_ALL=0`的slice。fat32/fat64大小端表均可解析，必须证明表长、
alignment、范围、无重叠及内层Mach-O64头的CPU/subtype一致；内层加载命令区必须
落在slice范围内。只有`MH_EXECUTE`、`MH_DYLIB`、`MH_BUNDLE`进入转换。
其它目标RID、未知subtype、多个相同候选、非loadable或证据不足保留原部署映像，
记录具体reason。缺目标架构继续由原ABI检查给出既有诊断。

读取只涉及有界fat表与目标头；选中后流式复制该字节区间，保留文件权限，以同目录
独占临时文件和原子rename提交。创建临时文件失败时没有删除该路径的权限；后续复制
失败只清理本次成功创建的临时文件。可选头部预算不足时记录保留，原ABI准入继续执行。

## 集合边界与发布合同

原collector的无扩展名magic入口目前覆盖fat32大端；fat64及交换字节序解析能力
服务于已经拥有的文件，未扩大任意资源扫描。真实无扩展名helper沿fat32入口，fat64
扩展沿原`.so`入口。disassembler输入、原始fat数据及其它非拥有资源保持原字节内容。
`CAFEBABE`也可出现在其它数据格式中，本实现没有仅凭magic增加全资源树准入。

单RID产品按该架构发布。需要双架构交付时分别构建各RID产品；本轮没有增加通用
双架构应用分发模式。原先已接受但缺少精确subtype依据的输入保留原映像，保持原准入。

架构转换独立于LC_ID、依赖边、版本和alias处理；转换后继续原架构/加载身份检查。
严格基线沿原策略重定位及adhoc重签，转换文件保留identifier、entitlements、flags及
runtime元数据；macOS15及以上使用平台要求的library-entitlements选项。上游签发者
身份没有被复制为新签名。仅提取slice时原slice内签名可能仍有效，ADR-0241依据平台
实际验签与未修改加载命令来决定是否保留，切片本身没有永久强制重签要求。

## 报告

provider报告新增`native_architecture_convergence`，记录冻结RID、拥有集合范围、
每文件原容器/fat表、选中CPU/subtype/区间、loadable类型、转换动作与skip reason、
原大小及最终重定位/签名后大小。`preserved`仅指没有slice转换，后续原重定位和签名
动作独立记录。`path_base=provider_configuration_directory`明确文件路径锚点：
canonical发布对应`internal`，旧flat及当前OneFile私有树对应原配置目录。
provider顶层`file_path_base`采用同一锚点；central artifact manifest仍相对公开根。

## 验证与有限合同

五项Rust结构门覆盖fat32/64与双字节序、三种loadable类型、非loadable与不明subtype、
范围/alignment/重叠/内层冲突、原ABI缺目标诊断、原子权限和已占临时文件保护。
旧冻结CLI同输入保留universal映像且缺少收敛报告，已保存红证。

两种独立原生消费者分别为传统单阶段DYLIB扩展与PEP489/heap-type BUNDLE扩展，
使用真实arm64/x86_64 clang输出组成fat32/fat64。每个消费者还有普通无PyInit共享库
与无扩展名原生helper。验收同时检查真实初始化导出、CIL回调、异常、身份、原WHEEL
universal标签、签名identifier/runtime/entitlements、最终thin头及库存大小。
固定v3曾在fat64 BUNDLE部署发现授权丢失；v4签名修复后又在真实运行发现显式资源锚点
被provider bundle位置覆盖。最终v5保留compiled_sources来源登记，让显式资源plan优先
决定模块位置，Runtime把同一位置写入真实D.__file__；原资源读取断言保留并转绿。JIT、NativeAOT、OneFile验收要求各自删除源输入、wheel、虚拟环境及构建目录后搬迁执行，
逐项与同次CPython三流比较。AOT/OneFile还真实读取集合外fat64资源的magic与长度。

fat64源签名另有平台差异：本机codesign把fat64当generic数据，仅给外层签名；
验收输入改为逐真实arm64/x86_64 slice签名并核验后再组成fat64，直接检查选中slice的
CodeDirectory，确保源授权确实属于待部署映像。修正输入后仍在fat64 BUNDLE部署发现
entitlements丢失；独立thin重签实验也确认preserve-metadata命令会丢授权。
现实现已在slice选取后、任何加载命令改写前捕获完整授权plist，真正重签时显式恢复，
公开AOT入口搬移加载边也复用同一恢复器。闭环证据由ADR-0241及最终六产品登记。

源helper曾使用runtime加自定义entitlements：平台静态验签成功，但实际exec被OS
SIGKILL。该红证完整保留；最终helper使用runtime且无entitlements，库继续保留真实
entitlements并由加载与回调验收。平台验签和真实loader/exec均为独立必验项。

最终固定CLI为`internal-layout-v5/ryupy`及其精确嵌入资产。两消费者×三形态六项全部
通过：源码/wheel/venv/构建目录物理删除、任意cwd与空PATH启动、CPython三流相同、
每项两次静态CIL回调与两次析构，所有Host终态计数为零，私有OneFile解包目录清理。
集合外资源在发布后逐字节与源相等，业务运行也成功读取完整fat64 magic及长度。
每消费者的三个真实原生映像由200208→101904字节、200784→102464字节，约减少49%。
该数字只覆盖此处三个夹具映像，完整应用载荷另行测量。最终候选同时包含ADR-0241的
重定位策略，本六项验证最终组合正确性；单变量启动对照使用独立冻结资产。

本轮只登记尺寸和正确性事实。正式启动性能由同源码、同布局、同Runtime与同输入的
单变量对照确定，旧flat与新canonical产品的墙钟不能归因于slice转换。首观察、平衡
重复轮、诊断分段及RSS分别记录，最终收益数据在正式独占测量后补充。

## 仓外证据

`~/.cache/ryupy/validation/macho-slices/`保存源签名失败、旧版红证和六产品原始日志。
最终六产品与逐项汇总位于`20261117T153001.854797Z/results.json`、
`verified-summary.json`、`*-convergence.json`、`*-host.json`及`commands.log`。
旧版红证`20261117T150420.651259Z`、v3授权红证`20261117T150319.053428Z`
及`source-fat64-signature-facts.txt`分别记录上述事实。
`/tmp/ryupy-macho-slice-unit.log`与`/tmp/ryupy-macho-slice-clippy.log`保存定向检查。
仓库只保存手写C输入、fixture构建器、验收驱动与本文，生成产物清理，普通文件不计算摘要。
