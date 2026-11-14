# 入口由RyuPy编译；依赖资源、metadata、传递依赖和文件副作用由同次CPython比较。
import closure_alpha
print(closure_alpha.run())
