"""同步字典在原生调用边界保留键顺序、值身份、别名及异常前副作用。"""
from mapping_contract import ordered_mutations, shared_values

ordered_mutations()
shared_values()
print("同步字典身份与顺序通过")
