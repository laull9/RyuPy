import sys
b=sys.stdout.detach();sys.stdout=b
b.close()
for n,f in [('write',lambda:b.write(b'x')),('flush',b.flush),('readable',b.readable),('writable',b.writable),('seekable',b.seekable),('isatty',b.isatty),('name',lambda:b.name),('mode',lambda:b.mode)]:
 try: sys.stderr.write(n+' '+str(f())+'\n')
 except Exception as e: sys.stderr.write(n+' '+type(e).__name__+' '+str(e)+'\n')
