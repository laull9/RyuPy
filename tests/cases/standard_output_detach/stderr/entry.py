import sys
old=sys.stderr
old.write('text-error:雪\n')
b=old.buffer
raw=old.detach()
sys.stderr=raw
print('stderr-identity',raw is b,old.buffer is None)
raw.write('binary-error:雪\n'.encode('utf-8'))
try: old.flush()
except ValueError as e: print(type(e).__name__,str(e))
