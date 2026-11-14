# 同一异常图的四个argv：NI/宿主容量是产品合同，type/value是未捕获异常的CP对照。
# NI和capacity绝不送CP采黄金；capacity依已静态确认的转换后、Append前容量拒绝。
import sys

mode = sys.argv[1]


class TraceNumber:
    def __int__(self):
        if mode == 'type':
            raise TypeError('original-type')
        raise ValueError('original-value')


class Untouched:
    def __int__(self):
        raise AssertionError('unsupported conversion must not call int')


class BoolInt:
    def __int__(self):
        return True


class CapacityNumber:
    def __int__(self):
        print('capacity-int')
        return 12


class CapacityFailure:
    def __int__(self):
        print('capacity-value-int')
        raise ValueError('conversion first')


if mode == 'type' or mode == 'value':
    print('%+06d' % TraceNumber())
elif mode == 'ni':
    for specification in ['%5s', '%0s', '%04r', '%04a', '%.3d', '%04.3d', '%*d', '%.*d', '%04x', '%04f', '%04c', '%04ld']:
        try:
            print(specification % Untouched())
            raise AssertionError('expected unsupported specification')
        except NotImplementedError:
            print('NI', specification)
    try:
        print('%+04d' % BoolInt())
        raise AssertionError('expected unsupported warning contract')
    except NotImplementedError:
        print('NI bool-int-warning')
    print('decimal-ni-done')
elif mode == 'capacity':
    try:
        print('😀%9223372036854775807d' % CapacityNumber())
        raise AssertionError('expected managed capacity rejection')
    except NotImplementedError as error:
        assert str(error) == 'str percent formatting: result exceeds managed string capacity'
        print('HOST_CAPACITY_NI')
    try:
        print('x%9223372036854775807d' % CapacityFailure())
        raise AssertionError('expected conversion failure before capacity rejection')
    except ValueError as error:
        assert str(error) == 'conversion first'
        print('CONVERSION_FIRST')
    print('decimal-capacity-done')
else:
    raise AssertionError('unknown decimal exception mode')
