# ADR-0167：真实 tzinfo 协议根与统一 aware 日期消费

- 状态：独立竖切实施中，尚未验收
- 日期：2026-11-12

## 问题与选择

真实 toml 已先后暴露词法多基、Decimal 依赖及 `TomlTz(datetime.tzinfo)`
继承边界。仅取消继承守卫会留下不能被日期对象消费的伪时区，因此以真实
标准库协议为单位实现，而不是按项目名放行导入。

选择无额外 payload 的 canonical PyClass tzinfo 根，模块实际导出这个根；
不创建旁路同名代理，不以字符串类名授予能力。根可实例化，默认抽象行为
明确 NotImplementedError；它不是 ABCMeta。原生根的不可变性与对象存储
能力分别建模：根任意外部属性修改均拒绝，用户子类仍可正常修改；不得用
拒绝全部继承的旧特殊布局标记代替不可变能力。

native timezone 与根的 isinstance/issubclass、真实 bases/mro 身份统一；
导出名重绑不得改变已经创建对象的实际根。内部身份元数据机制不新增公开
ABI，不改变其它 native 类型既有关系。

## 日期协议的单一消费路径

日期和时间保存实际 tzinfo 对象，而不是把用户回调快照成固定偏移。
构造只校验类型身份；datetime 消费时向回调传实际自身，time 传 None。
utcoffset/dst 接受 timedelta 或 None，并校验严格小于24小时的绝对偏移；
tzname 接受 str 或 None，包括真实 str 子类且直接方法调用保留返回对象。
原固定 timezone 继续直接偏移快路。

全部已开放 aware 操作必须共同适配：iso/str、replace/timetz、公开三方法、
比较、相减、hash、timestamp、fromutc 与 astimezone。相同 tzinfo 身份的
比较与相减不求偏移；跨身份按 CP 的 None/fold 规则处理。hash 使用 fold=0
并缓存结果。astimezone 真实调用目标 fromutc；用户 override 的返回值按
CP 原样返回，不能误加 datetime 强制检查。默认根 fromutc 自身仍检查输入
是 datetime 且 tzinfo is self，并实现默认协议算法。

## 边界与验证

不扩 zoneinfo、平台本地时区或旧 naive 平台转换；原明确拒绝保留。
有限协议准入不代表日期/时区的所有 Python 特性完成。

除固定 CP 根身份/回调/错误返回/fold/hash/fromutc 合同与双 PIC 外，须保留
原 DateTime C#、datetime e2e 和 fixed timezone 回归。原 toml 入口、内联表、
Decimal 真值及 Z/正负偏移日期往返探针均需完整运行。新能力尚未验证前，
不改变项目通过统计。Native 需同提交真实产物证据，不能由纯C#测试替代。
