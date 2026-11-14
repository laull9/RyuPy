# 函数式 TypedDict：独立测同一个文件来源消费者，不被 warning 首错遮住。
print("T:entry-before")
import pkg
print("T:entry-after")
