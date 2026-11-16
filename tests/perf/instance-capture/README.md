# 普通实例提前捕获：隔离验证计划

固定基线 `5d36db1e952f1db07aee6563d7a800cc1df94548`，候选取本次 CI 精确提交。
此目录只定义验证协议，不代表已执行、兼容完成或允许默认晋升。

## 语义与构建

- 候选分别执行默认和依赖限定 `ryupy-driver/records-increment-experiment` 的完整
  `make check`，保存真实日志、TRX、退出状态以及 Cargo 解析的 driver/MIR/e2e 关系。
  这证明编译时 feature 覆盖，不表示程序运行时启用了 records CLI 实验开关；本轮未开启它。
- 旧新 CLI 各自独立构建，保留实际源提交/树、CLI 二进制身份与 SDK 10.0.403；
  oracle 为同次 CPython 3.12.13，Rust 为 1.99.1。
- 原 `capture.py` 不删改，先明确复现固定基线 JIT/AOT 的 new-get / 101 差异，
  候选必须严格等于同次 CP 的 old-get / 9；基线结果明确非 CP 等价。
- 原 `ordinary_instance_method_capture.py` 保留原样和 pending 状态；支持域输入改用
  `supported_capture.py`，仅以 `MappingProxyType(Keywords())` 显式适配原自定义 `**` mapping，
  保留 keys/getitem 回调与方法重绑断言，候选仍须完整 CP/JIT/AOT 等价。
- 自定义 mapping 本身另由 `custom_mapping_boundary.py` 通过独立函数调用隔离。
  同次 CP oracle 必须精确匹配固定输出；旧新 JIT/AOT、两 PIC 态及搬迁必须精确重现
  产品拒绝事件，明确 `cp_equivalent=false`。任何新增输出差异仍失败，不放宽一般语义门。
- 两后端均在 `RYUPY_PIC=1/0` 下检查完整双流和退出码。NativeAOT 发布物复制到
  中文空格路径后隐藏实际编译输入和原发布目录再执行，两 PIC 态均要通过。
  仓库源码仍可见，不能据此宣称清洁机器或完整无源码发布验收。

## 性能与分配

四轮各自重建 JIT/AOT 两臂，构建顺序 AB/BA/BA/AB。每轮每项目每 PIC 态六次
新进程，执行顺序交替。方法负载每进程有两轮预热、四轮计时，四个暖样本相关，
不能当成独立进程。冷墙钟包含进程启动、所有预热/计时正文和 I/O，不能称纯调用时间。
`list_ops` 保持原程序规模作为普通容器控制。

每轮每项目每 PIC 态另跑 stats 进程记录全进程分配与 GC；它不参与正式墙钟数据。
全进程累计分配不是暖态逐调用分配，不能由其直接推出 bound-method 的单次分配成本。
所有不利样本原样保留；不设置自动性能晋升阈值，不因 CI 成功就宣称无退化。
仍需逐后端、PIC 态和四轮检查时间/分配变化。

## 证据保留范围

上传全部文本日志、TRX、解析清单和原生发布清单，以及语义搬迁的 standalone 发布物。
性能构建树与产物在完成各项目后删除；失败时残留构建树也不上传。因此只能离线核验
保留的语义二进制，不能把性能清单或入口摘要描述为完整性能二进制审计。
不改产品开关、不自动推 dev；完整检查失败时后续产品实验不运行。


## 43827788852 原失败与本轮范围

精确候选 `6555eeeb` 的默认/feature 完整检查通过，原 `capture.py` 的旧新
JIT/AOT、PIC 两态及 AOT 搬迁完成。更广的原始 pending 输入在候选 JIT 的
自定义 `Keywords` 双星展开处失败：`TypeError: argument after ** must be a mapping, not 'Keywords'`。
该输入的后续场景、后端以及全部性能测量当时均未运行，不能由早期成功补称通过。
原源码、Git 提交与失败工件保留；支持域适配不等于修复自定义 mapping，也不证明
原 pending 输入已经通过。新的独立负例须重新证实固定旧产品和候选结果一致，
不能仅凭候选的上轮错误预称该差异已在所有基线后端验证。
