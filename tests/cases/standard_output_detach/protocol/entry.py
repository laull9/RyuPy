import sys
old=sys.stdout
old.write('text:雪\n')
buffer=old.buffer
raw=old.detach()
sys.stdout=raw
sys.stderr.write('same-buffer '+str(raw is buffer)+'\n')
sys.stderr.write('detached-buffer '+str(old.buffer is None)+'\n')
for name in ['closed','name']:
    try: getattr(old,name)
    except ValueError as e: sys.stderr.write(name+': '+str(e)+'\n')
for name in ['flush','close','detach','isatty','readable','writable']:
    try: getattr(old,name)()
    except ValueError as e: sys.stderr.write(name+': '+str(e)+'\n')
try: old.write('bad')
except ValueError as e: sys.stderr.write('write: '+str(e)+'\n')
raw.write('binary:雪\n'.encode('utf-8'))
raw.write(b'zero\x00byte\n')
try: raw.write('bad')
except TypeError: sys.stderr.write('binary-rejects-text\n')
try: raw.write(buffer=b'bad')
except TypeError as e: sys.stderr.write('keyword: '+str(e)+'\n')
# no explicit flush: generated exit must preserve the binary stream
