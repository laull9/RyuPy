# 普通观察器先行；同一完整程序用于Legacy JIT和Native交付。
import observer
import delayed
import cleanup
import context
import protocol
import targets
import workloads

observer.run()
delayed.run()
cleanup.run()
context.run()
protocol.run()
targets.run()
workloads.run()
