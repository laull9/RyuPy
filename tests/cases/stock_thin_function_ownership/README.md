# 显式 stock 薄函数产品接线待办

全部输入保持 pending。`case.txt` 只登记标准 CPython 正面语义；当前尚无显式 stock 函数
编译开关，因此未添加无法解析的候选字段，也未用旧 `instance_module` 路径替代新路线。
接线设计见 [ADR-0226](../../../docs/adr/0226-stock-thin-function-product-contract.md)。

- `live_reload`：真实 FunctionType、globals 身份、位置/关键字调用、参数与异常身份、
  可写公开元数据、字典写删、删缓存重导的新旧环境、CP 循环回收。
- `failed_import`：真实 importlib 失败清缓存、外逃函数、重复失败的字典独立性和回收。

固定 CPython 3.12.13 与 3.13.3 运行这些入口可验证输入；产物/stdout oracle 由当次生成，
禁止保存 golden。普通 e2e 通过之后仍需专用真实宿主验证以下矩阵：

| 场景 | 必须观察 |
| --- | --- |
| CP-only | CLR H 已死并 drain；F 仍能进入已登记 CIL core |
| CLR-only | 删除缓存/CP GC 后 H 调用原 D；新 import 的 D 独立 |
| 回流 | 活 H 同身份；死 H 可重建；owned F 精确增减 |
| 双端释放 | finalize 前 F/哨兵/core 弱引用失效、capsule 原生析构、队列与强根回基线 |
| 变更拒绝 | 已持 H、再次 Acquire、原生 P 直调、替换 code 后 CP 调用均受守卫 |
| 重入/观察 | 转换回调删除 globals 值、trace 创建/调用/返回、profile、关闭竞争 |

托管接口单测只覆盖协议状态。真实 GIL、owned 引用、GCHandle 与原生析构仍需产品 shim
接线后测量。保留 ADR-0224 的四组合 proof；本目录未复用或修改其实现。
