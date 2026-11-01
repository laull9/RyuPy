; Assembly listing for method RyuPy.Runtime.PyUnittestAssertions:Add(RyuPy.Runtime.PyClass,System.String,System.Func`3[System.Object,System.Object,bool],System.String) (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 80
       lea      rbp, [rsp+0x50]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       vmovdqa  xmmword ptr [rbp-0x30], xmm8
       mov      gword ptr [rbp-0x08], rdi
       mov      gword ptr [rbp-0x10], rsi
       mov      gword ptr [rbp-0x18], rdx
       mov      gword ptr [rbp-0x20], rcx
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x30], rax
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyUnittestAssertions+<>c__DisplayClass2_0:.ctor():this]
       mov      rax, gword ptr [rbp-0x30]
       mov      gword ptr [rbp-0x28], rax
       mov      rax, gword ptr [rbp-0x28]
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x18]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x28]
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp-0x20]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       mov      esi, 4
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x38], rax
       mov      rdi, gword ptr [rbp-0x38]
       xor      esi, esi
       mov      rdx, 0xD1FFAB1E
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x38]
       mov      esi, 1
       mov      rdx, 0xD1FFAB1E
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x38]
       mov      esi, 2
       mov      rdx, 0xD1FFAB1E
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x38]
       mov      esi, 3
       mov      rdx, 0xD1FFAB1E
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 4
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x40], rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       mov      rdi, gword ptr [rbp-0x40]
       mov      esi, 3
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x48], rax
       mov      rdi, gword ptr [rbp-0x48]
       mov      rsi, gword ptr [rbp-0x28]
       mov      rdx, 0xD1FFAB1E
       call     [System.MulticastDelegate:CtorClosed(System.Object,nint):this]
       mov      rdi, gword ptr [rbp-0x10]
       mov      rsi, gword ptr [rbp-0x38]
       mov      rdx, gword ptr [rbp-0x40]
       mov      r8, gword ptr [rbp-0x48]
       mov      ecx, 3
       call     [RyuPy.Runtime.PyUnittest:Method(System.String,System.String[],System.Object[],int,System.Func`2[System.Object[],System.Object]):RyuPy.Runtime.PyFunction]
       mov      gword ptr [rbp-0x50], rax
 
G_M000_IG03:
       mov      rdx, gword ptr [rbp-0x50]
       mov      rsi, gword ptr [rbp-0x10]
       mov      rdi, gword ptr [rbp-0x08]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyClass:SetAttr(System.String,System.Object):this]
       nop      
 
G_M000_IG04:
       add      rsp, 80
       pop      rbp
       ret      
 
; Total bytes of code 386

; Assembly listing for method RyuPy.Runtime.PyNumericAccumulator:Add(System.Object,System.Object) (Tier0)
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
       vmovdqu  ymmword ptr [rbp-0x40], ymm8
       vmovdqa  xmmword ptr [rbp-0x20], xmm8
       mov      gword ptr [rbp-0x08], rdi
       mov      gword ptr [rbp-0x10], rsi
 
G_M000_IG02:
       mov      rsi, gword ptr [rbp-0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_CHKCASTCLASS]
       mov      gword ptr [rbp-0x18], rax
       mov      rax, gword ptr [rbp-0x18]
       movzx    rax, byte  ptr [rax+0x18]
       test     eax, eax
       je       G_M000_IG04
       mov      rsi, gword ptr [rbp-0x10]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       je       SHORT G_M000_IG04
       mov      rsi, gword ptr [rbp-0x10]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x20], rax
       mov      rax, gword ptr [rbp-0x18]
       mov      rax, qword ptr [rax+0x10]
       mov      qword ptr [rbp-0x38], rax
       mov      rax, qword ptr [rbp-0x38]
       add      rax, qword ptr [rbp-0x20]
       mov      qword ptr [rbp-0x30], rax
       mov      rax, qword ptr [rbp-0x38]
       xor      rax, qword ptr [rbp-0x30]
       mov      rcx, qword ptr [rbp-0x20]
       xor      rcx, qword ptr [rbp-0x30]
       and      rax, rcx
       test     rax, rax
       jl       SHORT G_M000_IG04
       mov      rax, gword ptr [rbp-0x18]
       mov      rcx, qword ptr [rbp-0x30]
       mov      qword ptr [rax+0x10], rcx
       mov      rax, gword ptr [rbp-0x18]
       xor      rcx, rcx
       mov      gword ptr [rax+0x08], rcx
 
G_M000_IG03:
       add      rsp, 64
       pop      rbp
       ret      
 
G_M000_IG04:
       mov      rdi, gword ptr [rbp-0x18]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyNumericAccumulator+NumericAccumulator:Value():System.Object:this]
       mov      gword ptr [rbp-0x40], rax
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdx, gword ptr [rbp-0x10]
       mov      edi, 257
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x28], rax
       mov      rdi, gword ptr [rbp-0x18]
       mov      rsi, gword ptr [rbp-0x28]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyNumericAccumulator+NumericAccumulator:Store(System.Object):this]
       nop      
 
G_M000_IG05:
       add      rsp, 64
       pop      rbp
       ret      
 
; Total bytes of code 263

; Assembly listing for method RyuPy.Runtime.PyNumericAccumulator:Add(System.Object,System.Object) (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 64
       lea      rbp, [rsp+0x40]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x40], ymm8
       vmovdqa  xmmword ptr [rbp-0x20], xmm8
       mov      gword ptr [rbp-0x08], rdi
       mov      gword ptr [rbp-0x10], rsi
 
G_M000_IG02:
       mov      rsi, gword ptr [rbp-0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_CHKCASTCLASS]
       mov      gword ptr [rbp-0x18], rax
       mov      rax, gword ptr [rbp-0x18]
       movzx    rax, byte  ptr [rax+0x18]
       test     eax, eax
       je       G_M000_IG06
       mov      rsi, gword ptr [rbp-0x10]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_ISINSTANCEOFCLASS
       test     rax, rax
       je       G_M000_IG05
       mov      rsi, gword ptr [rbp-0x10]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x20], rax
       mov      rax, gword ptr [rbp-0x18]
       mov      rax, qword ptr [rax+0x10]
       mov      qword ptr [rbp-0x38], rax
       mov      rax, qword ptr [rbp-0x38]
       add      rax, qword ptr [rbp-0x20]
       mov      qword ptr [rbp-0x30], rax
       mov      rax, qword ptr [rbp-0x38]
       xor      rax, qword ptr [rbp-0x30]
       mov      rcx, qword ptr [rbp-0x20]
       xor      rcx, qword ptr [rbp-0x30]
       and      rax, rcx
       test     rax, rax
       jl       SHORT G_M000_IG04
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, gword ptr [rbp-0x18]
       mov      rcx, qword ptr [rbp-0x30]
       mov      qword ptr [rax+0x10], rcx
       mov      rax, gword ptr [rbp-0x18]
       xor      rcx, rcx
       mov      gword ptr [rax+0x08], rcx
 
G_M000_IG03:
       add      rsp, 64
       pop      rbp
       ret      
 
G_M000_IG04:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x18]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyNumericAccumulator+NumericAccumulator:Value():System.Object:this]
       mov      gword ptr [rbp-0x40], rax
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdx, gword ptr [rbp-0x10]
       mov      edi, 257
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x28], rax
       mov      rdi, gword ptr [rbp-0x18]
       mov      rsi, gword ptr [rbp-0x28]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyNumericAccumulator+NumericAccumulator:Store(System.Object):this]
       nop      
 
G_M000_IG07:
       add      rsp, 64
       pop      rbp
       ret      
 
; Total bytes of code 327

; Assembly listing for method RyuPy.Runtime.PyNumericAccumulator:Add(System.Object,System.Object) (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1998

G_M000_IG01:
       push     rbp
       push     r15
       push     rbx
       lea      rbp, [rsp+0x10]
       mov      rbx, rsi
 
G_M000_IG02:
       mov      r15, rdi
       test     r15, r15
       je       SHORT G_M000_IG04
 
G_M000_IG03:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG10
 
G_M000_IG04:
       cmp      byte  ptr [r15+0x18], 0
       je       SHORT G_M000_IG08
       test     rbx, rbx
       je       SHORT G_M000_IG08
 
G_M000_IG05:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG08
 
G_M000_IG06:
       mov      rdi, qword ptr [rbx+0x08]
       mov      rax, qword ptr [r15+0x10]
       lea      rcx, [rax+rdi]
       xor      rax, rcx
       xor      rdi, rcx
       and      rdi, rax
       jl       SHORT G_M000_IG08
       mov      qword ptr [r15+0x10], rcx
       xor      rdi, rdi
       mov      gword ptr [r15+0x08], rdi
 
G_M000_IG07:
       pop      rbx
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG08:
       mov      rdi, r15
       call     [RyuPy.Runtime.PyNumericAccumulator+NumericAccumulator:Value():System.Object:this]
       mov      rsi, rax
       mov      rdx, rbx
       mov      edi, 257
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
       mov      rdi, r15
 
G_M000_IG09:
       pop      rbx
       pop      r15
       pop      rbp
       tail.jmp [RyuPy.Runtime.PyNumericAccumulator+NumericAccumulator:Store(System.Object):this]
 
G_M000_IG10:
       mov      rsi, rdi
       mov      rdi, rax
       call     CORINFO_HELP_CHKCASTCLASS
       int3     
 
; Total bytes of code 154

