import argparse
import ctypes
import json
import os
import signal
import struct
import sys
import time
from ctypes import wintypes


kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
psapi = ctypes.WinDLL("psapi", use_last_error=True)
advapi32 = ctypes.WinDLL("advapi32", use_last_error=True)

DWORD = wintypes.DWORD
WORD = wintypes.WORD
BYTE = ctypes.c_ubyte
BOOL = wintypes.BOOL
HANDLE = wintypes.HANDLE
LPVOID = wintypes.LPVOID
ULONG_PTR = ctypes.c_ulonglong
DWORD64 = ctypes.c_ulonglong
LONG = ctypes.c_long

PROCESS_QUERY_LIMITED_INFORMATION = 0x1000
PROCESS_VM_OPERATION = 0x0008
PROCESS_VM_READ = 0x0010
PROCESS_VM_WRITE = 0x0020
PROCESS_DEBUG_ACCESS = (
    PROCESS_QUERY_LIMITED_INFORMATION
    | PROCESS_VM_OPERATION
    | PROCESS_VM_READ
    | PROCESS_VM_WRITE
)
DBG_CONTINUE = 0x00010002
DBG_EXCEPTION_NOT_HANDLED = 0x80010001
EXCEPTION_DEBUG_EVENT = 1
CREATE_PROCESS_DEBUG_EVENT = 3
EXIT_PROCESS_DEBUG_EVENT = 5
LOAD_DLL_DEBUG_EVENT = 6
EXCEPTION_BREAKPOINT = 0x80000003
EXCEPTION_SINGLE_STEP = 0x80000004
EXCEPTION_MS_VC_THREAD_NAME = 0x406D1388
CONTEXT_AMD64 = 0x00100000
CONTEXT_CONTROL = CONTEXT_AMD64 | 0x00000001
CONTEXT_INTEGER = CONTEXT_AMD64 | 0x00000002
CONTEXT_FLAGS = CONTEXT_CONTROL | CONTEXT_INTEGER
TOKEN_ADJUST_PRIVILEGES = 0x0020
TOKEN_QUERY = 0x0008
SE_PRIVILEGE_ENABLED = 0x00000002


class EXCEPTION_RECORD(ctypes.Structure):
    _fields_ = [
        ("ExceptionCode", DWORD),
        ("ExceptionFlags", DWORD),
        ("ExceptionRecord", ULONG_PTR),
        ("ExceptionAddress", ULONG_PTR),
        ("NumberParameters", DWORD),
        ("ExceptionInformation", ULONG_PTR * 15),
    ]


class EXCEPTION_DEBUG_INFO(ctypes.Structure):
    _fields_ = [
        ("ExceptionRecord", EXCEPTION_RECORD),
        ("dwFirstChance", DWORD),
    ]


class CREATE_PROCESS_DEBUG_INFO(ctypes.Structure):
    _fields_ = [
        ("hFile", HANDLE),
        ("hProcess", HANDLE),
        ("hThread", HANDLE),
        ("lpBaseOfImage", LPVOID),
        ("lpStartAddress", LPVOID),
        ("lpThreadLocalBase", LPVOID),
    ]


class LOAD_DLL_DEBUG_INFO(ctypes.Structure):
    _fields_ = [
        ("hFile", HANDLE),
        ("lpBaseOfDll", LPVOID),
        ("dwDebugInfoFileOffset", DWORD),
        ("nDebugInfoSize", DWORD),
        ("lpImageName", LPVOID),
        ("fUnicode", WORD),
    ]


class DEBUG_EVENT_UNION(ctypes.Union):
    _fields_ = [
        ("Exception", EXCEPTION_DEBUG_INFO),
        ("CreateProcessInfo", CREATE_PROCESS_DEBUG_INFO),
        ("LoadDll", LOAD_DLL_DEBUG_INFO),
        ("raw", BYTE * 160),
    ]


class DEBUG_EVENT(ctypes.Structure):
    _fields_ = [
        ("dwDebugEventCode", DWORD),
        ("dwProcessId", DWORD),
        ("dwThreadId", DWORD),
        ("u", DEBUG_EVENT_UNION),
    ]


class M128A(ctypes.Structure):
    _fields_ = [
        ("Low", DWORD64),
        ("High", ctypes.c_longlong),
    ]


class XMM_SAVE_AREA32(ctypes.Structure):
    _fields_ = [
        ("ControlWord", WORD),
        ("StatusWord", WORD),
        ("TagWord", BYTE),
        ("Reserved1", BYTE),
        ("ErrorOpcode", WORD),
        ("ErrorOffset", DWORD),
        ("ErrorSelector", WORD),
        ("Reserved2", WORD),
        ("DataOffset", DWORD),
        ("DataSelector", WORD),
        ("Reserved3", WORD),
        ("MxCsr", DWORD),
        ("MxCsr_Mask", DWORD),
        ("FloatRegisters", M128A * 8),
        ("XmmRegisters", M128A * 16),
        ("Reserved4", BYTE * 96),
    ]


class CONTEXT(ctypes.Structure):
    _fields_ = [
        ("P1Home", DWORD64),
        ("P2Home", DWORD64),
        ("P3Home", DWORD64),
        ("P4Home", DWORD64),
        ("P5Home", DWORD64),
        ("P6Home", DWORD64),
        ("ContextFlags", DWORD),
        ("MxCsr", DWORD),
        ("SegCs", WORD),
        ("SegDs", WORD),
        ("SegEs", WORD),
        ("SegFs", WORD),
        ("SegGs", WORD),
        ("SegSs", WORD),
        ("EFlags", DWORD),
        ("Dr0", DWORD64),
        ("Dr1", DWORD64),
        ("Dr2", DWORD64),
        ("Dr3", DWORD64),
        ("Dr6", DWORD64),
        ("Dr7", DWORD64),
        ("Rax", DWORD64),
        ("Rcx", DWORD64),
        ("Rdx", DWORD64),
        ("Rbx", DWORD64),
        ("Rsp", DWORD64),
        ("Rbp", DWORD64),
        ("Rsi", DWORD64),
        ("Rdi", DWORD64),
        ("R8", DWORD64),
        ("R9", DWORD64),
        ("R10", DWORD64),
        ("R11", DWORD64),
        ("R12", DWORD64),
        ("R13", DWORD64),
        ("R14", DWORD64),
        ("R15", DWORD64),
        ("Rip", DWORD64),
        ("XmmSave", XMM_SAVE_AREA32),
        ("VectorRegister", M128A * 26),
        ("VectorControl", DWORD64),
        ("DebugControl", DWORD64),
        ("LastBranchToRip", DWORD64),
        ("LastBranchFromRip", DWORD64),
        ("LastExceptionToRip", DWORD64),
        ("LastExceptionFromRip", DWORD64),
    ]


class LUID(ctypes.Structure):
    _fields_ = [
        ("LowPart", DWORD),
        ("HighPart", LONG),
    ]


class TOKEN_PRIVILEGES(ctypes.Structure):
    _fields_ = [
        ("PrivilegeCount", DWORD),
        ("Luid", LUID),
        ("Attributes", DWORD),
    ]


kernel32.DebugActiveProcess.argtypes = [DWORD]
kernel32.DebugActiveProcess.restype = BOOL
kernel32.DebugActiveProcessStop.argtypes = [DWORD]
kernel32.DebugActiveProcessStop.restype = BOOL
kernel32.DebugSetProcessKillOnExit.argtypes = [BOOL]
kernel32.DebugSetProcessKillOnExit.restype = BOOL
kernel32.WaitForDebugEvent.argtypes = [ctypes.POINTER(DEBUG_EVENT), DWORD]
kernel32.WaitForDebugEvent.restype = BOOL
kernel32.ContinueDebugEvent.argtypes = [DWORD, DWORD, DWORD]
kernel32.ContinueDebugEvent.restype = BOOL
kernel32.OpenProcess.argtypes = [DWORD, BOOL, DWORD]
kernel32.OpenProcess.restype = HANDLE
kernel32.OpenThread.argtypes = [DWORD, BOOL, DWORD]
kernel32.OpenThread.restype = HANDLE
kernel32.CloseHandle.argtypes = [HANDLE]
kernel32.CloseHandle.restype = BOOL
kernel32.ReadProcessMemory.argtypes = [HANDLE, LPVOID, LPVOID, ctypes.c_size_t, ctypes.POINTER(ctypes.c_size_t)]
kernel32.ReadProcessMemory.restype = BOOL
kernel32.WriteProcessMemory.argtypes = [HANDLE, LPVOID, LPVOID, ctypes.c_size_t, ctypes.POINTER(ctypes.c_size_t)]
kernel32.WriteProcessMemory.restype = BOOL
kernel32.FlushInstructionCache.argtypes = [HANDLE, LPVOID, ctypes.c_size_t]
kernel32.FlushInstructionCache.restype = BOOL
kernel32.GetThreadContext.argtypes = [HANDLE, ctypes.POINTER(CONTEXT)]
kernel32.GetThreadContext.restype = BOOL
kernel32.SetThreadContext.argtypes = [HANDLE, ctypes.POINTER(CONTEXT)]
kernel32.SetThreadContext.restype = BOOL
kernel32.GetCurrentProcess.argtypes = []
kernel32.GetCurrentProcess.restype = HANDLE
advapi32.OpenProcessToken.argtypes = [HANDLE, DWORD, ctypes.POINTER(HANDLE)]
advapi32.OpenProcessToken.restype = BOOL
advapi32.LookupPrivilegeValueW.argtypes = [wintypes.LPCWSTR, wintypes.LPCWSTR, ctypes.POINTER(LUID)]
advapi32.LookupPrivilegeValueW.restype = BOOL
advapi32.AdjustTokenPrivileges.argtypes = [HANDLE, BOOL, ctypes.POINTER(TOKEN_PRIVILEGES), DWORD, LPVOID, LPVOID]
advapi32.AdjustTokenPrivileges.restype = BOOL


BREAKPOINTS = {
    0x006BA0: "AccountItem_SendClientClaimPendingItemGroup",
    0x006D00: "AccountItem_SendClientAccountItemTake",
    0x4A2100: "PrerequisiteManager_EvaluateTypeSlot",
    0x4A1790: "Prerequisite_CheckItemTradeSkill",
    0x4A17E0: "Prerequisite_CheckItemTradeSkillKnown",
    0x518B70: "AccountItemUi_ClaimSelectedPendingItemGroup",
    0x518BE0: "AccountItemUi_TakeSelectedAccountItem",
    0x4E50F0: "Lua_AccountItemLib_TakeAccountItem",
}


def err():
    code = ctypes.get_last_error()
    return f"{code}: {ctypes.FormatError(code).strip()}"


def enable_debug_privilege(log):
    token = HANDLE()
    if not advapi32.OpenProcessToken(
        kernel32.GetCurrentProcess(),
        TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY,
        ctypes.byref(token),
    ):
        log({"event": "debug_privilege_open_token_failed", "error": err()})
        return False

    try:
        luid = LUID()
        if not advapi32.LookupPrivilegeValueW(None, "SeDebugPrivilege", ctypes.byref(luid)):
            log({"event": "debug_privilege_lookup_failed", "error": err()})
            return False

        privs = TOKEN_PRIVILEGES()
        privs.PrivilegeCount = 1
        privs.Luid = luid
        privs.Attributes = SE_PRIVILEGE_ENABLED
        if not advapi32.AdjustTokenPrivileges(token, False, ctypes.byref(privs), 0, None, None):
            log({"event": "debug_privilege_adjust_failed", "error": err()})
            return False

        adjusted_error = ctypes.get_last_error()
        if adjusted_error:
            log({"event": "debug_privilege_not_assigned", "error": f"{adjusted_error}: {ctypes.FormatError(adjusted_error).strip()}"})
            return False

        log({"event": "debug_privilege_enabled"})
        return True
    finally:
        kernel32.CloseHandle(token)


def hex64(value):
    if value is None:
        return None
    return f"0x{int(value) & 0xffffffffffffffff:016x}"


def read_mem(process, address, size):
    if not address:
        return None
    buf = (BYTE * size)()
    got = ctypes.c_size_t(0)
    ok = kernel32.ReadProcessMemory(process, LPVOID(address), ctypes.byref(buf), size, ctypes.byref(got))
    if not ok:
        return None
    return bytes(buf[: got.value])


def write_mem(process, address, data):
    buf = (BYTE * len(data)).from_buffer_copy(data)
    written = ctypes.c_size_t(0)
    ok = kernel32.WriteProcessMemory(process, LPVOID(address), ctypes.byref(buf), len(data), ctypes.byref(written))
    if not ok or written.value != len(data):
        raise OSError(f"WriteProcessMemory({hex64(address)}) failed: {err()}")
    kernel32.FlushInstructionCache(process, LPVOID(address), len(data))


def open_thread(thread_id):
    THREAD_GET_CONTEXT = 0x0008
    THREAD_SET_CONTEXT = 0x0010
    THREAD_SUSPEND_RESUME = 0x0002
    h_thread = kernel32.OpenThread(
        THREAD_GET_CONTEXT | THREAD_SET_CONTEXT | THREAD_SUSPEND_RESUME,
        False,
        thread_id,
    )
    if not h_thread:
        raise OSError(f"OpenThread({thread_id}) failed: {err()}")
    return h_thread


def get_context(thread_id):
    h_thread = open_thread(thread_id)
    try:
        ctx = CONTEXT()
        ctx.ContextFlags = CONTEXT_FLAGS
        if not kernel32.GetThreadContext(h_thread, ctypes.byref(ctx)):
            raise OSError(f"GetThreadContext({thread_id}) failed: {err()}")
        return ctx, h_thread
    except Exception:
        kernel32.CloseHandle(h_thread)
        raise


def set_context(h_thread, ctx):
    if not kernel32.SetThreadContext(h_thread, ctypes.byref(ctx)):
        raise OSError(f"SetThreadContext failed: {err()}")


def int32s(data):
    if data is None or len(data) < 16:
        return None
    return list(struct.unpack("<4i", data[:16]))


def install_breakpoints(process, base, log):
    installed = {}
    for rva, name in BREAKPOINTS.items():
        address = base + rva
        original = read_mem(process, address, 1)
        if not original:
            log({"event": "breakpoint_install_failed", "name": name, "rva": hex(rva), "address": hex64(address)})
            continue
        write_mem(process, address, b"\xcc")
        installed[address] = {"rva": rva, "name": name, "original": original}
        log({"event": "breakpoint_installed", "name": name, "rva": hex(rva), "address": hex64(address), "original": original.hex()})
    return installed


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--pid", type=int, required=True)
    parser.add_argument("--seconds", type=int, default=90)
    parser.add_argument("--out", required=True)
    args = parser.parse_args()

    os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
    out = open(args.out, "w", encoding="utf-8")

    def log(record):
        record["ts"] = time.time()
        out.write(json.dumps(record, sort_keys=True) + "\n")
        out.flush()
        print(json.dumps(record, sort_keys=True), flush=True)

    stop = {"requested": False}

    def on_signal(signum, _frame):
        stop["requested"] = True
        log({"event": "signal", "signum": signum})

    signal.signal(signal.SIGINT, on_signal)
    signal.signal(signal.SIGTERM, on_signal)

    enable_debug_privilege(log)

    process = kernel32.OpenProcess(PROCESS_DEBUG_ACCESS, False, args.pid)
    if not process:
        log({"event": "open_process_failed", "pid": args.pid, "error": err()})

    base = None
    breakpoints = {}
    pending_single_step = {}
    attached = False

    try:
        if not kernel32.DebugActiveProcess(args.pid):
            log({"event": "debug_attach_failed", "pid": args.pid, "error": err()})
            return 3
        attached = True
        if not kernel32.DebugSetProcessKillOnExit(False):
            log({"event": "debug_kill_on_exit_failed", "error": err()})
        log({"event": "debug_attached", "pid": args.pid})

        deadline = time.time() + args.seconds
        while time.time() < deadline and not stop["requested"]:
            ev = DEBUG_EVENT()
            if not kernel32.WaitForDebugEvent(ctypes.byref(ev), 1000):
                continue

            continue_status = DBG_CONTINUE
            code = ev.dwDebugEventCode

            try:
                if code == CREATE_PROCESS_DEBUG_EVENT:
                    base = ctypes.cast(ev.u.CreateProcessInfo.lpBaseOfImage, ctypes.c_void_p).value
                    log({"event": "create_process", "base": hex64(base), "thread_id": ev.dwThreadId})
                    if not process and ev.u.CreateProcessInfo.hProcess:
                        process = ev.u.CreateProcessInfo.hProcess
                        log({"event": "using_debug_event_process_handle"})
                    if ev.u.CreateProcessInfo.hFile:
                        kernel32.CloseHandle(ev.u.CreateProcessInfo.hFile)
                    if base and not breakpoints:
                        breakpoints = install_breakpoints(process, base, log)

                elif code == LOAD_DLL_DEBUG_EVENT:
                    if ev.u.LoadDll.hFile:
                        kernel32.CloseHandle(ev.u.LoadDll.hFile)

                elif code == EXIT_PROCESS_DEBUG_EVENT:
                    log({"event": "process_exit"})
                    break

                elif code == EXCEPTION_DEBUG_EVENT:
                    ex = ev.u.Exception
                    ex_code = ex.ExceptionRecord.ExceptionCode
                    ex_addr = ex.ExceptionRecord.ExceptionAddress

                    if ex_code == EXCEPTION_BREAKPOINT:
                        bp_addr = ex_addr
                        if bp_addr not in breakpoints:
                            # Some contexts report RIP after int3; keep this defensive.
                            ctx, h_thread = get_context(ev.dwThreadId)
                            try:
                                if (ctx.Rip - 1) in breakpoints:
                                    bp_addr = ctx.Rip - 1
                                else:
                                    log({"event": "foreign_breakpoint", "address": hex64(ex_addr), "rip": hex64(ctx.Rip), "thread_id": ev.dwThreadId})
                                    continue_status = DBG_CONTINUE
                                    continue
                            finally:
                                kernel32.CloseHandle(h_thread)

                        bp = breakpoints[bp_addr]
                        ctx, h_thread = get_context(ev.dwThreadId)
                        try:
                            write_mem(process, bp_addr, bp["original"])
                            ctx.Rip = bp_addr
                            ctx.EFlags |= 0x100

                            detail = {}
                            if bp["name"] == "PrerequisiteManager_EvaluateTypeSlot":
                                detail["r8_int32x4"] = int32s(read_mem(process, ctx.R8, 16))
                            if bp["name"] in ("Prerequisite_CheckItemTradeSkill", "Prerequisite_CheckItemTradeSkillKnown"):
                                target_type = None
                                if ctx.Rdx:
                                    data = read_mem(process, ctx.Rdx + 0x80, 4)
                                    if data:
                                        target_type = struct.unpack("<i", data)[0]
                                detail["rdx_plus_0x80_i32"] = target_type

                            log({
                                "event": "breakpoint_hit",
                                "name": bp["name"],
                                "rva": hex(bp["rva"]),
                                "address": hex64(bp_addr),
                                "thread_id": ev.dwThreadId,
                                "registers": {
                                    "rip": hex64(ctx.Rip),
                                    "rsp": hex64(ctx.Rsp),
                                    "rcx": hex64(ctx.Rcx),
                                    "rdx": hex64(ctx.Rdx),
                                    "r8": hex64(ctx.R8),
                                    "r9": hex64(ctx.R9),
                                    "rax": hex64(ctx.Rax),
                                },
                                "detail": detail,
                            })

                            set_context(h_thread, ctx)
                            pending_single_step[ev.dwThreadId] = bp_addr
                        finally:
                            kernel32.CloseHandle(h_thread)

                    elif ex_code == EXCEPTION_SINGLE_STEP and ev.dwThreadId in pending_single_step:
                        bp_addr = pending_single_step.pop(ev.dwThreadId)
                        write_mem(process, bp_addr, b"\xcc")
                        log({"event": "breakpoint_reinserted", "address": hex64(bp_addr), "thread_id": ev.dwThreadId})

                    elif ex_code == EXCEPTION_MS_VC_THREAD_NAME:
                        # Retail WildStar raises the MSVC thread-name exception during login.
                        # Debuggers must consume it; passing it through as second chance can
                        # terminate the client even though it is not a real crash.
                        log({"event": "ignored_thread_name_exception", "address": hex64(ex_addr), "first_chance": int(ex.dwFirstChance), "thread_id": ev.dwThreadId})
                        continue_status = DBG_CONTINUE

                    else:
                        log({"event": "exception", "code": hex(ex_code), "address": hex64(ex_addr), "first_chance": int(ex.dwFirstChance), "thread_id": ev.dwThreadId})
                        continue_status = DBG_EXCEPTION_NOT_HANDLED

            finally:
                kernel32.ContinueDebugEvent(ev.dwProcessId, ev.dwThreadId, continue_status)

        log({"event": "finished", "hits": "see breakpoint_hit records", "seconds": args.seconds})
        return 0
    finally:
        for addr, bp in list(breakpoints.items()):
            try:
                current = read_mem(process, addr, 1)
                if current == b"\xcc":
                    write_mem(process, addr, bp["original"])
                    log({"event": "breakpoint_restored", "address": hex64(addr), "name": bp["name"]})
            except Exception as exc:
                log({"event": "breakpoint_restore_failed", "address": hex64(addr), "name": bp["name"], "error": str(exc)})
        if attached:
            if not kernel32.DebugActiveProcessStop(args.pid):
                log({"event": "debug_detach_failed", "error": err()})
            else:
                log({"event": "debug_detached", "pid": args.pid})
        kernel32.CloseHandle(process)
        out.close()


if __name__ == "__main__":
    sys.exit(main())
