# ADR-0155：有限JIT默认来源候选与分发环境

状态：实现候选，尚未完成同源Rust/C#/产品JIT与完整保护门；不提升项目通过计数。

## 目标与实际开关

在现有ordinary-dynamic-module-pilot编译策略下，真实普通JIT build可从入口目录及
有序search_paths准备有限源码与分发快照，不要求用户补catalog路径、instance、include
或distribution-metadata参数。ExplicitOnly、AOT、fixed-binding关闭自动路径；默认
feature-off仍保持原策略，不把pilot结果声称feature-off默认通过。现有显式接口不变。

## 固定来源与发行资源

编译器携带一个确定性未压缩USTAR，真实源码族为CPython四文件和完整许可证、manifest。
资源包是实际include_bytes唯一来源，不读宿主Python，也不把测试证据包当运行路径。
来源与重建方式见driver/catalog/README.zh.md。固定上游html/entities.py仍2513行，
本次批准为原样部署资源输入；维护代码各≤600行，不能把此范围推广为vendor豁免。

只读固定USTAR子集，无用户tar入口、新依赖或通用解包框架。外层digest、固定成员与
metadata、头校验、padding、manifest长度/hash与许可逐项核验，损坏全部硬停。需要的
成员恢复到最终output/compiler-sources/<族摘要>，create_new不覆盖已有未知内容；
最后nofollow读取同批字节后入SourceDb，SourceAbsolute为真实.py位置。实例能力令牌
绑定最终来源和digest，不按模块名、用户同名/同字节文件授予class能力。

普通resolver首先选顶层身份，后代仅沿已选普通父包目录查找。禁止跨搜索根补子级。
目录与源码不同来源的冲突显式失败，不使用最后登记覆盖。

## 非绑定发现、固定点与诊断

普通import别名的literal find_spec以及metadata.entry_points提示仅准备候选，未证明
callee或可达性。alias保留有限目标集合，嵌套重名import不能抹掉外层提示。每个首次
解析候选也继续收提示，metadata inventory只采一次，累计group选取完整快照。普通
导入/父链传递hard，hint/选中group产生soft，Soft→Hard只升级并再传播依赖；循环去重。
节点预算512、源码字节预算4MiB；软预算越界记录域外，硬超限失败。

单SourceDb，候选读/parse与编译诊断隔离。只有syntax真实ParseRejected，以及HIR
async-with产生点的明确SupportBoundary可延期；不按E000/E0100前缀推断。硬或显式
include失败保构建失败。未知、类型、MIR、链接、codegen、安全IO与catalog错误硬停。
拒绝源码自身零正文；真正加载报NotImplementedError，不承诺精确SyntaxError/位置协议。
成功调用方/父包/依赖副作用仍按实际执行保留。失败候选不入units；entry固定0，候选
尾部追加，成功units由现有link重新映射function/global/class/export编号。

## 冷注册与真实激活

候选在独立目录，不能提前进入Registry/Cache/父属性/ModuleForFile，也不改变硬图
native异常认证或产生跨模块直调证明。MIR携带候选名称与冷载荷；CIL只机械传递，不读
HIR、driver或文件。既有热函数/数字/global调用ABI与RuntimeFn枚举不变。

新增CLR冷方法仅四个：RegisterCandidateSource、RegisterCandidateInstance、
ConfigureOptionalSources、PyDistributionSnapshot.ConfigureAutoUnavailable。

find_spec保参数与目标缓存spec短路后，在父导入前检查候选依赖族。Import保目标Cache
最先返回，仅明确root且没有active实现时，在父Import前复用整组check/commit。普通
import已存在的native父不会仅因它属于另一个候选族而激活。检查不调用Python getter/
import/hash/eq，source与deferred成员都验证真实位置、已有Registry/Cache、别名、
hooks及受保护native父链；任何冲突全组零发布。已有缓存native父明确NI，不迁移身份。
提交成功才走原父导入、父后Cache复查、path校验、创建/缓存/初始化。提交后正文失败
沿原清理，不回退native。Unavailable实际失败在父与path检查之后，每次新异常。

Outside来自完整metadata中未规划的有效targets/前缀，不造source/path/loader，不覆盖
已有native/source/cache。EntryPoint.Load保持实时owner.import_module再getattr；重绑
可绕开真正Import，不能前置Outside检查。单线程新鲜JIT域，不引入并发/多宿主框架。

## 自动metadata

只扫描入口与有序-I直接分发根，不猜src上级、不扫宿主site-packages、不执行setup，
closed_snapshot不跟随动态sys.path。快照保全部group/value与所有metadata字节、顺序和
遮蔽，不把18个已选targets误报成Markdown总计19条。显式distribution-metadata完整覆盖
自动来源，原硬fail不变。自动已知环境不可读、超预算等为AutoUnavailable，不造假空或
部分快照；Observe/import/startup不抛，仅Require NI。内部不变量与载荷损坏不软化。

## 审计与未验事项

module-environment.json保原字段并追加candidate/hard/catalog_authenticated；
distribution-metadata.json保完整snapshot摘要与payload_sha256；optional-sources.json
记录schema1 roots/dependencies/deferred/outside/protected/locations和原诊断审计。
这些文件不是运行期authority，也不证明执行成功。

原完整parser A仍原样pending；独立双parser仅验证保留两类与各自globals的限域合同。
真实Markdown原两调用77B、独立parser48B、固定20逐ID保旧11、双策略make check及旧
source/ABI/trace/Native/AOT门仍需同源执行。不得用helper单测、显式18-target历史成功
或source部署成功替代默认产品结果。生产触点29，超初估18–25已单独记录，后续不扩域。
