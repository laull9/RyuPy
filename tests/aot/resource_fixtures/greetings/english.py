import os.path


def greet():
    return 'hello'


def source_file():
    return __file__


def load_text():
    path = os.path.join(os.path.dirname(__file__), 'data.txt')
    with open(path, 'r', encoding='utf-8') as stream:
        return stream.read().strip()
