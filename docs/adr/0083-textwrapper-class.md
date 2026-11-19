# ADR-0083：可继承 TextWrapper 与动态文本包装协议

状态：未提交候选；74项运行时专项通过，四应用分别通过（首轮2/4保留），正常完整门禁首次通过。未公开发布。

## 背景与范围

ADR0082的固定tabulate原入口在导入TextWrapper时失败，本切提供真实普通类而非模块占位或项目特判。沿用PyClass/PyInstance、真实继承和方法绑定，不新增编译器ABI或布局。模块wrap/fill读取当时TextWrapper，实际构造并调用方法，重绑工厂不会被缓存绕过。

固定CPython3.12.13唯一参考保78个语义记录（另有版本与完成记录），输入SHA `5d8cf861450826319d3cb49f67a016b1c8a43df7e538674cc929391f7524c4bb`、stdout SHA `d8321e69eeb3e357a1a22fac93a985a2435eab27323329a6a26774fa2812c4ff`，exit0、空stderr。当前合同71个支持目标和7个边界，不把参考记录计作Ryu应用通过。

## 对象与调用顺序

初始化真实绑定十个可位置配置和两个keyword-only配置，按原序写12属性，不提前验证宽度或转换传入对象。setter失败保已发生写入；再次初始化覆盖配置并保额外属性。wrap依次动态调用_split_chunks、按需_fix_sentence_endings、_wrap_chunks；fill动态调用wrap并连接结果。_split_chunks动态调用_munge_whitespace及_split，_wrap_chunks动态调用_handle_long_word。

显式根方法只绕过该入口的覆盖，内部hook仍动态。公开块算法操作调用者真实列表，保别名、原对象和失败前已发生修改；不以隐藏副本代替可观察状态。无关duck在实际消费缺失类表时产生属性错误，不假定拥有根默认表。break_on_hyphens的既有CP身份判据保留，数字1不冒真实True。

## 快路与成本

仅根wrap正文可在严格资格成立时复用旧源码区间引擎：exact实例、驻留根类、exact文本、实际模块导出仍同根、类版本未变、原方法Body/Defaults及各默认值引用未变、12槽为允许的原始值。资格检查不调用用户属性、相等或真值；失败回动态协议。新增槽或类改动保守失效，无法恢复资格也不猜测用户行为。直接CLR写内部类字典绕过既有失效协议不属于支持接口，无论是否并发。

模块级函数每次仍有真实构造/绑定成本。默认长词沿原区间扫描，公开动态块路径为保可观察列表可能反复复制尾串；不承诺同样复杂度，更不声称已测加速。原Engine正文和optparse内部调用不变；旧长连字符时限断言仅为回归守门，不是性能对照。

## 明确边界

根默认fix_sentence_endings算法、max_lines算法、float宽度、自定义分词表/翻译表仍明确未实现；子类自行覆盖相应hook不被提前禁止。placeholder在不消费时可以不同，不再按入口配置一概拒绝。用户函数__defaults__写入与普通子类__module__删除保已有对象框架差异，不为本切扩大通用框架；CP正期待保pending。参考使用vars观察不代表Ryu支持vars，应用以逐属性身份和setter事件检验可支持行为。

## 阶段证据

首次运行时编译仅两处测试多变量var声明产生CS0819，零测试执行。改显式异常类型后，16类/快路专项、53算法专项及原5项textwrap回归合计74/74，零失败跳过。原5项包含1200组合和87360小字母表摘要。

四个真实CP-first应用首次2/4：T3动态hook和T4工厂/完整用户链通过；T1在CP成功后编译失败，T2在CP输出标签解析失败，二者未执行JIT。原源码、原流与失败状态保存。T1是既有任意Call基类表达式E0100边界，T2为fill真实换行破坏测试单行标签协议；不是运行时语义失败。首次cargo继续执行四项，各项CP守门仍保，实际4版本探针、4CP、3build、2JIT，不冒整窗口首失败停止；不把运行时专项替代真实应用验收。

上述专项阶段尚未运行正常完整门禁；未运行本候选固定项目、AOT或性能组，历史项目分子不变。

两处已审测试窄迁后，T1与T2各精准复验一次通过，原T3/T4不重复。Alias保存真实基类求值且加identity断言，原Call语法正例保pending；fill仅输出repr，完整字符串断言不改。累计6个版本探针、6次CP、5次build、4次JIT，四个独立应用均已通过，76标签不是76应用。当时正常完整门禁进行中，终态见下节。

## 最终正常门禁

首次make check退出0，fmt、严格clippy、workspace、Release零warning/error、1411项C#零失败跳过、228项固定CP基线及202项e2e/原30 ignored通过，无快照迁移。日志SHA `e48b56b1c23bf9f01712b986911744fbe540e0163047c16ceb975aaed333c5ee`。四应用在正常门禁均通过，加专项首轮与两次精准复验，累计10版本探针、10CP、9build、8JIT；仍是4个独立应用，不把重复验收扩大为用例数。当时未重跑固定tabulate、AOT、性能或公开发布。

## 后续固定项目单入口

最终候选`818b3e7034694d749c878d7aa37a99678673d123`（tree `80e2b9c6efd90c29dac67a9f0fa21b8ddda51fcd`）持久备份后，原锁定tabulate0.9.0包、32源文件与原入口各执行一次CP、编译、JIT。CLI准备一次exit0、0.128408秒；三阶段退出0/0/1，总2.456418002秒、无timeout，保30/90/30阶段与180秒总预算、首失败停止。

CP stdout等于原golden且stderr空。新JIT stdout空，stderr只有`AttributeError: 'str' object has no attribute 'format'`，无调用帧。当前已不再因缺失TextWrapper导出停止，但不能据此定位唯一format调用、声称执行过全部TextWrapper消费者，或宣称表格入口/库通过。

结果SHA `ca941ba203deed9e4d27cdf2995e7b3ed5a4d362255353a8bb33278467067d30`；独立核验官方tar与32源exactset/逐字hash、入口/golden、工具、六原流与三产物。无重试、AOT、性能或公开发布，历史项目分子不变。
