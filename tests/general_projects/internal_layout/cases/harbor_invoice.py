"""无第三方依赖的命令行业务：数量与单价计算发票。"""
import sys

quantity = int(sys.argv[1])
unit_price = int(sys.argv[2])
print('harbor invoice', quantity, unit_price, quantity * unit_price)
