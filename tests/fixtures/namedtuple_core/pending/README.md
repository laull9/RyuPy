# 未纳本切的真实CP程序

这里每个`.py.pending`都是有实际断言/输出的Python程序，不是假的NI期望，也不自动注册进五应用。除name_intern_identity已作为唯一CP参考执行外，其余尚未执行；均不计RyuPy通过。后续运行窗口与源审另行取得。

- `tuple_index_bounds.py.pending`：普通tuple已有的index start/stop欠账，与namedtuple对照。
- `shared_call_and_index.py.pending`：重复/非字符串**键真实TypeError，以及自定义__index__成功；这些共享协议不在本切给namedtuple特判扩域。
- `copy_class_mutation.py.pending`：copy/deepcopy保类与引用，普通类属性/生成helper修改、用户再继承和dataclass对类的真实修改。首切RyuPy保护路径必须另写严格拒绝测试，不能把这些CP成功改为CP错误。
- `unicode_names.py.pending`：Unicode、NFKC字段/参数区别、合法str子类输入。NFKC关键字调用是根据固定源码形成的待验正例，不冒用原33参考当作已执行过该调用。
- `helper_duck_receivers.py.pending`：非生成类duck receiver的_asdict/__getnewargs__/_replace及普通tuple的未绑定repr；本切已把跨生成类helper纳入B4，非生成类边界继续明确未支持。
- `name_intern_identity.py.pending`：动态等值但非同对象的typename/field，经CP工厂跨类intern合并身份；保原is正例。实现选择schema内缓存，不引入CLR永久intern，此偏差不混入active等值/同类稳定metadata身份。

原33项参考中的str转换返回子类/intern顺序和复杂descriptor helper重绑证据仍保留于既有reference/source.py，不复制或重跑原脚本。当前生成类整体冻结、类__dict__读取NI、helper/field descriptor写NI、copy提前NI以及dataclass前置NI已由C#专项锁定；这些不等于相应CP合法程序通过，也不虚增源码差分应用数。
