# ryupy: pending
# CP即时oracle；跨模块传递、交错恢复、溢出回退与普通消费不得误用其他实例的委托。
import producer
import consumer

left = producer.positive()
right = producer.negative()
print("partial", next(left), next(right))
print("drain", consumer.drain(right), consumer.drain(left))
print("exhausted", consumer.drain(left), consumer.drain(right))
print("ordinary", consumer.ordinary(producer.positive()))
print("big", consumer.drain(producer.overflowing()))
print("start-big", consumer.drain_with_start(producer.positive(), 9223372036854775805))
closed = producer.positive()
print("close", next(closed), closed.close(), consumer.drain(closed))
print("fresh", consumer.drain(producer.positive()), consumer.drain(producer.negative()))
