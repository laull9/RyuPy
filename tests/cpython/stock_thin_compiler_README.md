# 真实 Python 薄函数产品验收

本门调用 Rust CLI 编译手写 Python，验证实际 CIL 业务体和程序正常关闭；
模块保留函数至退出，既不主动 `del f`，也不提前清模块字典。
资格是显式 `--experimental-stock-thin-functions`，固定 stock CPython 3.12.13 / 3.13.3，默认关闭。

先由总控完成 Runtime/provider 与 CLI 构建，再执行：

```bash
python3 -B tests/cpython/stock_thin_compiler.py --python /absolute/path/python3.12 --mode jit
python3 -B tests/cpython/stock_thin_compiler.py --python /absolute/path/python3.13 --mode standalone
python3 -B tests/cpython/stock_thin_compiler.py --python /absolute/path/python3.12 --mode onefile
```

`--case identity` 可用于首轮短验证；`--mode all` 串行运行三形态。
每格先运行指定 CPython 即时 oracle，随后编译、物理删除输入源码/venv/构建中间目录，
搬迁至中文空格路径。执行时 PATH 为空，PYTHONHOME/PYTHONPATH 指向不存在目录。
JIT 使用绝对路径 dotnet；Standalone/OneFile 直接执行发布的原生入口。

验收比较完整 stdout、stderr、退出码，读取真实 Host 报告，要求专用程序权限已使用、
native/core/owned-H/ledger/queue 全零且没有 finalize 后 DECREF。
原生调用及 owner 析构必须发生。OneFile 额外要求从本轮私有临时目录加载且自动清理解包目录。

成功和失败日志保存在仓外 `~/.cache/ryupy/validation/adr0208-20261117/`，
所有临时产品在成功、失败或超时后清理。驱动无需普通文件摘要。
本门与独立 Host Runner、固定二十项目兼容矩阵分别报告；三组证据分别维护。

2026-11-17 macOS arm64：两版各四输入 × JIT/NativeAOT/OneFile，合计 **24/24** 通过。
四输入为 identity、global_rebinding、builtin_and_function、shutdown；最后一项正文不调用 F，
唯一的 native core 调用来自真实 CP atexit。所有运行均 exit=0、stderr 为空，报告计数归零。
Standalone 的公开入口经原生映像校验，OneFile 私有解包目录在退出后自动删除。

日志名以 `stock-thin-compiler-3.12.13-` / `stock-thin-compiler-3.13.3-` 开头，
本轮运行标识为 `20261117T062000`、`062025`、`062119`、`062533`。
同源完整 make check 为 3424 Runtime pass、3 skip、503 e2e pass、297 pending，Rust/fmt/clippy 全部通过。

长入口循环的临时 owned 保留成本已发现，正在收敛入口登记结构；这些产品证据覆盖正常结束与搬迁。
吞吐、峰值内存保护以及更宽函数体资格继续独立验收，候选默认关闭。

## 局部绑定与身份选择扩围

函数签名继续固定为无默认值/注解/装饰器的顶层同步 `def f(x)`。函数体允许
单个名称目标的局部赋值、`return`、`pass`、`if/elif/else`，值来自名称、`None`、
单个 `is` / `is not` 比较或条件表达式；分支条件也必须是单个身份比较。
参数重绑定、局部词法遮蔽、分支未赋值检查和隐式 `None` 复用现有静态 MIR/CIL。
任意子表达式、不可达语句及未选中分支仍经过同一资格检查。

本切片不增加冷 ABI。`StockThinLoadGlobal` 独占 lease 借用能力，其余准入助手仅为
`DynNone`、`IsSame`、`DynTruthy`、`LoadChecked` 和 `MakeUnbound`；静态 body 的
局部/分支直接发射到 PE。上一切片验收时，算术、调用、属性、容器构造、循环和
业务 closure 均明确诊断；下一节登记通用表达式和值调用的扩围。

三个手写输入：`local_identity` 验证别名、参数重绑定、身份选择/早返回和隐式 None；
`local_unbound` 验证赋值前读取与未走分支局部的 `UnboundLocalError`；
`identity_order` 验证左右查名顺序、条件分支惰性及全局删除/重绑定。异常同时检查精确
类型、`name`、`args` 与消息，包括 Unicode 名称；缺名的原生 `NameError` 保存原查找名称。
默认产品门包含三项，普通 e2e 保持 pending，必须显式启用候选运行本门。

```bash
python3 -B tests/cpython/stock_thin_compiler.py --python /absolute/path/python3.12 --mode all \
  --case local_identity --case local_unbound --case identity_order
```

2026-11-17 macOS arm64：这三输入在 stock 3.12.13 / 3.13.3 各运行
JIT、NativeAOT、OneFile，新增 **18/18** 删除输入搬迁通过。完整三流及退出码与同次
oracle一致，共 120 次静态 core 调用、54 个 owner 析构；各报告终态计数全零。
驱动按手写输入核对每格精确调用/析构数，分别为 9/4、3/2、8/3。
全部临时产品已清理，OneFile 解包目录亦已自动清理。

成功日志为 `stock-thin-compiler-3.12.13-all-20261117T074010.104903Z.log` 与
`stock-thin-compiler-3.13.3-all-20261117T074017.515461Z.log`。
首次 JIT 的 `20261117T073741.152920Z` 日志保留缺失 `NameError.name` 的完整失败；
修复后保留原断言，并扩充 Unicode 名称及异常参数的现场对照。

## 通用表达式和值调用扩围

沿用显式 flag 和固定 `def f(x)` ABI，表达式新增 `int/float/bool/str` 字面量、
`+ - * / // % ** & | ^ << >>`、一元 `+ - ~ not`，以及普通可调用值的纯位置参数调用。
`if/elif/else` 和条件表达式接受上述任何值，按 Python 真值协议求值；表达式语句
保留运算和回调副作用。关键字、`*`/`**`、`@`、属性/下标、容器构造、闭包及循环
仍由薄体资格检查明确诊断。模块入口沿用既有合同。

二元/一元表达式保留动态 MIR，交给已有 PIC 与 Python 运算协议，包含大整数、反射
方法及异常；调用先求得实际 callee，再按源序求实参。零至三参调用复用静态 PIC，
更多位置参数走已有中性 `CallValue` 数组入口。零参 helper 只借实际首参槽和缺失类源，
不取得模块 Context 或 lease；lease 仍只借给冻结名称的 `StockThinLoadGlobal`。
所有 body 和 adapter 继续直接发射静态 CIL，不执行 Python 业务函数源码。

两个独立应用输入是 `numeric_pipeline`（动态数值/序列变换）与 `call_pipeline`
（文本处理、可调用值、零参/多参、嵌套调用及 callee 重绑定）。显式 `eval/exec` 段
使用调用者提供的精确字典，执行归属 CPython；已编译 `def` 正文和 adapter 的执行归属 CIL。
无继承 caller flags 的 `compile(..., 0, True)` 也保留普通值调用。第三项
`operator_protocol` 用 `type` 组装类型，魔术方法本身全部是编译薄体，检查
`NotImplemented`、子类反射优先级、返回身份、原异常和 `__bool__/__len__` 正常/失败路径，
同时保留 Runtime 字典和 CP-native 字典两种保存方法。

`dict_item_method` 验证保存的 `__getitem__`、根描述符、原值身份、重绑定后的活存储、
参数错误和键错误。同一源码通过 `runtime_dict_item/case.txt` 进入默认 Runtime e2e；
Runtime 专项另以固定 CPython 即时对照空表哈希抛错、异常身份和强制扩容的同键/其他键重入。
编译实现统一按 Python 协议分派，场景名称仅属于测试输入。

```bash
python3 -B tests/cpython/stock_thin_compiler.py --python /absolute/path/python3.12 --mode all \
  --case numeric_pipeline --case call_pipeline --case operator_protocol --case dict_item_method
```

`frame_boundaries` 是独立负合同，通过 `--negative-case frame_boundaries` 运行并标为
`PASS-NEGATIVE`。固定 CPython 必须先证明 `locals`、零参 `vars`、无显式作用域的
`eval/exec` 使用真实函数 scope；候选必须按实际规范内建对象身份明确拒绝这些未准入形状。
同名用户 callable 必须正常返回原对象，`globals` 与 `super` 别名由正例单独验证。
该负合同验证的是明确能力拒绝，成绩与正例的严格 CPython 三流差分分列。

默认 Runtime 字典方法已通过 5 项专项（错误与 hash 重入轨迹均现场对照固定 CPython）
及 153 项字典/哈希回归；默认无 provider NativeAOT 删除输入搬迁通过。
日志为 `runtime-dict-item-native-20261117T084109.855549Z.log`。

2026-11-17 macOS arm64：四个正例 × stock 3.12.13 / 3.13.3 × 三发布形态，
**24/24 严格 CPython 三流差分**通过，共 528 次静态调用、222 个 owner 析构。
四项每格精确调用/析构计数依次为 32/10、23/15、29/11、4/1。
直接 frame 负合同另有 **6/6**，36 次静态调用、24 个 owner 析构；这六格验证能力拒绝。
所有报告终态归零，所有输入、venv、build 和临时产品已删除；OneFile 解包目录自动清理。

最终日志为 `stock-thin-compiler-3.12.13-all-20261117T090323.465111Z.log` 与
`stock-thin-compiler-3.13.3-all-20261117T090323.486401Z.log`。
早期 `081037`、`081202`、`082811`、`082927` 与 `083052` 日志保留原始 disposed、
owned 关闭拒绝及 Runtime 字典缺属性失败；生命周期和字典协议修复后原场景全部参与最终验收。

`wrapped_frame_pending.py` 是完整业务 frame 的独立回归，包含 `partial(locals)` 与
`partial(eval)`。两版 CPython 基线为 `locals ['business', 'x'] 17 True` 与 `eval 23`。
直接对象守卫的验收范围由上述四项负合同界定；包装 callable 与 C 扩展间接读取 frame 的
业务 locals/求值 scope 随完整 frame 合同推进，普通值调用保持开放。

通过 `--pending-case wrapped_frame_pending --mode jit` 独立复现：2026-11-17 两版实际
候选均输出 `locals ['p', 'x'] None True`，`partial(eval)` 报缺少 `hidden` 的 `NameError`，
随后业务断言退出 1。Host 同时拒绝 owned 关闭，`terminal=false`；这项完整 frame 与
生命周期缺口保持 pending。日志为 `stock-thin-compiler-3.12.13-jit-20261117T091630.057546Z.log`
与 `stock-thin-compiler-3.13.3-jit-20261117T091631.217353Z.log`，包含完整三流、Host 报告和清理记录。
驱动以 `PENDING-OBSERVED` 单列这一实际失败形状，正例和直接负合同成绩分别维护。
