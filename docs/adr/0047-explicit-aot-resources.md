# ADR0047：显式随包数据与诊断分离的模块位置

状态：合同已审，隔离实现中；未通过资源 NativeAOT 验收。
日期：2026-11-02。

## 背景

ADR0046 已建立同次发布清单，但资源复制与运行时定位仍未闭环。已有最小 CPython/JIT
复现：静态条件 import 两个包模块的代码在隐藏源码后仍可运行，而包内
`open(os.path.join(os.path.dirname(__file__), 'data.txt'))` 在包外 cwd 的 JIT 执行失败。
现诊断 basename 被同时用于模块 `__file__`；不能仅复制数据而保留错误定位。
上述仅为 CPython/JIT 证据，不称已经执行该资源程序的 NativeAOT。

## 决定：显式有限数据集合，而非自动扫描

仅 `build --aot` 新增 `--resources <JSON>`。清单 schema1 含 modules 数组，各项为
实际编译模块名和精确 files 相对路径列表。选择必须匹配本次解析的源码模块身份，
每模块一次；不搜索未知模块、不展开 glob、不递归收集用户目录。资源相对实际源码
文件所在目录，而非配置文件、cwd 或点分名称猜出的目录。空 files 仅选择模块定位。
入口可用 `__main__`；父包不因子模块选中自动改变。JIT 不接受这个部署开关。

诊断显示名、manifest 的逻辑来源和运行时模块位置分别承载：

- JIT 实际 build 使用解析时的绝对源码位置，运行 cwd 不影响包旁数据读取；源码数据
  之后改变仍正常可见，不是快照。
- 明确选中的真 AOT 模块使用可执行文件目录下 `_ryupy_resources/v1/<logical_source>`。
  这是虚拟模块定位锚点，不部署 Python 源码；`open(__file__)` 不因此获得源码。
- 未选中的 AOT 模块保持旧诊断 basename 定位，不回退编译机源目录。

同一个冷解析结果供 PyModule 初始 File 与模块内部可写 `__file__` 槽初始化；不得
只修外部属性而让模块正文继续读取旧 basename。诊断 traceback、Function.source_file
及警告定位保留原用途，不把路径修复扩大成所有诊断语义重写。初始位置缓存与公开
可写槽分离：新显式注册协议的公开读取遵循实际全局槽，后续 None/非字符串重绑不
被强制转字符串或回退初始值；旧原生注册路径不随之扩大。ExecutableRelative
只用于真实 NativeAOT，不用 JIT 的 dotnet 宿主目录冒应用位置。

## 身份、路径与预算

module_id 是关联键；`pkg` 与 `pkg.__init__` 等不同身份可能共享同一逻辑来源。相同
实际文件、相同目标可去重复并记录全部 consumer；不同来源即使内容相同，也不能
默默合并到同一目标。

portable 相对路径检查覆盖用户项、所有虚拟锚点及完整最终目标。拒绝绝对路径、
反斜杠、盘符/UNC/ADS、控制字符、空段、点段、尾空格/点、Windows 保留设备名；
采用固定 Unicode 数据的 canonical casefold/NFC 键检查冲突，不以 lowercase 冒充
完整折叠；拟复用 normalization 0.1.26（Unicode 18）及 caseless 0.2.2（full fold
Unicode 16），明确这是版本化应用策略，并非最新所有映射或各文件系统的精确等价。
原拼写用于复制，输入及规范化输出均受预算限制。目标表包含
隐式祖先目录，拒绝文件与目录/祖先冲突，也预留模块锚点及发布元数据名称。
不选择本次源码文件，首切另拒 .py/.pyc/.pyo 数据；不将数据解释为 Python 模块。

源根、路径段及资源拒绝 symlink/reparse 与非普通文件。检查实际打开对象身份、
读取长度与前后变化，采用流式 SHA256；不承诺抵抗恶意并发替换整个父目录的沙箱。
首切上限：配置 1MiB、选定模块 256、去重资源 1024、单文件 16MiB、总字节 64MiB。
超限明确失败，不静默截断；普通无资源 AOT 不增加资源扫描或复制。

## 唯一快照阶段及成功边界

ILC 前将已选择资源安全读入本次独占 staging，完成实际字节计数/摘要/身份校验。
这是唯一冻结字节阶段；多文件不是文件系统全局原子快照。ILC 后只从 staging 复制
到 publish 并重核摘要，不因源变化重读或重新取样。staging 变化则失败。额外临时
资源磁盘最多 64MiB，复制到 publish 时两份合计最多 128MiB，不含 SDK/native 产物。

只有带 --resources 的成功发布输出 manifest schema2；无开关保留 schema1 契约。
schema2 新增 selected module_locations 和 resources（目标、字节数、SHA、consumer
module_id），artifacts 中 declared_resource 必须逐项对应。resources_included 表示
实际非空数据，declared_resources_complete 表示显式选择已全部处理（可为空）。
动态依赖完整性、ILC 保根证明和 selfhash 仍为 false；不把声明完整偷换全依赖完整。

native、所有数据、最终 exact-set/hash 验证及 manifest 同目录原子提交全成功后，
才返回唯一 CLI 成功路径。失败只清理本轮 owned root，不返回旧产物。构建中的
publish 目录不承诺外部可用，不新增目录级部署事务、签名或自动安装器。

## 固定验收与边界

固定一个 generic 两模块程序及原始 UTF-8 数据，CPython 3.12.13 校准 en/fr；真实
JIT 在包外 cwd 与原/改数据逐字对照，并锁模块内外 __file__ 和重复 import 身份。
每个现支持 RID 仅增加一次真实资源 publish：Linux x64 glibc、macOS arm64、Windows
x64 MSVC。完整 bundle 搬迁、源与 IL 不可用后，在另一个 cwd 运行两个分支；修改
原源数据不能改变 AOT 快照。删除搬迁资源后验证器须拒绝，直接执行应真实缺文件，
不回 cwd/原源搜索。保原七配置与四模式验证，不新增性能样本或追逐门槛。

单元/结构门禁覆盖预算、源身份、包与普通 __init__ 同路径、大小写/NFC/祖先冲突、
链接/reparse、冻结后源变化、staging 变化、旧产物不变、两处定位及 schema 失败。
Linux 可延伸无 dotnet/Python 容器证明；Mac/Windows 仍须明确 SDK 已安装环境，
不能把 PATH 移除称为真实干净目标机。

本切不实现 importlib.resources、任意运行时导入、zip/wheel 加载、源码检查、C 扩展
或 C ABI 绑定；不改变已经暂缓的 16/20 项目兼容门槛。后续原生依赖与构建期绑定是
独立交付，不能用增加 manifest 字段代替实际可用能力。
