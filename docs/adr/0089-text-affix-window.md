# ADR-0089：字符串前后缀窗口与真实子类方法捕获

状态：候选已接线；新增23项运行时专项与34项旧回归通过，首次driver3/5，受影响两应用随后分别通过，累计四应用与一结构通过。正常完整门禁通过。未发布。

## 背景

固定 tomli 2.0.1 原入口在候选 c402 的 Linux JIT 运行中抛出 `startswith() takes exactly one argument (2 given)`。原错误没有帧，不能据此认定唯一调用点。仓内旧文本方法仅接受一个前后缀参数；保存的方法还有独立的旧参数数目限制。不能通过只放宽直接调用来宣称语义一致。

## 决策

文本 startswith/endswith 共用窄绑定与窗口内核。保留直接、保存及根描述符的接收者校验和关键字错误优先级，接受起止位置、None、整数与类型级 __index__。先固定三个原参数对象，再依次转换 start/end，防复用调用点参数数组的重入污染；end 的类型槽仍在 start 回调完成后才读取。

tuple 候选按实际内部槽顺序短路，不提前校验未访问成员。窗口按码点定位，超尾 start 不夹成有效空窗；只用 ordinal span 比较，不复制窗口。匹配成功后常数时间检查边界，防正常补充平面字符被孤立半代理匹配。独立代理来源丢失 D009 仍未修复。

真实 PyStringInstance 在实参前读取方法，覆盖所有该布局实例的方法而非仅这两个名字。使用已有捕获 ABI，不新增编译器操作。property 和 __getattr__ 用户正文不广 catch；仅本地制造的缺失属性等错误承担 Prepare。普通实例、精确 CLR string 及原 module/namedtuple 路径不扩大语义。

## 性能与边界

单参数整串路径不预扫码点长度、不创建 substring 或候选数组。常用机器整数直接处理；负位置从末尾定位。正码点位置仍需从首定位，重复大位置调用可能累计平方级；本切没有持久字符串索引缓存，也没有性能测量结论。

现捕获判定的两次调用各增加真实字符串子类类型判断。字符串子类方法提前属性查找，覆盖方法不再复用原方法 PIC 命中，捕获调用的部分参数数目会创建数组。因此不宣称零成本；精确字符串仍不被包装。

bytes/bytearray 的既有前后缀实现不在本切修改。警告型 index 返回、未支持 tuple 派生及复杂 descriptor 长尾保 CP 正期望 pending，不把 NI 称为 CP 一致。D009 独立保留。原项目成功、AOT及性能均需另外证据。

## 当前证据

唯一 CPython 3.12.13 参考：14组80事实，exit0、空 stderr、无超时；0.032134秒仅为预算记录。脚本 SHA256 2bd574b0338ce58fcff8ccb47cb4d7113c5e84e7d8485d9ed79b6e7575227b4f；stdout SHA256 78796b543348f33b7dd07264895de010895323fa585c567c41eb507b5c01d9cc。参考与最终设计已经独立核验，尚不代表 RyuPy 实现通过。

计划验收为聚焦运行时测试、四个 CP/JIT 应用与一个结构测试，再正常完整门禁。保原流与首次失败，不削用户异常帧，不以该单入口更新历史项目通过率。

## 首次候选验证

新增 affix 14项与 capture 9项均首次通过，另34项既有字符串/准备方法/bytes/tuple/module捕获回归通过。driver 首次3/5，窗口应用、完整cause/context用户链及结构通过；两应用在顶层给原生异常写入 `__cause__` 时遇旧 AttributeError，尚未执行其后断言。现运行时支持该属性读取、尚未支持本例赋值；保原CP成功源为pending，随后以真实 `raise error from cause` 建立原因，不删除identity/context/cause断言。原失败证据保留，不扩生产异常框架。

两受影响应用迁移后各独立 CP/JIT 首验通过，getter/index正文首次raise-from保原identity/context/cause/suppress断言，没有预先raise。其余三项不改不重跑；首次3/5仍保留。随后正常完整门禁通过。

## 完整门禁终态

本次首个 `make check` exit0：fmt、严格clippy、Release零警告/错误、1630项C#（0失败/跳过）、固定CPython基线228、workspace及e2e202通过+30原ignored。driver全组在门禁中亦5/5通过。初次专项3/5的cause写入旧债及两次针对性迁移证据仍保，不改旧pending为支持。该门禁阶段尚未重新执行tomli，也未运行AOT或性能测量；候选尚未公开。

## 后续固定 tomli 入口

在已验提交 `83c050fd63463e96da7d65a428e965716e13117a` 上，以官方锁定tomli2.0.1原包、原入口及include路径执行一次CLI准备和CP→compile→JIT。九个源文件与锁定tar逐成员字节/hash一致；CLI准备exit0，0.141649秒。阶段分别exit0/0/1，无timeout，runner窗口0.598744秒；30/90/30秒及总180秒预算，首失败停，无重试。时间仅记录预算，不是性能测量。

CP原stdout与40字节golden一致、stderr空；JIT stdout空，stderr为54字节单行 `AttributeError: 'set' object has no attribute 'clear'`。原参数数目错误已不再是本次首错，但新错误无帧，不能据静态源码指认唯一运行现场。原流/解释器/CLI/runtime与三产物hash均独立核验；不称tomli整个库或固定入口通过，不改历史5/20，不冒main或AOT。

本次result SHA256：`d86f05203ad91353484c9e9c2616c0ab3113df1597f9e94c12ffe5921808dfc1`。
