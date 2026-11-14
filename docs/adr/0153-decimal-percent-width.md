# ADR0153：通用str十进制百分号 flags/literal width

状态：实现候选，待独审与产品验证；2026-11-10。不是项目通过或发布声明。

## 背景与范围

未改Markdown inline placeholder的treeprocessors.py:92使用`%04d`；旧attr_list stdout117B后
格式NI无栈，仅支持强静态定位，不是动态命中证明。修复所有权在PyPercentFormat唯一格式入口，
不得按项目名/格式常量特判，不修改消费者、日志格式器、RuntimeFn、热ABI或编译后端。

扩展d/i/u的ASCII flags `#0-+ `及literal ASCII width，保重复/顺序自由、符号优先级、
左对齐压过补零、符号后补零和超宽不截断。#十进制无效果。宽度64位解析，
超long.MaxValue为ValueError“width too big”；非ASCII数字仍非法转换。
原Decimal不改，真实整数/bool/BigInteger/double/用户__int__/__index__及异常原规则保留。

完整mapping键先取值再解析本字段，位置参数先解析再取值；逐字段立即转换，
不预扫描整串、不缓存值、不重复用户回调。未知specifier仍在参数足量检查后报错误。
precision/star、s/r/a修饰、长度修饰、其它specifier、bytes仍明确未支持。
返回bool的转换警告边界不随本切解除。无修饰已有正确路径保留。

## 宿主表示边界

官方.NET v10.0.14 System.String.MaxLength为0x3FFFFFDF UTF16单位：
https://raw.githubusercontent.com/dotnet/runtime/v10.0.14/src/libraries/System.Private.CoreLib/src/System/String.cs

StringBuilder的int.MaxValue容量不是最终string容量。采用已核真实上限而非新增业务配额。
所有literal、%%、旧转换及新字段实际append前累计检查；数值转换完成之后才拒绝容量。
用additional > MaxLength-current差值判断，不能先计算current+long.MaxValue造成回绕。
纯计长helper可测试边界，无需巨量分配。整数装饰直接重复Append字符，不额外造pad字符串。

超出表示容量抛带Python调用位置的NotImplementedError，明确属于宿主偏差：
不宣称CP同值NI，不保证OOM次序等价。不可表示的前缀会阻止后续转换；不预执行那些回调。
容量内仍可能物理OOM，沿现有宿主政策，不catch后重试，不承诺无中间内存峰值。

## 验证与尚欠证据

一次固定CP3.12.13参考进程662次观察，exit0、stderr空；不是662产品通过。
其中540条代表flags/width/value组合、39条数值协议、错误顺序/旧域及排除域；
默认mapping repr含地址的原行只作事件顺序观察，不作逐字golden；未修改原件重跑。

新增PyPercentDecimalWidthTests七Fact与PyPercentCapacityTests三Fact，覆盖有限合同及宿主边界。
旧PyPercentFormatTests仅将%+d的NI期待改为+12正期待（固定CP原行39），其他三个旧NI项保持；原六Fact与
Mapping五Fact均保留。不是声称旧测试源码逐字不变；原Decimal正文须逐字保护。

尚待独审及同源hosted C#、CP/JIT、独立Native、异常traceback原保护、正常完整门禁、
原完整20逐ID保护与既定Native矩阵。未改attr_list三例目标仍为原202B/exit0；
full extra原77B黄金、Native来源/实例及TreeBuilder后继分别保留，不拿API通过替代项目通过。
CI003整体NOT_GO不变。未执行本地产品或装SDK，不以静态审阅冒充编译通过。
