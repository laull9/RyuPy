import frost_state as state
def read(reader):
    return reader()
def match(kind):
    try:
        raise kind('frost')
    except state.problem as error:
        return 'attribute', type(error).__name__, str(error)
    except (TypeError, ValueError) as error:
        return 'tuple', type(error).__name__, str(error)
