# ADR0133：精确原生 OrderedDict 的双顺序与有限消费者

- 日期：2026-11-06。
- 状态：有限首切及正常完整门禁通过；性能风险开放，不等于远端已发布。
- 基线：`21748729ba978393bd4da46fe7a35ca4ca9c1835`。

## 背景与决定

XML首切后，原四源窄Markdown入口首错为collections缺OrderedDict。原库的HTML
后处理器及extra脚注扩展真实使用构造、下标、truth和有序keys，不能用dict别名过导入。

选择独立PyNativeObject及稳定PyNativeType，保dict/object祖先与Mapping/MutableMapping。
组合sealed PyDict保存底层插入序，再用OD专有双向链及节点索引保存公开顺序。
只开放精确标量键；不实现旧IPyNativeMapping，不扩大PyDictLayout.TryGet，也不修改
普通PyDict字段和存储算法。所有操作沿既有动态ABI，无Rust或RuntimeFn改动。

直接继承现有PyCollectionsMapping会丢双顺序，复用dict更新会改变keys消费时点、
pair解包及异常文案；改写普通dict存储则扩大热点影响。这三种方案均不采用。

## 有限合同

构造/re-init/update、常用映射方法、move_to_end/popitem、正反活视图与游标、浅copy/
fromkeys及循环repr真实实现。update交错next/getitem/提交；pair只消费N+1，不求hint。
失败保已写前缀和用户异常链；参数绑定先于来源或last真值回调。

OD相等先按raw序比较内容，再比较公开键序；普通dict反向比较保OD子类型优先，
兄弟dict派生仍按实际左侧分派。eq/ne独立，foreign非bool表达式结果明确NI。
OD公开消费者dict构造/update及**使用链序；显式dict只读根看raw，dict.copy看公开序。
根写入/分配与union在写前NI，不误报OD不是dict。未接通消费者在输入处明确NI。

空move先执行last真值，再检查当前目标：精确标量及四种精确空builtin容器进入已验
缺键路径；其余未知非标量在hash/eq/repr前NI，**不是CP等价**。缺None仅move叶子
产生KeyError空args，不改变全局异常或getitem/pop。Python派生、实例属性、用户hash
键、union/GenericAlias、完整视图集合操作、pickle等另切，不冒称完整OrderedDict。

## 验证、历史与代价

最终003的39项专项与2577项隔离全C#通过；31份完整CP合同真实PE实测22严格通过、
9继续pending。原13在既有异常cause setter处失败且不改源；独立19的raise-from链
完整通过。独审09、binary、roots、compare、sibling完整通过；独审07仍是NI边界。
12次实施attempt、旧候选和探针08自身CP失败均保留，不把后来的成功覆写首失败。

原docopt16B仍过；默认Markdown保375B find_spec NI，原窄入口越过OD到57B的
缺typing.NoReturn，仍未通过，不增加项目分子。原两段入口与extra未删。

普通dict比较确实新增OD类型判断，hash/消费者也有分派成本；OD另有双索引、链节点、
方法和视图分配。唯一默认ABBA保120正式/48暖样本，getitem/setitem两对同向不利，
equality第二对+36.812%；不以汇总下降掩盖，见[完整性能原报告](../ordered-dict-performance.md)。
PyDict存储不变不等于零成本或无回退。正常make check最终通过2577 C#/900 Rust/
631 CP/401 e2e、234 ignored。canonical新DLL与ABBA所测隔离003分别登记，见
[合同与验收](../ordered-dict-core-slice.md)；具体commit/恢复包由交付记录另列。
