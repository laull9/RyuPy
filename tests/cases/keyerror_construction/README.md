# KeyError默认域构造候选：三图五角色

所有新case仍pending，尚未真实JIT/Native验收。CP29合同观察不替代这些产品。

- `01_direct/entry.py`：无全局KeyError绑定，默认argv测试完整位置参数、list/tuple/用户对象身份、延迟repr、用户str与原dict缺键。整数只断值/类型，不扩既有身份合同。
- 同一direct产物argv `uncaught`：stdout空，exit1，异常末行 `KeyError: 'missing'`。
- 同一direct产物argv `bad-repr`：stdout空，exit1，异常末行 `KeyError: <exception str() failed>`；实际traceback/cause/context按既有错误case规则核验，不只验末行。
- `02_shadow/entry.py`：local/free/global、保存别名、callee先于参数、不可调用值及旧删除回退。不得代替direct图。
- `03_default_owner/entry.py`：只把owner设为InstanceGlobals，consumer保持静态默认域；真实owner暴露共享默认字典验证重绑、缺失、None值、callee捕获，另保函数捕获custom字典与globals优先。禁止用import builtins假模块、C#直调或把consumer改instance凑绿。

新冷ABI通用按名字读取默认字典，但本切MIR仅接KeyError direct/value。静态自定义builtins完整owner仍旧pending；用户异常repr override、ForKey eager repr不在本切晋升。不变的原percent完整33704B三流、NI/capacity/type/value、旧消费者和公共ABI全部继续保护。

`raise KeyError`先读取值，沿既有NormalizeRaise；静态 `except KeyError` 仍可走canonical CLR类型匹配，原动态/InstanceGlobals异常链另走已有分派。本切不宣称完整动态异常匹配、用户异常基类重绑或全部builtin名称迁移。
