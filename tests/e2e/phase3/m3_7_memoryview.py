# 真实一维 B/c buffer 视图，不能复制数据假装共享，也不能把 c 迭代当成原始 buffer。
data = bytearray(b'abcd')
root = memoryview(data)
chars = root.cast('c')
part = chars[1:3]
print(type(root) is memoryview, isinstance(root, memoryview))
print(root.format, chars.format, root.itemsize, root.nbytes, root.readonly, root.obj is data)
print(list(root), list(chars), list(part))
data[1] = 88
print(list(part))
root[2] = 89
chars[3] = b'Z'
print(data)
out = bytearray()
out.extend(part)
print(out, part.tobytes())
try:
    out.extend(list(part))
except TypeError:
    print('c 迭代不是 buffer')
try:
    data.append(1)
except BufferError:
    print('禁止改变共享长度')
root.release()
chars.release()
print(list(part))
try:
    data.extend(b'!')
except BufferError:
    print('子视图仍有租约')
part.release()
part.release()
data.append(1)
print(data)
for action in ['len', 'item', 'cast', 'bytes']:
    try:
        if action == 'len':
            len(root)
        elif action == 'item':
            root[0]
        elif action == 'cast':
            root.cast('B')
        else:
            root.tobytes()
    except ValueError:
        print('已释放', action)
with memoryview(data) as scoped:
    scoped[0] = 81
print(data)
try:
    len(scoped)
except ValueError:
    print('with 已释放')
immutable = memoryview(b'ab')
print(immutable.readonly, immutable.obj == b'ab', immutable.tobytes())
try:
    immutable[0] = 1
except TypeError:
    print('只读写入拒绝')
immutable.release()

# resize先验证/消费输入，失败不能提前吞掉用户迭代副作用。
locked = bytearray(b'a')
lock = memoryview(locked)
def incoming():
    print('输入一')
    yield 65
    print('输入二')
    yield 66
try:
    locked.extend(incoming())
except BufferError:
    print('消费后拒绝resize', locked)
try:
    locked.extend([999])
except ValueError:
    print('元素错误优先')
locked.extend(b'')
locked += b''
locked *= 1
print('空操作', locked)
try:
    locked += locked
except BufferError:
    print('自增受锁')
try:
    locked *= 0
except BufferError:
    print('重复清空受锁')
try:
    del locked[99]
except IndexError:
    print('下标错误优先')
del locked[1:1]
lock.release()
# 待gate完成后追加到memoryview pending，避免正在枚举的基线文件变化。
keyword_view = memoryview(object=b'ab')
keyword_chars = keyword_view.cast(format='c')
print(keyword_chars.tolist())
print(keyword_view.tobytes(order='C'), keyword_view.tobytes('F'), keyword_view.tobytes('A'), keyword_view.tobytes(None))
try:
    keyword_view.cast('B', None)
except TypeError:
    print('显式None不是缺省shape')
keyword_view.release()
for method in ['cast', 'tobytes']:
    try:
        if method == 'cast':
            keyword_view.cast(1)
        else:
            keyword_view.tobytes(1)
    except TypeError:
        print('参数类型先于released', method)
try:
    keyword_view.cast('B', None)
except ValueError:
    print('released先于shape验证')
keyword_chars.release()
hashed = memoryview(b'ab')
saved_hash = hash(hashed)
hashed.release()
print('hash已缓存', hash(hashed) == saved_hash)
unhashed = memoryview(b'ab')
unhashed.release()
try:
    hash(unhashed)
except ValueError:
    print('hash未缓存检查released')
# 非法元素之后的next不得发生；有效输入仍在BufferError之前完整消费。
sequence = bytearray(b'a')
export = memoryview(sequence)
def invalid_stream():
    print('无效元素之前')
    yield 999
    print('错误：不该继续消费')
try:
    sequence.extend(invalid_stream())
except ValueError:
    print('立即校验当前元素')
class ByteIndex:
    def __index__(self):
        print('byte index')
        return 66
try:
    sequence.append(ByteIndex())
except BufferError:
    print('转换后才查导出锁')
export.release()
class EqualOther:
    def __eq__(self, other):
        print('右反射比较')
        return True
comparison = memoryview(b'ab')
print(comparison == EqualOther())
comparison.release()
print(comparison == EqualOther())

class FailingByteIndex:
    def __index__(self):
        raise OverflowError('用户索引异常')
error_data = bytearray(b'a')
error_view = memoryview(error_data)
try:
    error_data.append(FailingByteIndex())
except OverflowError as error:
    print(str(error))
try:
    error_view[0] = FailingByteIndex()
except ValueError as error:
    print(str(error))
error_view.release()

class ReleaseKey:
    def __index__(self):
        active_view.release()
        del active_buffer[:]
        return requested_index
class ValueAfterKey:
    def __index__(self):
        print('key后仍转换value')
        return 65
class ReleaseValue:
    def __index__(self):
        active_view.release()
        del active_buffer[:]
        return 65
for release_mode in ['key', 'value', 'get_outside', 'set_outside']:
    active_buffer = bytearray(b'a')
    active_view = memoryview(active_buffer)
    requested_index = 0
    if release_mode in ['get_outside', 'set_outside']:
        requested_index = 999
    try:
        if release_mode == 'key':
            active_view[ReleaseKey()] = ValueAfterKey()
        elif release_mode == 'value':
            active_view[0] = ReleaseValue()
        elif release_mode == 'get_outside':
            active_view[ReleaseKey()]
        else:
            active_view[ReleaseKey()] = 65
    except (ValueError, IndexError) as error:
        print(release_mode, type(error).__name__, active_buffer)

# 切片转换临时持租约：原视图释放后仍可生成独立子视图，resize仍被阻止。
class SliceRelease:
    def __index__(self):
        slice_parent.release()
        if resize_in_slice:
            del slice_buffer[:]
        return 0
for resize_in_slice in [False, True]:
    slice_buffer = bytearray(b'ab')
    slice_parent = memoryview(slice_buffer)
    try:
        slice_child = slice_parent[SliceRelease():]
        print('切片释放后仍建子视图', slice_child.tobytes())
        slice_child.release()
    except BufferError:
        print('切片临时租约', slice_buffer)
    slice_buffer.append(99)
    print('切片临时租约已释放', slice_buffer)

# 子视图独立存活时，父的整数协议释放不得借旧owner写入。
child_buffer = bytearray(b'a')
child_parent = memoryview(child_buffer)
child_live = child_parent[:]
class ReleaseParentOnly:
    def __index__(self):
        child_parent.release()
        return 0
try:
    child_parent[ReleaseParentOnly()] = 66
except ValueError:
    print('子视图保持原字节', child_live.tobytes())
child_live.release()

# 切片大整数裁剪与step0必须先于start回调，异常后临时租约不得残留。
bounds_buffer = bytearray(b'ab')
bounds_view = memoryview(bounds_buffer)
print(bounds_view[-(2**100):2**100].tobytes())
class UnexpectedSliceStart:
    def __index__(self):
        print('不应调用start')
        return 0
try:
    bounds_view[UnexpectedSliceStart()::0]
except ValueError as error:
    print(str(error))
bounds_view.release()
# 上面的临时子视图按D001可能晚回收，因此不借其立即析构测试resize。

class ConversionFailure:
    def __index__(self):
        if failure_kind == 'type':
            raise TypeError('inner type')
        elif failure_kind == 'value':
            raise ValueError('inner value')
        else:
            raise RuntimeError('inner runtime')
for failure_kind in ['type', 'value', 'runtime']:
    failure_view = memoryview(bytearray(b'a'))
    try:
        failure_view[0] = ConversionFailure()
    except Exception as error:
        print(type(error).__name__, str(error), error.__context__)
    failure_view.release()
