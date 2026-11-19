body_dict = globals()
body_dict["body_written"] = 17
body_seen = body_written
def body_read():
    return globals()
