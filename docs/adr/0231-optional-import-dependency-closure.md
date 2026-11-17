# ADR-0231：可选导入的依赖闭包

- 日期：2026-11-17。
- 状态：已接受，六组静态边界、四产品搬迁、三负例和完整门禁通过。

## 问题与决定

ExplorerPro 原入口的依赖收集首先被缺少 `PyPDF2` distribution metadata 阻断。
原程序使用 `try/except ImportError` 为 PDF 依赖提供后备路径；无差别扫描全部导入
会把这种合法的可选依赖提升为必需依赖，使构建比原 CPython 执行更严格。

依赖收集记录导入是否位于捕获导入异常的词法保护范围。缺失可选依赖保留可审计的
收集结果，已经安装的可选依赖仍完整收集其资源、metadata 和原生库。相同模块只要
存在必需导入或用户显式声明，即按必需处理，缺失仍报错。

保护仅适用于实际执行在对应 `try` 正文内的导入；handler、else、finally 不因该
`try` 自动取得保护。函数正文在未来调用时执行，需要重新判定；类正文在定义时执行，
保持实际外层保护。该规则只调整依赖闭包，不改变源码执行归属和 CIL 语法资格。

仅识别未被源码绑定的内建异常名；宽泛异常、属性异常名和 `except*` 继续按必需处理。
`except*` 会重置外层保护，避免 ExceptionGroup 穿越普通 ImportError handler 的误判。
已存在源码/扩展却无 metadata 的可选模块仍报缺少发行登记，避免遗漏已安装能力。

## 验收

依赖收集器的词法反例、必需优先、显式声明和已安装分发分别验证；使用真实 CLI
运行同次 CPython 差分及 NativeAOT 删除输入搬迁。大型项目后续编译诊断单独登记。

缺包/已装包 × JIT/NativeAOT四格全部通过；已装场景包含传递依赖与导入副作用，
构建阶段不执行应用导入。必需同根、显式动态及已装无metadata三负例通过。
原FastAPI与真实Tk SanBudget各一项最终NativeAOT搬迁通过；后者保留原compiled-app合同。
结果、日志与大型项目下一阻塞见[第四轮验收](../cil-execution-fourth-slices-validation.md)。
