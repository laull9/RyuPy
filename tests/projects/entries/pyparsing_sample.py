# 解析：pyparsing 的表达式与结果命名。
import pyparsing as pp

word = pp.Word(pp.alphas)
number = pp.Word(pp.nums).set_results_name("value")
expr = pp.Suppress("[") + pp.delimited_list(word) + pp.Suppress("]")

print(expr.parse_string("[alpha, beta]").as_list())
print((word + number).parse_string("x 42").as_dict())
