"""反向导入编译中的应用；两域必须共享同一半初始化模块。"""
import __main__ as compiled_module

compiled_state = compiled_module.compiled_state
compiled_state.append('Python 反向导入')
