# 四core实际JIT证据

Diffable文本保原样与SHA；等文本不等于证明真实地址布局/调度相同。默认tier可能产生不同晋级时序，不把某一Tier0当最终全部。

## fib / default
- 全文相等：False；code字节序列 B=['238', '398', '398', '533', '268']，C=['238', '398', '398', '533', '268', '301']
- baseline方法版本：
  - `; Assembly listing for method RyuPy.Generated.Module:m0_fib_rec(long):long (Tier0)`
  - `; Assembly listing for method RyuPy.Generated.Module:m0_fib_iter(long):long (Instrumented Tier0)`
  - `; Assembly listing for method RyuPy.Generated.Module:m0_fib_iter(long):long (Instrumented Tier0)`
  - `; Assembly listing for method RyuPy.Generated.Module:m0_fib_iter(long):long (Tier1)`
  - `; Assembly listing for method RyuPy.Generated.Module:m0_fib_rec(long):long (Instrumented Tier0)`
- candidate方法版本：
  - `; Assembly listing for method RyuPy.Generated.Module:m0_fib_rec(long):long (Tier0)`
  - `; Assembly listing for method RyuPy.Generated.Module:m0_fib_iter(long):long (Instrumented Tier0)`
  - `; Assembly listing for method RyuPy.Generated.Module:m0_fib_iter(long):long (Instrumented Tier0)`
  - `; Assembly listing for method RyuPy.Generated.Module:m0_fib_iter(long):long (Tier1)`
  - `; Assembly listing for method RyuPy.Generated.Module:m0_fib_rec(long):long (Instrumented Tier0)`
  - `; Assembly listing for method RyuPy.Generated.Module:m0_fib_rec(long):long (Tier1)`

## fib / disabled
- 全文相等：True；code字节序列 B=['155', '205']，C=['155', '205']
- baseline方法版本：
  - `; Assembly listing for method RyuPy.Generated.Module:m0_fib_rec(long):long (FullOpts)`
  - `; Assembly listing for method RyuPy.Generated.Module:m0_fib_iter(long):long (FullOpts)`
- candidate方法版本：
  - `; Assembly listing for method RyuPy.Generated.Module:m0_fib_rec(long):long (FullOpts)`
  - `; Assembly listing for method RyuPy.Generated.Module:m0_fib_iter(long):long (FullOpts)`

## mandelbrot / default
- 全文相等：False；code字节序列 B=['687', '696', '687', '629', '877']，C=['687', '696', '687', '629', '884']
- baseline方法版本：
  - `; Assembly listing for method RyuPy.Generated.Module:m0_mandel(double,double,long):long (Instrumented Tier0)`
  - `; Assembly listing for method RyuPy.Generated.Module:m0_mandel(double,double,long):long (Tier1-OSR)`
  - `; Assembly listing for method RyuPy.Generated.Module:m0_mandel(double,double,long):long (Instrumented Tier0)`
  - `; Assembly listing for method RyuPy.Generated.Module:m0_mandel(double,double,long):long (Tier1-OSR)`
  - `; Assembly listing for method RyuPy.Generated.Module:m0_mandel(double,double,long):long (Tier1)`
- candidate方法版本：
  - `; Assembly listing for method RyuPy.Generated.Module:m0_mandel(double,double,long):long (Instrumented Tier0)`
  - `; Assembly listing for method RyuPy.Generated.Module:m0_mandel(double,double,long):long (Tier1-OSR)`
  - `; Assembly listing for method RyuPy.Generated.Module:m0_mandel(double,double,long):long (Instrumented Tier0)`
  - `; Assembly listing for method RyuPy.Generated.Module:m0_mandel(double,double,long):long (Tier1-OSR)`
  - `; Assembly listing for method RyuPy.Generated.Module:m0_mandel(double,double,long):long (Tier1)`

## mandelbrot / disabled
- 全文相等：True；code字节序列 B=['370']，C=['370']
- baseline方法版本：
  - `; Assembly listing for method RyuPy.Generated.Module:m0_mandel(double,double,long):long (FullOpts)`
- candidate方法版本：
  - `; Assembly listing for method RyuPy.Generated.Module:m0_mandel(double,double,long):long (FullOpts)`

## matmul / default
- 全文相等：True；code字节序列 B=['1906', '4240', '47']，C=['1906', '4240', '47']
- baseline方法版本：
  - `; Assembly listing for method RyuPy.Generated.Module:m0_matmul(System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)`
  - `; Assembly listing for method RyuPy.Generated.Module:m0_matmul(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)`
  - `; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_matmul(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)`
- candidate方法版本：
  - `; Assembly listing for method RyuPy.Generated.Module:m0_matmul(System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)`
  - `; Assembly listing for method RyuPy.Generated.Module:m0_matmul(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)`
  - `; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_matmul(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)`

## matmul / disabled
- 全文相等：True；code字节序列 B=['2442', '47']，C=['2442', '47']
- baseline方法版本：
  - `; Assembly listing for method RyuPy.Generated.Module:m0_matmul(System.Object,System.Object,System.Object):System.Object (FullOpts)`
  - `; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_matmul(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)`
- candidate方法版本：
  - `; Assembly listing for method RyuPy.Generated.Module:m0_matmul(System.Object,System.Object,System.Object):System.Object (FullOpts)`
  - `; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_matmul(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)`

## nbody / default
- 全文相等：True；code字节序列 B=['5531', '8658', '47']，C=['5531', '8658', '47']
- baseline方法版本：
  - `; Assembly listing for method RyuPy.Generated.Module:m0_advance(System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)`
  - `; Assembly listing for method RyuPy.Generated.Module:m0_advance(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)`
  - `; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_advance(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)`
- candidate方法版本：
  - `; Assembly listing for method RyuPy.Generated.Module:m0_advance(System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)`
  - `; Assembly listing for method RyuPy.Generated.Module:m0_advance(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)`
  - `; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_advance(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)`

## nbody / disabled
- 全文相等：True；code字节序列 B=['8106', '47']，C=['8106', '47']
- baseline方法版本：
  - `; Assembly listing for method RyuPy.Generated.Module:m0_advance(System.Object,System.Object,System.Object):System.Object (FullOpts)`
  - `; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_advance(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)`
- candidate方法版本：
  - `; Assembly listing for method RyuPy.Generated.Module:m0_advance(System.Object,System.Object,System.Object):System.Object (FullOpts)`
  - `; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_advance(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)`

