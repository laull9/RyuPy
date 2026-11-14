# Legacy生成器动态异常链合同（候选）

全部case仍pending；CP3.12.13前置已实际运行，RyuPy编译/JIT/Native待hosted验证。
`default/observer_control.py`先独立验证普通函数观察器；`default/entry.py`执行六组完整
通用合同，使用参数Name或参数对象属性取得动态异常类型，不读取sys.exception、不动态造类。
观察器真实重抛，仅比较异常身份/上下文，不声称原traceback等价。

- delayed：无异常不读取目标、暂停后重绑、后项短路与send改局部目标。
- cleanup：as保存/清理、normal/throw/close、第二handler selected=1且未命中名字保留、
  pending return穿过finally内yield，以及break/continue。
- context：getter阶段活异常、getter失败跳过同级捕获、双生成器与caller身份隔离。
- protocol：yield-from转发throw/return、close和PEP479的cause/context身份。
- targets：合法/非法类型及tuple、空tuple、bare与目标失败相位。
- workloads：click和networkx真实源控制流形状的合成合同，不冒termios/Graph或完整项目运行。

`frames`独立不调用observer，保未捕获getter错误链与源行；`instance_negative`是纯函数
参数Name的显式Instance程序，必须明确命中生成器动态except守卫，不用class/del提前失败
冒证明该边界。CP合法不代表Instance正向支持。

普通控制、聚合Legacy、精确Instance负例和无observer错误帧分别验收；新Native使用同一
完整default程序，不修改七项旧Native门或默认Instance集合。固定20入口仍另跑，10个既有
通过项目不得回退；本切没有吞吐或历史结构相等承诺。
