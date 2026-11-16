# 函数内静态 import

M3.7 的函数导入复用既有原生模块注册与编译源码模块缓存，不调用 CPython、不创建模块 stub。

## 已实现语义

- `import m`、`import a.b`、`import a.b as alias`、`from m import a, b as alias` 可出现在普通函数、方法、嵌套函数和生成器的执行位置。
- 别名参与函数的词法局部扫描；在导入前读取同名局部是 UnboundLocalError，不能回退到同名模块变量。
- `global` 指向现有模块槽，`nonlocal` 指向外层共享 cell；不把函数导入别名误导出为模块属性。
- 导入调用完成后才写目标。失败保留原绑定；新局部失败仍未绑定。from 多项导入逐项获取并绑定：后项失败保留已成功前项，不覆盖后项的旧值。
- dotted 无别名导入先初始化完整子模块，再绑定根包；别名导入绑定完整模块。相对导入按所属模块 `__package__` 静态解析。
- 重复导入复用同一缓存对象；失败模块沿原运行时规则移出缓存，成功依赖保留。循环导入仍读取初始化中的同一模块，不为本切另造副本。
- try/with 的传播与清理复用现有异常机制；生成器只在恢复执行至 import 时导入。

## 分层与成本

HIR 的 Import/ImportFrom 目标是 Binding（Local / Free / ModuleVar），MIR 调用 ImportModule/ImportAttr 后使用既有 store_binding。没有新增运行时 ABI。

函数绑定扫描与模块赋值扫描分开，避免把稳定原生异常导入误判为二次赋值。driver 使用递归依赖收集器发现函数、嵌套函数和类方法中的静态 import；模块绑定扫描仍不进入这些独立作用域。图发现不会执行函数或导入语句。

这是 AOT 静态依赖图：即使函数未调用，找到的依赖源码仍要通过编译；可能扩大编译时间、程序集和不支持语法的阻断范围。缺失模块沿已有运行时导入错误处理，不靠伪实现让项目通过。

含 import 的函数沿当前类型策略走 Dynamic；未含导入的静态函数不因模块图扫描被统一降级。新导入局部即使成功写入，也保守保留后续 LoadChecked：当前线性确定赋值集合不能把成功路径传播到 except。先前已确定赋值的目标保留原状态；cell 保持 CellGet 检查。这增加局部读取检查，不声称解决完整异常控制流确定赋值分析或提升性能。

## 明确边界

不开放动态 `__import__`、import hook、运行时搜索路径修改、星号导入或类体 import。函数本地异常别名不提升成全模块身份。ADR0075 的本地候选仅接受 driver 证明原生来源、函数直属且先于 handler 所属直属语句的稳定导入；独立函数表拒绝参数/重绑/删除/条件导入、global/nonlocal/free/cell，任一元组项未证则整条 E0100。局部失败不回退同名 builtin，局部别名不污染 protected_exports。动态 except 仍未开放。函数内 future import 仍非法。

## 验收与项目证据

`m3_7_function_imports` 目录验证懒执行、缓存身份、dotted/相对导入、逐项失败绑定、失败重试与循环导入；`m3_7_function_import_bindings.py` 验证 local/global/nonlocal、with 清理与生成器执行点。HIR 单测验证作用域和依赖扫描分离，driver 保存 HIR/MIR 快照。

固定项目定向复测（2026-11-01，CPython 3.12.13）：toml、attrs、click、python-dateutil 四项仍 unsupported，未新增 native 项目。首阻断分别推进到不支持语句、Enum 成员表达式、特殊基类和 globals；不能把函数导入接受或两条差分当成项目兼容率提升。完整固定分母继续为 20，不用此定向子集改写总通过率。

后续有限候选的局部异常身份、真实 getopt 解析及错误链已通过5项CP/JIT差分；正常完整门禁920C#/228CP/202e2e+30原ignored通过，尚未公开或重跑项目。见[ADR0075](adr/0075-getopt-local-exception-imports.md)。
