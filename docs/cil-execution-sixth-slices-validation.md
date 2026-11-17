# 第六轮验收：权威模块环境与真实原生调用

日期：2026-11-17，平台macOS arm64。新profile通过显式
`--experimental-stock-module-namespaces`选择完整本地源码图，底层复用原版CPython。
生产选择规则基于冻结来源与实际能力，第三方名称只出现在测试输入、原项目和验收说明。
通用C ABI、pyi及原生组件分发沿第五轮已提交合同继续使用。

## 同一字典与真实执行

Runtime复用InstanceGlobals；provider为普通源码模块分配真实CP模块及其唯一D，入口
复用规范__main__。模块、CIL读写和外部CP写入共同使用D；loader保部分初始化、失败
回滚与重试。模块载体一次进入整个CIL正文，必要原生边界按可信来源切换现场。
资源、code来源与跨堆生命周期见ADR-0236/0237。

第一组四应用的双版本JIT/NativeAOT共16个产品通过：eager三模块、annotations四模块、
harbor_pair和frost_pair各三模块。后两套名称和数据布局独立。每次先生成固定CPython
即时预期，实际构建后删除源码、venv和build，中文空格路径搬迁、空PATH及失效Python
环境运行，核对完整退出码/stdout/stderr、所有源码的编译归属以及真实关闭计数。
3.12日志`20261117T114637.828735Z.json`、3.13日志`20261117T114810.414056Z.json`，
均位于`~/.cache/ryupy/validation/stock-module-namespaces/`。

急切推导式继承当前执行环境，普通def与惰性genexpr在定义点捕获；AnnAssign保留精确
目标域dict、右值/目标/注解/实际mapping写入顺序，回调重绑后活读。规范builtin身份
在用户代码执行前捕获，默认基类使用规范object；类桥启动只保留必要legacy wrapper，
其它描述符继续lazy。实际替换__import__后，无显式import的类创建没有隐藏导入。

两模块生成器产品另通过双版本JIT/NativeAOT四格：send/throw/close、finally、惰性读取、
yield-from的真实终值与再次耗尽，以及同名callee一次求值和对象跨暂停身份。实际测试
发现旧tp_iternext的MoveNext吞掉终值，修复为真实PyGenerator的既有Next协议。
验证器继续核验源码callee快照，只排除精确私有帧槽的机械保存/恢复；四个MIR测试含
context来源、保存邻槽及恢复邻槽的破坏拒绝。产品日志分别为
`20261117T115007.910822Z.json`与`20261117T115742.698468Z.json`。

## 导入钩子与原生窗口

实际导入读取本次捕获builtins中的hook并仅调用一次，保留五实参、真实globals、普通
函数locals=None、fromlist/level和任意返回对象。IMPORT_FROM继续真实属性/缓存回退
与ImportError字段；class的prepared namespace仍按其独立合同推进。
两个无关命名/布局的hook输入，双版本JIT/NativeAOT共八产品搬迁通过；原属性删除
输入保留，foreign模块删除转发同一真实对象的既有DelAttr协议。中性Runtime删除门
52项通过，含缺属性、错误身份及失败前副作用；旧静态槽守卫继续保留。
最终hook日志为`20261117T115755.428136Z.json`、`20261117T115851.073239Z.json`。

真实最小Cocoa输入保留`QApplication([])`，JIT、NativeAOT和OneFile均删除源码、venv、
组件输入与构建目录后搬迁通过，实际cocoa窗口visible/exposed，截图已视觉核验，完整
三流与同次CPython一致，本地业务委派为零。证据位于
`~/.cache/ryupy/validation/explorerpro-products/`下
`20261117T115636.399118Z`、`20261117T115851.074664Z`、
`20261117T115851.074451Z`。完整原入口及原GUI业务按其实际后续阻塞分别登记。

主审发现IMPORT_FROM的name_from布局只存在于3.12+，无版本守卫会破坏旧provider。
保留七版preflight中3.9–3.11失败记录，补真实头文件版本条件后3.9–3.15严格native
编译全部通过；日志`generic-interfaces-20261117/native-versions-guarded.json`。
函数现场后续变更还需同样复验，平台运行与完整frame能力逐项记录。

## 性能与集成门

六输入独占AOT对照发现普通foreign键识别使原字典/集合热路回退30–48%，随后只对
精确本地布局恢复原hash/equality入口。相同冻结CLI的单变量对照，字典、集合、容器、
推导式墙钟分别下降约28%、22%、44%、32%；推导式RSS增加2.219MiB，hello增加
0.121ms，字符串近乎持平。原不利数据、CPU/RSS分列与六输入范围完整保留于ADR-0238。
修复在独立worktree完整make check通过后以`207ebd6c`提交并推送。

权威namespace/carrier的新函数builtins与Python回调进入CIL来源仍在联合收尾：原红测
分别实际观察到定义点builtins被外层模块覆盖，以及Python A→CIL B→CAPI仍读A环境。
完整原输入、退出码与原生计数保留。最终联合完整门、函数现场产品复验和必要边界成本
在冻结实现后追加；C1–C4完整业务闭包和默认晋升继续按各自门槛验收。
