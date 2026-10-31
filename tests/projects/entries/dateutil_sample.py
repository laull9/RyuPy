# 日期：dateutil 的宽松解析与月份偏移。
from dateutil import parser
from dateutil.relativedelta import relativedelta

moment = parser.parse("2020-01-31T10:20:30")
print(moment.isoformat())
print((moment + relativedelta(months=1)).date())
print(parser.parse("2020/02/03").month)
