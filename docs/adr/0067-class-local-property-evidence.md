# ADR-0067：类体方法名与 property 的既有能力补证

状态：固定CP参考及一项真实CP/JIT差分通过；仅测试和文档，未发布。fmt及workspace全target严格clippy通过。

## 目的与已有路线

TODO及semantics #45仍称类体`v = property(getv, setv)`报E0106。源码已有完整路线：
类体按源序把完成的FunctionDef加入ClassScope，已绑定名字降为ClassLocal，MIR发
ClassNamespaceGet，运行时从类的own Attributes读原值，不提前绑定descriptor。
原m2_12_identity_sort_cache中的getter/setter定义在模块级，不能拿其旧通过证明类内行为。
本轮因此先验证原实现，而不是为过期条目新增生产修复。

## 有限验收

新增class_local_property一个真实应用，同一109行source先固定CPython3.12.13，再编译/JIT，
退出码及stdout/stderr逐字相等。另一次有界CP参考也执行同源，28秒TERM加2秒强制上限，
exit0、空stderr，无超时。两者不混作项目矩阵或性能测量。

断言覆盖同类getter/setter供property实读写、raw函数及property别名身份、定义前后模块名
与own名字遮蔽、defaults/decorator读当时原值、条件未绑定时回退模块、方法及其内部闭包
仍读模块late名字，以及property getter的super owner。类定义阶段没有误执行getter/setter。

不覆盖任意元类/descriptor、class-body del、嵌套类、类失败对外发布或完整动态LOAD_NAME。
不能从这份成功例推导普通ClassId早发布等独立边界已修。

## 证据与成本

参考/fixture SHA256：1fb1ce08d6288a1613ac78341119b2a0f00a364086740e3d5e01b6f0ffed8ba3。
参考stdout SHA256：2701ede0a96556b6d8c2716f37622799881f689304817908a58eb92d0eac3de8。
driver首次1/1、零ignored；日志SHA256：5d3852b611437eb5514a7ba522f20c6ebf09365f7b9cc85b0931048b95fe0558。

生产代码、ABI及工作流均无变化，本次不会新增运行时操作或分配；没有性能测量或提速声明。
基础1b1fc96已有完整门禁802C#/228CP/202e2e+30ignored；本test-only批次只补专用差分、
fmt与严格clippy，不声称在新提交重新运行全部门禁。未重跑pyfiglet、AOT或性能组，历史5/20不变。
