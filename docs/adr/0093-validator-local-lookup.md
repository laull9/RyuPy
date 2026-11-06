# ADR-0093：MIR 校验局部槽位查询的有界等价替换

日期：2026-11-04。状态：源码独立审与首次7项专项通过，首次正常完整门禁通过，已完成一次同90秒包复验，仍未在预算内编译完成。

## 背景

ADR-0092 的90秒有界诊断中，已闭合校验阶段累计81.497秒，pass-run累计0.043786秒。这证明校验是该截断窗口的主要成本，但没有证明某个查询或算法占比，不据此宣称已经找到全部瓶颈或具体加速倍数。

## 决定

只替换 validate.rs 五处 locals arena 查询：参数存在性、赋值目标存在性、operand_type(Local)、MakeGenerator 恢复函数参数类型、validate_operand。其余 function/block/global 查询及其他 validator 不动。

实际依赖 la-arena 0.3.1 没有 Arena::get。Arena 内含稠密 Vec，Idx 只有 raw u32，iter 按下标枚举。私有 local_decl 先比较 raw 下标与当前 arena 长度，仅在有效时索引，返回 Option。非法下标继续 None，避免直接索引造成 panic。foreign arena 的同 raw ID 仍解释为目标 arena 对应位置，不新增身份检查。

所有错误消息、累积顺序、短路和可达性规则不变；生成器仍先检查参数数目再索引 params[0]，且查询恢复函数自身 locals。默认校验覆盖和每个 pass 后校验频率不变；不缓存跨 pass 结果、不新增堆分配。

单查询由线性扫描变为常数时间边界检查和索引，不承诺整个校验线性，也不推断包产物或兼容性结论。

## 验证计划与当前边界

测试专用旧 scan oracle 对比空 arena、合法首尾、len、u32::MAX、foreign raw、clear及重新分配；测试五处真实查询路径，完整错误 Vec 固定预期锁定消息、次序和重复项。固定预期根据原代码推导，未声称旧实现已执行。生产不引入查询模式或双执行，不复制整份 validator。

该设计阶段尚未运行测试。接线后首次validate专项7/7通过（新增6项及原1项），完整错误Vec预期未改；随后首次正常make check exit0，fmt/严格clippy/workspace/Release、1647 C#零失败跳过、228 CP基线、202 e2e+原30 ignored均通过。门禁日志SHA256 `30039d2f9e5b93f24602197079a4d0bb90a864676f9b03130c07fc69bae7591f`。该门禁阶段尚未执行包复验或获得速度结论。已限定后续最多一次同90秒诊断编译、0CP/JIT，不延长或重复；执行前须完成门禁、独审与恢复备份。

## 暂不处理

其他 validator 的 locals/blocks 扫描、固定点 facts 克隆/相交、参数线性检索等仅作为候选记录；本次不扩大到这些位置，也不修改 exact_types 中被 leaf_inline 共用的分析。

## 唯一同预算复验

候选 `74af537aa93348319f8bac89f54ae9e761804a57`，tree `e87dd45058a6c3511349ad8d678e62c569236222`；CLI单次准备exit0，0.620048秒。锁定501源码与原入口不变，沿用同一开发构建、pass诊断开关和90秒编译上限。本轮0 CP、0 JIT，不重复或扩大预算。

实际90.004799秒超时、SIGTERM退出-15，stdout空、无产物。1096条阶段记录，547个闭区间；原 `langbulgarianmodel` 已完整越过，最后停在后续 `langrussianmodel` 第6个 `interval_narrow` 之后的校验（88.117033秒开始），外层pipeline未闭，未进入link/emit/write。结果SHA256 `748893a5dda6e4ff855697da98b2ec213f5b881152605a4ebf35bdb2926d25ce`；stderr SHA256 `78751da3423b20d48d1d2198fa86c2b5140b27c69df899db1efa004475272eaa`。

已完成且不相互嵌套的pass-run累计0.064425秒、pass-validate累计78.727744秒。两次90秒窗口覆盖模块不同，不直接相减这些累计值。共同完整模块的pipeline记录，例如jpcntx由16.290312秒变为7.748870秒、big5freq由9.381675秒变为4.852100秒；这只是各一次带日志的开发构建观测，显示该窄候选的局部成本改善及更远推进，不是稳定倍率/正式性能基准。

编译预算失败仍未解决，不能据此宣称chardet运行通过、所有校验已线性或已定位剩余瓶颈。后续只读候选包括其他校验器的局部扫描与数据流成本，需要另行范围与证据；本轮不继续追加目标执行。
