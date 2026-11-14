# mutable replacement 的真实 CP 正例与差异边界

这些文件保留阶段F时的真实首败与未实现边界。除下述 ADR0098 后续复验明确登记的原切片源外，其余不注册到 driver、不自动执行，不计 RyuPy 已通过能力。

- `inputs_templates_slice_assignment.py.pending` 是 F2 首次应用原 source 的逐字副本，SHA256 `56458872296c59d3646aac9358040fed5c358014ac341ee954d3a3eb9563e9d8`。固定 CP3.12.13 实际 exit0/30标签；Ryu JIT 实际 exit1/0标签，stderr 为 `TypeError: 'bytearray' object does not support item assignment`，没有用户帧。原应用中的7处 bytearray slice assignment 未实现；静态首候选原第40行，不把推断说成 traceback 定位。现注册 R1 仅将这些准备变异迁到已有 index write 或 slice deletion + extend，原观察点内容、同对象、resize 检查、count 次数及所有断言保持。
- `view_templates_reference.json` 是唯一67行 CP 补参考中29条 view sub/expand 记录的原值副本，SHA256 `f54557e7e4fcefc7720e44c68058293e01ac555b81862ffd5d2c396cabe7d34d`。源 probe SHA256 `620cabfa92b860866d119250054ca447da6cbecaf42f109ff636bdedf476cb26`，原 stdout SHA256 `b07420ffec80a9328c58c34ef0fd2c6c9859910a344f7a2163bf99b8aa4e221b`；固定 CP3.12.13 exit0，67行含 version/complete。该 JSON 是只读提取，不是另一次参考执行。
- `identity_interning.py.pending` 保全原 CP empty/single-byte identity 正例。`EMPTY=b''` 不改为 regex 自产空对象；局部 Empty 不代表全局驻留已经兼容。

view JSON 的 V 行包含 literal、template、expand、expand_literal，O/K 保无效模板、cold released、count release 与 warm cache 边界。活跃 literal sub、readonly template/expand、warm后release仍成功都是真实 CP 成功。writable模板的 hash ValueError、readonly非法模板的 AttributeError、冷release的 ValueError亦保原分类。F2 中所有 view 作为 sub/expand 参数仍整体 NotImplementedError，不能把该差异当作 CP 错误已通过；view escape 是独立已纳域入口。

原67行参考和首次应用原双流/命令/退出码/源/产物hash随阶段验证包持久保存。首次三应用归档名 `application-f2-evidence`，其中 R1 失败保留，不以窄迁后的复验覆盖。该包以其自身 `archive-files.json` 为准。

2026-11-05 后续 ADR0098：`bytearray_slice.rs` 直接 include 原 `inputs_templates_slice_assignment.py.pending` 的全部字节，固定 CP/JIT 首次通过，并随正常完整门禁通过；原 SHA 不变。文件仍放在原位置保存历史失败来源，不再将 bytearray slice 赋值阻断视为当前支持域的未实现能力。view模板与驻留身份文件仍 pending，未借此转正。
