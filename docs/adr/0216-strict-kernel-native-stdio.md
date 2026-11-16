# ADR-0216：严格内核复用原生 _io 与纯 C UTF-8 codec

- 日期：2026-11-16
- 状态：独立内核原型已实现；macOS arm64 源码构建和 C 探针通过，产品接入待办
- 接续：[ADR-0208](0208-cil-execution-libpython-kernel.md)

## 决定

严格内核关闭 importlib 后，补齐真实 `_io` 标准流。复用 FileIO(closefd=False)、
BufferedReader/Writer 和 TextIOWrapper，避免另造功能不足的输出对象。
纯 C UTF-8 CodecInfo 与增量编解码器取代 encodings 的启动依赖；错误处理使用上游 C
实现和注册表，不引入 Python helper。全部流由解释器原生命周期管理。

标准流与文件系统固定 UTF-8，未知编码明确失败。增量解码保留跨 chunk 未完成字符；
不可变 bytes 状态保存/恢复保留身份。编码器 final 与原 Python 实现一样完全忽略，
不能为参数解析调用其真值协议。错误处理器的检查时机由同次 CPython 验证：release
模式允许未知名称直到真实编码错误发生；dev 模式的 stdio 配置按上游初始化校验。

同时补齐无 importlib 的 C API 空指针边界，代码执行入口在发布模块前拒绝。
严格分派优先进入默认 evaluator 的拒绝门，自定义 PEP 523 hook 不能抢走帧；
生成器 throw 的原异常保留为拒绝异常的 context。

## 验证与边界

十五种 C 探针覆盖真实 math、启动/关闭零帧、严格求值拒绝、导入负例、中文输入输出、
增量与编码错误、缓冲、EOF、flush/close、坏描述符和初始化失败。
五种 stdio/codecs 探针用同一 C 输入在固定 CPython 3.12.13 上同次对照。
源码和产物均在仓库外，默认结束清理；原型前缀保留许可，不含 .py/.pyc。

本切仅独立内核；现有 auto provider 的 Python 3.9–3.15 合同继续保留。
完整 codecs 模块、StreamReader/Writer、动态原生扩展导入、CIL 模块路由、其它版本及
Linux/Windows 验收仍需后续实现，详见 [内核说明](../../runtime/kernel/README.md)。
