# 实体key候选对固定CPython的事件差分

状态：首轮真实差分发现既有dict子类tuple键布局拒绝，已拆分明确已知不兼容门；修订待重跑。默认MIR没有调用新入口。
此门称为**runtime候选对CP差分**，不能称为Python编译优化命中或NativeAOT验收。

## 两个独立执行臂

- `records_materialized_cp_oracle.py` 在固定CPython3.12.13即时执行真实
  `d[key] = d.get(key, 0) + 1`，生成25条规范化事件。无提交golden。
- C# `PyDictMaterializedCpOracleTests` 以实际PyDict/PyTuple/PyInstance运行同一动作。
  精确dict尝试真实 `TryIncrementMaterializedKey`，拒绝后经公开 `PyOps.CallMethod`、
  `PyOps.Binary` 与 `PyOps.SetItem` 原协议一次。dict派生不进入候选。

C#不是用C#算术模拟Python加法；fallback使用产品整数/用户协议，候选与普通路径
分别计数。主比较规定311次候选命中、6次fallback，防止全走普通路径却通过事件对照。
主比较22条事件必须逐条顺序一致；不能只比最后计数。

另一个独立Fact保留CP的3条dict派生成功事件，对照Runtime的get/set、
精确NotImplementedError类型/消息及零写入。它要求0快路、1回退，
报告scope为known-incompatibility和cp_equivalent=false；全局all_cp_compatible=false。
这验证已知边界仍存在，不是修复该兼容性问题，也不能算进CP相等的通过数。

## 比较范围

25条事件涵盖：首次插入/命中/删除重插的原key身份；-5/256缓存边界、非缓存257、
long上下边界与BigInteger升级、bool回退；两个在RyuPy中发生哈希碰撞的不同字符串键；
用户add回调删除重插，异常后保持旧值；用户key hash/eq的逐次调用顺序与原实体；
dict派生get/set协议；超过256槽时外部修改导致回退及清空恢复。

对象身份比较为对象之间的关系，不跨进程比较裸id。小整数参考对象在CP中由int(str)
取得，在C#中直接使用产品PyInt.Box合同；不将另一个字符串解析入口的缓存行为计入本候选。
输入/输出均无计时，因此此门不产生性能结论。

## 首轮真实结果

run43688017399，提交95a996455af7af25c228fad10b2c3884220a96bd：原15及新11合同通过，
实际CP及C#前22事件一致；dict子类的根setitem明确拒绝tuple键，导致Fact失败。
当时预算/clear场景未执行，不能用原合同或合成证据代替本次重跑。
不修改产品scalar布局边界，不用底层raw.Set绕过。

## 隔离与证据

仅 `RecordsCpOracleTest=true` 的测试构建包含两个Fact；默认测试项目不读取外部oracle。
运行必须提供本次oracle路径、实际事件输出路径、精确提交和随机token。oracle带版本、
schema、scope、提交与token；输出CreateNew禁止覆盖旧文件。失败仍尽量保留部分实际事件。
CI必须在新目录用固定CP生成oracle，真实build显式开启测试开关，执行精确Fact并核TRX，
再次从工件严格核主22条匹配及311/6统计，并独立核已知差异3条及0/1统计，保留失败日志。

主比较成功仅证明运行时候选及fallback在22条匹配事件中与CP一致，不证明编译器产生了该调用。
未来接线还要以实际MIR命中证据及同次CP/JIT/NativeAOT差分、26项保护和搬迁门验收。
