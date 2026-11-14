# 字符串累加：先拆分归因，再做局部优化

最新基线 dev `9bbc0c1f`；用户 onefile/Python WIP `937d5703` 的改动在provider/driver/OneFile，
本切只新增测量输入与驱动，不修改其实现或默认模块归属。

## 已知证据与假说

原 `str_concat` 混合10万次模块级 `+= "ab"` 和10万次list.append＋join。
直接wall run43597810478的AOT约2441.6ms、CP约286ms；旧单独诊断观察到末次累计托管分配约20GB。
源码中精确str加法走String.Concat，逐次复制全部当前内容；仅UTF-16 payload理论累计
4×(1+…+100000)=20,000,200,000字节，与观测量级相符。join路径则物化序列/string数组后
String.Join，是线性路径。以上是待拆分验证的归因假说，不能把混合实测全部归给+=。

## 固定拆分

四个手写输入保持10万次、piece="ab"，只分开操作和作用域：

- append_module：模块级+=。
- append_local：普通动态函数局部+=，不修改现有优化准入。
- join_module：模块级list构造＋join。
- join_local：函数局部list构造＋join。

复用 direct-wall-v1、同次CP oracle和同AOT PE的JIT，固定Python3.12.13/SDK10.0.403。
各臂一次预运行＋六轮，84个无统计进程（72计时）后12个独立诊断进程，总96个。
三臂位置仍均衡；四负载在六轮循环轮转，负载位置不完全均衡，不宣称消除顺序/机器噪声。
输出为长度与两端片段，完整语义另由差分门覆盖；原混合输入不删改、不用拆分结果冒充它通过。
所有构建前记录各输入MIR，以核局部holder是否真实命中；IR只进仓库外CI工件，不入Git。

## 待审优化边界

现有NumericAccumulator仅针对函数局部、非cell/闭包/生成器/异常区域、单入口/出口循环，
且累加槽循环内只读写一次、不被rhs读取。原模块级基准不满足该准入，不应为了速度放宽。
候选是在此既有holder中给精确字符串增加延迟缓冲：空串身份、不可变旧别名、物化缓存、
非str/子类/反射回退、异常旧值都需独立差分验证。实施前先独审guard并获得本拆分实测；
任何局部收益都不能声称解决原模块级20GB问题。优化A/B必须同runner同源工具链执行，
原模块混合负载、数值holder、别名/身份及异常路径作为回退保护。

## 局部候选实现与验收边界

归因 run43601322562：local += AOT约3340ms / 累计托管分配20.0037GB，
module +=约2373ms /20.0278GB；join local约27.5ms /8.12MB。
工件MIR确认local使用NumericAccumulator而module仍为dyn.Add.inplace。
这是同次归因观测，不是新优化收益。

候选仅扩展已有holder：精确System.String采用内部StringBuilder，保持同版本物化缓存，
空追加不换身份、空seed首次追加保rhs身份，旧快照永不修改；动态/子类操作先物化再执行
完整原inplace协议，只有成功后替换状态。追加容量预备后才更新逻辑内容。
缓冲改变资源需求，不能保证OOM发生在与旧实现相同的迭代或相同内存阈值。
MIR额外保守拒绝全函数FunctionLocals ABI，仍拒绝模块、cell、闭包、EH、yield、
循环内别名/自引用和多出口。数值路径不改逻辑，仍须同机回归计时。

运行时单测、MIR源码pipeline形状断言及m3_10_string_accumulator差分覆盖上述语义。
提交阶段若本地没有C#/Rust工具链，只能记录CP预检与Python驱动测试，不能声称产品验收。
完整CI必须先执行这些产品测试及CP/JIT/AOT差分，再做同runner同源工具链的旧/新交错A/B；
原模块+=保留为预期不受益对照，join与数值累加保留为回归对照。

## 自定义str槽的已知兼容缺口（不计作通过）

run43610133094通过fmt、双产品编译、MIR12项及C#9项后，在base JIT的语义用例
定义str子类`__radd__`时失败；另外三臂当次未执行，不能推断它们表现相同。
基线PyStringClass明确拒绝自定义`__radd__`/`__iadd__`槽，该文件未被本优化修改。
因此将这两项保留为独立`string_compatibility_fixtures/custom_slots.py`：CP成功oracle
与四个产品臂逐一实际记录；仅精确既有NotImplementedError标记属于已知不兼容，
意外成功或新错误必须拒绝并重新审查。此记录不是CP差分通过或优化正确性证据。
普通str子类及非str动态协议仍留在严格CP全输出差分门，不能因已知缺口跳过。
