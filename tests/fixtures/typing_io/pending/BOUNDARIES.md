# typing.IO 的独立 pending 正例

- `with_traceback.py.pending`：原完整 I3 草稿逐字保留。异常 with 向 __exit__ 传递真实 traceback 是既有 HIR 未完成项；active 保异常类型/值身份、调用顺序和不抑制，不把 None 当作 CP 正确结果。
- `builtin_delattr.py.pending`：首次 I2 完整原文逐字保留，SHA256 `09a3d7f131daed0c2942bf50c130f7cb51a0196e323fba65c1cce7cd621bb781`。首次该程序 CP exit0，RyuPy 编译唯一 E0100 为 builtin delattr 尚未实现，JIT 未启动。active 仅把八种已知 property 名的删除改为显式 del 分支；接收者、属性名、全部错误全文与其余断言不变，不扩 builtin/compiler。

以上文件不注册为额外应用，不计通过。首次四应用结果保留为三项真实 CP/JIT 通过、一项编译失败；active 修订尚须独立源审和批准重跑，不能追改首轮结果。

2026-11-05后续：ADR0100的delattr竖切已由独立driver逐字执行builtin_delattr.py.pending，CP-first/JIT及完整门禁通过。原文件与首次失败历史保持不变；不代表其余pending已完成。
