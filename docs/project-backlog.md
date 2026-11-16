# 固定项目后续阻断：按影响面与成本排序

此表保留 ADR-0032 / M3.7 阶段的阻断排期，不是当前候选的完整复测结果或新增实施清单。
分母保持20、目标至少16项原生通过。命中数只统计该历史阶段明确首阻断，**不是实现后保证
通过数**；语法/API解除后仍需完整入口复测，不按库名特判、不跳过第三方代码。

已归档的完整历史基线为M3.7最终矩阵：**5 native / 14 unsupported / 1 failed**。
progress已完整矩阵确认原生通过；chardet的Flag完成后，实际失败为bytes正则，
按用户批准的有界收尾不追加到M3.7。

| 历史后续候选 | 当时首阻断直接影响 | 成本/风险判断 | 排序依据与边界 |
|---|---|---|---|
| list/dict/str 的真实内建类型继承 | sortedcontainers、docopt、tabulate、markdown（4） | 高：实例表示、基类构造、魔术槽、类型身份、容器存储/守卫 | 影响面最大；必须先做表示/守卫设计，不能用普通PyClass假装容器 |
| 静态可发现 star import | pyparsing、networkx（2） | 中高：实际 `__all__`、逐项读取/绑定顺序、模块导出与AOT图 | 复用模块系统，保运行期失败；不能只把源码所有名称复制到表 |
| locals/globals/__import__ 的明确范围 | binaryornot、python-dateutil、six（各1） | 高：帧/模块活绑定、动态写入、导入图与缓存边界 | 三个API不能合称一个容易功能；优先分析是否可用编译期结构而非再造解释器 |
| TypeVar/Generic 参数化类 | click（1） | 中高：真实类型/别名/参数元数据、基类解析、特殊类调用 | 已有独立pending与只读设计，不在M3.7生产；不把Generic替换为object |
| Enum 动态成员表达式 | attrs（1） | 中：成员定义顺序、别名与用户副作用；后续attrs仍有其它需求 | 保真类身份与表达式求值，不扩完整元类前先限定可验收范围 |
| 动态 except 类型表达式 | tomli（1） | 中高：异常出现时求值/匹配、错误替换、handler顺序与上下文 | 不在CIL后端解释Python；已有生成器逻辑匹配可作设计参考 |
| 函数内类 + dict/多继承 | toml（1） | 高：闭包/类创建时序、容器派生、MRO | 单独开放class语法不足以证明可运行，避免拆成虚假通过项 |
| exit 内建 | pyfiglet（1） | 首阻断低、完整项目未知：需真实SystemExit值/清理及后续资源路径 | 可作为独立小候选，但未估计解除后字体/文件等链成本，不保证项目立即通过 |

## M3.7 当时两项有界收尾

- progress：普通类体if与真实mapping `%` 已解除明确阻断，定向入口输出一致；
  完整矩阵与最终门禁已确认，计入5/20而非16/20。
- chardet：真实Flag子集已通过，实际新首阻断为 bytes 正则；源码还明确使用 bytes ASCII分类、
  codecs BOM常量等。这些新链记录准确运行错误并留在此后续队列，
  不为“已经写到这里”而无限追加本轮需求。

M3.8桥接结果另计，不增加原生分子。M3.9/3.10按用户要求以有类型CIL、JIT机器码、
分配与守卫证据推进；性能完成也不能勾选仍未达到16/20的M3.11。

## 后续候选状态与下一步（2026-11-04）

pyfiglet、tabulate、tomli、chardet 已分别在未发布候选完成原固定 Linux JIT 入口验证，
见[候选证据索引](compatibility.md#未发布候选固定入口证据索引2026-11-04)。
上表的 exit、str 基类、动态 except 等是历史首阻断，不再代表这些已测候选的首败。
四项使用不同提交，不汇总成当前9/20；没有重新运行完整项目矩阵。

chardet 5.2.0先前候选6fe244e在90秒编译预算停止、未进入JIT；随后校验器局部查询优化
解除该候选编译预算问题，再按真实首错补齐有限BOM、可变正则eager、ASCII分类和sub/subn。
候选59be1c1仅原固定Linux JIT入口通过，CP/编译/JIT各一次exit0且原golden相同；
详见[完整演进记录](compatibility.md#2026-11-04-chardet-候选单入口编译诊断与运行演进)
及[ADR-0096](adr/0096-mutable-regex-subjects.md)。这不是完整编码探测库、main或AOT通过。
mutable finditer后续候选已通过401专项/3应用及正常完整门禁1719C#/228CP/202e2e+30原ignored；mutable replacement阶段F的晚Join、bytearray模板/expand与有限buffer escape已接候选，429专项通过，应用首2/3后旧slice测试窄迁并单独1/1通过，首次完整门禁1747C#/228CP/202e2e+30原ignored通过；view模板和更多buffer格式仍未实现。该后续API验收不增加项目入口运行或成功计数。
callback替换最坏O(N×M)全窗口复制和共有对象/绑定成本保持明确，未作新性能测量。

binaryornot 依赖 chardet 且另有历史 locals 阻断，不同时开启第二条实现链。
清单卫生另待核：python-dateutil 的 manifest 依赖列与历史表中的 six 不一致，
lock 中还有未列入20项选集的 humanize；本次不改依赖、版本锁或分母。

## 2026-11-05 binaryornot后续候选

普通函数直接locals有限实现及完整门禁通过后，候选c1ed015对binaryornot0.4.4原固定入口
唯一复测：CP/golden一致、编译成功、JIT首错为bytes.translate缺属性。locals不再是该候选
编译首阻断；仍未原生通过。下轮应优先核helpers.py:71/83真实translate(None, delete)合同，
保bytes/bytearray身份、buffer/alias及严格绑定，不把完整bytes API一次并入。证据见ADR0101。

translate通用候选已按实际需求推进，覆盖完整256表与合法内建buffer，而不是只补项目None分支。
18C#及三应用已通过，最终正常完整门禁1858C#/228CP/202e2e+30原ignored通过；原入口单次窗口另记。审查发现的重复**keyword公共展开
差异保原CP/JIT证据并独立排期；不能由translate binder补错位检查冒充完整展开支持。

2026-11-05：功能提交f3ba26b的binaryornot原固定Linux JIT入口现已通过，CP/compile/JIT
各一次exit0且原golden一致，详见ADR0102。locals及translate不再是该候选此入口阻塞，
不能继续把旧首错当当前问题。下一条实现从其它未过项目的当前有界原入口证据选择；
或独立评估已保留的通用重复**展开差异，不重跑已过入口来增加成功次数。

## 2026-11-05 attrs/docopt当前窗口与列表拆分

177cd406上两个未过项目各一个原入口窗口，均无重试/改源/golden：attrs仍编译E0100，实际首错
为enum.auto（_make.py:67）；docopt只报TokenStream(list)及Dict(dict)，两者均未进入JIT。
attrs另有动态compile/eval/运行期类生成静态链，先保完整证据，不为一个库扩完整元类系统。

下一方向为真实list布局，但其依赖的exact-list五组常用语义已独立复现，因此先完成
[必要列表基础修复](adr/0103-list-primitives.md)，再接布局/根描述符/默认协议/守卫。
三份list派生CP应用与设计保存为后续证据，没有冒充活动通过测试。docopt的dict仍是独立下一阻断。
