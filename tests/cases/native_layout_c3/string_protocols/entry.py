# dict 布局的保存 __str__、super 根描述符与常规 str/repr 必须一致。
class M: pass
class D(M, dict): pass
x = D(a=1)
for f in [x.__str__, super(M, x).__str__]:
    try:
        print(f())
    except Exception as e:
        print(type(e).__name__, str(e))
print(str(x), repr(x))
