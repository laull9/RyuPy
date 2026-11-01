import warnings
class CustomWarning(UserWarning):
    def __str__(self):
        print('render called')
        return 'rendered'
warnings.simplefilter('error')
warnings.warn('original', CustomWarning)
