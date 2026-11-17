# 通用权威模块命名空间

该显式实验profile将本次本地源码图编译为CIL，模块存储统一采用provider实际字典。选择依据是整个源码图和能力字段，生产实现不识别第三方库名、模块名或业务符号。旧显式Instance、默认原生和薄函数路径保留原资格。

```bash
# 先由总控统一冻结Runtime/provider和CLI
python3.12 -B tests/cpython/stock_module_namespaces.py \
  --cli target/debug/ryupy --python /absolute/cpython3.12 --mode all
```

公开选择参数为 `--cpython-provider auto --experimental-stock-module-namespaces`。Runner 的 `--case eager`、`--case annotations`、`--case harbor_pair`、`--case frost_pair`、`--case generators` 可分别验证；`--mode jit` / `standalone` 指定形态，默认两种。

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

尚未实现的NamedExpr保留完整输入 `tests/cases/stock_module_namespaces/pending/eager_walrus.py`，继续精确E0100。`pending/eager_capi_builtins.py` 保留完整CAPI frame的 `f_builtins` 差分：模块急切推导式在字典重绑/删除后保留原快照；普通函数在定义点捕获新字典后，候选CAPI现场仍沿外层模块字典，双版即时CP期望True、候选False。普通CIL名称查找快照的正例沿 `eager` 独立验收。同步生成器及生成器表达式由 `generators` 场景单独验收；生成器/类体handler、PEP695、dataclass及既有类体推导式边界持续由相应预检/降级诊断保护。
