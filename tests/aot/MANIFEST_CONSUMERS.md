# 当前产物清单消费者合同

本页登记测试工具消费当前源码生产者的合同，不是新的产物schema版本或历史清单迁移器。
生产者为driver的ManifestContext与native_binding_manifest；本次不改生产代码。

## 统一严格字段

schema1/2的manifest_verify与schema3的binding_manifest共享manifest_options.validate_options：
compiler.options必须精确七键，未知/缺字段拒绝。

- experimental_direct_accumulator、experimental_foreach_accumulator、experimental_leaf_inline、
  diagnostic_scalar_slp、experimental_packed_slp：精确bool，不接受0/1、字符串或null。
- instance_modules：list，每项为非空str；允许空列表。生产者原样记录Session Vec，允许重复，
  消费者不得暗中去重或新增唯一性规则。这不是开放任何额外实例/AOT能力。
- module_environment_policy：只允许explicit-only或ordinary-dynamic-module-pilot。

两套module对象都必须包含environment_reason，严格非空str；它是生产者的动态原因文本，
不是封闭枚举。其它module字段仍精确匹配，schema3另保origin字段，不允许未知键。

## 历史与范围

旧生产者曾在同一顶层schema号下输出四/五/六项options和无environment_reason的模块。
当前核验器明确拒绝这些旧形状，不补默认值、不把旧artifact冒充当前完整来源。
如需重验历史清单应使用其已冻结对应版本工具，不宣称本次兼容所有旧格式。
顶层schema1/2/3及其资源、绑定、hash、路径、安全边界均未改变。

共享调用覆盖普通AOT、资源、平台/默认布局/构造、覆盖率、bench及schema3绑定smoke。
合成fixtures已同步当前生产者，并跨三schema测试键集合、精确类型、policy与reason反例。
另对run42364073965的四份真实Native manifest执行只读schema验证；这不执行归档ELF，
不把资源smoke的旧失败回写为通过，不替代下一轮真实publish/搬移/容器验收。

## 同轮完整门禁的getopt来源合同

getopt_core测试不再比较某台机器的安装目录。仍要求固定CPython3.12.13，读取该解释器
getopt.__file__真实绝对文件，并检查官方源码SHA256：
0ce875700c8798193b8e2748f7a27fc542cc4d525a1e6fc403767450ec92be99。
库帧匹配用这个已验证路径的精确文本，原行号/函数/源码行/异常连接词与用户帧断言不变。
不会按basename放宽过滤，也不创建旧机器目录或改版本门。Rust完整检查只在授权CI执行。
