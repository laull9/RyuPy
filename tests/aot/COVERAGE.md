# 固定裁剪语义与SLP策略覆盖（首次真实CI通过）

本组是ADR0043 N1增量，不是性能验收。保留原完整e2e源码与固定CPython3.12.13期望：

| 源码 | 编译策略 |
| --- | --- |
| phase2/m2_1_bigint.py | 默认 |
| phase1/m1_5_dataclass.py | 默认 |
| phase3/m3_3_dataclass_protocol.py | 默认 |
| phase3/m3_10_numeric_list_island.py | 默认 |
| phase3/m3_10_float_slp.py | 默认、显式packed、显式scalar各一份 |

共5份源码、7次真实CLI --aot，每份CP一次，每配置JIT/native各一次，完整stdout/stderr/exit对照。
前置smoke继续保模块/闭包/生成器/错误与无dotnet/Python容器搬迁。实验SLP不因此改默认。
AOT ISA-off与NaN payload位级矩阵尚未验，不能用JIT环境变量假证明ILC分支选择。

每publish180秒、整组内部20分钟、外step22分钟留清理/写结果余量；超时通过本轮独占进程组
终止CLI及dotnet/ILC，保部分双流，不遗留昂贵子进程。shell→sleep永久轻自测先于CI构建。
首失败停止剩余昂贵发布，部分结果不得称7项通过；修复错误后按同一清单复验，不删不利项。

每项保源码与native哈希、大小和构建墙钟（仅观测，不冒性能收益）；成功后只删该项自有
host/bin、obj缓存，保项目/Program/CIL/runtime/native与SDK日志。失败保诊断树，原始记录
仅在本地target及Actions工件，不新增Git raw logs。本地语法、超时自测及smoke
prepare-only通过后，真实CI41405464604七配置全通过，见[摘要](../../docs/validation/aot-coverage/README.md)。
