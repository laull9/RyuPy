# 独立动态联合表示原型

仅验证CLR布局/数值子集/调用/容器成本，不是生产Python运行时。身份反例输出false是预期：
未实现共享物化/origin-ID，不能据吞吐将该语义上线。无引用/数值重叠、无unsafe代码。
详细界线见 docs/notes/2026-11-01-m3.10-dynamic-representation.md。

复现时把Program.cs、union.csproj和run.py复制到独立临时目录，然后：

```sh
dotnet build union.csproj -c Release -o out
dotnet out/union.dll check
python3 run.py
DOTNET_TieredCompilation=0 DOTNET_JitDisasm='Numeric:Call*' \
  DOTNET_JitStdOutFile="$PWD/jit-calls.txt" dotnet out/union.dll jit
DOTNET_TieredCompilation=0 DOTNET_JitDisasm='Work:Arithmetic*' \
  DOTNET_JitStdOutFile="$PWD/jit-arithmetic.txt" dotnet out/union.dll jit
```

run.py每种tier启动3进程，每场景3轮预热/7轮计时，每轮100万操作；验证每个返回值。
分配包含初始化和最终返回盒固定费用。所有时延原值保留在default/disabled原始jsonl与
results.json；summary.json是中位汇总，不删除慢样本。布局/GC/身份checks单独归档。
默认tier结果包含该预热协议下的PGO/tiering行为，不冒称都已最终稳定；GC根存活测试不等于
长期GC压力或弱缓存生命周期已验证。实验中object算术是对称自写数值子集，不是完整PyOps。

类型子集不表示全部组合都实现：仅检查long溢出升级BigInteger、bool+long和long+double；
boxed BigInteger+float未实现生产转换/溢出协议。浮点时延含分派/转换差异，不是布局单变量。
