# ADR-0186：CPython平台布局与封闭原生加载

- 日期：2026-11-14
- 状态：实现与验证中；平台实测结果由专用验收文档登记

## 决定

在driver集中定义osx-arm64、linux-x64和win-x64的固定PBS20260623/CPython3.12.13布局，
头文件、stdlib、扩展后缀、libpython路径、来源及build_options共同决定平台绑定。
JIT与NativeAOT部署消费同一冻结计划。macOS和Linux固定pgo+lto，Windows固定pgo。
每个原生映像验证目标格式和机器架构，非系统依赖必须在分发内唯一解析。

Linux要求readelf与patchelf成功执行，收集版本化共享库并重写每个映像的相对rpath；系统库按
精确名字匹配。Windows解析普通与延迟导入表，限制目录、原始节、描述符及字符串边界。
VC运行时属于产品依赖，须随包；白名单仅保留Windows操作系统库和API-set族。
Windows宿主使用SetDefaultDllDirectories、AddDllDirectory及带明确搜索标记的LoadLibraryExW，
仅注册随包home与DLLs目录，屏蔽开发PATH和cwd。目录注册及原生库同寿命，保留到进程退出。

嵌入解释器采用隔离PyPreConfig，固定UTF-8模式，避免进程locale引起默认文件读写退化为ASCII。
默认中文读写通过真实CPython oracle、RyuPy编译入口、JIT/NativeAOT搬迁验证。

## 验收

三平台分别用本机工具链构建、执行同一应用并搬迁。CI必须运行产品应用并比较三流和副作用，
包含输入删除、缺库负例及清洁PATH；复制DLL或由宿主Python读写文件的测试仅作为辅助检查。
干净目标机的独立交付另验Python/.NET/SDK均不存在的运行环境。
系统CA证书作为宿主数据依赖单列，HTTPS证书链待独立验证。
