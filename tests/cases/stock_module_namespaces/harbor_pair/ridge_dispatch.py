import harbor_owner as owner
def run(values):
    return owner.measure(values)
def catch(kind):
    try:
        owner.fail(kind)
    except (ValueError, owner.problem) as error:
        return type(error).__name__, str(error)
