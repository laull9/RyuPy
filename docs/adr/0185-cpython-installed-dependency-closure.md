# ADR-0185：从明确安装环境冻结CPython依赖闭包

- 日期：2026-11-14
- 状态：实现；实际验证范围见专用测试说明

## 决定

在既有schema 1 provider声明中增加可选`dependencies`，包含`environment`、PEP508
`requirements`和`dynamic_modules`。旧`modules/payloads`继续有效。driver在选择模块域前
完成收集，JIT与NativeAOT沿现有事务部署同一计划，入口继续经过RyuPy编译链。

基础发行仍固定CPython3.12.13/PBS20260623；安装环境可使用venv。构建期只启动明确基础发行
的解释器，以`-I -S -B`运行随driver编译的静态工具，显式读取安装环境site-packages。
不执行应用、扩展、site初始化、`.pth`，不进行安装或联网。工具依赖固定`packaging==25.0`，
由用户准备到构建环境，仅用于PEP508、版本约束、extras、marker及wheel标签判断。

入口及本地依赖源码的AST导入建立第三方根；发行METADATA的Requires-Dist构建传递闭包，
按目标固定解释器的marker环境求值，extras使用不动点传播。动态插件通过显式声明补足。
没有metadata的源码依赖继续使用原有手工payloads/modules；自动收集缺失时明确报出模块、
需求链与安装环境。安装闭包采用wheel RECORD文件列表，完整保留模块、命名空间、包数据、
原生库、dist-info与许可证；拒绝editable、路径越界、文件缺失、重复所有权和site启动钩子。
RECORD摘要字段不参与普通文件校验，清单记录来源、版本、文件路径及最终部署大小。

自动模式排除基础发行的site-packages，随后只部署已选载荷。包附带的原生库进入现有平台
relocate审计；包外未解决的原生边由现有审计阻断，并提示通过显式载荷补足。
所有运行时搜索路径继续相对于产品目录，运行时只读取随包内容。

## 取舍

采用安装元数据闭包，避免以执行导入探针猜测应用需求；应用依赖内部可选导入、插件和数据驱动
导入由显式requirements/dynamic_modules补足。源码中静态出现的第三方导入严格要求可解析，
包括条件导入；构建环境须准备这些依赖。应用包不在构建阶段执行，副作用由真实业务测试验证。

安装来源以明确环境、发行名称/版本、安装器、WHEEL标签、RECORD及direct_url记录；
索引安装缺少direct_url时记录为installed_distribution，保留现场可获得的来源信息。
需要精确归档来源的发布流程应在准备安装环境时保存锁文件并使用direct URL wheel安装。
当前收集针对本机固定解释器；跨平台准备与运行需要各自平台工具链和验收。
