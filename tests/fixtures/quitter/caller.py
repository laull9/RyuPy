import sys
class BadInput:
    def close(self):
        print('FORBIDDEN CLOSE')
sys.stdin = BadInput()
def caller():
    exit(1, 2)
caller()
