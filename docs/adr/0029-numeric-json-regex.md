# ADR-0029：数值、JSON 与有限 Python 正则适配

状态：已接受。M3.5 整里程碑聚合门禁与最终只读代码/边界复审通过。

## 目标

交付项目常用 math/time/json/re API，而非把 .NET 同名库直接视作 Python 标准库。
保留原 M3.5 数值/JSON/正则 pending 验收，用新边界差分扩大证据；项目分母仍固定 20。

## 数值与计时

math 入口对定义域、NaN、无穷、溢出、整数与 __float__/__index__ 协议显式适配；
floor/ceil/trunc 的协议与大整数路径分别处理，log/log10 不先把巨大整数溢出成 double。
有限普通超越函数使用宿主数学库并以误差容忍验收，不承诺跨 libm 的逐位同一结果。
time 时间戳与单调计时分别使用 UTC 时钟及 Stopwatch；不保证单调时钟的具体锚点。
sleep 使用单调截止时间与分块宿主休眠，允许系统调度导致超时，不忙等伪造纳秒精度。

## JSON

独立递归下降解析器保留字典插入顺序、大整数、重复键覆盖与 hook 顺序，
不把宿主 JSON 异常的位置/消息误当 Python JSONDecodeError。公开错误位置按 Python 码点换算。
编码器复用稳定排序与 Python 浮点格式；全字符串键使用一次码点排序键，避免 UTF16 ordinal 排错。
默认循环检测保留对象身份。dump 按块调用当前 fp.write，编码后项失败时前缀仍可见，
不先构建整串再吞掉写入副作用；dumps 汇总同一编码器结果。默认处理器与解析 hook 正常传播异常。
自定义编码器/解码器 cls 尚未实现，明确拒绝，不忽略传入类。
布尔选项仅开放 bool/int/None；自定义真值对象的短路/次数依赖CPython编码快路，
首批明确 NotImplementedError，不提前调用用户 __bool__ 伪称副作用一致。

JSON 深度保护为 256，防止宿主栈耗尽；这是公开的容量边界，不宣称等于 CPython 可调递归限制。
默认整数十进制转换上限 4300；parse_int 自定义 hook 可自行处理字符串，不提供 sys 调整上限接口。
输入字节支持 UTF8/16/32 检测及 BOM，严格解码；CPython surrogatepass 接受的孤立代理码点字节编码路径（合法Python字符串域）
尚未兼容，当前明确抛 UnicodeDecodeError，已有 UTF16 字符串代理项表示限制继续适用。
返回完整字符串/排序键/解析容器均会分配；dump 避免整串峰值但未测性能收益，不宣传更快。

## 正则

Python 模式先经明确解析/翻译，不直接透传 .NET Regex 方言。捕获组使用内部名字并维护
Python 编号，输入维护 UTF16 与码点映射，Unicode 分类和大小写等价使用固定 CPython 数据表。
字面量、字符类、常用量词/交替/捕获/命名组/前瞻和常用 flags 按测试开放；
bytes、后顾、条件组、原子/占有量词、LOCALE 和 scoped flags 等未实现项明确拒绝。
空匹配后同位置非空重试另有差分，不用宿主 Matches 的推进规则代替 Python 行为。
替换回调None表示空替换并计入次数；达到count后立即停止，不额外扫描可能昂贵的尾部。
groupindex只读映射暂未实现，明确拒绝，不能返回可改dict冒充mappingproxy。
模式缓存最多64项FIFO；辅助fullmatch/非空重试执行器按需创建；
用户模式最多16384个UTF16单元，译后主体最多200000字符，组嵌套最多256层（超限RecursionError）；
单次宿主Regex.Match超过2秒抛RuntimeError，不承诺完整 CPython 无时间上限语义。
Unicode 表附固定版本生成脚本与来源许可（third_party/python/LICENSE.txt保留原文），不依赖执行时安装 Python。

## 模块异常与分层

json.JSONDecodeError / re.error 只从经过 driver 模块图确认的原生模块导入识别；
本地同名源码、可见别名重绑或作用域遮蔽不能仅凭拼写误认。typed catch 使用内部限定名字，
与同名用户异常类区分；逻辑生成器 handler 映射回同一内建异常身份。
这两个原生模块异常导出暂为只读，动态异常表达式仍不在静态 handler 子集。
实际被handler使用的import/from别名经protected_exports传递到模块setter，跨模块写入明确拒绝；
初始化正常，未用于handler的普通别名仍可改写，不把所有模块导出冻结。
HIR 负责身份诊断，MIR 保留异常类型名字，CIL 仅映射显式运行时 CLR 异常类。
完整动态 except 表达式需要独立 runtime 匹配设计，不能靠裸名字推断兼容。

## 聚合验证

最终 make runtime / make check：241 C#、105 编译 e2e / 17 ignored、118 CPython 基线通过。
固定项目仍原生0/20；详细日志与差分证据见本轮M3.5实现笔记。
