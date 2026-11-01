import warnings
class CustomWarning(UserWarning):
    def __str__(self):
        print('render called')
        raise ValueError('bad string')
warnings.simplefilter('error')
warnings.warn('original', CustomWarning)
