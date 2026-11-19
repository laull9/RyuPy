# 同run完整暖kernel Native成对窗口

此门只依赖同次run/attempt的warm与allocation成功，下载精确命名的透明tar，包内HEAD/run/attempt、成员hash/mode与源码来源均核验；原旧保护没有反向依赖。原warm M6和allocation ABBA不删不降级。SDK仅恢复既有锁定的官方10.0.403，免重编Rust/CLI/Runtime。上游两Runtime不同必须仅来自三个已审生产文件，warm新Runtime与allocation新Runtime必须逐字同。

两臂共享实际业务kernel PE和原静态Host源码；机械替换原工程两个HintPath和SDK记录位置，保严格AOT/trim属性。各自正常publish一次，RuntimeFrameworkVersion固定10.0.14。保存实际Host.dll和ILC响应，正常Deterministic/PathMap将各arm根映射到/_/paired；要求两实际Host仅唯一Module.Mvid、COFF时间戳及明确CodeView/PDB身份字段可不同，其余全部PE字节相等才进入C3。额外GUID/EncId、IL/token或未知debug变化立即拒绝。保完整两Host及SHA，明确不是同DLL，不承诺Native代码布局无扰动，MVID/布局可能混杂，结果不是纯LRU算法因果归因；不用缓存绕过真实publish。Native dist与构建输入另存tar保mode和被上传obj排除规则遮蔽的响应文件。

六个合同必须逐字等CP438B、stderr空：旧新各一次同PE JIT、ELF原位、完整dist搬移。之后每臂6进程/72点原A/A门，均过才进入6个四进程块ABBA/BAAB交替（24进程/288点）。仍每进程4暖批+12计时批、N100000，checksum/cache逐批核；不删异常值或补采。每进程median、块内各arm几何均值比值；进程内与arm早晚均0.95–1.05，arm块内第二/第一位置几何均值0.95–1.05且逐块0.9–1.1，ABBA/BAAB层比0.95–1.05。完整六块同向、总体B/A≤0.95且两顺序层同向才标本窗口几何平均用时降低≥5%。否则不报告收益；没有置信下界/跨机器泛化。A/A不能证明不存在所有系统噪声。

预算：两个下载各1分钟、SDK4分钟、controller总1800秒（步骤31分钟含失败收件宽限）、上传2分钟，总job40分钟另留约1分钟checkout/纯检。publish各最多360秒、六合同各最多30秒、测量子60秒/阶段最多900秒，全部受controller总deadline约束；阶段上限不可保证同时耗尽，超总即失败、停止不补采。正常预计两publish约数分钟、计量约3–5分钟，非时长承诺。始终保原流/失败状态，网络/构建超时不算语义失败。

复现需要同run两个输入tar、两完整dist.tar/build-inputs.tar、SDK/Clang身份及包解析清单、所有命令/原流/RESULT/FROZEN与本提交；源码完整恢复仍依赖既有四bundle+本次delta（不把输入包称源码备份）。原CLI不在输入包，重放同PE不需要CLI；重新发射PE需恢复源码/编译器另行验证。当前实现只做纯检查，不表示本窗口Native或计时已运行。禁止拿跨run0.297/0.229计算收益。
