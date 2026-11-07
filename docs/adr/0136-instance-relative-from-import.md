# ADR-0136：显式普通实例模块的相对from-import

状态：实施候选，专项与完整门禁结果另记；不表示wcwidth迁移已完成。

## 决定

一个HIR AST需求扫描产生带位置事实，按结构化InstanceCapabilities验证。原自动P1总是旧空集合；
显式普通ModuleFile、非AOT/固定绑定计划才授予relative_from_import。包/入口原能力不变。
能力随ModuleInfo、实际MIR函数、UnitMeta和ModuleDef传递；validator检查调用能力、可信state与
入口一致性。理由文字和模块名字不决定语义。其它语法拒绝（尤其except）不动。

HIR保留相对level/原名，静态绝对化只供原Legacy路径及driver依赖发现。MIR先调用
InstanceRelativeImport处理完整fromlist，后逐项InstanceRelativeImportAttr和真实绑定存储。
Runtime消费当前目标globals的package/spec及捕获builtins，不保存名字/metadata镜像。
codegen只发机械ABI。共享表对象不复制；from绑定不随依赖后来重绑而更新。

## 支持域

默认import hook身份先检查；未知hook不执行。非法非None package先TypeError；字符串package
仍检查原生未修改ModuleSpec.parent。spec字段顺序为locations再name，未知协议先NI。
package与parent一致、package为str且spec为None/缺、package None/缺且有合法spec均支持。
空包/层级过深为ImportError。mismatch DeprecationWarning与无package/spec fallback
ImportWarning协议当前明确NI，不能跳过警告后冒兼容成功；完整自定义spec/finder不开放。

来源只用已登记源码图；未知来源不具备磁盘负存在性证明，明确NI，不假造ModuleNotFoundError。
普通非包缺export在第二阶段ImportError。包只在需要发现缺失名字时检查当时真实path及名字；
前项初始化可改变后项消费的name/path，不能复用旧快照；缓存命中不检查path，改名后只检查真实目标父链。一般后项初始化错误或未知域拒绝保前项
依赖副作用，但本条尚无alias；第二阶段后项缺export可已有前项alias。这不同于CP已证明
不存在子模块时的忽略发现失败，后者未登记域不计strict通过。已登记子模块抛ModuleNotFoundError且
精确字符串name等于当前子模块全名时，按CP延至第二阶段；name缺失/None或不同字符串则
原样传播，未知name比较协议明确NI。

父包/重入/失败重试复用原加载器。循环IMPORT_FROM在父属性未发布时仅读当前公开name对应的
已有缓存对象，不重新import。非字符串公开name在首次需要时NI；已有export读取不消费它。

## 验收与边界

新增CP oracle固定3.12.13相位、pending完整多文件CP/PE、运行时元数据/fromlist/失败/缓存专项，
MIR缺能力、伪context和非法entry负测。原自动计划集合、Legacy静态导入、AOT/fixed边界及
固定九项项目保护须另跑全门禁。未实施普通Name except、完整wcwidth实例迁移、性能实验或
默认扩域；原预登记__defaults__/__code__反射负例仍保留。


显式位不是自动P1过滤器：完整签名、方法、嵌套普通函数、生成器恢复各有真实CP/PE证据。
类体import仍由既有HIR结构拒绝，不能因模块含class而删去旧显式能力，也不开放新类体语法。

ModuleNotFoundError同名name分支已有运行时机制测试；额外Python完整源在旧通用异常属性
写入处先AttributeError，原流与pending入口保留，不能将机制通过当该异常属性API已支持。


相对导入的name读取与ImportError.name公开读取共用同一读取实现，但显式声明比较能力：native
缺name为None；类项仅精确str/None时允许实例字段优先。仅relative消费者对未知类项/descriptor在字段前NI，
普通公开读取仍保留原Bind能力，例如数值类字段返回数值，不能继承relative的比较限制。
不执行PyProperty或自定义getter；不会把其拒绝吞成默认None。relative读期间保存/恢复
原missing上下文。普通name setter仍未支持，原完整pending不变；合法用户异常子类的
实例name、类name、实例遮蔽及同显示名不同真实祖先已有另一个完整源合同，不替换失败源。
