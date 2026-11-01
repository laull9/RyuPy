// 独立诊断：对象构造分配量、纯属性访问与动态算术分开，不替换项目基准。
using RyuPy.Runtime;
using System.Diagnostics;
var cls = new PyClass("Record");
PyOps.DeclareDataclass(cls, ["x", "y"], false);
PyOps.ApplyDataclass(cls, false);
object?[] shared = [3L, 4L];
var value = cls.Invoke(shared, [], []);
object? sink = null;
void Measure(string name, int count, Action body) {
 for(int i=0;i<10000;i++) body();
 GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
 long alloc=GC.GetAllocatedBytesForCurrentThread(); var sw=Stopwatch.StartNew();
 for(int i=0;i<count;i++) body();
 sw.Stop(); alloc=GC.GetAllocatedBytesForCurrentThread()-alloc;
 Console.WriteLine($"{name} ns={sw.Elapsed.TotalNanoseconds/count:F2} bytes={((double)alloc/count):F2}");
 GC.KeepAlive(sink);
}
Measure("construct_prepared",500000,()=>sink=cls.Invoke(shared,[],[]));
Measure("construct_args",500000,()=>sink=cls.Invoke([3L,4L],[],[]));
Measure("array_reads",2000000,()=>{sink=shared[0];sink=shared[1];});
Measure("pic_reads",2000000,()=>{sink=PyPic.GetAttr(3000,value,"x");sink=PyPic.GetAttr(3001,value,"y");});
Measure("generic_reads",2000000,()=>{sink=PyObject.GetAttr(value,"x");sink=PyObject.GetAttr(value,"y");});
object total = 0L;
Measure("numeric_only",2000000,()=>{total=PyPic.Binary(3002,PyOps.Add,total,PyPic.Binary(3003,PyOps.Add,shared[0]!,shared[1]!));sink=total;});
total=0L;
Measure("pic_read_sum",2000000,()=>{total=PyPic.Binary(3002,PyOps.Add,total,PyPic.Binary(3003,PyOps.Add,PyPic.GetAttr(3000,value,"x")!,PyPic.GetAttr(3001,value,"y")!));sink=total;});
if(args.Length>0) {
 var asm=System.Reflection.Assembly.LoadFrom(args[0]);
 asm.EntryPoint!.Invoke(null, asm.EntryPoint.GetParameters().Length==0 ? null : [Array.Empty<string>()]);
 var method=asm.GetTypes().SelectMany(t=>t.GetMethods(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)).First(m=>m.Name=="m0_field_reads");
 object?[] invokeArgs=[200000L,value];
 for(int i=0;i<3;i++)method.Invoke(null,invokeArgs);
 long bytes=GC.GetAllocatedBytesForCurrentThread();var sw=Stopwatch.StartNew();
 for(int i=0;i<5;i++)sink=method.Invoke(null,invokeArgs);
 sw.Stop();bytes=GC.GetAllocatedBytesForCurrentThread()-bytes;
 Console.WriteLine($"compiled_field_reads ns={sw.Elapsed.TotalNanoseconds/1000000:F2} bytes={bytes/1000000.0:F2} result={sink}");
}
