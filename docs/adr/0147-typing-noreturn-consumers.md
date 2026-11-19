# ADR0147：NoReturn 的 canonical 特殊形式与冷消费者

- 日期：2026-11-10
- 状态：未提交候选；CP 合同与源码检查完成，C#/CLI/JIT/Native AOT 均待同候选 hosted 验证。

## 起点与范围

固定 Markdown 3.5.2 原完整闭包在 run42746664956/attempt1（remote
7c9c895178181664d06a113ba369185e19650688，生产树与已验基线一致）编译 exit0，
JIT exit1，stdout0B、stderr57B：不能从 typing 导入 NoReturn。不能以编译成功、
导入修复或本 API 用例代替 Markdown plain 48B、原固定完整入口及扩展依赖验收。
旧默认项目矩阵11/8/1不变；metadata.entry_points/EntryPoint.load、re.Scanner
独立边界不在本切追逐。JIT与Native AOT分别验，不由一方推断另一方。

NoReturn 复用 PyTypingForm 的稳定私有 Forms 身份、_SpecialForm 类型与现有缓存。
模块导出可改绑/删除，保存根不随导出改变。加入基础表示、名称、origin/args、
调用/订阅错误、类检查（保 tuple 短路）、基类禁止、OR 与反射 OR。未新增运行时
函数ABI、解释器、owner/frame缓存、编译器入口特判或初始化期未知协议拒绝。

## 跨消费者合同

原 SpecialTypeArgument（含 Union/Optional/ClassVar、十一普通根、Tuple、Callable
result）、TypeVar bound、NamedTuple annotation 都把 NoReturn 当未导出 helper。
现在按原相位依次取 LiteralString、当前 NoReturn、Never/Self/TypeAlias 后再比较。
整个组先求值，即使参数为 Any，也不能绕过删除 NoReturn 的 NameError。Any 的身份
命中可绕过后续比较，但不能绕过前面全局读取。未匹配的保存 NoReturn 是裸特殊形式，
必须 TypeError，不能落入普通 PyTypingForm 接受分支。改绑 int/Union/ClassVar 按当前
membership 生效；未知比较协议仅在真正需要比较时明确 NI。

TypedDict 的独立旧有限检查器只在新字段消费处接 membership：保原参数/字段 key/
alias拒绝次序、字段表达式先完成、来源 metadata 原位；None先转NoneType，旧字符串
转换边界在前；空字段及继承字段不新读全局。其完整 ForwardRef、qualifier 和内部
helper 协议不在本切宣称完成。TypeVar constraints、native PEP585 参数及 Callable
参数仅转换的位置不新增 NoReturn 检查。暖缓存保原始键命中，冷检查失败不污染缓存。

Literal仅扩已有不可变PyTypingForm身份值：根不允许用户改写hash/eq，去重仍按真实
身份；不同form不合并，旧bool/int类型敏感关系保留。任意对象、Enum等旧边界仍NI。
不靠一个新增名字特判通过，沿现有Kind/Equal/Key/Hash关系接入。旧Literal仍按
归一化参数Intern，CP按原始变长参数缓存；Literal[n]与Literal[n,n]的跨原键身份
不作CP一致承诺，本切只测去重内容、相等和hash关系，不扩改该旧缓存边界。

## 明确边界

不支持任意用户 hash/eq、未知替换对象或自定义元类比较。NoReturn 订阅经过现有
SpecialForm 缓存的原始键协议守卫再进入禁止订阅正文，不直接吞掉 hash；未知合法键
与 exact list 的完整回退仍 NI，CP-only 边界记录真实事件，不能计作支持。
私有槽反射、修改根元数据和完整 typing 操作符/类型树协议仍未完成。既有普通根
缓存、原构造消费者守卫不移到模块导出初始化或热命中路径。

## 证据与未运行阶段

CPython 3.12.13 合同与边界清单在实施前冻结，原流另行保存；
指定 CP3.12.13 二进制 SHA256
021044895e95be79dc2f110367607e684119afbc8ce75f6f0eec94844e0acec7。
**单批次4个CP进程**：contract.py、boundary.py各两次，命令均为该二进制
`-B <固定脚本>`，每次timeout20s。四次exit0、stderr空；contract两次stdout1482B，
SHA256 4ca42299e1eb9a42407935a54db7321401ba3b729edeac827fb4b53d0ceffdf1；
boundary两次stdout800B，SHA256
8a7ab09a7d7b446785f6a9ccab0eec84c4dd6f959c4cb7ca532bbeee77128939。
二进制/两源前后SHA相同，原流和结果JSON保留；未下载包、未重复项目矩阵。

新 e2e 保 pending；C# 新测试是待执行产品断言，不冒充通过。两个旧测试仅移除
“NoReturn=None一定NI”的旧未导出假设，新专项补普通bound/NamedTuple/Callable在
None替换下合法的明确断言，其它helper断言未删。迁移前原行可从本候选父提交读取。
环境仅约30MB空闲且无dotnet，不做本地.NET/Rust产品构建；最终同HEAD实际C#、
编译/JIT、Native AOT、旧typing保护、原11项目及Markdown两阶段结果必须另记。
