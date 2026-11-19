# ADR-0246：macOS 应用包与显式 wheel 载荷裁剪

- 日期：2026-11-19。
- 状态：macOS arm64 本地发布。

## 决定

`ryupy main.py --mode app --app-name MyApp` 在 macOS 上生成 `MyApp.app`。入口位于
`Contents/MacOS/`，原 `internal/` 是邻接目录；保留既有加载边、运行时 marker 和分发清单。
Finder 使用 `Info.plist` 直接启动 NativeAOT 入口，不加脚本、二次启动器或解包阶段。
只在本轮独占目录构造应用；已有同名 `.app` 不覆盖。没有生成 Developer ID 签名或公证。

`--cpython-native-components` 的声明可另附 `payload_pruning`。每条列明规范化的
`distribution`、精确 `version`、`omit` 路径数组和 `reason`。路径是 wheel 下的完整
文件或目录，不使用通配符。每项必须存在且为该 wheel 独占，范围不得重叠；发行元数据、
许可证与共享文件不能删除。每个删除文件和源文件字节数进入 provider 报告。裁剪后仍
校验保留原生映像的加载闭包；Python 和 Qt 动态导入另由实际业务测试保护。
无裁剪声明时不改变既有发布内容。

## ExplorerPro

固定 PySide6 6.9.2 的 [精简声明](../../tests/general_projects/github_large/explorerpro-native-lean.json)
仅移除没有被原应用导入的 WebEngine、对应 QML 和原生依赖者；Qt Widgets、Cocoa、SQLite、
原预览依赖、声明元数据和许可保留。原 [Cocoa/SQLite 声明](../../tests/general_projects/github_large/explorerpro-native-selection.json)
保留用于对照。

原 `internal/python` 文件字节数 847,753,895，精简 Cocoa 业务产品为 495,012,466（−41.6%）。
原业务探针删除输入后在中文路径的 `.app` 内运行，通过了真实窗口、导航、预览、SQLite、
窗口关闭及线程检查；对应证据 `explorerpro-products/20261119T020343.150076Z/`。
`tests/aot/macos_app_smoke.py` 也用真实 LaunchServices 启动删除输入后的中文路径应用，
并验证同名目标保留。只删 WebEngine Core 而不删原生依赖者的负例被发布器明确拒绝。
其它平台、默认 profile 及全部业务矩阵没有因此晋升。
