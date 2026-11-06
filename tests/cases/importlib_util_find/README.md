# importlib.util.find_spec 合同用例

首批15例由固定 CPython 3.12.13 双轮生成并复查 stdout；后加 cache_readonly_supported、nonpackage_parent_supported 两例也已完成父级单次 CP 基线。以下8个完整源已用冻结 runtime 和固定 CLI 编译为 PE，并在独立 dotnet 进程逐字节对照仓库 stdout.txt：8/8通过，退出0、运行 stderr为空，已解除 pending。其余9个完整延期源继续 pending；不把 CP 通过计作 RyuPy 通过，也不以独立有限病例替代原完整边界源。

## 本轮已验证支持域（8个完整源）

- uncached_regular：已登记普通源模块/包、fresh spec/loader 的身份与等价、元数据、父包实际初始化、目标不执行/不缓存/不挂父。
- parent_initializes_target：父包先导入目标后仍执行 finder 的 CP 顺序；与再次调用的缓存原身份区分。
- cache_readonly_supported：读取当前 __spec__ 原引用、赋任意值/假值/None；只读 sys.modules。独立新增病例，不代表完整 cache_contract 转正。
- relative_names：绝对名/相对名/纯点号/越级/缺 package 与无关子包不初始化。
- binding_aliases：函数身份、别名/容器/星号调用、全部常见绑定错误及原始错误消息。
- argument_effects：调用方参数求值和错误绑定先于正文 hook；绝对名忽略 package。
- nonpackage_parent_supported、parent_failure：实际父初始化、异常 name/cause、失败缓存清理与重试。前者是独立新增病例，不代表完整 nonpackage_parent 转正。

## 延期且保留 CP 事实

cache_contract 的原完整源继续 pending，保留模块 __spec__ 删除、sys.modules None/任意对象写入、相对错误等所有断言；nonpackage_parent 的原完整源也继续 pending，保留缓存写入绕过父包检查的断言。不能删除这些后段来换通过，支持计数按实际完整源通过计算。

pending_finder_hooks 保留自定义 finder 顺序、参数、返回身份、异常和缓存短路；pending_path_changes 保留父包路径重绑定、原地修改、恢复与缓存短路；pending_missing_names 保留 CP 的真实 None，不改成 RyuPy 闭世界未登记名称的 NI；pending_name_protocols 保留非精确字符串和 package 用户协议。

pending_clone_complete、pending_failure_complete、pending_parser_complete 从设计程序抽取完整正文，保留双实例函数/类/闭包/生成器/容器/别名/重复执行、直接 exec 失败与普通 import 清理、真实 html.parser 处理差异的全部断言。parser 只是 CP 标准库证据，不增加 CP 源码依赖，也不宣称 RyuPy html.parser 可用。

## 模块图与清单

现有 case.txt 不支持 include-module；runner 使用默认 Session，未转交 include_modules。需要查找未缓存目标的候选用例以查询后的真实 import 自然登记模块图，且验证后续正文执行，不写永假 import。只有动态名称而没有图内真实 import 的专项，需要另行扩展清单/runner 或由 driver/C# 测试显式登记；本目录不偷加未知 manifest 字段。

每例 stdout.txt 是固定 CP 的原样输出。外部验证目录 cp-find-spec-001 保存首批30次执行及复查；cp-find-spec-002 保存后两例的单次 CP 基线。candidate-release-001 保存冻结 DLL/PDB、全 runtime 源身份与 green-003 的关联；candidate-cases-001 保存8份完整源编译/JIT命令、输入和产物哈希、原输出、退出码及逐字节比较。编译 CLI 在 stderr 写入“已生成”成功提示，运行 stderr 则全部为空。
