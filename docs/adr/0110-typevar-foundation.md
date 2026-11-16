# ADR0110：真实 TypeVar 与有限别名参数元数据

- 日期：2026-11-05
- 状态：有限基础及正常完整门禁通过；性能风险保留，Generic 尚未实现
- 前置：2242d963 的原 Markdown3.5.2 入口仍被 Generic[_T] 基类表达式阻塞

## 拆分与真实对象

先补 TypeVar 值和现有 typing 组合，再处理真正 Generic 类、别名及动态基类。
不把 Generic[T] 擦成 object，不放行 HIR 后假装具备参数化继承。新鲜 Markdown
入口 CP 输出77字节等于原 golden，编译仅 E0100，未启动 JIT；这一步不增加项目通过数。

TypeVar 导出为 canonical PyNativeType，而非返回字符串的伪函数。每次成功构造都
产生新身份，同名变量不相等，hash 与字典键身份不随可写元数据变化。TypeOf 在原生
冷分支返回权威类型；实例类型错误标签为 typing.TypeVar，类型短名称仍是 TypeVar。
名称保留 str 子类原对象；约束 tuple 稳定，元素原引用不转换，可变元素仍能观察后续变化。
核心名称、bound、constraints 和 variance 只读；module 可赋任意值及删除，删除后
回落 typing。extra 可写删，包括显式特殊方法名，但没有默认可读 __dict__。
__class__ 写删按 CP 拒绝，不让 extra 遮蔽真实类型身份。

## 构造与来源次序

按固定 CP3.12.13 的实际组合错误核对 missing name、未知关键字、重复 name 和名称
类型检查。flags 的真值调用固定 covariant、contravariant、infer_variance 顺序，三项
均完成再验证冲突；保持用户异常及副作用。bound 转换先于单 constraint 和 bound/constraints
冲突。constraints 不走 _type_check，不把名字类似的 Python 值擅自规范化为类型。

有限 bound 包括 None、既有类型/别名/TypeVar/ForwardRef、精确标量及 list。精确 tuple
产生真实类型错误；字符串只复用已有有限 ASCII ForwardRef 语法。未知自定义比较、被
替换的 typing helper/相关名字、超域字符串明确 NotImplementedError，不能静默省略
CP 会执行的比较回调，也不把合法超域表达式伪报 SyntaxError。完整动态 _type_check
不是此切的完成项。

独立 fresh exec 探针最初只证明新帧能保存任意 __name__，不能推断每次调用读 globals。
后续顶层、函数、嵌套、实际 imported helper 实测证明：读取当前执行帧函数的可写
__module__；顶层执行源在模块开始时固定。旧函数不随 globals 名变化，新函数定义时
捕获新值，修改函数.__module__ 会即时影响其中构造的变量，删除后为 None。
因此复用既有 GetCallerModule，不新增函数布局字段或按文件名猜来源。未知 native
来源明确 NI，C# 专测安装真实模块执行帧，不通过假 __main__ 掩盖缺来源。

## 特殊槽与别名

TypeVar 的隐式操作只查询固定类型槽，绝不读取 extra。统一接入 TryMethod 和 unpack
hint、list extend、NamedTuple 序列、int trunc 与 ABC 结构检查旁路。其它原生值维持原
行为；callable/CallValue 原本依真实 callable 接口判定，不额外读取实例 __call__。
不同对象的 eq 返回 NotImplemented，以保留右操作数真实反射比较。

现有 List/Dict/Tuple/Optional/Union/Callable/Literal 及有限 PEP585 组合接受真 TypeVar。
__parameters__ 按嵌套参数首次出现的顺序、对象身份去重；同名不同变量都保留，Literal[T]
也含 T。参数首次读取才收集并缓存，旧 concrete alias 构造不新增容器分配或扫描。
有参数时使用引用 HashSet 加有序列表，无用户 hash/eq 回调；空结果复用私有空 tuple。
NamedTuple/TypedDict 简单 TypeVar 注解只补真实对象的既有 allowlist，不修改派生机制。

## 明确边界与失败留存

不实现 Generic/Protocol、用户泛型类/别名、变量替换/alias 再订阅、TypeVarTuple、ParamSpec、
PEP695。旧 alias cache/equality 的 origin 域问题不扩展到用户泛型，也不声称已修。
完整 TypeVar 反射、联合运算、序列化、__typing_subst__、__format__/__sizeof__/__init__ 和
特殊 wrapper 非标准参数绑定保持未实现边界。隐式 hash/eq 当前仍会分配绑定 Method，
不宣称零分配。公共调用展开中的既有长尾不由本构造器专项一并宣布完成。

主合同先 CP 冻结，再记录旧基线失败。测试发现的无关 globals/global del/operator 缺口
保留每版原源及失败流：赋值换为 CP 等价的直接模块槽赋值；合法全局删除和 operator
版本各另存完整 pending。复杂字符串原错误次序保留独立 pending，主合同改用已支持的
非法 tuple bound 检查同一先后顺序。用户 bound、动态 helper、exec namespace、再次订阅
同样保留 pending；旧 m3_7_typing_generic 没有删断言或转正。

首候选四主中三个通过，第四唯一差异是旧通用 isinstance 非法 arg2 的错误文字。
按 CP 统一修改公共错误文字，不特判 TypeVar；新增 int/list/TypeVar 非法 arg2 与 tuple
短路回归。第二候选四主均严格 CP/compile/JIT 通过，原失败证据不覆盖。
独审补充发现新 TypeVar 走旧 native 解包分支时文案不符；第三候选将这个已知不可迭代
值接入既有初始解包错误路径，星号/非星号均核对，不宽 catch 用户 TypeError。独立
Python 三流及 C# 公开旁路通过后，原探针接入第五永久 case；五主 CP/JIT 和16项
C# 专项均通过，两个解包、NamedTuple、list.extend 全部执行且 poison 调用为零。

## 验证与性能安排

正常 make check 在提交前执行；记录实际 C#、CP、e2e 和逐名 ignored，不用专项代替全门禁。
独立公开探针补 ABC、反射比较、字典键经 extra 写删、list.extend、NamedTuple 和 unpack
旁路。性能同一生成 CIL/host 比较旧路径，新 TypeVar 能力只报绝对成本；默认 ABBA
有界保留全部原样本及不利结果。ADR0108/0109 的旧性能风险不因此关闭。

本地提交后打包六件恢复集，严格低于32 MiB；保全部历史原源、失败、性能样本和恢复
清单，以不可变对象和有界单层差分去重，不删证据换空间。独立实际恢复核对 Git、全部
逻辑文件及历次源快照后才提供上传路径，不覆盖上一已验证备份。

## 有界性能实测

同一 CLI 生成 CIL 和统一旧接口 host，CP oracle18/18、旧基线 smoke10/10、候选
smoke18/18 及每个计时样本语义检查均通过。默认 tiering 的唯一 ABBA 四进程、280
原样本共12.66秒，全部保留。公共10条路径分配中位数未变，但 view_equal 配对
2.127×/1.151×、ordinary_equal 2.131×/1.111×、ordinary_field 2.290×/1.110×，
不能宣称无时间回退。短窗口/tiering 变异明显，未可靠归因，也没有删不利样本重测。

新能力只报告完整 Python 函数成本：同名两次独立构造约497ns/432B，六核心属性读取
约333ns/168B，hash约310ns，两个候选进程分配208B与144B均原样列出；已缓存嵌套
参数读取约63ns/0B。不能把函数成本当单指令成本或把这一项0B外推全TypeVar。
固定特殊槽仍有 Method 包装和调用成本，留待后续独立优化。原分区/普通属性风险继续
保留；本轮按有界要求完成这一组，不以功能通过消除性能欠账。

## 正常完整门禁

2026-11-05 09:56:15Z至09:58:14Z，首轮正常 make check exit0：2072项C#、
258项CP基线、216项e2e通过；46 ignored逐名核对为原39不变，加本切7个明确边界。
完整门禁前后运行时 SHA256 为
9d8ce8bc5968b04b76c11b666b2622d1de3a73a857d761d755d5e90a34513121。
五主转正前专项与转正后完整门禁均通过；没有把旧pending转正来改变统计口径。

## 干净功能提交原项目复测

功能提交 aa0c54ee499c3801eeeeb2e211f88991cc438790（tree
60e2fea6d30fff4771f5c0eb98137331691db1f2）上，原 docopt0.6.2 的唯一
CP/compile/JIT 均 exit0，CP/JIT stdout 严格等于原 golden；JIT stderr 空。
CP 四条上游非法转义 SyntaxWarning 保留，不声称全部阶段 stderr 空。

原 Markdown3.5.2 的 CP exit0 且77字节stdout等于原golden；compile exit1，
首个实际诊断仍为 util.py:272:16 的 Registry(Generic[_T]) 任意基类表达式 E0100。
没有 JIT、没有产物或重试，也没有超时。TypeVar 基础不冒称解决 Generic 继承。

两项使用原20根 CP 顺序，编译仅各自原包根。3187份原始源和3187份复用冻结源
均逐个重新读取校验，前后零新增/缺失/变化，原归档、入口、golden、工具以及
HEAD/tree/clean 均一致。不是完整20项矩阵或AOT/跨平台结论，不将历史窗口相加。
