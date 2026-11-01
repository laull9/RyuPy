# bytes / bytearray 前后缀匹配

本切实现通用 `startswith(prefix[, start[, end]])` 与 `endswith(...)`，不是针对
chardet 的特判。chardet 5.2.0 `universaldetector.py:180–207` 以 bytearray 接收器检查
单个或 tuple BOM 前缀，是实际后续调用需求；项目仍有更早 TypedDict 阻断，尚不计通过。

## API 与求值顺序

- 仅 1–3 个位置参数，不接受关键字；方法应能作为一等绑定值读取。
- prefix 接受 bytes、bytearray、已实现的连续 B/c memoryview，或这些对象组成的 tuple。
  空 tuple 返回 False，候选从左向右转换/比较；一旦命中，不读取后续无效或已释放对象。
- start/end 接受整数、None 或 `__index__`；顺序是 start、end、prefix。整数无界输入
  按平台 index 范围夹取，负值按接收器长度正规化；不把越过末尾的 start 压回末尾，
  因而 `b'ab'.startswith(b'', 3)` 仍 False。用户转换异常原样传播。
- 比较原始字节，不通过 str 解码。B/c view 作为 prefix 使用原始 buffer，而非 c 的
  单字节 bytes 迭代元素。已释放 view 只有被实际消费时才 ValueError。
- `__index__` 返回 bool 涉及弃用警告，沿本项目已登记转换子集明确 NotImplementedError。

## 已知重入边界（原合法场景保留 pending）

固定 CPython 3.12.13 实测，bytearray 边界回调前已捕获匹配长度，但比较仍能看见同长写入：
将 b'ab' 原位改为 b'xb' 后 startswith(b'x') 为 True。若回调扩展为 b'abcd'，即使显式
end=999，startswith(b'abcd') 仍 False；缩短为 b'a' 后 endswith(b'a') 仍 False。
本实现不读取旧指针或冒险访问缩短后的 CLR List，也不复制接收器来隐藏同长写入。

因此在 start/end 均成功转换后、消费 prefix 前，若 bytearray 的 resize epoch 相对调用初始值已改变，
明确 NotImplementedError；即使回调恢复原长度也不放行，此前回调副作用保留。转换本身先抛错则原样传播，不替换为能力错误。
原合法 CPython resize 场景独立保留于 `m3_7_binary_affix_resize.py`，不能转正或删分母。
这是有限能力边界，不是声称与 CPython 完整一致；后续若扩展需单独评估存储与重入协议。

## 性能及验证状态

每个 bytearray 新增 8 字节 resize epoch 字段，每次成功改变长度后递增一次；
同长写、空 extend、空删除及 *=1 不递增，检查或转换失败不提前递增。
Append/Extend/DelItem/+=/*= 全部既有长度修改入口覆盖该结构版本，不扩大存储布局重写。
此守卫保证明确拒绝结构重入边界，不作加速承诺。

bytes/bytearray 前缀直接以 Span 比较，不分配子切片或转换字符串。当前 memoryview 前缀
通过现有 `CopyBuffer()` 做一次临时复制，保留读取时刻与 release 语义，但分配成本是
O(prefix 长度)；未宣称所有前缀零分配，后续可通过受保护 raw-span 接口优化。

新增 resize epoch 后 4 项专测及 Release 构建已通过，纳入
`/tmp/ryupy-typed-dict-runtime-focused.log` 的 11 项聚合专测；独立只读审查已确认接点闭环。
最新 epoch 真实差分已重跑：正向 passed，resize 仍为明确 NotImplementedError 的独立 pending，
记录 `/tmp/ryupy-typed-dict-affix-results.jsonl`；整体仍待 checkpoint5 聚合门禁。
此前正向用例
`m3_7_binary_affix.py` 已通过固定 CPython 3.12.13 与 RyuPy 真实差分并转正。
`m3_7_binary_affix_resize.py` 实测为明确 NotImplementedError，保留原 CPython 合法期望与 pending。
证据 `/tmp/ryupy-binary-affix-runtime.log`、`/tmp/ryupy-binary-affix-release.log`、
`/tmp/ryupy-binary-affix-results.jsonl`；不称整 M3.7 完成。
