# NativeAOT 同次发布清单：有限三平台验收

本次源码/测试提交为 `eca62838ac976546caa4f58418c2f01808392727`；生产Rust实现来自
`3ff7744`，后两次提交只修独立验证器/测试并保留失败记录，不是新性能优化。

## 实际验证范围

- [Linux x64：CLI smoke、无运行时容器搬迁及七配置](https://github.com/laull9/RyuPy/actions/runs/41437645315)
- [macOS arm64 / Windows x64：真实ILC、完整目录搬迁及原四模式](https://github.com/laull9/RyuPy/actions/runs/41437645313)
- 本地完整门禁：454项C#、228项固定CPython 3.12.13基线、202项e2e加30项ignored，
  fmt/clippy及Release零warning；3项真实PE报告测试和driver26项通过。
- 独立Python验证器最终11项：各平台10通过、1个其它平台用例跳过；没有把跳过当实跑。

CI全部成功；最终逐文件摘要与独立复核结果见同目录 `summary.json`。原始双流、发布目录、
SDK记录及行为输出只保留在上述CI artifact（14天），不将raw logs或二进制提交Git。

## 用户获得的能力

正常使用 `ryupy build app/main.py --aot --out-dir out`。只有同次原生产物、文件摘要及
清单全部成功，CLI才在原产物行之外打印 `AOT 清单：.../publish/ryupy-artifact.json`。
请按实际返回位置使用产物，不扫描旧out目录猜本次成功文件。

清单记录真实RID/原生格式、同次MSBuild的SDK版本、本次编译文本SHA、实际发射CIL的
模块入口/访问器和委托引用，以及publish中每个普通文件的大小/SHA。清单不包含自己
的hash；托管IL/runtime仅作为构建输入摘要，不要求部署。完整publish树（含符号及JSON）
可一起搬迁，并由仓库 `tests/aot/manifest_verify.py` 对实际文件集合和字节独立复核。

该清单不是签名或供应链证明。报告中的ldftn引用不等于ILC最终保留根集；模块名与类型名
本身仍可能包含用户信息。编译器revision未知则明确null，不从用户源码目录运行git猜测。
`compiled_text_sha256`是parser实际UTF-8文本摘要，不冒原磁盘编码字节摘要。

## 失败历史与纠正

1. 本地首轮新PE测试两个Constant名称被双glob导入混淆；显式限定MIR枚举后原断言通过。
2. [首轮3ff](https://github.com/laull9/RyuPy/actions/runs/41435814924)的Windows轻测试
   用文本模式写显式CRLF，被再次转为CRCRLF。只改为精确UTF-8字节输入，不放宽SDK解析；
   该Windows轮尚未构建/ILC，Mac及独立Linux已实际通过。
3. [第二轮0b](https://github.com/laull9/RyuPy/actions/runs/41436488887)Windows真实ILC
   成功，但验证器跨path/fd元数据五元组一致性校验失败。固定CPython Windows两API的ctime语义不同；
   修正为各API前后ctime稳定，跨API仍严格dev/ino/size/mtime/birthtime（缺字段拒绝）。
   SHA、exact-set、reparse与读长度检查不削弱，新增逐字段变化负例及实际Windows四快照。
   原失败未记录四值，不能唯一归因于某字段；最后小文件快照ctime恰相同，也不伪称观察到差异。

## 未完成边界

- `dynamic_dependency_closure_complete`、`resources_included`、`native_retention_proven`
  与`manifest_self_hash_included`均为false；未知动态导入、包资源复制与C ABI依赖部署未因此实现。
- 已有低层 `dotnet::publish_aot(dll,out)` 接任意旧DLL，没有本次源/报告，不伪造清单；
  上述成功清单保证属于公开CLI同次编译的AOT路径。
- macOS/Windows是仍安装SDK的hosted runner上移除本轮源/IL并限制环境后的搬迁，
  `clean_machine_without_runtime_tested=false`。Linux有独立无dotnet/Python的容器运行证明。
- 首切仍只这三种host架构；UNC网络共享、其他RID、ISA-off、NaN payload、完整动态协议
  和所有原生依赖闭包未据此扩称通过。没有新增性能采样，也未重测I64性能数值。
- 源PE和原始SDK日志原有路径不因manifest不写绝对源路径而自动脱敏；发布目录检查不宣称
  抵抗同一用户恶意并发替换文件系统的无竞态沙箱。

下一资源切需独立解决模块内部及公开`__file__`两通路与显式选取/复制，不以本清单存在冒称完成。
