
# 更新基础：外源流式、内对完整物化、getitem fallback、keys 获取失败。
def emit(k,v):print(k,v)
def attempt(f):
    try:return ['ok',f()]
    except Exception as e:return [type(e).__name__,str(e),None if e.__cause__ is None else type(e.__cause__).__name__,None if e.__context__ is None else [type(e.__context__).__name__,str(e.__context__)],e.__suppress_context__]
log=[]
class Outer:
    def __iter__(self):log.append('outer.iter');return self
    def __length_hint__(self):log.append('outer.hint');raise AssertionError('outer hint')
    def __next__(self):
        log.append('outer.next')
        if log.count('outer.next')==1:return ('a',1)
        raise StopIteration
x={};x.update(Outer());emit('outer-no-hint',[x,log])
class Pair:
    def __iter__(self):log.append('pair.iter');return self
    def __length_hint__(self):log.append('pair.hint');return 4
    def __next__(self):
        log.append('pair.next');i=log.count('pair.next')
        if i<=4:return i
        raise StopIteration
log.clear();x={};emit('pair-full-materialize',[attempt(lambda:x.update([('prefix',0),Pair()])),x,log])
class Sequence:
    def __getitem__(self,i):
        log.append(['sequence.get',i])
        if i==0:return ('a',1)
        if i==1:return ('b',2)
        raise IndexError
log.clear();x={};x.update(Sequence());emit('outer-getitem-fallback',[x,log])
class InnerSequence:
    def __getitem__(self,i):
        log.append(['inner.get',i])
        if i==0:return 'a'
        if i==1:return 1
        raise IndexError
log.clear();x={};x.update([InnerSequence()]);emit('inner-getitem-fallback',[x,log])
class MissingKeys(Sequence):
    @property
    def keys(self):log.append('keys.get');raise AttributeError('missing-keys')
log.clear();x={};x.update(MissingKeys());emit('keys-get-attributeerror',[x,log])
class BadKeys(Sequence):
    @property
    def keys(self):log.append('keys.get');raise RuntimeError('bad-keys')
log.clear();x={};emit('keys-get-other-error',[attempt(lambda:x.update(BadKeys())),x,log])
class CallMissing(Sequence):
    def keys(self):log.append('keys.call');raise AttributeError('keys-call-error')
log.clear();x={};emit('keys-call-attributeerror',[attempt(lambda:x.update(CallMissing())),x,log])
class NonCallable(Sequence):keys=None
log.clear();x={};emit('keys-noncallable',[attempt(lambda:x.update(NonCallable())),x,log])
class BadPair:
    def __iter__(self):raise TypeError('inner-iter-error')
x={};emit('pair-typeerror-rewrite',[attempt(lambda:x.update([('prefix',1),BadPair()])),x])
class OtherPair:
    def __iter__(self):raise RuntimeError('inner-runtime-error')
x={};emit('pair-other-error',[attempt(lambda:x.update([('prefix',1),OtherPair()])),x])
class KeysIterable:
    def __iter__(self):log.append('keys.iter');return self
    def __length_hint__(self):log.append('keys.hint');return 2
    def __next__(self):
        log.append('keys.next');n=log.count('keys.next')
        if n<3:return str(n)
        raise StopIteration
class Mapping:
    def keys(self):log.append('keys.call');return KeysIterable()
    def __getitem__(self,k):log.append(['mapping.get',k]);return int(k)
log.clear();x={};x.update(Mapping());emit('keys-materialization',[x,log])
class HintFails:
    def __iter__(self):return self
    def __length_hint__(self):raise TypeError('hint-failed')
    def __next__(self):raise AssertionError('must not consume')
x={};emit('pair-hint-typeerror',[attempt(lambda:x.update([('prefix',1),HintFails()])),x])
class NextFails:
    def __iter__(self):return self
    def __next__(self):raise TypeError('next-failed')
x={};emit('pair-next-typeerror',[attempt(lambda:x.update([('prefix',1),NextFails()])),x])
class LateKeys:
    def keys(self):return self
    def __iter__(self):return self
    def __next__(self):raise RuntimeError('keys-next-error')
    def __getitem__(self,k):raise AssertionError('must not get')
x={'old':0};emit('keys-failure-no-new-prefix',[attempt(lambda:x.update(LateKeys(),z=2)),x])

# 保存方法与构造器共用输入协议；保存方法必须读实时目标。
log.clear()
target = {'old': 0}
saved = target.update
target['live'] = 3
saved(Mapping(), tail=4)
emit('saved-mapping-keywords', [target, log])
log.clear()
emit('constructor-mapping-keywords', [dict(Mapping(), tail=4), log])
log.clear()
emit('constructor-pair-sequence', [dict(Sequence(), tail=4), log])
def fail_source():
    log.append('yield-prefix')
    yield ('prefix', 1)
    log.append('raise-source')
    raise ValueError('constructor-source-failed')
log.clear()
emit('constructor-source-failure', [attempt(lambda:dict(fail_source(), tail=4)), log])

# iter(user) adapter不得隐藏用户mapping协议优先级。
class UserIteratorMapping:
    def __iter__(self):
        log.append('adapter.iter')
        return self
    def __next__(self):
        raise AssertionError('must not consume adapter pairs')
    def keys(self):
        log.append('adapter.keys')
        return ['adapter']
    def __getitem__(self, key):
        log.append(['adapter.get', key])
        return 9
log.clear()
adapter_target = {}
adapter_target.update(iter(UserIteratorMapping()))
adapter_constructed = dict(iter(UserIteratorMapping()))
emit('user-iterator-mapping-priority', [adapter_target, adapter_constructed, log])
