/* 同后缀普通共享库：导出本地C函数，但没有任何CPython模块初始化入口。 */
int ordinary_scale(int value) { return value * 7; }
