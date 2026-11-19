import subject
assert list(subject.generate(ValueError)) == ["ready", "handled"]
print("instance generator remains guarded")
