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

生成器源码except链随后补齐最终逻辑路由证明：逐项核对实际active窗口、typed匹配、
handler PC、Retry的私有帧/待接异常写入、恢复分派和Catch槽清理，保原for/with合成
StopIteration路由。可达根包含真实EH入口；只豁免最终CFG确不可达的已清空块。
二十项实际IR变异涵盖错误区间、异常/PC仍可达块、整数溢出及恢复槽，均明确拒绝。
两独立多模块输入双版JIT/AOT八产品通过：`20261117T121559.974900Z.json`、
`20261117T121625.617702Z.json`、`20261117T121625.617720Z.json`。

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

首轮窗口探针通过QTimer在`app.exec()`约300ms后截图并退出。用户随后实际观察到鼠标
持续加载，验收已追加持续心跳、真实鼠标/键盘输入及原生关闭事件；这一组交互正在验证。

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

函数定义点builtins与Python回调来源现已完成有限合同：两套独立模块输入双版JIT/AOT
八产品通过，原pending原样迁入四模块sunrise场景；canonical、外来及字面量builtins
均补验真实CP dict类型，四产品通过。日志为`20261117T121951.009514Z.json`、
`20261117T122126.203157Z.json`、`20261117T122218.500702Z.json`、
`20261117T122351.389323Z.json`。Runtime相关41项及冷边界两项通过。

来源选择区分真正新CIL帧和Python内部协议。Python A调用CIL B时切到B的真实D及
定义点builtins，返回后恢复A；无新增CIL帧的协议回调保持A的globals/locals/builtins及
code/file，反向四产品亦通过。CLR错误展开残留的诊断帧在错误导出期不能取得现场切换权。
运行期载体在业务执行前以对应版本真实PyFunctionObject布局安装owned func_builtins，
业务D保持原样；该布局使用目标头文件构建，七版本严格编译已复验。

完整函数frame仍有明确待做项：原样差分中CP为`(True,17,'read_frame',8)`，候选为
`(True,None,'<module>',1)`，对应globals已正确，locals/name/line继续开发。真实异常
产品另发现CLR入口EH强根延迟释放载体；入口现用NoInlining私有内层结束catch栈，外层
再执行Finish；十二个双版JIT/AOT搬迁产品已通过RuntimeError、字符串SystemExit及原生
KeyError的退出码/stdout/错误末行核验，真实carrier/token/origin均为零。完整frame四项
继续保原语义红测，其关闭计数已为零。产品索引为仓外
`~/.cache/ryupy/validation/ownership-carrier-final-20261117.json`。

完整ExplorerPro新profile已越过生成器except，当前到archive_service.py的实例
dataclass合同。原完整入口、原业务probe、窗口交互与各发布形态分别登记。最终联合
完整门及必要边界成本在冻结实现后追加；C1–C4完整闭包和默认晋升按各自门槛验收。
