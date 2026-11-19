# 通用权威模块命名空间

该显式实验profile将本次本地源码图编译为CIL，模块存储统一采用provider实际字典。选择依据是整个源码图和能力字段，生产实现不识别第三方库名、模块名或业务符号。旧显式Instance、默认原生和薄函数路径保留原资格。

```bash
# 先由总控统一冻结Runtime/provider和CLI
python3.12 -B tests/cpython/stock_module_namespaces.py \
  --cli target/debug/ryupy --python /absolute/cpython3.12 --mode all
```

公开选择参数为 `--cpython-provider auto --experimental-stock-module-namespaces`。Runner 的 `--case` 可分别选择下述任一case；`--mode jit` / `standalone` 指定形态，默认两种。

每个产品先用固定CPython即时运行同一套手写多模块输入，再调用真实CLI。读取实际 `cpython-provider.json` 中 `dependency_collection.module_namespaces`，要求源码全部已编译、使用InstanceGlobals/provider_namespace，入口及本地模块没有业务源码委派。随后删除源码、venv和构建树，仅搬迁公开产品，在空PATH和失效Python环境下执行，严格比较退出码/stdout/stderr，并审计实际原生路径与Host关闭计数。日志保存在仓库外 `~/.cache/ryupy/validation/stock-module-namespaces`；临时产品自动清理。

`eager` 验证list/set/dict推导式的词法目标、最左iterable、过滤器、嵌套for、字典key/value顺序、错误身份，以及在模块 `__builtins__` 重绑/删除后继续使用当前frame的builtins快照。普通def仍按定义点捕获；另一个模块原地修改真实D，已保存函数和后续推导式必须读到新值。

`annotations` 验证SETUP_ANNOTATIONS、精确dict类型、右值→目标→注解→当前mapping写入的顺序、注解失败前的已提交绑定、本地注解不执行、属性/下标/括号名称，以及注解回调重绑 `__annotations__` 的晚读取。另一个模块读取这些结果；延迟注解沿既有规范字符串化合同单列验证。输入直接读写 `builtins.__import__` 并恢复原值：临时替换它与 `builtins.object` 后，无显式基类仍使用规范object，内部类桥不会触发业务导入钩子。

`harbor_pair` / `frost_pair` 使用不同名称及数据布局，覆盖真实module/sys.modules/D身份、保存的函数/闭包、外部重绑、builtin回退和动态异常目标。

`generators` 保留真实惰性genexpr与GeneratorFrame，覆盖yield/send/throw/close、finally、
yield-from的完整返回值与再次耗尽、定义点捕获builtins、外部重绑globals，以及源码中
同名callee的求值次数和结果跨暂停的对象身份。双版JIT/NativeAOT四产品通过，
3.12日志`20261117T115007.910822Z.json`，3.13日志`20261117T115742.698468Z.json`。
真实产品曾发现tp_iternext的布尔迭代接口吞掉终值，修复后原输入完整保留；
MIR另有真实恢复体与三个修改来源/槽位的拒绝测试。

`sunrise_context` / `quartz_context` 专门验收函数定义点的CAPI globals/builtins。输入包含定义前后字典替换/删除、字面量dict的既有桥接投影、嵌套定义、不同捕获的重入、异常同对象，以及显式Python函数A→CIL函数B→原生调用后恢复A现场。函数内部检查真实D中缺少 `__builtins__`，约束临时写回业务字典的实现。原始差分输入完整保留为 `sunrise_context/inherited_frame.py`。双版JIT/NativeAOT八产品通过，日志为 `20261117T121951.009514Z.json` 与 `20261117T122126.203157Z.json`。`quartz_context` 另用原版CPython的type/dict验证canonical、foreign和literal三类捕获的f_builtins精确dict类型；auto的Synchronized投影四产品通过，日志为 `20261117T122218.500702Z.json` 与 `20261117T122351.389323Z.json`。

尚未实现的NamedExpr保留完整输入 `tests/cases/stock_module_namespaces/pending/eager_walrus.py`，继续精确E0100。类体handler、PEP695、dataclass及既有类体推导式边界持续由相应预检/降级诊断保护。完整函数locals、逐语句原生frame行号等字段继续按独立frame合同推进。

`courier_hooks` / `marsh_hooks` 使用独立命名及平面/嵌套package布局，验证每条IMPORT_NAME
活读当前捕获builtins中的实际hook：模块传D/D，当前未物化locals的普通函数传D/None；
name/fromlist/level保原语法，返回值可以是任意对象。一次整批fromlist调用后按源码顺序
IMPORT_FROM并立即绑定，后项失败保留前缀；属性缺失才读同一sys.modules缓存，缓存None
与缺项分开，getter其它异常保身份。输入覆盖hook替换、删除/None、dotted-as遍历、相对
层级、错误name/path与副作用轨迹。双版JIT/NativeAOT八产品通过。class import的prepared
namespace、fromlist常量元组身份共享与3.13 shadowing错误提示细节仍按有限合同单列。

`inlet_handlers` / `quarry_handlers` 验证生成器源码动态handler：throw对象身份、暂停后
Name/tuple/属性目标重绑、as局部删除、getter异常越过同级、嵌套handler中的当前异常，
以及for/with的continue和close清理。双版JIT/NativeAOT八产品通过；MIR独立核验实际
物理catch→区间首次匹配→handler PC→Retry槽保存→恢复分派与异常槽读取/清除，含20组
破坏输入。旧Instance的生成器handler资格保持原诊断。详细决定见ADR-0237。

`synchronized_maps` 通过真实 `types.SimpleNamespace`、`operator` 与 `json` 跨域共享
字典，不引入业务委派。覆盖大字典不变边界、双向覆盖与删除重插、小字典中原键身份及
子容器别名，并保留缺键异常前已经写入的数据。降复杂度设计见
[ADR-0243](../../docs/adr/0243-linear-synchronized-dictionary-deltas.md)。
