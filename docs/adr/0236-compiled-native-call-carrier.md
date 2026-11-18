# ADR-0236：编译模块的通用原生 C API 调用现场

状态：已验模块现场与定义点 builtins 子合同；完整函数 frame 继续开发。

## 问题与决定

默认 CIL 模块通过 provider 直接进入 C 扩展时，CPython 没有执行帧。
真实 Cocoa 产品在空参数构造时进入原生代码，后者把 `PyEval_GetGlobals()` 的 NULL
直接交给 `PyDict_GetItem`，发生原生崩溃。该问题来自调用现场，修复不得包含第三方包名分支。

具权威 CPython 字典的模块采用一次固定 carrier 执行整个 CIL 模块体。该字典同时作为
CP frame 的 globals/locals，源文件来自 Runtime 已冻结模块描述。业务体仍为 CIL，
carrier 不向业务字典写临时名字。普通外部 Host 没有 CP frame 时可复用真实 `__main__`
环境。真实 Python 协议内部保留其现场；Python A 回调 CIL B 时，只有回调入口之后新增的
可信 CIL 帧才能切换到 B 的现场。跨实例调用按可信 `CallerSource` 读取实际 D 与定义点
builtins，需要时建立嵌套现场，返回后恢复原现场。异常展开导出阶段撤销新增帧的切换权限。

旧 Legacy 静态槽、旧本地 Instance `PyDict` 与未知旧来源保持原执行路径。新现场能力
仅授予同一 Host 的真实精确 CP dict；权威存储迁移由 ADR-0237 单独验收，不能临时制造
`__file__` 字典替代模块环境。

## 中性能力与资源时序

Runtime 内部构造 `PyCompiledExecutionRequest`，绑定 provider、真实 globals、定义点 builtins 与文件来源；
公开构造器不可用，同一请求只能被匹配 provider 消费一次。实现类保持 provider 内部，
跨组件只新增必要中性能力。`PyTraceback.CompiledNativeCallSite` 冷读可信来源，区分
权威实例、Legacy、未知旧来源与外部 Host，不通过文件名或模块公开名字重建 D。

原生 CFunction thunk 的 capsule 持单次上下文：未调用、活动、完成、失效。
模块结束或任何构造失败先失效、从活动链摘除，再释放 CLR token 和 CP 实参。
CP 可以继续持有 escaped frame/code/thunk，后续 thunk 调用明确报执行上下文过期；
活动期重复调用同样拒绝。capsule 真实 dealloc 才释放自身 C 记录，失效对象不再持有
Host、执行体或可复用 GCHandle。

活动 carrier 使用 GIL 内双向链，按实际 code/globals 匹配；退出按节点摘链，支持线程
释放 GIL 后交错返回。普通 Python frame 在没有活动 carrier 时不查 code；所有路径都不扫描用户常量表。

固定 code 用 `PyFunction_New` 建立尚未执行的新函数；目标完整 CPython 头上的
`func_builtins` owned 字段在首次调用前接入定义点视图。code flags 保持模块形式，locals
仍为 D；不临时重写 `D["__builtins__"]`。同 D、不同 captured builtins 的函数需要不同现场。
可信 Runtime dict 保持既有字典投影或代理协议，同 Host 外来 dict 保持身份，跨 Host 拒绝。

## 必要胶水及审计

| 来源 | 版本 | 阶段 | 静态正文 | 业务归属 |
| --- | --- | --- | --- | --- |
| `native/call_carrier.h` | carrier-v1；目标原版 CPython | 模块执行、原生边界、必要退出回调 | `(0).__call__()`；公开 `code.replace` 绑定原生 thunk 与来源 | CIL 模块；此帧为必要 Python 胶水 |
| `native/stock_thin_function.h` | stock-thin-A；目标版本 shim | 运行与退出 | 原固定 `return p(x)`；冷设置真实定义名、文件、首行 | CIL 薄体；此帧为必要 Python 胶水 |

按照 ADR-0223，胶水的源码、来源、阶段和执行次数单列。真实 `co_filename/co_name` 保留，
来源归属按实际 code 身份登记，审计聚合键加入 `origin`。`stock_thin_glue`、
`call_carrier_glue` 与普通 `python` 分开；code 复制或 replace 不继承原 code 的身份。

每个登记 code 的末尾常量仅增加一个 origin capsule，capsule 持 weakref 与纯 C id，
没有 owner/Host/core 回边。登记链只借用记录；code 死亡的真实 weakref callback 摘链，
capsule dealloc 清理记录。callback 的自参数是单调 id，且必须核对同一 weakref 确已死亡，
避免手动调用撤销活 code 登记及逃逸 callback 访问失效 C 指针。

薄函数 owner 对初始 code 持强见证，纳入 GC traverse/clear；其 code 不含 P/owner 常量，
新增 origin 见证仅弱指 code，保持强环可见，替换后的地址复用不能通过原 code 验证。

## 普通 Host 关闭

实际使用过 carrier 的普通 Host 在拒绝新外部租约、活动数归零后，于无 gate/GIL 时
执行一次 CLR GC 与 finalizer 等待，再沿原合法租约 Drain CP decref。该步骤只清理已经
不可达的 CLR owner；活别名仍持有 wrapper 和原生引用。未使用 carrier 的旧路径没有
新增完整 GC。原始未捕获错误的退出码与 stderr 保留；用户析构允许重入，unraisable
原错误与 atexit 顺序经独立测试。关闭 GC 的墙钟成本单列，尚无最终整程序性能结论。

真实 CLI 的 CLR typed catch 可在 Main 返回前继续持有已处理异常。provider 入口把原
启动、用户调用与 typed catches 放入私有 `__run_provider_body`，以 NoInlining 返回
退出码；外层 Main 随后调用 Finish。这样关闭 GC 发生于异常处理栈真实结束之后，
无需清业务字典或强制退休活 wrapper。非 provider 入口保持原 IL/EH，未新增 Runtime ABI。

## 验证与成本口径

产品验证包含两个独立原生扩展：直接读取 globals/locals/frame/code/source，原生异常与
重入、嵌套 D、逃逸 code/thunk 失效、构造失败回滚、真实 dealloc 和终态计数。
JIT、NativeAOT、OneFile、输入删除及搬迁分别验收；机械层 CP oracle 另列。
新增胶水要求记录初始化成本、必要边界调用成本和执行阶段，正式计时关闭 audit/report；
诊断耗时单列，保留不利结果。当前候选尚无最终吞吐晋升结论。

模块级 locals=D 为本合同。普通 CIL 函数的完整局部变量、精确函数帧链、逐语句行号、
与异常栈映射继续保留实际差分输入；定义点 builtins 的已验合同独立于完整函数局部变量。
薄函数冷 code 的定义首行、名字、文件单独实测，其行表仍是固定胶水模板，完整业务行号
映射继续开发。

2026-11-17 定向门：双版 JIT/NativeAOT 共 104 项正合同与 4 项既有不可见跨堆环负合同
通过；额外独立来源优先与相同 file/name 来源审计通过。新入口边界下，普通 RuntimeError、
字符串 SystemExit、原生 KeyError 的 12 个删输入搬迁产品保留原退出码、stdout 与错误
末行，载体、托管 token、code origin 全部归零。完整函数 frame 的 4 个搬迁观察仍保留
原断言失败：CP 为 `(True, 17, "read_frame", 8)`，候选为
`(True, None, "<module>", 1)`；四项关闭计数均为零。
