# AOT 列表推导式私有收集器直接追加

## 实现

HIR继续将列表推导式降为独立的隐式嵌套函数。MIR构建时，
`private_comp_append.rs` 对合成的 `<listcomp>`、无源码定义信息、非生成器、
非参数/非cell/free的私有 `.result` 局部、root首条空列表初始化和单一定义逐项检查。
所有语句的绑定写入穷尽匹配，包含导入与异常handler；删除、类定义和来源不明的默认值语句拒绝特化。
只有 `append` 和唯一位置实参通过后才开始求值及发射MIR。

接收者先于元素求值，各一次，然后调用既有 `RuntimeFn::ListAppend`，返回None。
普通用户append、自定义类、set/dict及生成器表达式保留原路径。推导式的独立作用域、
闭包late binding、默认值绑定、最左迭代器时机和多层过滤顺序保留。
本轮没有新增Runtime ABI、容量提示或函数去包装，也没有修改CIL局部保活策略。
决定见 [ADR-0196](adr/0196-private-list-comprehension-append.md)。

原始 `comprehensions.py` 三个列表推导式20轮共追加79.8万次，直接消去这些收集操作的
通用方法分派与参数校验。集合推导式仍执行40万次add；次数由遍历元素决定。
五个原始推导式均无free/cell环境，不能将开销全归因于闭包。
CIL的locals init也不能单独证明GC保活，需原生代码活跃性证据。

## 正确性验收

- 8项真实源管线/MIR结构测试及严格Clippy通过。直接读取原始基准，验证三个ListAppend直调、
  set.add和普通append保留；覆盖重赋值、非空初始化、cell、删除和关键字实参拒绝。
- `phase3/aot_list_comp_append` 已转正。固定CPython3.12.13与JIT同次差分通过；
  覆盖副作用、捕获、默认参数、实例self、多层过滤、短路和已绑定同名全局。
- 主用例真实NativeAOT及源码/构建输入隐藏、中文目录搬迁、空PATH执行通过，完整双流及退出码一致。
- 元素和过滤异常分别执行旧/新NativeAOT，完整双流及退出码一致；与CP逐行核对时仅单列
  既有的额外一条listcomp帧，其余异常帧、消息、源码行和退出码均一致，见semantics §4.17。
- 随包NumPy2.3.5数组作为迭代源的列表推导式及sum，固定CP/显式provider NativeAOT、
  全输入隐藏搬迁及空PATH执行完整双流一致。

模块未绑定list读取的既有内建回退缺口保留原始pending反例
`phase3/aot_list_comp_builtin_rebinding`，见semantics §4.16。

## 配对性能验收

Apple M5、macOS arm64、CPython3.12.13、.NET10.0.403；A为已验证的sum/PIC/字符串版本，
B仅增加本次私有列表追加特化。26项均采用直接脚本、无插桩墙钟/RSS、1warm5交替执行，
每次严格验证退出码与双流；统计量来自独立插桩轮。
每个旧/新发布目录均搬迁后隐藏原位置、空PATH再执行通过，临时构建树已清理。

| 原始基准 | CPython ms | A ms | B ms | RSS MiB A / B |
| --- | ---: | ---: | ---: | ---: |
| comprehensions | 42.94 | 130.08 | 117.66 | 46.86 / 46.88 |
| generators | 118.57 | 341.56 | 346.82 | 21.88 / 21.91 |
| sieve | 77.17 | 183.32 | 178.42 | 52.73 / 52.75 |
| records | 66.58 | 134.87 | 134.91 | 50.83 / 50.84 |
| containers | 35.17 | 23.97 | 24.33 | 40.48 / 40.47 |
| str_concat | 171.31 | 11.12 | 11.30 | 22.67 / 22.69 |

推导式墙钟下降9.54%；分配量两臂均约119.80MiB、二进制均11,170,952字节，RSS持平。
收益主要体现在分派路径，元素分配和GC压力继续优化。其余项目的小幅变化保留在对照中；
例如generators本轮增加1.54%，源码生成路径未被本特化命中，当前没有因果证据。
排除hello，几何平均CP/Ryu由1.1223变为1.1272；候选按中位数速度达标13/26、RSS3/26、同时2/26。
逐项目标尚未达成，推导式仍慢于CPython且占用更多内存。

## 完整门禁

固定CPython、项目target、空RUSTC_WRAPPER、仓库外独占TMPDIR，`make check`退出0：
fmt、workspace严格Clippy和Rust套件通过，CP基线746通过，C#3134通过/2跳过，
编译e2e471通过/285忽略。共享工作区生成器活跃性仍为test-only，另留WIP。
默认启用本次已证明的私有收集器直调，下一竖切评估布尔列表存储。
