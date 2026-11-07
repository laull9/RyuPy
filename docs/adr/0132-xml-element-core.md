# ADR0132：真实有限Element对象，不以字符串或空壳冒XML

- 日期：2026-11-06。
- 状态：首切功能接受；完整XML、warnings文件名及性能仍开放。

## 决定

以CPython3.12.13默认C加速Element作参考，运行时新增真实Element/ElementTree/QName、
Comment/PI与HTML_EMPTY，沿既有原生模块/对象协议接线，不编译整个XML栈或借解释器。
Element保tag/text/tail原引用、attrib构造浅复制/赋值共享、无parent的children图。
选择常用读写字段、append/insert、keys/items快照、整数/读切片、活迭代和有限前序。

QName仅精确文本域；未知用户比较/非文本写入及改变模块QName绑定明确NI。
工厂保定义globals和旧alias身份，未知Element回调不偷偷回退。未承诺repr/str明确NI，
QName.str仍真值；Element无公开__iter__，不能因内部游标虚报ABC形状。

TreeBuilder事件状态机、tostring/解析、clear/extend/remove、写删children、完整派生/
反射等另切。bool发真实DeprecationWarning，但旧basename定位差异完整保pending。

## 验证与代价

2538 C#/900 Rust/600 CP/379 e2e通过、225 ignored；22新完整场景中9正13pending。
原docopt保持通过，默认Markdown仍find_spec NI，窄配置推进到缺OrderedDict，未更新
20项目通过率，也未声称原Markdown自己的serializer已跑完。

属性keys扫描、绑定包装、对象分配及前序栈/活动路径集合有成本，无新性能/AOT测量。
所有旧失败与边界保留，详见[合同与验收](../xml-element-core-slice.md)。
