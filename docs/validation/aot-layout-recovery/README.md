# AOT布局与bench恢复验收

被验源码：`ec6ff3b0a265523cef80f242e52b83cfe24853fa`，tree `f8954afb0112d998992f77e411500d5466e16702`。

2026-11-03，以下现有流程全部成功，共6个job、18次原生发布；没有追加固定36或构造专项：

- [Linux smoke、7coverage、资源及hello](https://github.com/laull9/RyuPy/actions/runs/41602253595)：1job，10publish。
- [Mac/Windows普通与资源，Mac原ABC](https://github.com/laull9/RyuPy/actions/runs/41602253692)：2jobs，5publish。
- [固定C ABI绑定三平台](https://github.com/laull9/RyuPy/actions/runs/41602253634)：3jobs，3publish。

Windows首轮LNK1104仍见ADR0052。修后实际rsp及七种已知SDK路径、全部绑定行为与完整owner输入树均已核，成功不构成首失败唯一因果证明。

原ABC无-o默认目录和hello公开make bench-aot均为真实执行；hello只验证命令管线，不发布速度结论。其CP/native6次计数来自原runner实现及成功输出校验，不是独立PID追踪。

精简[产物索引与关键输入哈希](summary.json)保留官方artifact ID/ZIP摘要、原ABC及hello源/输入/原生文件摘要，以及Windows实际SDK相对路径与长度；不重复全部owner文件清单。

## 归档边界

CI原artifact保留14天。以下SHA标识本轮逐原始字节审计后的精简记录，不承诺私有临时文件永久可访问；官方artifact以run链接为准。CLI本体若未归档，其hash仅按执行记录交叉核。完整保留树中的隐藏SDK文件已重核；普通/资源/coverage工具主动删改的输入不冒完整可复建ledger。复制owner只作证据，ZIP mode不冒原宿主执行权限实测。SDK安装、系统库依赖、Mac非macOS12实机边界保留。

- windows_binding：21678 B，SHA256 `c37db277c307bfd8944e24767cd5b7565abb6f0d064e45305dab991a19958772`。
- linux_macos_binding：44551 B，SHA256 `fcf25c81700915cd34221b5826f0779eb2e40c8e7e3a69ae5b809a591dee18b2`。
- macos_default_abc：5379 B，SHA256 `6946f27a7e9cf4bf0611df047a34a61e9a8400fa6f3d56cb677ddb52cb60b988`。
- linux_hello：4881 B，SHA256 `590d4be12790d999ecf9cbf1e3cd68f80784c1bebe2d466935317eb1fb0ea63a`。
- linux_smoke_coverage_resources：43507 B，SHA256 `98ff8ca0641256b99ed83561fa3136728a2dc532282eab8db06cc72e75c3c516`。
- macos_windows_ordinary_resources：48647 B，SHA256 `6ff0170cacecc2cdc907aed603bf4834c56d37d23eaaa0f8aa2fe99ba290971f`。

本记录不含新的性能样本，不改变历史matmul禁tier回退结论。源码仍在WIP，main与正式发布未变。
