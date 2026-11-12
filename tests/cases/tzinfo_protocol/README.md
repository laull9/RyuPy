# tzinfo 用户派生协议合同

五组纯通用 CPython 3.12.13 合同，已通过产品双 PIC 精确 stdout、stderr 和退出码差分。保留 time 回调 None、错误文案及用户返回对象身份，不对差异做归一化。

- 根身份、重复导入、timezone 继承关系；根不可修改任意属性，用户类及实例可存储与修改属性。
- datetime 回调传原对象，time 回调传 None；tzname 保留 str 子类返回身份。
- offset/dst/name 返回校验，None、错误类型、严格小于正负一天、异常身份透传。
- 同 zone 比较/相减跳过 offset；跨 zone 的 fold 相等保护、hash 使用 fold=0 与缓存。
- astimezone 调目标真实 fromutc override，包括异常透传及任意 tuple 返回；继承默认 fromutc 的参数检查。

真实未修改 TomlTz 的 Z/正负偏移 parse+dumps 是单独项目 probe，不计入本目录分母。
不扩展 naive 本地时区、系统 TZ、zoneinfo、pickle 或完整 datetime API。CP 成功不是产品完成声明。
