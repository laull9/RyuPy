# 用户hash返回归约：隔离driver候选

准备时仅隔离候选；后续实际结果见末节。未新增项目、AOT、性能或公开发布。

三个新应用，单独hash_results driver，不改既有四项hash_slots：
1. hash_result_values：固定64位数值期待，与唯一CP全部24项return对齐；bool、i64保持、超i64 signed modulus、-1规范化；真实finalized/继承/late/root；坏类型不走转换协议、活动context。
2. hash_result_containers：必要整数hash闭包的真实宽long、正负BigInt及11组精确等值double混键；固定hash数值、真实dict/set双向查询与覆盖、一次用户hash、原异常对象和当前context。先判float==integer，显式锁float(P)!=P及float(HI)!=HI。四个用户故意给错hash的宽i64合同仍保raw。
3. hash_result_error：原已验证error.py原字节拷贝，完整九项链；固定CP投影在build前校验。路径名改为hash_result_error.py；源码行不变。

父任务批准必要numeric闭包后，已只读核对唯一CP结果（未再次执行）：
/workspace/shared/ryupy-hash-result-validation/reference/，exit0、85行终标、stderr空。
执行源码SHA256：4f5944bc08dbeb32d462e9be9316910ff39a9731156cbe9c72e1694389607ca6。
真实int子类仍是唯一CP边界，不计本切应用通过。

历史hash_return_and_empty_lookup.py.pending原文件不删除、不改字节，不将整份转正。SHA256：aa6c096daa03141cb25100ac16a5b0adaf75679a86123b57014ebb5e305530b7。
独立empty_valid_hash_lookup.py.pending仅复制其空表部分，明确继续pending。原混合文件视为历史证据，不计活跃覆盖。
reused_hash_exception_context.py.pending保持原字节；本切每操作新建异常，但同一次原始对象身份、context与一次回调仍严格断言。

父代理纳入：三个.py、空表与int子类两个.pending、独立hash_results.rs。已送review_hash_result_contract源审；父负责后续唯一执行。不改编译器/ABI，不扩大空表有效hash次数或重复异常raise修复。

## 首次应用验收

新三个应用首次3/3通过，原四项hash槽回归同时4/4通过；C#新11+旧16共27通过。
随后首次正常完整门禁通过（850 C#、228 CP基线、202 e2e＋原30 ignored）。原历史mixed pending及独立empty pending保持，不将未执行尾部冒本切完成。

`hash_result_int_subclass.py.pending` 保唯一CP参考中的三个合法返回期望及协议零调用，
此独立文件未执行、不计通过；真实int子类布局尚未实现。
