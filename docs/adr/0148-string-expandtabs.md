# ADR-0148：str.expandtabs 的有限通用实现与真实字符串容量

- 状态：有限方案获准实施；源码候选待独审、构建和产品验证，不代表功能验收
- 日期：2026-11-10

## 问题与决策

Markdown 的原 NormalizeWhitespace 路径对无tab文本也调用 expandtabs。实现通用字符串方法，不添加项目识别，不在无tab时跳过tabsize绑定或转换。新 PyTextExpandTabs 统一绑定、一次类型级index、计长和填充；精确CLR字符串快路不创建绑定方法，保存方法、根描述符与真实str派生继承/super沿既有协议进入同一权威。覆盖与实例遮蔽仍然有效。不扩MIR/CIL/PIC/RuntimeFn、全局调用协议或TextWrapper。

绑定先保原公共求值与展开，再根self验证，最后按已冻结CP事实处理总参数数与keyword名称。回调前固定raw text及tabsize原对象，不回读共享参数数组。C-int严格限制[-2147483648,2147483647]，大正负整数均OverflowError；空串/无tab同样转换一次。

冷index仅查实际类型槽，保留普通PyInstance、带PythonClass异常和既有native域。普通index函数/static/class/property遵循各自绑定；custom descriptor的类型级__get__按raw三参调用，不对该raw槽再次绑定。raw classmethod/property不可调用，局部拒绝，不利用全局CallValue的宽松解包。用户getter/index/callable/warning-write异常保原身份；只准备本层自造错误。不扩任意descriptor图。

## 容量取舍

按码点列计数，有效surrogate pair一列、孤立surrogate原样copy，仅CR/LF清列；tabsize<=0删除tab。两遍扫描用long计长，最终string.Create一次结果分配，O(input+output)时间、O(1)辅助空间。

本API选择真实.NET10最大字符串容量0x3FFFFFDF个UTF16单位，超过时明确NotImplementedError：`str.expandtabs: result exceeds managed string capacity`。这是本API新登记的宿主表示偏差，不是CPython OverflowError或CP容量等价保证。不得采用TextWrapper的1M业务阈值，不让用户低额度变成语言语义。C-int越界与输出容量分开；仅用纯容量比较和Measure首遍检验limit-1/limit/limit+1，禁止巨大分配试探。容量内物理内存不足沿既有宿主失败政策，本切不新增MemoryError映射。

## 警告、边界与验证

index返回bool沿现有warnings过滤/记录/error/真实caller来源和stderr.write路径，直接bool不警告。warning error先于输出分配，write回调可同站点重入，原异常不重包。源位置、完整文本不能以C#无帧测试代替；既有格式/来源边界单列，不重写warnings框架。

真实int派生布局、D009两个独立surrogate合并、普通dict **重复keyword和任意mapping展开、任意属性协议仍是旧债。tests/fixtures/text_expandtabs内全部源码pending；boundaries.py保完整CP正期待，不能把NI改成通过。孤立surrogate正向合同使用chr/ord，避免HIR字面量损失。原128与27 CP批次保持冻结且不重跑；移植源码的新CP/JIT/AOT须另批一次有界预算。未改CI、NoReturn合同或项目golden；默认11/8/1不变，不预报Markdown +1。性能及完整同HEAD门尚未执行。
