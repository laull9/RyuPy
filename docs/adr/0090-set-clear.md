# ADR-0090：set.clear 与有限集合迭代状态

状态：候选已接线，47项运行时专项/旧回归与3应用+1结构首次通过；正常完整门禁通过，尚未发布。

## 背景与决定

锁定tomli2.0.1固定入口在83c候选上CP0/compile0/JIT1，首错为无帧的set.clear缺失。不能只补可导入占位或据静态代码指认唯一运行现场。本切对精确PySet实现真实原地clear，零参数返回None、保原集合及背板身份，不调用键的hash/eq；保存方法固定原接收者。所有已有直接/PIC/保存/暂停入口共用窄binder，先拒关键字、再拒位置参数，绑定失败不清空，不撤销实参自身已发生的副作用。

唯一CP3.12.13参考74行含首尾，exit0、空stderr、无timeout，0.032131秒只是预算。源SHA62d0d7d51311cde077c86bb32b26b5359e59a0ffd5aeacf6f59aef28c537baeb，stdout SHA c56e0958e02054dd8848b6afaca30ea7be8cb6ebdcd5b76eaf9f8d89ef2f9a24。原流已独立核，不等于候选支持证明。

## 迭代语义与明确限制

既有set迭代是确定性快照；“CP修改集合未定义”的旧文案不准确，实际大小变化有明确错误。仅set分支改用独立PySetIterator，不给list/dict/frozenset游标添加字段。保O(n)建立快照；每次推进先检查真正耗尽，再检查粘滞size错误或当前size，再检查非空clear epoch，最后推进或确认耗尽。

大小变化产生`RuntimeError: Set changed size during iteration`，第一次错误后恢复大小仍失败。只交付最后一个值尚未确认StopIteration时仍须检查变化；真正耗尽后clear/add不复活。空clear不增加epoch，失败绑定也不变epoch。Current只返回最近已交付值，不重新hash或追加修改检查。

CP允许某些同size clear/refill继续返回新成员。当前快照无法正确模拟CP槽位，本阶段明确NotImplementedError，并保完整CP正例pending，不回放已删除的旧值。无clear的同size remove/add继续保既有快照旧债，不宣全部变异迭代兼容，不扩大成所有结构修改均NI。

## 范围和成本

每个PySet增加一个long epoch；成功clear多两项绑定长度检查、Count读取及非空epoch更新。迭代仍O(n)快照时间/空间，普通每步增加常数状态检查；保存的普通方法增加PySet/名称守卫，不宣零成本或性能优化。clear本体直接HashSet.Clear，不重建表、不进行用户回调。独立载体会改变旧CLR泄漏的迭代器类型名，这不是完整Python迭代器反射支持。

NamedTuple已有无回调pair迭代器白名单增加精确PySetIterator，保原set pair成功域，不开放任意IPyIterator。原增长时继续快照的旧测试改为真实size错误，另锁无clear同size旧行为不变。

根set.clear描述符、set子类、重复/非字符串关键字精确诊断及任意**mapping仍为独立边界；本切不改全局FlattenArgs，不将CP合法域改成CP错误期待。参考原脚本/原流保留，仓内result为去执行路径摘要，私有恢复保原JSON。

计划为聚焦runtime、三应用CP/JIT与一结构，再正常完整门禁。tomli另行限时验收，不因本切推断项目通过；不运行AOT或性能组。

## 首次专项

运行时筛选47/47通过，含17新增Facts及旧Prepared/语义/NamedTuple回归；原增长快照期待按CP明确迁移，原无clear同size边界仍锁。3个真实CP/JIT应用与1结构首次全部通过，完整用户异常链保留，未执行pending为通过。随后正常完整门禁通过；当时尚未重跑tomli，也未执行AOT或性能。

## 完整门禁

本次首个make check exit0，fmt/严格clippy、Release零警告/错误、1647项C#（0失败/跳过）、固定CP基线228、workspace与e2e202通过+30原ignored；三应用及结构在门禁再次通过。该门禁阶段没有重新执行tomli，也未执行AOT或性能，没有公开推送，pending不转正。


## 后续锁定 tomli 固定入口

在已验候选`1d305806d0767866236180e24f809fc29f8c3fee`（tree `5158dc9fa0400f1bb2805001103d2b61f8c68ea0`）上，官方锁定tomli2.0.1原包、原入口及include路径仅执行一次CLI准备和CP→compile→JIT。CLI准备exit0，0.138525秒；三阶段各一次exit0、无timeout；CP/JIT stdout均与40字节golden逐字相同、stderr空；编译stdout空、stderr仅86字节生成产物信息。输出为`pkg [1, 2]`和`decode error TOMLDecodeError`两行。

三阶段runner窗口0.596086秒只作预算记录，不是性能测量；30/90/30秒及总180秒上限、超时清理和首失败停止规则不变。九源与锁定tar成员逐字核验，原入口未改。结论仅该未发布候选在Linux JIT下固定入口通过，不更新历史5/20，不宣整个tomli库、公开main、NativeAOT或性能通过。set迭代合法长尾pending仍保留。

本次result SHA256：`3d74bb44f336452ddff43168bc5f3edb0659cbfb24955a90b70f20099dcd5358`。
