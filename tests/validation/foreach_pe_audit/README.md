# ForEach真实PE独立审计

本工具为本次重建新写，不使用历史审计脚本或历史结果。只用.NET 10标准库和Python标准库，不加载或执行被审计程序集，不依赖MIR/CIL文本dump来决定方法等价。

## 运行

先激活拥有.NET 10和Python 3的构建环境，再执行：

```sh
dotnet build tests/validation/foreach_pe_audit/PeAudit.csproj -v:q
python3 tests/validation/foreach_pe_audit/run_suite.py <本轮fixture根目录> <全新报告目录>
```

单个off/on PE也可独立运行：

```sh
python3 tests/validation/foreach_pe_audit/audit.py off/foreach_fixture.dll on/foreach_fixture.dll --output <报告目录>
```

批量入口要求每case包含source、off/on/foreach_fixture.dll。planner_static为同源独立off/on编译的typed控制，必须linked.cil逐字节一致；empty_seed_identity用于负控制。缺PE的case明确列为跳过，不能算通过。报告目录不得存在，避免混轮覆盖。

## 证明内容

- 遍历真实PE全部MethodDef，保留每条IL原字节和操作数，解析成员身份和完整方法签名、locals、EH、MaxStack、InitLocals及方法属性。分支指向实际指令边界，浮点常量保留精确位模式。
- 用求值栈来源确定PyPic调用的首实参，只允许该确证位置的整数作为site ID。按全PE方法/指令顺序要求ID从0连续递增、唯一。新增holder恰好删去对应Binary的site，其后编号严格按删除集合映射，不归一化普通整数。
- 新增命中必须恰有Create/Add/Value各一个，固定原ABI；locals仅尾增一个System.Object。由PE控制流独立检查P/H/B/X：P末Create紧邻goto，H为唯一Next及两分支，B首为同迭代器Current，Add后唯一回边，X首Value回写total，单返回，无EH。
- 原Binary必须是固定ABI、操作码257（Add|inplace），精确的total及相同rhs求值片段，后面必须是tmp写入、tmp读取、total回写。只恢复这三处，删除段内部不允许分支目标，不允许holder额外使用。
- 恢复后逐方法比较完整标准化IL和EH。只统一等价短/长分支、整数及局部槽编码，成员元数据token解析为精确身份，分支字节偏移解析为恢复后的指令序号。其余所有普通常量、调用、Line、global/callee/RHS求值及次序均参与比较。其它方法也必须一致，只允许上述已证明的PIC重编号。
- typed控制除同样的完整PE方法检查外，另要求linked.cil文件逐字节一致，并记录源码和CIL SHA256。

## 工具自身控制

每轮先验证off/off与on/on正控制，再在真实on PE中定长改写三个位置并重新解码：普通SetLine常量、命中函数的Current调用目标、PIC ID。三者都必须被拒绝；保存变异PE、修改文件偏移、原/新字节及拒绝原因。变异PE不执行。

## 边界

这是已提供PE对的结构等价证明，不是全部Python程序的语义证明，也不是性能或traceback/frame可见性验收。缺PE、前端拒绝、运行时已知语义偏差仍需由fixture原流报告说明。编译器Session/feature/planner开关的源码与测试审计、冻结runtime一致性及CPython/off/on执行原流由上层验收负责，本工具不拿函数计数代替IL证明。

本工具不声称整个PE文件字节一致：元数据token位置和分支编码可合法变化；检查范围为全部方法签名、体、locals/EH和列出的属性，不是程序集自定义属性或全部非方法元数据。求值栈来源分析专门服务本发射器已验证的顺序IL，不是通用CLR验证器；不支持的新模式应失败而非宽松跳过。
