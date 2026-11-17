# ADR-0238：精确本地键保留哈希热路径

- 日期：2026-11-17。
- 状态：已接受；隔离语义、NativeAOT对照与完整make check通过。
- 接续：[ADR-0232](0232-generic-native-interface-distribution.md)。

## 实测问题与决定

通用foreign精确标量键使Python与CLR标量可以双向覆盖，并保留第一次插入的原key身份。
但第五轮冻结Runtime的原生AOT六输入对照发现普通字典、集合、容器和推导式墙钟比
`178bbdae`增加30–48%。原始不利数据保留，普通本地键无需进入foreign事实识别分派。

comparer为精确CLR long/string/int/bool/double/BigInteger/None及精确PyTuple根布局
直接采用同一PyHash hash/equality。其余类型继续经原foreign事实接口；tuple派生类型
保留完整协议路径。该判定只描述实际布局，不改原key对象，不新增每实例字段或缓存，
不跳过foreign对象的存活、执行域、原码点与数值相等性检查。

## 证据与范围

macOS27.2.1 arm64/Apple M5、SDK11.0.100、固定CPython3.12.13。同一冻结CLI为
`5dcee07e`，候选Runtime仅替换PyHash的本地键分派；CPU独占采样，所有构建先结束。
每输入各预热一次，正式三轮CP/旧/新、旧/新/CP、新/CP/旧；逐样本核对完整三流。
墙钟与RSS来自无REPORT/STATS的新进程及wait4。环境查询在采样结束后单独补录。

19项hash/foreign/字典协议测试通过。两轮各十二个NativeAOT产品先删除输入和构建树、
中文路径搬迁、空PATH执行；比较前hello管线已通过。第一轮保留原回退，第二轮只对
冻结checkpoint与单变量候选比较。下表是第二轮三次中位数，RSS单位MiB。

| 输入 | CP ms | checkpoint ms | 候选 ms | 旧/新速度 | checkpoint RSS | 候选 RSS |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| hello | 8.471 | 4.864 | 4.985 | 0.976 | 13.531 | 13.531 |
| strings | 56.927 | 80.446 | 80.287 | 1.002 | 35.719 | 35.625 |
| dict_ops | 47.819 | 81.653 | 58.743 | 1.390 | 35.344 | 33.062 |
| set_tuple | 68.191 | 96.541 | 75.206 | 1.284 | 45.047 | 44.875 |
| containers | 34.639 | 37.564 | 20.968 | 1.792 | 43.000 | 40.312 |
| comprehensions | 41.865 | 107.545 | 73.408 | 1.465 | 36.344 | 38.562 |

推导式RSS增加2.219MiB，hello短进程约增加0.121ms；这些结果保留。第一轮字符串
相对178bbdae仍有约5.9%时间差，本次快路对字符串整体接近持平，继续独立定位。
本表仅覆盖六输入，完整26项与新C4权威namespace/carrier候选另行验收。

仓外证据目录：`generic-interfaces-20261117/runtime-pair-20261117T184609`与
`generic-interfaces-20261117/native-key-pair-20261117T185806`，位于
`~/.cache/ryupy/validation/`。首次preflight驱动把文本stderr交给bytes布局解析器，
在hello构建后停止且未采样；修正类型后重新开始两轮，原错误日志保留。

独立干净worktree的完整`make check`通过：固定CPython即时基线803项、Runtime 3446项
（3项旧skip）、编译e2e 514项（299项旧ignore），fmt/clippy及Rust测试全部通过。
本次提交只包含本地键分派与本ADR，后续C4源码环境改动独立验收。
