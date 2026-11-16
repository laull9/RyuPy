import helpers
class Subject:
    @helpers.save
    def ident(self):
        return __class__
    helpers.maybe_fail()
