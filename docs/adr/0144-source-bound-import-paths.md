# ADR-0144：普通JIT source-bound首次Import父路径

- 状态：候选实施；CP3.12.13合同已测，C#/真实JIT/完整20/旧Native待验。
- 日期：2026-11-09。

## 背景

真实旧显式include反例：父path空、child未缓存仍错误成功。不能只给未来自动发现加guard，也不能套find_spec整套spec/loader或原list引用守卫。

## 决定

按[精确合同](../source-import-paths.md)在ordinary cache miss读取实际父当前path。复用kind1对象来源证明父子目录；空list/tuple真MNFE，同值新list/tuple/[d,d]合法。先getter、finder边界、相干证明，再判空；未知目录/协议NI，不假报不存在。

保cache/State1、父后二查、失败只删本对象和成功依赖。抽取唯一初始化尾段给ordinary/discovery，后者保strict验证和末次cache，不重复getter。无新公开ABI、长期cache或skip布尔。

单成员Legacy调用方补同名MNFE抑制、退出handler后的属性/cache第二阶段和正常kind1源错误字段；不改实例Relative时序、不扩多成员ABI/可变元数据诊断协议。host子名须为单路径分量。

父spec私有圆环、kind0/2/native/旧ABI不扩域。自动资源发现不变，warning/TypedDict来源独立竖切。

## 代价与门槛

未知路径NI是公开收窄，真实项目原正确正臂不豁免。冷路新增成本未测，cache语义不变不等于性能相同。

CP十案/旧include原流是需求证据，不是产品通过；新四目录pending。必须验旧原样explicit include JIT修复、正常正臂、完整default/pilot/20和旧真实Native，不提前计通过率。
