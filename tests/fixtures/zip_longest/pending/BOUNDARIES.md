# 不计本切通过的边界

本目录源码只有安全、有限、真实CP正例，未注册到三个driver应用，也未由此worker执行。

- descriptor_fallback：唯一参考已确认CP任意descriptor取iter失败可回退getitem；本切
  runtime仍将任意descriptor列明确NI。保存成功正例，不能用NI替换CP期待。
- state_pickle：CP3.12的reduce/setstate/pickle仍可用且产生弃用警告；本切未开放反射状态。
  改fill仅影响未来行，既有行保旧引用；pickle是独立能力，不能据这些草稿声称支持。
- subclass_slots：真实CP原生类型子类、实例字典与类侧slot descriptor调用，未纳本切布局。

危险深重入不提供可被普通baseline误执行的程序。递归next期间耗尽/异常更改共享slot、
外层随后继续的组合尚未测，可能触CP内部边界；本切仅验证一次flag且内层均有值的安全
重入。没有CP成功或失败oracle，不假造expected异常，也不把它算作已验收pending程序。

无外部引用时CP tuple复用、即时析构/回调重入与refcount路径也不作CLR兼容承诺；已保存
行的普通tuple内容、元素身份和不同留存行独立性仍属于active真实合同。
