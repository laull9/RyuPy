# 同一文件按两个完全限定名字导入时是两个独立实例。
import mc_state
mc_state.runs += 1
value = mc_state.runs
mc_state.events.append('child:' + __name__)
