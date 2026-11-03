# ADR-0051：受控原生一参函数的无数组调用候选

状态：本地WIP，专项和完整正常门禁通过，最终候选原生/成本验收待；未main。

## 原因与范围

先前固定36进程组保留模块调用约32MB额外分配及matmul禁tier约23%回退。已核math.One实际返回PyFunction，捕获一参入口强制创建object[1]；满参posonly绑定器本来复用数组，并不存在需要再消除的第二次绑定数组。

本切只为math.One共同13函数提供受信一参入口，不扩pow/log、一般用户函数、2/3参数、Rust/MIR/CIL ABI或通用FFI。不能以math改善抵消matmul负项；旧实测保留，不跨不同CI相减推断收益。

## 表示与语义

PyNativeFunctions的私有sealed holder保readonly operation/signature/Body/Owner；构造全部完成后发布同一PyFunction。优化资格由实际holder和Owner、Body两项引用身份确认，不读可写Name或module属性；复制Body到另一函数也不获资格。无每个PyFunction新增字段、CWT、全局强根、反射或动态代码。

数组入口仍按原x/posonly/必填签名及self.Defaults绑定、null转PyNone；scalar只在captured1标志为true且准确资格成立时使用。参数早已求值，旧callee捕获顺序不变。false分支首行回原PIC，不增加holder检查。13函数以外和任意Body仍fresh-array CallValue，保返回后逃逸和重入；不使用scratch或池。

旧captured1公开helper允许StarArg。新增PyCallOps内部无分配类型判定把此marker留给原CallValue展开，保空/单/多元素及错误，不能仅凭当前编译器通常不产生它而删除合同。除此不改Flatten算法。

操作只调用一次，转换、异常及原上下文行为不新增catch/Prepare；帧专项只证明scalar/array parity，不宣称补齐math原本未提供的caller帧。holder取代原closure但增加自身owner/body引用，初始化成本不预称零；结果装箱等仍存在。

## 实际本地验证与剩余门禁

源码独立审通过后首次C#专项16/16（新8与原捕获8，无skip）；Release runtime重建后真实默认优化driver4通过/1原static-def身份ignored，其中新增1个83行fixture与固定CPython3.12.13原双流逐字相同。
覆盖13函数值/类型、保存与导出别名、求参重绑、一次转换/重入、同名用户函数、原数组逃逸、复制Body、不可信目标、metadata改名、错元数/关键字/StarArg和false列表PIC。原完整ignored正期待未改。

首次完整正常make check终态exit0：587 C#、228 CP基线、202 e2e通过加30 ignored，fmt/clippy/workspace通过；未使用快照批量接受或FORCE，无快照变更。未运行新的性能组或NativeAOT；最终候选若获批准才沿原baseline8b15、固定两源/三臂/36进程/20分钟一次同机配对，所有点/负项保留。不能用结构上不再创建该数组代替实际性能及AOT证据。

既有Linux/host两套workflow仅在原3分钟C#专项filter追加新类的8项测试，原33保留，期望41；现module_call_capture target自然包含新增1差分。原job/publish矩阵和timeout不变，尚未新远端执行，不以接线当通过。
