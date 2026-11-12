# 普通 MI/C3 有限同源门

固定 CPython 3.12.13 与 .NET SDK 10.0.403，同一 HEAD 新构建 CLI/Runtime。四份已核支持源各 CP 一次、普通 Legacy JIT PIC=1/0 各一次；stdout 原黄金及空 stderr 均严格匹配。源文件在仓库内，构建前后全 Git blob/mode 核验；原业务 PE、工具及每次 argv/完整双流/退出码失败也上传。耗时只作诊断，不是性能基准。

原三份完整源与历史 CP 黄金独立保留：`original_positive` 的 instance.__dict__ 是 JIT AttributeError（保原部分输出），`original_failures` 类体直接 raise 为编译 E0100，`original_publication` 的 Legacy globals 为编译 E0100。它们始终 product_pass=false，不计支持用例通过。检查器可以确认预期边界而成功，但 `overall_compatibility=false`，不称原项目或全部类体系完成。

新 MI NativeAOT 在发射前拒绝，并要求无任何输出 DLL；不拿旧 AOT 成功证明新 MI AOT。native 布局/元类/结构变更/跨模块未知单基 MI/词法 MI 的精确保护以本轮生产 Rust/C# 测试为准，未新增未经运行确认的业务负例阶段。class-body traceback 缺帧未修，完整 Click 未验。

主 CI 精确标题 `validation: ordinary-mi-c3-001` 同时选择本独立门、旧默认/显式 AOT 和 metadata 旧无 resources 真实红。双 fullcheck、固定20、S1/ABI不依赖本门，任何失败不会豁免保护。公开冷 ABI 本轮仅新增 PrepareOrdinaryMiClass，精确逐签许可由原S1门维护，不由本脚本猜计数。

本地纯检查：`python3 -B -m unittest discover -s tests/aot/ordinary_mi -p 'test_*.py'`。纯模型不运行产品、不代表 Native 支持。

固定20按同HEAD逐项目记录，原通过项目继续保护；新NI可改变失败阶段，不硬编码总通过/拒绝计数，也不声称项目增加。

## 动态单基的旧 Native 能力与实际 MI 拒绝

同一 job 追加 `runtime-single-base-native`，不替换四个 MI JIT 图、原红例或
已知 MI 发射前拒绝。独立的两份小图都先核固定 CP3.12.13 黄金、实际 PE 调用
`PrepareRuntimeDerivedClass` 的数量/签名，再运行双 PIC JIT。

- `ordinary`：真实源码再导出、模块属性基、继承 `super()` 及 str/list/dict
  别名。真实 Native 原位/搬迁都必须成功，输出逐字节符合 CP。
- `mi_rejected`：实际 Sequence 基经普通变量槽进入新准备入口，CP/JIT 成功；
  Native 原位/搬迁都必须 exit1、空 stdout、精确动态码能力 NI。它不是
  发射前拒绝，更不是 MI Native 支持。

每次 Native 发布限600秒，复用已有40分钟step/55分钟job预算，不增加工作流
选择开关。两ELF在执行前保完整dist归档、manifest文件身份与managed/runtime
输入摘要；执行时隐藏本门源码副本、清空继承环境，并核搬迁前后dist不变。
obj/bin发布中间缓存不上传，必要SDK输入/原流及真实dist归档保留。检查器纯测试
通过、内部false能力参数或本地JIT都不能替代同提交hosted真实Native验收。
