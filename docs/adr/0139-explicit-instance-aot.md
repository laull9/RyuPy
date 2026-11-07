# ADR-0139：显式普通实例 AOT 与部署相对来源

状态：候选实现，尚未通过 Rust 格式/编译、完整门禁或真实 Native 验收。

## 决定与边界

仅显式选择真实 ModuleFile 可以申请此 AOT 域，入口、包、原生绑定不能申请。
编译任何模块前，driver 必须验证资源计划覆盖全部非入口源码，所有点分包祖先
均在实际图中且为 Package，并完整检查所选源码 AST。私有成功结果决定显式计划
及 relative_from_import、ordinary_name_handlers 两能力；最终 UnitMeta 再核 kind2。
该内部合同不是手构 IR 的安全隔离；原 HIR/MIR 语义验证继续保留。

空 files 资源声明只创建 ExecutableRelative 元数据锚点，不部署源码，不承诺
__file__ 对应物理文件存在。原资源门仍禁止 .py/.pyc/.pyo。native 内建无伪造来源。

新 AST 域必须有普通动态 def，拒 class、完整 typed 签名、嵌套 def 和生成器。
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
