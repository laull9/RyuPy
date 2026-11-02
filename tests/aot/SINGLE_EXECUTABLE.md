# 同次发布的仅可执行文件验证

状态：a256000 三平台真实仅exe四模式验收通过；[证据](../../docs/validation/aot-single-executable/README.md)。不是新增 NativeAOT bundler 或资源单文件支持。

只对已有无资源 schema1 smoke 产物，从同一次 ILC 的已验证发布树复制 native 到全新
`single-executable` 目录，不复制 manifest、调试符号、托管DLL或资源。复制前后及运行
后核对原清单 native SHA/大小与恰一个普通文件（空目录也拒），保执行位；原源码/IL
必须已按既有合同删除。完整发布证据仍保留，不用删证据伪造简单目录。

- Linux：仅该目录挂载到既有runtime-deps容器，禁网/只读、查无dotnet/Python。原完整
  发布树在容器不可见。复用原四模式all/argv/exit/error与完整双流比较，退出0/0/7/1。
- macOS/Windows：新目录作为cwd、既有restricted环境运行同四模式；只证明无相邻
  sidecar的同机执行，SDK和其它宿主目录仍可见，不称干净目标机或完全自包含。
- 系统libc/框架/UCRT等依赖仍存在；显式资源bundle继续要求完整 `_internal` 树，
  不进入本测试。没有新增发布/ILC/性能采样，更不是任意项目单文件打包保证。

每平台仅新增4应用运行（Linux为容器），各30秒且受同次整组剩余预算约束。Mac/Win
继续原30分钟累计预算；Linux仅main补同30分钟deadline、外层step32分钟留清理余量，
不改变共享Runner默认值、原七配置或资源验证。失败保原日志，不自动重试至成功。

`test_single_file.py`只测试复制、所有权、SHA、单文件集合、旧目录哨兵、tamper与symlink；
测试普通bytes不作为可执行证明。真实结果只由上述三平台CI产生，原始日志留artifact。

本轮 Linux 41454408111 / Mac和Windows 41454408061 完整CI成功；每平台四新增模式
均0/0/7/1且同原双流/异常判据，复制前后/运行后SHA和单文件集合一致。此前完整发布
及资源检查继续通过，没有额外ILC或性能样本。
