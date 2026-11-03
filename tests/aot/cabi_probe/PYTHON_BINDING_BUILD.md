# 固定 C ABI 构建候选：恢复版

2026-11-03 从真实远端 `fded7f01` 重新实现。旧工作区中的四提交目前不可读，本实现不冒旧SHA、
旧二进制或旧验证结果；当前尚未 main 推广。原生模块 A 入口沿基线保留，B 在新环境重新验收。

## 单声明与有限接口

候选命令：`ryupy build app.py --native-binding binding.json`；添加 `--aot` 选择静态链接。
普通 Python `import demo_native` 得到真实生成模块/一等函数，不按用户调用名替换为特殊指令。

```json
{
  "schema_version": 1,
  "module": "demo_native",
  "export": "add_i32",
  "signature": "cdecl_i32_i32_i64",
  "library": "demo_native_c",
  "symbol": "demo_add_i32",
  "target": {
    "rid": "linux-x64",
    "shared": "native/libdemo_native_c.so",
    "archive": "native/libdemo_native_c.a"
  }
}
```

只支持一顶层 ASCII 模块/一导出和 `cdecl (i32,i32)->i64`，拒未知字段；不是 ctypes、
CPython C API、任意 wheel、指针/字符串/回调/所有权框架。只接受当前受支持宿主 RID：
Linux x64 glibc、macOS arm64、Windows x64 MSVC，不承诺交叉编译。
JIT 必须提供 shared，AOT 必须提供 archive；只校验实际消费的模式输入，另一项不冒已验证。

声明≤64KiB、路径文本≤4096字节、选中 native 输入≤64MiB、archive≤4096成员。
相对声明目录的输入拒绝绝对路径、`..`、symlink/reparse；宿主中文、空格与 `&` 保留。
输入以活文件对象有界读取/冻结并核哈希，archive 拒 thin、明显 import object、截断/不前进偏移。
这些只验证有限格式，不认证 ABI、恶意竞争安全或静态依赖闭包；输入须为调用者信任的自有库。
错误 ABI 仍可能崩溃，不能承诺映射为 Python 异常。

## 发布与加载

含绑定的 JIT 构建使用新建独占 `ryupy-binding-…/bundle/`，包含生成DLL、运行时DLL、
runtimeconfig、固定 `_internal/native/v1/b0/library.so|dylib|dll` 和 schema3 产物清单。
失败只清本轮根，不复用旧输出；必须搬完整 JIT 包。模块冲突、保留导出名与资源组合写前拒绝，
本切不允许同时指定 `--resources`。无绑定旧输出布局及 schema1/2 不变。

真实生成程序集在初始化前统一登记 resolver，但不急切加载库；首次 native 调用只从 standalone
应用根的固定绝对路径加载。未知名/错 owner/缺库抛错，不返回零去默认裸名搜索，不从 cwd、
dotnet 安装位置或公开 `__file__` 选择根。JIT/AOT 不以动态代码能力开关猜模式。
成功句柄保活至进程结束，不承诺任意嵌入 host 或可卸载 ALC。

AOT 在 SDK 前冻结实际 archive，精确 `DirectPInvoke library!symbol` 与 `NativeLibrary`
指向该快照，SDK后复核；原 shared/managedDLL/archive 不是 AOT 运行包的一部分。
系统库依赖仍存在，不自动收集第三方传递依赖。生成模块与真实源码在 schema3 分列，
声明SHA与实际 PInvoke/委托引用来自本次记录，不伪造 `.py`/源码SHA；闭包与ILC保根仍未证明。
JIT未执行SDK publish，SDK版本为null；AOT使用同次SDK记录，清单最后成功提交。

## 固定验证计划与恢复状态

每host一次JIT构建、一次AOT publish，9次应用（Linux多一次无SDK容器）+C/CP参考各一次。
绑定阶段总1800秒，publish600/JIT180/应用30等单次上限受剩余预算截断；首失败停，不追加性能。
负例仅修改独立副本；原源码/输出改名保留证据，原路径不可用，不能冒Mac/Windows干净机器。
缺库负例的诱饵只通过子进程平台搜索环境设置；Mac变量实际可见性未证明时不称已验证可搜索诱饵。
Linux镜像预拉取同总预算，避免唯一30秒容器应用承担网络首拉。原始日志/二进制仅CI artifact。

新环境已重跑：thin运行时8项、B编译PE9项、driver绑定14项、固定CP3.12.13工具11项。
CLI失败生命周期2/2与严格clippy也已本次通过；正常完整门禁 exit0：
546 C# /228固定CP3.12.13基线 /202 e2e+30 ignored。
上述结构/失败测试本身不等于实际C或ILC。
本轮另外执行一次本机Linux自有C shared编译、公开CLI JIT构建与固定core应用：
五边界数值、全部严格参数/求值/alias/重绑标记原字节通过，独立schema3核实际源/input/产物集合SHA通过。
这是新环境真实功能预检，不是继承旧结果；未做ILC、搬迁或性能采样，三平台仍待CI。
恢复的[原36成本](../../../docs/validation/aot-module-call-pair-cost/README.md)仍保禁tier matmul约23%回退
与模块调用约32MB增量，未追加性能组，也未把小修称为性能问题已解决。
