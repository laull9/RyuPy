"""不同入口与输入形态：读取两行标准输入并输出收据。"""
customer = input()
amount = int(input())
print('copper receipt', customer.upper(), amount + 7)
