# ModuleSpec 与普通已编译源模块元数据

本切只提供 importlib.machinery.ModuleSpec 真实普通类及已登记源模块的元数据。
它不是 importlib.util，也没有 find_spec、module_from_spec 或 loader.exec_module。
原 Markdown 三步加载与独立实例全局仍未完成。本页说明有限语义，阶段证据、完整门禁与性能范围见[ADR0121](adr/0121-module-spec.md)。

## 对象与消费阶段

导出的 ModuleSpec 是稳定 PyClass，实例使用既有 PyInstance 字段/property 协议，
不是字典空桩。定义模块字符串与固定 CP3.12 的公开类型一致：_frozen_importlib。
构造的两个必需参数可位置或关键字，origin、loader_state、is_package 仅关键字。
绑定先完成，再执行 is_package 真值；name/loader/origin/loader_state 保存原引用。
包新建独立 search_locations 列表，非包为 None。任意合法对象不在构造时被字符串化。

parent 按当前 locations 是否 None 判断，不按真假；普通模块才调用 name.rpartition。
has_location 真值转换成功后才写入。cached 为惰性路径元数据，显式值保引用，
None 不作为永久负缓存。已计算路径不随 origin 自动变化，写 cached=None 才重新计算。
默认限定固定 CP3.12、优化等级0、无 pycache_prefix 的源缓存路径；.pyc 原值保引用，
未知后缀为 None。文件不必存在，也不创建 pyc。自定义 origin 路径协议在实际消费时明确 NI；
没有 endswith 属性的对象仍在该读取阶段产生 AttributeError。跨平台路径尚无本轮实机验收。

cached 的条件检查、origin路径计算与最终_cached返回各按CP重复读取，保留描述符副作用。
repr 按当前类名逐属性读取，对 locations 与动态类名调用空format协议，不用str替代自定义__format__。
eq 忽略 loader_state，
先在 try 外读 self.locations，再在 try 内顺序比较 name、loader、origin、locations、
cached、has_location；try 内 AttributeError 返回 NotImplemented。实例不可hash。
通用比较 ABI 目前返回 bool，无法完整保留自定义字段比较的非bool结果，故直接字段
含自定义 __eq__ 或原生比较对象时在实际消费该字段处明确 NI，不提前扫描后续字段。
真实sealed object()载体没有可变比较协议，按身份比较；此豁免不开放其它原生对象或用户hook。
普通数据/容器及无自定义eq对象沿既有比较协议。描述符/子类的全部长尾未验收。

## 模块初始化与来源

resolver 的显式包标记与定位模式经 MIR 传入新 RegisterSourceLocated；不得由 file 猜包。
旧十参 RegisterResourceLocated 保持缺属性与资源消费顺序，不按 fileBackedByGlobal 猜测新元数据能力。
每次新源入口导入尝试先创建 spec、来源描述loader、cached及包path，再发布部分初始化缓存。
生成模块入口先按既有规则重置全局绑定位，随即将初始元数据放入真实动态全局槽，才执行用户正文。
裸名字、外部读取/写入均使用这些原槽，普通函数和全局 ABI 不变，没有逐次读取旁表。
类型层只豁免模块main的编译器合成ModuleMetadata初值引起的整体降级；四槽仍是Dynamic，
普通数值/字符串全局保原推导与绑定位。用户实际读取元数据、动态局部及其它动态表达式
仍沿原降级规则，不按元数据变量名宽泛豁免。B4曾意外污染普通global，原错与B5修复见ADR0121。
__name__/__package__ 的公开读取也使用源模块真实槽，不再遮蔽用户重绑定。

__file__ 与 spec.origin、__cached__ 与 spec.cached、__loader__ 与 spec.loader 初始共享引用；
包 __path__ 与 spec.locations 共享同一个列表。之后重绑定不双向同步，列表原地修改仍共享。
重复成功import复用模块及spec；失败清缓存，重试创建新spec，成功依赖不被移除。
资源 files() 检查本次spec/loader身份及spec来源字段，原覆盖拒绝及getter异常仍保留。
loader对象自身name/path不作为资源定位输入，本切不宣称校验其任意字段修改。

loader 的真实类型名为 _ryupy_importlib.CompiledModuleLoader，name/path 记录真实已编译来源。
它不伪装 CP SourceFileLoader，也不导出 create_module、exec_module 或 get_source。
源绝对位置以及显式资源部署位置都只描述已编译代码来源；即使源文件变化，也不重新编译它。

## 明确未完成

- 旧失败 PyModule 的getter仍连同一静态全局槽，重试会改变其可见状态。不能用元数据特判冻结旧模块
  冒充完整实例语义；该完整CP边界源保pending，实例全局是后续独立切。
- 全局裸名字删除原HIR E0100，外部模块属性删除原运行时NI；未实现删除后的builtins回退。
- 原生替代模块、无显式来源的旧注册及声明绑定不伪造built-in/frozen规格，缺失spec/loader访问明确NI。
- __main__的spec/loader/cached为None，现有package空串与CP脚本入口的None差异仍单列pending。
- sys.modules写入、动态发现、新实例执行、任意loader和完整html.parser均不属于本切。
- 混合源loader/native/main反射边界完整保留，不把已通过的对象片段计成整份通过。

最终B5正常完整门禁已通过，五主源通过、两完整边界保持pending；验收范围见ADR0121。
干净889ba9a7原docopt固定入口CP/编译/JIT通过；Markdown CP/编译通过，JIT仍缺importlib.util。
两入口新窗口身份、原流与范围见ADR0121，不计Markdown通过，
不改旧20项目矩阵，不外推AOT、跨平台或完整importlib。
