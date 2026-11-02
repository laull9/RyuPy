"""Windows 独占 Job：主线程挂起→加入 Job→恢复；不允许未受控 SDK 执行。"""
import ctypes
from ctypes import wintypes as w
import subprocess


class BasicLimits(ctypes.Structure):
    _fields_ = [("ProcessTime", ctypes.c_int64), ("JobTime", ctypes.c_int64),
                ("Flags", w.DWORD), ("MinWorkingSet", ctypes.c_size_t),
                ("MaxWorkingSet", ctypes.c_size_t), ("ActiveLimit", w.DWORD),
                ("Affinity", ctypes.c_size_t), ("Priority", w.DWORD),
                ("Scheduling", w.DWORD)]


class IoCounters(ctypes.Structure):
    _fields_ = [(name, ctypes.c_uint64) for name in
                ("ReadOps", "WriteOps", "OtherOps", "ReadBytes", "WriteBytes", "OtherBytes")]


class ExtendedLimits(ctypes.Structure):
    _fields_ = [("Basic", BasicLimits), ("Io", IoCounters),
                ("ProcessMemory", ctypes.c_size_t), ("JobMemory", ctypes.c_size_t),
                ("PeakProcessMemory", ctypes.c_size_t), ("PeakJobMemory", ctypes.c_size_t)]


class ThreadEntry(ctypes.Structure):
    _fields_ = [("Size", w.DWORD), ("Usage", w.DWORD), ("Id", w.DWORD),
                ("Owner", w.DWORD), ("BasePriority", w.LONG),
                ("DeltaPriority", w.LONG), ("Flags", w.DWORD)]


def api():
    kernel = ctypes.WinDLL("kernel32", use_last_error=True)
    signatures = {
        "CreateJobObjectW": ([ctypes.c_void_p, w.LPCWSTR], w.HANDLE),
        "SetInformationJobObject": ([w.HANDLE, ctypes.c_int, ctypes.c_void_p, w.DWORD], w.BOOL),
        "AssignProcessToJobObject": ([w.HANDLE, w.HANDLE], w.BOOL),
        "TerminateJobObject": ([w.HANDLE, w.UINT], w.BOOL),
        "CloseHandle": ([w.HANDLE], w.BOOL),
        "CreateToolhelp32Snapshot": ([w.DWORD, w.DWORD], w.HANDLE),
        "Thread32First": ([w.HANDLE, ctypes.POINTER(ThreadEntry)], w.BOOL),
        "Thread32Next": ([w.HANDLE, ctypes.POINTER(ThreadEntry)], w.BOOL),
        "OpenThread": ([w.DWORD, w.BOOL, w.DWORD], w.HANDLE),
        "ResumeThread": ([w.HANDLE], w.DWORD),
        "OpenProcess": ([w.DWORD, w.BOOL, w.DWORD], w.HANDLE),
        "WaitForSingleObject": ([w.HANDLE, w.DWORD], w.DWORD),
    }
    for name, (args, result) in signatures.items():
        function = getattr(kernel, name)
        function.argtypes, function.restype = args, result
    return kernel


class Job:
    def __init__(self):
        self.kernel = api()
        self.handle = self.kernel.CreateJobObjectW(None, None)
        if not self.handle:
            raise ctypes.WinError(ctypes.get_last_error())
        limits = ExtendedLimits()
        limits.Basic.Flags = 0x2000  # KILL_ON_JOB_CLOSE；不允许 BREAKAWAY。
        if not self.kernel.SetInformationJobObject(self.handle, 9, ctypes.byref(limits), ctypes.sizeof(limits)):
            error = ctypes.WinError(ctypes.get_last_error())
            self.close()
            raise error

    def start(self, command, *, cwd, env):
        process = None
        try:
            process = subprocess.Popen(command, cwd=cwd, env=env,
                stdout=subprocess.PIPE, stderr=subprocess.PIPE, creationflags=0x4)  # CREATE_SUSPENDED
            # CPython 3.12 Popen 持有真实进程handle；初始线程在恢复前不能创建子进程。
            if not self.kernel.AssignProcessToJobObject(self.handle, int(process._handle)):
                raise ctypes.WinError(ctypes.get_last_error())
            self._resume_initial_thread(process.pid)
            return process
        except BaseException:
            try:
                if process is not None and process.poll() is None:
                    process.kill()  # 归属失败时也回收仍挂起的本次根进程。
            finally:
                # 恢复异常时线程可能已执行；先关Job杀子孙，再等待管道EOF。
                self.close()
                if process is not None:
                    process.communicate()
            raise

    def _resume_initial_thread(self, pid):
        snapshot = self.kernel.CreateToolhelp32Snapshot(0x4, 0)  # SNAPTHREAD
        if snapshot == ctypes.c_void_p(-1).value:
            raise ctypes.WinError(ctypes.get_last_error())
        ids = []
        try:
            entry = ThreadEntry()
            entry.Size = ctypes.sizeof(entry)
            ok = self.kernel.Thread32First(snapshot, ctypes.byref(entry))
            while ok:
                if entry.Owner == pid:
                    ids.append(entry.Id)
                entry.Size = ctypes.sizeof(entry)
                ok = self.kernel.Thread32Next(snapshot, ctypes.byref(entry))
            if ctypes.get_last_error() != 18:  # ERROR_NO_MORE_FILES
                raise ctypes.WinError(ctypes.get_last_error())
        finally:
            self.kernel.CloseHandle(snapshot)
        if len(ids) != 1:
            raise RuntimeError(f"挂起新进程必须恰一个初始线程：{pid}: {ids}")
        thread = self.kernel.OpenThread(0x2, False, ids[0])  # THREAD_SUSPEND_RESUME
        if not thread:
            raise ctypes.WinError(ctypes.get_last_error())
        try:
            previous = self.kernel.ResumeThread(thread)
            if previous != 1:
                raise RuntimeError(f"初始线程挂起计数异常：{previous}")
        finally:
            self.kernel.CloseHandle(thread)

    def terminate(self):
        if self.handle and not self.kernel.TerminateJobObject(self.handle, 124):
            raise ctypes.WinError(ctypes.get_last_error())

    def close(self):
        if self.handle:
            self.kernel.CloseHandle(self.handle)
            self.handle = None
