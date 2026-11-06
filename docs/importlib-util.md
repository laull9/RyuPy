# importlib.util.find_spec：已编译发现域

本切只增加真实的 `find_spec(name, package=None)`。它查询已经登记的代码描述，不执行目标，
也不创建第二个模块实例。`module_from_spec`、`create_module`、`exec_module`、可写
`sys.modules` 与完整 `html.parser` 仍未实现，不因 util 可导入而计作 Markdown 通过。

## 身份、绑定与顺序

- util 导出稳定的真实函数对象；from import、别名、容器和星号调用走同一入口。
- name 必填，package 可位置或关键字；错误绑定发生在正文前，调用方参数仍先按原顺序求值。
  绝对名字不读取 package 的真值或属性。普通绑定错误消息使用固定 API 名；更改函数
  `__qualname__` 后的错误前缀尚未单独适配。
- 精确字符串相对名按点号层级解析；缺 package 与越过顶层分别抛 ImportError。
  非字符串缺少首次消费的 startswith/rsplit 时保 AttributeError；bytes 的非法 str 参数
  保 TypeError。合法自定义名字、字符串子类及 package 真值/方法协议明确延期，不先运行 hook。
- 首次缓存命中返回模块**当前** `__spec__` 原对象，不校验其类型、name 或真假值。
  None 为 ValueError；属性缺失为另一条 ValueError，并保留 AttributeError context 与
  `from None` 的 cause/suppress 状态。其它 getter 异常不吞。相对名字错误保原输入文本。
- 首次缓存未命中，逐层真实导入未缓存父包；每层先检查实际父路径与finder配置，再执行
  正文。缓存中间父不重查祖先。最终读父包 `__path__` 后进入发现路径。源非包父报
  ModuleNotFoundError，name 为当前查找名；最终查询显式from原AttributeError，
  中间普通导入相位from None，二者均保原AttributeError context及其外层异常。
- 父包正文可能已经导入目标。此时仍执行 finder 并返回新规格，不再次查缓存偷拿父正文
  修改过的 `__spec__`；下一次 find_spec 才按正常缓存路径返回当前值。

## 已登记源代码与未编译域

仅显式 `RegisterSourceLocated` 且位置 kind 为源码绝对路径或真实可执行文件相对位置的
普通源模块/普通包可被发现。模块图仍由实际静态导入和 `--include-module` 建立，不扫描
磁盘、不推断调用中某个字符串、不为特定第三方项目补假导入。

查找创建新的真实 ModuleSpec 和新的 CompiledModuleLoader；普通包每次有独立路径列表。
规格元数据来自登记描述，不读取用户改过的目标全局。查找不执行目标正文，不向
`sys.modules` 发布目标，也不给父包挂子属性。随后普通 import 仍独立执行正文和正常缓存。

未登记名字或父包不能证明“宿主确实没有该模块”，所以明确抛 NotImplementedError，
不能假返回 None。旧登记、无源元数据原生登记及未完成的 native 规格也不冒领能力。
未知父的检查不会吞掉已登记父正文抛出的异常。

当前 sys.path 仍为既有只读部署路径。显式配置 sys.meta_path、path_hooks 或
path_importer_cache，以及替换/扩展父包路径，在真正需要 finder 时明确 NI；缓存命中
绕过这些检查。未建立全局负缓存，恢复路径后可重新查找。完整 finder/path 动态协议保 pending。

## loader 的有限协议与性能范围

真实类型仍为 `_ryupy_importlib.CompiledModuleLoader`，不伪装 CPython SourceFileLoader。
name/path 为可变实例字段；两个不同实例按当前字段相等，hash 按 name/path 计算。
普通两精确字符串槽有无字典分配的相等快路；新增/删除字段仍参与比较，值先比较身份，
再支持无用户协议的标量比较。同一 NaN 原对象不会误判不等。不同复杂容器/用户值在实际
比较处明确 NI，不先执行回调再拿旧字段快照冒充实时结果。ModuleSpec 仅豁免此未被替换、
保证返回 bool 的比较实现；修改 loader 类
`__eq__` 后仍进入自定义比较的明确边界，不把非 bool 结果悄悄变成 bool。

本切没有修改 Rust 编译器、普通函数/global ABI、模块注册委托签名或旧十参入口。
来源对象新增比较协议与 util 冷登记仍有实际初始化成本；“没有改变编译器”不等于零成本。
性能必须看相同真实 PE 配两版 runtime 的完整冷热矩阵，不用新 API 成本抵消旧路径不利。

## 实例执行前置

当前模块内部全局、函数/defaults/来源/类槽仍是静态字段。直接复用这些委托新建模块，
或只复制属性字典，都会使函数、类方法、闭包与生成器继续读旧全局；临时交换静态字段
也不能承受回调、异常或暂停。因而本切不提供假的创建/执行方法。

下一切将显式选择实例能力，使用唯一真实全局字典和函数所属环境，未选择的普通程序保
原静态路径。重复 exec 必须保留未覆盖键、旧函数仍看同一字典、旧默认值对象保原引用；
普通失败 import 重试则创建新环境，旧失败对象不能被覆盖。函数/类名字、builtins 遮蔽、
异常类身份、类元数据和生成器恢复都要同时验收，不能只验证两个模块属性不同。

## 测试边界

`tests/cases/importlib_util_find/` 保存完整 CP3.12.13 事实。普通缓存只读病例与完整可写缓存
病例分开，非包父窄病例与包含缓存写入的完整病例分开；没有删除原完整源后段来换通过。
clone/failure/parser 的原完整正文保留，包括函数 globals、方法归属、闭包/生成器、容器、
别名、重复执行、直接 exec 失败和真实解析差异。最终通过范围和原始失败见 ADR0122。
