# ADR-0163：以真实类统一 ABC、C3、view 与有限实例布局

- 状态：本地完整回归通过；同提交 hosted CI / Native 保护待验
- 日期：2026-11-12

## 顶层决定

ABC 不能只是让 isinstance 返回 True 的标签，更不能通过放开 native base 守卫
让无法解释的布局进入普通类。十八个 collections.abc 根采用唯一规范 PyClass，
继承、查找 owner、super 和实例化共同复用普通 C3 权威。模块名是公开身份，
内部规范根集合才是能力身份；用户重绑定导出不获得认证。用户派生类保持名义
判定，只有规范根有已声明的内建结构性判定，不引入一般 ABCMeta/register。

HIR 只认证有限稳定 from-import 来源并选现有动态普通类准备路径，不在后端
复制 Python 类型语义；运行期仍校验真实 base。兼容的纯 import try/fallback
只使这个准备路径可用，不等价于静态承诺实际导入对象。来源被 shadow 时撤销。
本次新 ABC 域的 Native AOT 尚不开放，必须在产物发射前明确拒绝。

## 布局与行为

- view 保存真实 _mapping 固定槽，迭代、长度和成员操作读取映射协议，不能复制
  为快照或用任意实例字典键假装槽。描述符只有自身的真实类型可获得特殊待遇。
- 普通有限空 __slots__ 冻结布局；是否具有实例字典由继承决定。未声明 slots
  的后代恢复字典，有字典祖先不能被子类空 slots 消除。复杂用户非空 slots
  继续明确不支持，不扩大任意 native 布局混合。
- 新 slots 类族采用真实 PyDict 作为动态属性唯一权威，固定槽独立且优先。
  旧普通实例反射边界不趁机全部开放；新族属性 PIC 使用正确冷路径。
- 抽象状态基于直接父类已冻结抽象集合及当前自己的真实抽象成员完成，不重扫
  所有祖先而复活已具体化的方法。有限默认方法按 CPython 实际行为实现。
- mixin 的有限迭代器遵守延迟、副作用、终结及逃逸 StopIteration 的行为；
  不假装完整生成器 send/throw/close 控制协议，域外访问明确失败。

## 真实项目与后续缺口

原 sortedcontainers 的包、入口及黄金输出不修改。ABC/view/slots 只是必要条件，
不是项目验收。恢复后的入口先在 SortedSet 取 set.isdisjoint 时失败；补通
通用 set 方法后，原入口默认 PIC 与 PIC=0 均已完整 exit0、stderr 空、stdout
逐字节符合 CPython 3.12.13。扩展探针 SortedKeyList 又暴露 map 内建 fallback
缺口；map 本体已存在，此次只补已完成 map 的普通模块未绑定槽回退。
扩展探针继而完成 key 排序、实时 view、集合运算，最终用户跨模块再派生
SortedList 仍被旧 deferred-publication 合同守卫拒绝。保留完整失败探针，
不以删除末尾类定义冒充扩展通过；跨模块类认证另切设计。dict 子类七协议和 set/map 依赖按各自的通用协议实现、
独立差分和保护测试推进，不能以项目名、固定对象或固定输出特判。

扩展探针不是固定二十项目集的新分母，也不以局部成功替换原入口。验收要求
原入口完整 stdout/exit/stderr、双 PIC 专项、完整回归与旧 Native 能力保护。

## 成本与证据

新增实例布局字段、规范 ABC 类及冷分派存在内存/启动/调用成本，尚未测量；
不宣称零成本或性能提升，原性能风险不被本切关闭。兼容性完成后再统一测量。

一次工作区重置丢失了未发布的原 ABC 补丁。当前是按恢复材料重新实现，
不能用旧日志证明新字节；每阶段重新测试并滚动私备，最终以同源完整检查为准。

## 本轮本地验收

修复两项旧 S1 能力守卫回归及 set 交集等值元素身份偏差后，冻结实现的
完整 make check 通过且前后文件摘要一致：CPython 基线 687、C# 2927
（另 2 项既有未执行）、e2e 415 通过 / 279 ignored。新 ABC 五例及
slots、dict 七槽、set 家族各自另以默认/PIC=0 验证。既有 pending 不因此关闭。

完整二十项目在两配置分别核原 stdout、exit、stderr；CP 基线均 20/20：

- 无 pilot：12 native / 7 unsupported / 1 failed，Markdown 仍旧 find_spec 限制。
- ordinary-dynamic-module-pilot 编译配置、默认 JIT 会话：13 native /
  7 unsupported / 0 failed，保留既有十二项并新增 sortedcontainers。

这里 native 是项目门禁的 JIT 标签，绝非十三项 Native AOT 已通过。
来源计划的严格 protected 列表仅同步新增规范 collections.abc，不允许任意
新项、漏项、重复或乱序。正式提交后的双配置完整 CI 与既有 Native 保护
仍须独立收集；本地通过不代替这些结果。
