# 原生AOT真实验收（独立桩与CLI分阶段）

恢复基线M3.10。首个AssemblyRef身份修已完整门禁通过，但本地ILLink任务所需Unix IPC
被执行沙箱禁止；不改SDK、不关闭trim，以正常GitHub Actions标准Linux runner验证真实ILC。

`smoke.py --prepare-only --out target/aot-prepare`只运行固定3.12.13与真实JIT对照，
不能计AOT通过。默认模式调用真实 `ryupy build --aot`；driver生成独立薄C#宿主，int Main返回真实生成入口值，dotnet publish
开启AOT和裁剪警告阻断；没有全程序集root、反射执行或JIT回退。

行为覆盖同一源码的模块槽/跨模块调用、kwargs/default、nonlocal闭包、生成器恢复与清理、
argv空串/Unicode、SystemExit7和用户异常。成功流/退出码逐字对照；异常CP只规范化绝对
路径和源码展示行，完整(frame文件名/行号/函数)+最后异常类型消息仍相等；原stderr归档。
JIT与native的stderr不作该规范化，必须逐字一致。

发布成功后仅复制真正native产物至新目录，移除本测试创建的源码、IL与桩项目，换CWD/净化
环境运行；再在官方 `mcr.microsoft.com/dotnet/runtime-deps:10.0.14-noble` 容器中，
确认没有dotnet/Python命令、仅挂载native目录且关闭网络后运行同样模式。宿主库依赖由
file/ldd与镜像明确记录，不宣称完全静态或无需系统库。当前只验Linux x64，不冒其它平台。

CI固定SDK10.0.403、Rust1.99.1、CPython3.12.13，日志/退出码和失败工件保留。
首次远端执行已通过，原始证据见 `../../docs/validation/aot-smoke-ci/README.md`。
此前通过的是独立薄桩探针；当前driver已接入，CLI实际ILC/搬迁仍待本次CI，不能沿用旧探针结果冒通过。

本地预检（2026-11-02）：三份Python文件语法检查通过；固定CPython3.12.13与修后JIT
在all/argv/exit/error四模式全部匹配，prepare-only实际exit0。随后真实CI也通过；预检本身不替代NativeAOT运行。Rust action固定到1.99.1分支实际SHA，避免仅用滚动stable。

CLI接线本地门禁（2026-11-02）：3项driver结构/保留名与2项真实CLI失败隔离专项通过；完整make check为434 C#、225固定基线、199 e2e+30 ignored。两失败测试用同轮Cargo构建CLI与子进程DOTNET，只模拟非0/无新产物失败，不以假文件计AOT成功。成功SDK原始双流先复制归档再删除本轮构建目录；真实含空格、中文、&路径交远端CLI ILC验证。
