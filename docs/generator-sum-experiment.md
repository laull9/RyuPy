# 生成器消费融合接线候选

本候选接续已通过的25项MIR结构原型，但不是其自然晋升。`generator-sum-experiment`
Cargo feature默认关闭，CLI→driver→MIR显式传播；尚待真实编译、普通CP差分、独立已知差异
与同机性能验收。用户dev不因此变更。

## 首次接线边界

- 只在同次普通模块构建内消费显式genexpr来源与未降级MIR；不靠函数名或`range`名字推断。
- 同时通过单静态定义/支配/唯一暂停边/原字段读写PC映射的候选才安装额外委托。
- 真正运行时按工厂实参定位原`.0`字段，仅真实sealed `PyRangeIter`通过守卫。
  守卫失败不提前推进任何元素，调用同一个普通resume一次，把值交回原sum。
- 保留普通resume，`next`、`send`、`throw`、`close`不改。只在真正内建sum入口且精确long累计时
  使用实例绑定的额外能力，trace产品域拒绝批量。
- 每次原yield先写驻留字段与PC，再调用精确long累计；拒绝项没有相加，原值直接交回普通sum。
  乘法仍使用既有动态整数路径，大整数升级没有替换成unchecked乘法。
- 当前消除重复外层恢复/trace上下文进出，不承诺消除每项乘法装箱、字段读写或内部派发。
  count_up、用户回调、闭包、过滤与yield-from不是此首轮快路范围。

## 验证门

1. 默认与feature构建均需fmt、Rust及Runtime真实编译测试。原25项MIR负例不得删改，feature
   增加接线形状测试；原4项MIR ignore精确保留。Runtime新公开ABI只接受手写精确增量，不能
   放宽历史provider判定器，历史DLL未执行的专项不能冒称通过。
2. `tests/e2e/phase3/m3_generator_sum_fusion.py`保持pending，固定CPython 3.12.13同次生成oracle，
   对关闭/开启feature的JIT、AOT分别核退出码、完整stdout/stderr；实际恢复与搬迁另验。
3. `tests/perf/generator_known/empty_start_identity.py`单列既有差异：RyuPy空输入保留整数start身份，
   CPython可能重新装箱。不能将其混入普通差分成功统计；候选必须与关闭feature臂一致。
4. 原26项性能套件与三个generator-consumers归因输入各做同runner配对，原始样本、分配、
   程序输出、SDK清单和实际发射入口必须保留。新进程wall、共享runner噪声不是暖态内核结论。

只有以上证据闭合并审查结果后，才讨论默认启用、更多消费者或消盒扩展。

## 首轮真实差分阻碍（待基线定性）

原模块级重绑定输入在候选关闭feature的JIT上于首个sum调用报
`NotImplementedError: builtin fallback for unbound module name 'sum' is not supported`。
原输入原样保留为`tests/fixtures/generator_sum/original_module_rebinding.py`，必须用固定
81758bb dev构建对同输入的CP/JIT行为取证，再与候选关闭/开启feature分别比较，未取证前
不能归类为既有差异或忽略。普通差分改为函数参数先绑定后重绑定，继续覆盖替换实际sum/range，
不以该拆分代表模块级行为已通过。
