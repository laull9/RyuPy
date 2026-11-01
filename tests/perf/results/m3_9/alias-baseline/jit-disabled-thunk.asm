; Assembly listing for method RyuPy.Generated.Module:__thunk_0(RyuPy.Runtime.PyFunction,System.Object[],System.String[],System.Object[]):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 1 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:
       push     rbp
       mov      rbp, rsp
 
G_M000_IG02:
       mov      r8d, dword ptr [rsi+0x08]
       cmp      r8d, 1
       jne      SHORT G_M000_IG04
 
G_M000_IG03:
       cmp      dword ptr [rdx+0x08], 0
       je       SHORT G_M000_IG06
 
G_M000_IG04:
       mov      r8, gword ptr [rdi+0x20]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyBinder:Bind(RyuPy.Runtime.PySignature,System.Object[],System.String[],System.Object[],System.Object[]):System.Object[]]
       cmp      dword ptr [rax+0x08], 0
       jbe      SHORT G_M000_IG08
       mov      rdi, gword ptr [rax+0x10]
 
G_M000_IG05:
       pop      rbp
       tail.jmp [RyuPy.Generated.Module:m0_twice(System.Object):System.Object]
 
G_M000_IG06:
       mov      rdi, gword ptr [rsi+0x10]
 
G_M000_IG07:
       pop      rbp
       tail.jmp [RyuPy.Generated.Module:m0_twice(System.Object):System.Object]
 
G_M000_IG08:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 77

