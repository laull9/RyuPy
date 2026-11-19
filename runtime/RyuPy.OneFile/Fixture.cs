// 忽略式真实 NativeAOT 测试的内层程序；覆盖参数、三流、cwd、资源及非零退出。
using System.Text;
if (args.Length == 1 && args[0] == "--sleep")
{
    Console.Error.WriteLine(AppContext.BaseDirectory);
    Thread.Sleep(TimeSpan.FromSeconds(60));
    return 0;
}
Console.Error.WriteLine(AppContext.BaseDirectory);
Console.WriteLine(Directory.GetCurrentDirectory());
foreach (string arg in args) Console.WriteLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(arg)));
Console.WriteLine(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "资源.txt")));
Console.Write(Console.In.ReadToEnd());
return 37;
