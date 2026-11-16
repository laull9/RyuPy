from importlib.resources import files
import left
import right

for package in (left, right):
    original = package.__file__
    root = files(package)
    print(package.label, root.joinpath('data.txt').read_text(encoding='utf-8').strip())
    package.__file__ = None
    print('none', files(package).joinpath('data.txt').read_text(encoding='utf-8').strip())
    token = []
    package.__file__ = token
    print('shared', package.__file__ is token, files(package).joinpath('data.txt').read_text(encoding='utf-8').strip())
    package.__file__ = original
    print('restored', package.__file__ == original, files(package.label).joinpath('data.txt').read_text(encoding='utf-8').strip())
