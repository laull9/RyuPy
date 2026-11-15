# 实体key候选对固定CPython的事件差分

状态：隔离测试切片，尚待真实SDK/CP3.12.13运行。默认MIR没有调用新入口。
此门称为**runtime候选对CP差分**，不能称为Python编译优化命中或NativeAOT验收。

## 两个独立执行臂

- `records_materialized_cp_oracle.py` 在固定CPython3.12.13即时执行真实
  `d[key] = d.get(key, 0) + 1`，生成25条规范化事件。无提交golden。
- C# `PyDictMaterializedCpOracleTests` 以实际PyDict/PyTuple/PyInstance运行同一动作。
  精确dict尝试真实 `TryIncrementMaterializedKey`，拒绝后经公开 `PyOps.CallMethod`、
  `PyOps.Binary` 与 `PyOps.SetItem` 原协议一次。dict派生不进入候选。

C#不是用C#算术模拟Python加法；fallback使用产品整数/用户协议，候选与普通路径
分别计数。规定311次候选命中、7次fallback，防止全走普通路径却通过事件对照。
事件必须逐条顺序一致；不能只比最后计数。

## 比较范围

25条事件涵盖：首次插入/命中/删除重插的原key身份；-5/256缓存边界、非缓存257、
long上下边界与BigInteger升级、bool回退；两个在RyuPy中发生哈希碰撞的不同字符串键；
用户add回调删除重插，异常后保持旧值；用户key hash/eq的逐次调用顺序与原实体；
dict派生get/set协议；超过256槽时外部修改导致回退及清空恢复。

对象身份比较为对象之间的关系，不跨进程比较裸id。小整数参考对象在CP中由int(str)
取得，在C#中直接使用产品PyInt.Box合同；不将另一个字符串解析入口的缓存行为计入本候选。
输入/输出均无计时，因此此门不产生性能结论。

## 隔离与证据

仅 `RecordsCpOracleTest=true` 的测试构建包含该Fact；默认测试项目不读取外部oracle。
运行必须提供本次oracle路径、实际事件输出路径、精确提交和随机token。oracle带版本、
schema、scope、提交与token；输出CreateNew禁止覆盖旧文件。失败仍尽量保留部分实际事件。
CI必须在新目录用固定CP生成oracle，真实build显式开启测试开关，执行精确Fact并核TRX，
再次从工件对照完整25条事件和311/7命中统计，保留失败日志。

这证明运行时候选及fallback在本组事件中与CP一致，不证明编译器产生了该调用。
未来接线还要以实际MIR命中证据及同次CP/JIT/NativeAOT差分、26项保护和搬迁门验收。
