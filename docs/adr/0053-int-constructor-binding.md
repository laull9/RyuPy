# ADR-0053：int 构造参数绑定与进制文本解析

- 状态：本地专项及完整门禁通过，远端未验
- 基线：main `671a48cdcb40d6ba2300c49073fa10139cc18ffa`
- 范围：已有常用内建构造的正确性；不新增原生发布或性能实验

## 问题与取舍

当前真实最小差分为 `int("10", base())`，其中 `base()` 打印标记并返回16。固定CPython3.12.13输出标记和16，旧RyuPy退出0却只输出10。旧MIR只读取第一个参数，既丢弃进制，也漏掉其副作用；运行时一等调用也只取首参。类型检查另把合法的 `int()` 拒绝为缺参数。

本切优先修复已有API的静默错误，不按旧项目表猜测新增通过数。bytes正则还涉及完整Pattern/Match/替换和可变buffer生命周期，Hashable还涉及隐式hash槽、dataclass及ABC缓存，另行排期。

## 设计

仅恰好一个普通位置参数保原静态数值转换路径。其它形态通过既有完整参数求值、展开和 `CallBuiltin`：零参、显式base、关键字、星号、过多参数均不得省略表达式。无需新IR或CIL ABI。普通单参的动态文本转换与一等int调用共享严格parser。

`x` 为位置专用；`base` 可第二位置或关键字，必须区分省略与显式None/0。绑定错误在参数求值后报告。显式base先调用并验证 `__index__`，再读文本，bytearray在该回调之后读取，保留回调修改。无base数值路径保持，普通对象依次使用真实类型上的 `__int__` / `__index__`。

文本支持str、bytes、bytearray，base0或2至36、符号、合法外侧空白、严格下划线和前缀、Unicode十进制数字及BigInteger。bytes使用ASCII字节语法，不把UTF8字节当Unicode数字。旧的删掉所有下划线再解析不能沿用；源码字面量解析不随此切改写。

非2幂进制采用CPython默认4300位限制；不增加sys可配置阈值接口。2幂进制采用线性位打包，不借移位预算另加不相关的文本位数上限。容量按实际表示检查，不捕获资源失败后重放参数或副作用。不得据实现形式宣称已测加速。

## 有限边界

int/str子类布局、int.__new__、完整warnings系统不在本切。返回bool的自定义 `__int__` / `__index__` 和仅有弃用 `__trunc__` 的路径明确报NotImplementedError；[原CP正例](../../tests/fixtures/int_constructor/deprecated_protocols.py.pending)已保留；直接bool数值与base=False不因此拒绝。静态float转I64的既有宽度限制、float/abs/str等其它构造器的参数欠账另行保留，不声称所有构造器已修复。

无base的memoryview文本转换也保留CP正例，本切明确NotImplementedError；显式base按CP拒绝为TypeError。原正例只作未实现边界记录，不增加通过或ignored数量。

正常Python错误须在实际调用点保留caller和异常上下文，不漏CLR解析异常。新测试必须覆盖直接、一等别名、传参和局部遮蔽，不能只测手工runtime helper。

## 验证计划

固定CP3.12.13探针已覆盖参数错误优先级、进制索引回调/失败、bytearray修改、Unicode及下划线边界。原main失败结果保留，不当作修复后通过。后续先runtime与MIR结构专项，再真实编译差分和完整 `make check`，最后同步实际终态。制定计划时尚未运行修复后的RyuPy测试，实际专项终态见下文。

不运行固定20项目或旧36；历史5/20仅指2026-11-01指定Linux JIT入口，matmul禁tier回退仍未关闭。公开推送前保留可恢复源码包，避免使用旧WIP触发大批AOT验收。

首次runtime编译因两个新public类型缺XML summary触发CS1591，未进入测试；仅补类说明后重编，原失败记录保留。


专项终态：runtime16/16、ty1/1、MIR结构1/1通过。首次真实driver2/3，主结果差分正确，但旧内部caught异常的快照多带module帧。迁移时误复制旧冻结稿又得到同一2/3；未将该次当新修复证据。随后按已审独立补丁把原context置于模块级，int错误仍在被调用函数内，完整8行链与固定CP相等，driver3/3通过。原源码逐字保留为nested_context_frames.py.pending，不削投影或改变traceback。完整正常make check终态exit0：fmt、strict clippy、603项C#、228项CP基线、workspace及202项e2e通过/30项原ignored。未设置FORCE或批量接受快照；本轮无快照改动。尚未新增远端、NativeAOT或性能结果。
