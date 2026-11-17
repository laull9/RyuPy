# ADR-0236：编译模块的通用原生 C API 调用现场

状态：实施中；模块现场与真实 globals 的产品门独立于完整函数 frame。

## 问题与决定

默认 CIL 模块通过 provider 直接进入 C 扩展时，CPython 没有执行帧。
真实 Cocoa 产品在空参数构造时进入原生代码，后者把 `PyEval_GetGlobals()` 的 NULL
直接交给 `PyDict_GetItem`，发生原生崩溃。该问题来自调用现场，修复不得包含第三方包名分支。

具权威 CPython 字典的模块采用一次固定 carrier 执行整个 CIL 模块体。该字典同时作为
CP frame 的 globals/locals，源文件来自 Runtime 已冻结模块描述。业务体仍为 CIL，
carrier 不向业务字典写临时名字。普通外部 Host 没有 CP frame 时可复用真实 `__main__`
环境；真实 Python frame 优先保留。跨实例的 CIL 调用按可信 `CallerSource` 读取实际 D，
需要时建立嵌套现场，返回后恢复原现场。

旧 Legacy 静态槽、旧本地 Instance `PyDict` 与未知旧来源保持原执行路径。新现场能力
仅授予同一 Host 的真实精确 CP dict；权威存储迁移由 ADR-0237 单独验收，不能临时制造
`__file__` 字典替代模块环境。

## 中性能力与资源时序

Runtime 内部构造 `PyCompiledExecutionRequest`，绑定 provider、真实 globals 与文件来源；
公开构造器不可用，同一请求只能被匹配 provider 消费一次。实现类保持 provider 内部，
跨组件只新增必要中性能力。`PyTraceback.CompiledNativeCallSite` 冷读可信来源，区分
权威实例、Legacy、未知旧来源与外部 Host，不通过文件名或模块公开名字重建 D。

原生 CFunction thunk 的 capsule 持单次上下文：未调用、活动、完成、失效。
模块结束或任何构造失败先失效、从活动链摘除，再释放 CLR token 和 CP 实参。
CP 可以继续持有 escaped frame/code/thunk，后续 thunk 调用明确报执行上下文过期；
活动期重复调用同样拒绝。capsule 真实 dealloc 才释放自身 C 记录，失效对象不再持有
Host、执行体或可复用 GCHandle。

活动 carrier 使用 GIL 内双向链，按实际 code/globals 匹配；退出按节点摘链，支持线程
释放 GIL 后交错返回。普通 Python frame 在没有活动 carrier 时直接走快路，不扫描其常量表。

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

## 验证与成本口径

产品验证包含两个独立原生扩展：直接读取 globals/locals/frame/code/source，原生异常与
重入、嵌套 D、逃逸 code/thunk 失效、构造失败回滚、真实 dealloc 和终态计数。
JIT、NativeAOT、OneFile、输入删除及搬迁分别验收；机械层 CP oracle 另列。
新增胶水要求记录初始化成本、必要边界调用成本和执行阶段，正式计时关闭 audit/report；
诊断耗时单列，保留不利结果。当前候选尚无最终吞吐晋升结论。

模块级 locals=D 为本合同。普通 CIL 函数的完整局部变量、精确函数帧链、逐语句行号、
独立捕获 builtins 与异常栈映射继续保留实际差分输入；模块现场成功不能推出这些能力。
薄函数冷 code 的定义首行、名字、文件单独实测，其行表仍是固定胶水模板，完整业务行号
映射继续开发。
