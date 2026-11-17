# ADR-0241：Mach-O最小必要重定位与原签名保留

- 日期：2026-11-17。
- 状态：候选已接入，六项计划门及三项真实重签授权门通过；真实产品启动性能待验，默认晋升待定。
- 接续：ADR-0240目标架构切片；ADR-0239应用依赖目录布局。

## 问题与直接证据

真实GUI产物的首次启动出现秒级等待。带阶段日志的NativeAOT OneFile外壳首次
进入Main约3.237秒，进程CPU仅0.014秒；同一轮macOS AMFI记录至Gatekeeper
评估完成相隔3.220秒。内层程序也有3.114秒的同类评估窗口。普通目录产物
首观察的5.301/9.977秒，与逐原生映像评估窗口5.082/9.770秒相吻合。
后续外壳进入Main只有毫秒级。首次观察和复用系统状态分别记录。

相同外壳交替使用`/tmp`与系统临时目录，各三轮均完成首心跳并清理私有目录。
两种根的ready约7.6–11.0秒和8.6–8.7秒；临时目录位置没有稳定收益。
除首轮外Extract约2.85秒，剩余主要耗时位于inner加载阶段。

源vendor映像抽样显示原签名为`adhoc,linker-signed`；旧重定位无条件改写
LC_ID、把可局部解析的`@rpath`改成`@loader_path`，并对全部映像重签。
部署副本变为普通adhoc签名。目标架构瘦身与最小必要重定位分别验收。

## 决定

保留既有拥有集合、依赖目标解析、显式别名、架构与ABI版本检查。
先完成这些检查，再对冻结的原始身份表制定改写计划。源wheel保持原样。
实现只使用Mach-O结构、路径与平台签名事实，无第三方名称分支。

1. `@loader_path`边仅在实际规范路径仍落于部署根、与已核验target相同，且
   target拥有唯一可搬迁相对LC_ID时保留。
2. `@rpath`边要求当前映像的局部runpath第一命中就是已核验target；所有相关
   路径均为部署根内的`@loader_path`。完整拥有集合中的映像都视为潜在祖先，
   任一未知、绝对、`@executable_path`、逃出路径或相同leaf的其它实际命中，
   都回到原严格`@loader_path`改写。读取所有映像的LC_RPATH，包括自身没有
   `@rpath`依赖的映像，以覆盖祖先链提供的搜索目录。
3. 重复LC_ID和同名不同版本沿原严格路径。真实同进程双版本输入的原路径输出
   `41/41`，将两个消费者分别改为局部`@loader_path`后输出`41/83`。
4. 唯一相对LC_ID可以保留；其它身份仅在与严格目标ID不同的时候实际改写。
   显式别名继续按旧解析与重定位规则处理。
5. 只有没有实际加载命令修改、拥有可核验唯一相对LC_ID、并通过
   `codesign --verify --strict`的部署映像才保留原签名。无ID、unsigned、
   invalid签名或实际修改的映像执行原adhoc重签；真实已签名源的重签保留原有
   identifier、entitlements、flags及runtime元数据合同。

从fat文件提取原slice与修改该slice内被签名字节是两个独立动作。
真实vendor universal2仅提取arm64，在两个不同名称与深度的仓外目录均通过
平台验签，保留linker-signed元数据及原LC_ID，独立进程成功加载。
因此切片提取本身不强制重签，决策依据是提取后的真实签名验证。

## 报告与失败合同

每个映像记录保留/重写依赖边数、加载命令是否修改及最终signature动作。
架构选择与签名动作分列；`preserved_valid_original`须有本轮平台验证。
架构报告的`path_base=provider_configuration_directory`明确所有文件路径相对
provider配置所在目录，新布局对应`internal`，旧flat产品对应原根。
文件大小在最后一次改写/签名后读取。

macOS实证发现：即使同时传入`--force-library-entitlements`，仅依赖
`--preserve-metadata=entitlements`仍会在实际重签时丢失库授权，命令却返回成功。
因此在目标slice提取后、任何加载命令修改前读取完整授权plist；只有需要重签时
才以`create_new`在本轮拥有的构建目录创建0600临时文件，显式传入`--entitlements`。
成功与失败均释放临时文件。保有效原签名路径无需写文件。原始plist直接交给
系统签名器，字段名、嵌套结构及类型保持完整。应用根入口的实际加载边重定位
也复用同一快照与重签实现。

闭包缺失、身份不兼容和已有严格诊断继续报错。计划器遇到未知或冲突使用旧
重定位策略。缺失签名工具等执行错误明确失败；平台验签不通过则重签。
无共享可写解包缓存、跨产品复用目录或系统安全设置变更。

## 已验与晋升门

`cargo test -p ryupy-driver --lib macho_relocation_plan -- --test-threads=1`
六项通过，覆盖不同名称与目录深度、重复身份双版本、祖先shadow、未知RPATH、
符号链接逃出、缺失身份和实际签名回退。真实C闭包通过生产重定位函数后搬迁：
独立消费者输出`11/29`，双版本消费者输出`41/83`；原fat切片保留有效签名，
主动移除签名的映像自动重签并通过验证。临时构建产物在仓外并清理。

另三项`macho_relocation_signature`门强制进入实际重签：dylib真实改LC_ID后
重签、MH_BUNDLE强制重签，嵌套dict/array/int/bool授权由平台输出并用plistlib
比较语义，identifier、runtime标志与平台验签同时通过；签名失败也清理临时plist。全owned集合原本均为thin的生产门同时改写依赖边与LC_ID，核对真实授权、identifier、runtime标志、平台验签及实际执行；宿主能力与metadata保留均独立于slice转换。
fat64输入必须先对真实thin slices签名再组包，平台把fat64外壳视为generic文件时
产生的外层签名不能代替目标架构内的真实授权证据。

真实GUI NativeAOT/OneFile与独立产品的完整搬迁门、稳定运行及启动性能继续执行。
正式比较冻结同一布局、编译器、Runtime、Python、输入及依赖，只改变重定位
策略；严格基线CLI与完整assets已单独冻结。首观察与平衡重复轮分别报告，
首心跳墙钟、阶段CPU和RSS各自标明口径。正式性能窗口保持独占。

若候选没有实测收益，退出默认调用并保存证据。当前源码接入属于待验候选，
尚未宣布启动加速或默认晋升。

## 仓外证据

根目录：`~/.cache/ryupy/validation/onefile-compression-20261117/startup-os-diagnostic/`。
`FINDINGS.md`连接系统日志、同外壳临时目录交叉结果、源/部署签名结构比较；
`slice-signature-20261117T221227/result.json`保存真实切片验签与加载；
`independent-inputs-20261117T221807.json`保存独立C输入的双版本红绿结果。
仓库仅保存手写测试输入与测试驱动代码。
