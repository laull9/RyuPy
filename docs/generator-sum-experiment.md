# 生成器消费融合接线候选

本候选接续已通过的25项MIR结构原型，但不是其自然晋升。`generator-sum-experiment`
Cargo feature默认关闭，CLI→driver→MIR显式传播；首轮真实编译、普通CP差分、独立已知差异
与单轮同机性能验证已通过，但不足以默认启用。用户dev不因此变更。

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

## 首轮真实差分阻碍（已固定基线取证）

原模块级重绑定输入在候选关闭feature的JIT上于首个sum调用报
`NotImplementedError: builtin fallback for unbound module name 'sum' is not supported`。
原输入原样保留为`tests/fixtures/generator_sum/original_module_rebinding.py`，已在run 43699236478用固定
81758bb477729aee4636f2ca6d6b0815b949849a dev、候选关闭/开启feature三臂逐字比较：
三臂均以相同错误失败，CP正常完成，故是已有未支持项，而非普通兼容通过。普通差分改为函数参数先绑定后重绑定，继续覆盖替换实际sum/range，
不以该拆分代表模块级行为已通过。

## 下一轮隔离验证

- `cross_module/main.py`从独立producer取得三个生成器工厂，在consumer中消费；核跨模块
  FnId重映射、部分消费、耗尽、普通list消费、乘法/累计溢出与关闭。固定CP即时oracle，
  feature off/on的JIT、NativeAOT及搬迁分别比较完整退出码与双流。清单必须包含三个源模块，
  off无批量委托，on精确两个不同批量目标，第三个负乘数工厂保守回退；不以同模块结构断言替代真实链接执行。
- feature-on完整workspace检查另跑，不能以28项MIR定向测试代替完整产品套件。
- 历史ABI以固定来源dev现构DLL对照，保全源码身份、DLL散列与实际测试证据，不能冒称
  任意历史发行DLL均兼容；新公开成员只能精确八项。
- 更多轮同runner重建配对，平衡构建/执行先后顺序；保留原26项回退和新进程/输出成本限制。
  第一轮genexpr_sum约1.85倍、generators约1.21倍仅为该次观察，分配各增加152字节，
  不代表消盒、普通for/yield-from改善或暖态内核收益。

跨模块原输入未修改：`negative`的`x * -3`在动态高层IR保留`DynUnary`，目前range证明器
明确不批准该操作，不应把三个工厂等同三个批量入口。回归同时核真实拒绝原因与降级后未安装
批量委托；跨模块完整CP差分继续验证其普通路径。此修订不扩大融合准入或删除失败输入。
