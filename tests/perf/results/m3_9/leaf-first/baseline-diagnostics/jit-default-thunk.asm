; Assembly listing for method RyuPy.Generated.Module:__thunk_0(RyuPy.Runtime.PyFunction,System.Object[],System.String[],System.Object[]):System.Object (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 64
       lea      rbp, [rsp+0x40]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  xmmword ptr [rbp-0x38], xmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x28], rax
       mov      gword ptr [rbp-0x08], rdi
       mov      gword ptr [rbp-0x10], rsi
       mov      gword ptr [rbp-0x18], rdx
       mov      gword ptr [rbp-0x20], rcx
 
G_M000_IG02:
       mov      rax, gword ptr [rbp-0x10]
       cmp      dword ptr [rax+0x08], 1
       jne      SHORT G_M000_IG04
       mov      rax, gword ptr [rbp-0x18]
       cmp      dword ptr [rax+0x08], 0
       jne      SHORT G_M000_IG04
       mov      rax, gword ptr [rbp-0x10]
       xor      ecx, ecx
       cmp      ecx, dword ptr [rax+0x08]
       jae      SHORT G_M000_IG06
       mov      edx, ecx
       lea      rax, bword ptr [rax+8*rdx+0x10]
       mov      rdi, gword ptr [rax]
       call     [RyuPy.Generated.Module:m0_twice(System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 64
       pop      rbp
       ret      
 
G_M000_IG04:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x30], rax
       mov      rdi, gword ptr [rbp-0x08]
       call     [RyuPy.Runtime.PyFunction:get_Defaults():System.Object[]:this]
       mov      gword ptr [rbp-0x38], rax
       mov      r8, gword ptr [rbp-0x38]
       mov      rsi, gword ptr [rbp-0x10]
       mov      rdx, gword ptr [rbp-0x18]
       mov      rcx, gword ptr [rbp-0x20]
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyBinder:Bind(RyuPy.Runtime.PySignature,System.Object[],System.String[],System.Object[],System.Object[]):System.Object[]]
       mov      gword ptr [rbp-0x28], rax
       mov      rax, gword ptr [rbp-0x28]
       xor      ecx, ecx
       cmp      ecx, dword ptr [rax+0x08]
       jae      SHORT G_M000_IG06
       mov      edx, ecx
       lea      rax, bword ptr [rax+8*rdx+0x10]
       mov      rdi, gword ptr [rax]
       call     [RyuPy.Generated.Module:m0_twice(System.Object):System.Object]
       nop      
 
G_M000_IG05:
       add      rsp, 64
       pop      rbp
       ret      
 
G_M000_IG06:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 197

