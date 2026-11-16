# 本切不改变的模块名称边界

两个pending保留Python合法成功期望，不把RyuPy当前错误改成参考答案，也不计通过率：

- `unselected_before_def.py.pending`：无其它本地模块绑定的单个普通def，定义执行前应未绑定。
- `external_only_rebinding.py.pending`：只在另一模块通过属性写入改绑；provider内原普通函数名不在本切选择集合。

源码核心逐字来自已审唯一CP参考的两个boundary组，只添加pending说明和独立执行所需的`print(result)`。
`binding_provider.py`与该次真实导入helper逐字一致，SHA256为
`57b096388cf919b5b9ed20570ee3a514766d8c898b3bc06b30a8ce18090321e8`。
不是sys.modules伪导入，也不是在RyuPy不支持处改参考期望。

两个`*.reference.json`是唯一CP原stdout中对应行的逐字归档，保留case/scope/result；
其中result是真实CP执行事实，不是另跑这两个新增standalone文件得到的stdout。
归档后未额外执行CP或RyuPy。未来验收仍应运行完整pending源并比较实际双流，不把这次结果转换当作fixture执行证据。

唯一参考源SHA256：`c6a480f9f1f1e8bb0fbc50fe0749554e4c7fde20321c9fbf2fc33d5e04d5d2ab`。
CPython 3.12.13，exit0、18组（16selected/2boundary），完整stdout SHA256：
`7e43d40e0e7631b5a0ab8d4d3597aa77163c896753f85dfc9ba7dd271c5dd18b`。
