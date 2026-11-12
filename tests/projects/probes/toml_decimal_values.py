# 真toml执行Decimal值编码，不以空import或未触及Decimal的入口计验收。
from decimal import Decimal
import toml
from toml.encoder import TomlEncoder
values = {'exact': Decimal('0.1250'), 'big': Decimal('123456789012345678901234567890.125'),
          'negative_zero': Decimal('-0.00'), 'scientific': Decimal('1E-7')}
encoder = TomlEncoder()
print(Decimal in encoder.dump_funcs, type(values['exact']) is Decimal)
for name, value in values.items():
    print(name, encoder.dump_value(value))
encoded = toml.dumps(values)
print(encoded, end='')
print(toml.loads(encoded)['exact'] == 0.125)
