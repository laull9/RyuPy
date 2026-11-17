# ryupy: pending
# 独立多模块消费者：右值、目标、注解、mapping协议和失败后的可见值顺序共同验收。
import notes
import reader
import future_notes

print(notes.startup, notes.startup_exact, notes.initial_keys, notes.initial_identity)
print(reader.observe())
print(notes.failed_same, notes.local_result, notes.same_globals)
print(notes.events)
print(notes.implicit_original, notes.hidden_imports)
print(future_notes.__annotations__, future_notes.identity.__annotations__, future_notes.result, future_notes.events)
print('namespace-annotations-ok')
