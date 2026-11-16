# ADR-0074：公共 format 的显式 g/G 浮点域

状态：隔离合同/源码已独立审，首次49项C#及新增1项真实CP/JIT通过；首次正常完整门禁通过，未发布。

## 有界目标

接续ADR0073，仅为公共format开放显式g/G及现符号/对齐/宽度/补零；不改旧f-string、
整数的浮点presentation、f/e/%、无type带precision、z/#/grouping等其余域，合法未实现仍NI。
这是tabulate默认g的必要通用能力之一，不解除getopt异常识别等其它阻断，也未重跑该项目。

## 数值算法与快路

Python默认g精度6，显式0精度按1；.NET默认G或G0是最短往返，不能直接复用。
使用固定.NET10标准InvariantCulture显式g/G精度后端，不作二次round/parse-double。
其舍入后的指数选择和至少两位指数规则与所测CP域一致；源码依据不代替实际差分。

NaN忽略原signbit，Inf与负零保正确符号，大小写由g/G控制。NaN/Inf/±0直接短结果，
不构造后端格式参数或调用ToString；只有非零有限值需要实际数值后端。
统一CP precision>INT_MAX检查仍先于所有这些分支；非法接收类型组合先于合法未实现。
.NET标准数值格式实际精度上限999999999仅约束非零有限值，超出但尚在CP整数精度域时明确NI，
不假ValueError、不暗中截精度，也不让根本未调用后端的特殊数/零值受该限制。

最终UTF16容量在符号、数字或填充拼接前检查，沿阶段2真实CLR容量边界；OOM不捕获重试。
常用精度6复用固定格式字面量，其他精度生成格式字符串；仍有结果分配和动态binder成本，
不声称零分配或实测性能提升。不为通过一个例子扩全格式化框架。

## 依据与验证

固定CP3.12.13 `Python/formatter_unicode.c` 与.NET10 `Number.Formatting.Common.cs`、
标准浮点格式实现已独立源码审计。CP错误先后与.NET真实后端范围分别处理，不能混为任意cap。

新22项C#加原27项首次49/49通过；原SpecTests将已实现g的NI期待迁为仍未实现f，
精度非法和其它原断言保持。C#正负NaN使用两个显式bitpattern，避免double.NaN本身符号不确定。
大精度特殊数/零值只验证固定短结果；大精度非零只验证guard，不尝试巨额分配。

新增唯一`general_float.py`应用，固定297次format调用、成功290行输出：
默认/0/6/17/18/50/100精度、舍入ties、科学阈值、最大值/最小正规/次正规、±NaN/Inf、
负零、符号和填充。先核CP成功，再build/JIT比较原双流，不打印原float repr引入无关差异。
首次1/1通过，旧四driver正文/异常投影/结构断言未变，当时完整门禁待终态。
源SHA256 `07c02c7e9d9eebf17633ae76d951c2cde5f616be8a21ac68ef01f967cc00c708`。
专用driver成功后清理独占临时树；通过日志与实际执行的严格比较证明本轮差分，不冒仍持有可下载
的全部成功原流。无新的独立CP探针、项目矩阵、AOT或性能组，不推公开远端。


## 完整门禁终态

首次正常make check退出0：fmt/严格clippy/workspace、Release零warning/error、899项C#
零失败跳过、固定CP3.12.13基线228通过、RyuPy e2e202通过及30原ignored；五个format应用
在正常门禁中全部通过。没有快照迁移、放宽断言或新增ignored，未修改AOT工作流。
日志SHA256 `858e567ee21422b9e6a9c9aab366452f1292da0799b83e8a4467c812ec7cdec2`。
本轮没有再次执行tabulate原入口，因此不依据format局部通过改写其编译阻断或历史5/20。
