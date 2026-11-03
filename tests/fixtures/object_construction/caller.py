class Failure:
    def __new__(cls):
        raise ValueError('new-body')
def caller():
    Failure()
caller()
