# 首切外真实正例

本目录无driver注册、无本worker执行，不计通过。各文件保CP正确期待，不把运行时NI写成CP错误。

- mutable_subjects：bytearray Match当前内容/固定span、finditer即时导出与耗尽释放、sub前缀捕获和未来变更。
- mutable_replacements：直接bytearray模板、回调原对象在末join读取、escape(buffer)真实bytes结果。
- memoryview_subject：view自身export/release约束、原view的Match.group，不是只复制底层bytes。
- syntax_and_split：现有I-backref/scopedflags/lookbehind/LOCALE/split旧域，不纳首切syntax。
- bytes_subclass：非exact bytes真实输入/捕获类型区分，不能继承exact全片identity期待。

前3组及bytes子类的关键语义已有父唯一52JSON参考；新增小正例（如escape bytearray、普通I-backref）
只按官方CP常规语义准备，尚未运行，不声称唯一参考覆盖全部文件。
完整反射、scanner、groupindex只读映射、str子类consumer和不确定GC释放时点仍沿原文档边界。

typed str模板的非ASCII组名尚缺完整Unicode identifier判定，本切明确NI；bytes模板组名仍保原错误。
最终join按顺序验完片段类型后，仅在真实Array.MaxLength容量越界时报NI，不设更小人为限额，也不捕获OOM。
此处只登记B边界，不新增正例、应用或已验声明。
