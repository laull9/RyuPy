# 公开编译模块实例执行：第一切验收记录

本切沿用已有显式实例编译模式，以真实代码能力和唯一 globals 支持公开
`importlib.util.module_from_spec`、compiled loader 的 `create_module` 与
`exec_module`。不把发现到规格、创建成功或宿主委托单测等同于原项目通过。

## 合同与范围

- 创建得到独立、未执行、未缓存的模块，保输入 spec、loader、包路径对象身份；
  创建阶段不插入 `__builtins__`，不发布 `sys.modules` 或父包属性。
- 执行来源必须是 loader 捕获的不可变实例代码，不能根据可写名字重新查注册表，
  也不能执行目标原来的代码。全部属性读写、定义归属和执行使用目标唯一 globals。
- 重执行不清字典，不重置元数据；失败保留部分写入及原异常，不删除原导入缓存。
- 首切限原生未改 ModuleSpec 与自有 compiled loader；静态源码仍可发现，
  但不得通过复制静态字典取得实例执行能力。普通导入及旧宿主 ABI 保持原行为。
- 自定义 spec/loader 回调、namespace、任意 ModuleType、动态磁盘源码执行、
  可写模块缓存、资源 clone 与其它未验协议仍是边界，不用 NI 输出替换 CP 预期。

本切还支持原生 ModuleSpec 的 `loader=None` 非包空模块创建，以及 loader A 的代码
执行到 B 所创建的真实实例字典（目标名须匹配 A 或为 None）。目标的 spec/loader 不被改写。
`None` 名字采用 loader 名做代码选择但保留目标元数据；缺名字保 AttributeError 的 name/obj，
错误精确字符串名先于 builtins 检查抛 ImportError，name 为错误目标名。

实现只增加运行时冷能力，不修改 Rust、实例 compiler ABI 或 S1 词法类 cell/super。
模块属性读取增加显式字典存储分支，loader/spec 冷创建增加弱能力与协议快照成本；
这些变化尚未做性能归因，不声称普通路径零成本。

## 完整候选与固定参考

最初准备的两个完整目录用例如下，最终扩展验收见末节：

1. `public_clone`：公开别名、关键字调用、双实例、无提前正文或 builtins、函数/类/
   closure/generator 的真实归属、容器隔离、重执行、原缓存不变。
2. `direct_failure`：直接执行失败的部分写入、原缓存不变、逃逸函数继续指向原
   globals、成功重执行不使旧函数换 owner。

两份最初从既有设计候选逐字复制并保pending；真实编译/JIT逐字对齐后已转正。仅去除pending标记及更新说明，执行语句、断言与输出未裁剪。宿主测试未替代此步骤。

固定解释器：`/workspace/shared/toolchains/data/uv/python/cpython-3.12.13-linux-x86_64-gnu/bin/python3.12`。
在各目录以 `PYTHONDONTWRITEBYTECODE=1 "$PY" entry.py` 独立运行，两份均退出 0、
stderr 空；原设计两份 stdout 也逐字一致。CP 自检不是 RyuPy 支持证明。

原 `importlib_util_find/pending_clone_complete`、`pending_failure_complete`、
`pending_parser_complete` 完整保留；这些宽边界不能被新窄候选替换。

## 尚需独立记录的验收

运行时定向测试、真实 PE/JIT、最终完整门禁、原 docopt/Markdown 入口和性能观察的
命令与结果由主验证报告分别登记。本文的 CP 结果不覆盖这些阶段，不关闭 S1、B2 或
更早的性能风险，也不将 Markdown 计为通过。

## 定向运行时结果（隔离候选）

14项新增机制测试与既有模块/发现/规格/loader集合共143项通过，0失败/跳过。
覆盖双字典、原spec/path身份、跨代码目标、注册替换、重复/失败执行、builtins引用捕获、
名字异常字段与context、保存bound method与类方法替换、同名静态目标拒绝、弱能力GC。
原三个测试文件只更新“公开函数/方法不存在”的旧预期，资源及相等语义断言保留。

真实红测发现通用binder在未知关键字之前拒绝超量位置参。本API局部绑定修正该次序，
未改其它函数的通用binder或热调用路径。原失败日志保留，不修改CP oracle。
以上143项仅为早期定向集合，完整最终门禁另列如下。

## 最终验收与保留的失败

最终正常make check通过：2423 C#、900非e2e Rust、5清单自检、533 CP、533框架自检、
358编译端到端，179 ignored。门禁前后全部源内容/权限一致。生产仅runtime改动，CLI仍
为S1的1757a3f1…030f4db，最终Release runtime为4ca61bdf…1baca。

6个完整正例进入常规端到端：public_clone、direct_failure、binding_aliases_focused、
cross_target_focused、name_ordering_focused、package_metadata_graph。它们分别经固定CP与
真实CLI/PE/JIT；没有宿主手写正文替代Python代码。原4个较宽候选依赖实例except计数器，
完整保留pending，不删断言；callbacks、namespace/resources、来源修改、静态/自定义名字
等4例及只能手动include图的package_metadata_focused也保pending，共9个新增边界。

首次门禁在22.97秒因5份case.txt使用未支持的include_module字段失败，原双流/源清单保留。
修正仅将这些字段注释为手动CLI配置；既有Python正文完整保留。另建package_metadata_graph，
全文保留focused例，末尾真实导入并断言父属性、缓存和独立对象，使源码自然进入图；不是
永假导入，也没有扩Rust测试清单或删除断言。新例CP及真实JIT通过后才转正。

最新Release运行时下三个原入口重新编译/JIT：docopt通过（16字节golden一致）；默认Markdown
仍find_spec缺source metadata的原NI（375字节stderr）；未改四源窄配置已经执行html.parser
正文，在parser.py:42因re不支持lookbehind失败（348字节stderr）。源和历史CP身份核验后
复用，未另跑项目CP；不是新的20项目矩阵，也不把窄配置推进算作默认Markdown通过。

本切没有新性能采样或完整runtime CIL等价审计。GetAttr额外标志分支、loader弱能力表、
协议快照和元数据冷创建均是真实新增成本；时间/分配影响仍未测。S1及更早全部不利观察
保留，不能把S1旧runtime的观测迁移成当前runtime性能结论。AOT、跨平台未验。
