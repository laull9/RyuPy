# ADR0131：受限新增模块别名，不冒完整可写缓存

- 日期：2026-11-06。
- 状态：B首切功能接受，完整A协议及成本开放。

## 决定

保留typed Cache与Import返回PyModule的ABI，只准入空闲、非注册、非包前缀的精确
无点非空str新键到真实模块原引用。State1拒绝，State0独立实例允许且不触发执行。
所有检查在写入前完成；别名来源标记不代替值存储，五注册写路和声明预检作逆向守卫。
普通cache hit与属性成功读取不增分支，未知alias子域/缺属性回退冷拒绝。

仅把view setter改为posonly真实写入口，不伪装真dict，不支持覆盖/删除/None/任意值。
完整可写缓存将需要object返回ABI、from单次对象选择、生命周期收尾、legacy新旧globals
与原生静态证明等跨层改造，不是放开setter即可。自定义spec初始化检测等旧差异保完整pending。

## 证据与代价

2519 C#/900 Rust/578 CP/370 e2e通过，212 ignored；5新完整正例、24新完整pending。
原docopt过，默认Markdown仍find_spec NI，四源窄配置推进到缺xml。两项独立真实PE
证明注册名不可覆盖及原生typed handler稳定，不计入CP正例分子。

HashSet懒分配、每新键两个存储引用、线性冷冲突检查与注册守卫均有成本，未做新性能
采样，不关闭历史风险。完整记录及所有失败见[验收报告](../module-cache-alias-slice.md)。
