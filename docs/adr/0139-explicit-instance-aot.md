# ADR-0139：显式普通实例 AOT 与部署相对来源

状态：保留初版合同及其历史范围；下方普通类增量是待同源 hosted Native 验收的候选，不将本地结构/JIT控制结果写成 Native 通过。

## 决定与边界

仅显式选择真实 ModuleFile 可以申请此 AOT 域，入口、包、原生绑定不能申请。
编译任何模块前，driver 必须验证资源计划覆盖全部非入口源码，所有点分包祖先
均在实际图中且为 Package，并完整检查所选源码 AST。私有成功结果决定显式计划
及 relative_from_import、ordinary_name_handlers 两能力；最终 UnitMeta 再核 kind2。
该内部合同不是手构 IR 的安全隔离；原 HIR/MIR 语义验证继续保留。

空 files 资源声明只创建 ExecutableRelative 元数据锚点，不部署源码，不承诺
__file__ 对应物理文件存在。原资源门仍禁止 .py/.pyc/.pyo。native 内建无伪造来源。

初版 AST 域必须有普通动态 def，拒 class、完整 typed 签名、嵌套 def 和生成器；
其中 class/方法分类由下方普通类候选增量取代，其余边界不撤销。
完整遍历包括默认值、装饰器、不可达分支，并叠加全部既有 Instance validator。
JIT 不增加上述限制，自动 AOT 仍为 aot-not-migrated。

lambda 仅允许固定 posonly/positional/required kwonly、无默认值/变长参数；body
正向限定为参数 Load Name、str/bytes/int/float/bool/None 标量、Attribute 和不展开
的 Call，递归检查每个操作数。其它节点拒绝。它没有外部词法 cell 或直接 global/
builtin 名字读取，但不是纯函数，可调用参数并产生副作用，仍须捕获 owner/builtins。
原 wcwidth 0.2.13 的 Python2 分支有一个此域 lambda；不靠死分支删除或项目名放行。

## 验收安排（不是结果）

新独立 HIR、MIR、driver 测试验证准入、拒绝阶段、free/cells/env、真实 context。
固定六源哈希的 hosted 显式测试先走真实产品 validator/链接/PE，再允许两产物发布。
PE delegate 报告与 CIL 记录静态 owner thunk/core 根，不增加裁剪根或抑制警告。

第一产物保持原四调用入口。第二 Legacy 合同入口依次执行原样 owner/failure，
再执行可达 lambda 双实例/绑定/副作用/失败传播/重执行和独立循环、初始化重入、
普通 import 失败重试合同。失败重试检查保留的既有依赖，不宣称本次新加载依赖的
完整回滚语义。所有被测实例必须保持显式选择与 kind2，不能静默切回 Legacy。

产物执行前归档 ELF/dist，保持 stdout/stderr/exit，搬迁后及固定镜像 ID 的禁网、
只读容器中复验。保留旧 Legacy AOT、资源门、default/pilot 完整检查与九项目保护。
运行期/codegen/ABI 暂不修改；出现真实 Native 新问题先保存证据另审。

## 当前可核事实

固定 CPython 3.12.13 下完整联合合同 exit 0、stderr 空、stdout 549 字节，前428字节
严格保持原 owner/failure 的313+115字节。离线脚本测试五项通过。
这些是 CP/脚本结果，不代表 RyuPy validator、PE、Native 或完整项目通过。


## 普通类增量：限定显式 AOT（候选）

完整 AST 按模块、类、函数 scope 分别计数：顶层普通类的直接方法属于合法动态
函数，不能误作函数内 nested def；仍拒真正嵌套 def/class、类装饰器/关键字/元类、
多重继承、slots/自定义属性及类创建协议、生成器与原 lambda/typed 边界。
死分支、默认值、装饰器和类 namespace 写入同样检查；不按项目名放行。
普通单基类表达式保留真实求值和完整运行期基链验证，不能把任意值或 native
特殊布局仅凭源名称当作普通用户类。跨 Legacy 普通父类不自动获其它能力。

只有 driver 完成实际资源/AST准入的显式 ModuleFile AOT 才设置内部
`aot_class_guards`；不是新的 Runtime ABI、用户开关或自动 catalog AOT 能力。
MIR 在类体/普通终结后、最终名字发布前调用既有 ClassCellGuard：无 class cell
也必须检查真实父链 `__init_subclass__`、字段值真实类型的 `__set_name__`；有 cell
路径不重复，不制造假 cell。动态装入这两种未支持 hook 时明确 NI，保留已发生的
合法副作用和旧名字绑定；CP 实际执行 hook 的正证不计为产品等价通过。

定义时 builtins/真实模块字典仍由既有 owner 持有。两个模块副本、旧 bound method、
同 state 重新执行必须保持各自来源及可观察重绑定，不改为 singleton 或反射根。
实际方法静态根需核真实 MIR FnId、源码定义身份和同次 PE thunk/body/delegate 边，
不以引用表存在或名称标签代替；保原 trim 严格门，不整体保全程序集。

kind2 是真实 NativeAOT 进程的可执行文件相对来源；普通 dotnet/JIT 执行同 kind2 PE
会按既有合同拒绝，不能把 dotnet 宿主目录当应用目录。因此普通 aot=false JIT
控制与 aot=true Native 是独立产物，不要求/声称同 AOT PE 在 JIT 通过。
Native 原位及完整搬移负责新 guard/hook/资源合同；普通 JIT 不证明这些新能力。
每模块 `files: []` 仍只提供合法虚拟来源锚，不部署 `.py` 或声称文件实际存在。

原 Markdown 77B 双调用和真实 HTML 102B 补例是业务终点，另保固定项目与旧
AOT/ABI/资源保护；不得以方法构造通过代替完整业务。class-body traceback 缺帧
仍为既有独立红界，原 uncaught 反例不豁免、不记修复。默认 catalog AOT、类工厂、
完整元类/descriptor/trace/heap 协议均未因本增量开放。
