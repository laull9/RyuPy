class Negative:
    def __len__(self):
        return -1
def convert():
    return bool(Negative())
try:
    raise TypeError('original')
except TypeError:
    convert()
