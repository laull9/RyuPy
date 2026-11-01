; Assembly listing for method RyuPy.Generated.Module:m0_matmul(System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 496
       lea      rbp, [rsp+0x1F0]
       xor      eax, eax
       mov      qword ptr [rbp-0x1E8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x1E0], xmm8
       vmovdqa32 xmmword ptr [rbp-0x1D0], xmm8
       mov      rax, -384
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x20], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
 
G_M000_IG02:
       mov      dword ptr [rbp-0x1F0], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x80], rax
       mov      edi, 16
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      rdi, rax
       call     [RyuPy.Runtime.PyOps:MakeList(System.Object[]):System.Object]
       mov      gword ptr [rbp-0x88], rax
       mov      rax, gword ptr [rbp-0x88]
       mov      gword ptr [rbp-0x48], rax
       mov      edi, 17
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x90], rax
       mov      rdi, gword ptr [rbp-0x90]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x98], rax
       mov      rdi, gword ptr [rbp-0x40]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0xA0], rax
       mov      rax, qword ptr [rbp-0x98]
       mov      qword ptr [rbp-0xA8], rax
       mov      rax, qword ptr [rbp-0xA0]
       mov      qword ptr [rbp-0xB0], rax
       mov      qword ptr [rbp-0xB8], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x1F0]
       dec      eax
       mov      dword ptr [rbp-0x1F0], eax
       cmp      dword ptr [rbp-0x1F0], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x1F0]
       mov      esi, 94
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       cmp      qword ptr [rbp-0xB8], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0xBC], eax
       mov      rax, qword ptr [rbp-0xA8]
       cmp      rax, qword ptr [rbp-0xB0]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0xC0], eax
       mov      rax, qword ptr [rbp-0xA8]
       cmp      rax, qword ptr [rbp-0xB0]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0xC4], eax
       mov      eax, dword ptr [rbp-0xBC]
       and      eax, dword ptr [rbp-0xC0]
       movzx    rax, al
       mov      dword ptr [rbp-0xC8], eax
       cmp      dword ptr [rbp-0xBC], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0xCC], eax
       mov      eax, dword ptr [rbp-0xCC]
       and      eax, dword ptr [rbp-0xC4]
       movzx    rax, al
       mov      dword ptr [rbp-0xD0], eax
       mov      eax, dword ptr [rbp-0xC8]
       or       eax, dword ptr [rbp-0xD0]
       movzx    rax, al
       mov      dword ptr [rbp-0xD4], eax
       cmp      dword ptr [rbp-0xD4], 0
       jne      SHORT G_M000_IG06
       jmp      G_M000_IG08
 
G_M000_IG06:
       mov      rdi, qword ptr [rbp-0xA8]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0xE0], rax
       mov      rax, gword ptr [rbp-0xE0]
       mov      gword ptr [rbp-0x50], rax
       mov      edi, 18
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x50]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE8], rax
       mov      rax, gword ptr [rbp-0xE8]
       mov      gword ptr [rbp-0x58], rax
       mov      edi, 19
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      rdi, rax
       call     [RyuPy.Runtime.PyOps:MakeList(System.Object[]):System.Object]
       mov      gword ptr [rbp-0xF0], rax
       mov      rax, gword ptr [rbp-0xF0]
       mov      gword ptr [rbp-0x60], rax
       mov      edi, 20
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xF8], rax
       mov      rdi, gword ptr [rbp-0xF8]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x100], rax
       mov      rdi, gword ptr [rbp-0x40]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x108], rax
       mov      rax, qword ptr [rbp-0x100]
       mov      qword ptr [rbp-0x110], rax
       mov      rax, qword ptr [rbp-0x108]
       mov      qword ptr [rbp-0x118], rax
       mov      qword ptr [rbp-0x120], 1
       jmp      SHORT G_M000_IG09
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0xA8]
       add      rax, qword ptr [rbp-0xB8]
       jo       G_M000_IG26
       mov      qword ptr [rbp-0xA8], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      edi, 26
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x1E8], rax
       jmp      G_M000_IG24
 
G_M000_IG09:
       mov      eax, dword ptr [rbp-0x1F0]
       dec      eax
       mov      dword ptr [rbp-0x1F0], eax
       cmp      dword ptr [rbp-0x1F0], 0
       jg       SHORT G_M000_IG11
 
G_M000_IG10:
       lea      rdi, [rbp-0x1F0]
       mov      esi, 300
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG11:
       cmp      qword ptr [rbp-0x120], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x124], eax
       mov      rax, qword ptr [rbp-0x110]
       cmp      rax, qword ptr [rbp-0x118]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x128], eax
       mov      rax, qword ptr [rbp-0x110]
       cmp      rax, qword ptr [rbp-0x118]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x12C], eax
       mov      eax, dword ptr [rbp-0x124]
       and      eax, dword ptr [rbp-0x128]
       movzx    rax, al
       mov      dword ptr [rbp-0x130], eax
       cmp      dword ptr [rbp-0x124], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x134], eax
       mov      eax, dword ptr [rbp-0x134]
       and      eax, dword ptr [rbp-0x12C]
       movzx    rax, al
       mov      dword ptr [rbp-0x138], eax
       mov      eax, dword ptr [rbp-0x130]
       or       eax, dword ptr [rbp-0x138]
       movzx    rax, al
       mov      dword ptr [rbp-0x13C], eax
       cmp      dword ptr [rbp-0x13C], 0
       jne      SHORT G_M000_IG12
       jmp      G_M000_IG14
 
G_M000_IG12:
       mov      rdi, qword ptr [rbp-0x110]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0x148], rax
       mov      rax, gword ptr [rbp-0x148]
       mov      gword ptr [rbp-0x68], rax
       mov      edi, 21
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x150], rax
       mov      rax, gword ptr [rbp-0x150]
       mov      gword ptr [rbp-0x70], rax
       mov      edi, 22
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x158], rax
       mov      rdi, gword ptr [rbp-0x158]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x160], rax
       mov      rdi, gword ptr [rbp-0x40]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x168], rax
       mov      rax, qword ptr [rbp-0x160]
       mov      qword ptr [rbp-0x170], rax
       mov      rax, qword ptr [rbp-0x168]
       mov      qword ptr [rbp-0x178], rax
       mov      qword ptr [rbp-0x180], 1
       jmp      G_M000_IG17
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x110]
       add      rax, qword ptr [rbp-0x120]
       jo       G_M000_IG26
       mov      qword ptr [rbp-0x110], rax
       jmp      G_M000_IG09
 
G_M000_IG14:
       mov      eax, dword ptr [rbp-0x1F0]
       dec      eax
       mov      dword ptr [rbp-0x1F0], eax
       cmp      dword ptr [rbp-0x1F0], 0
       jg       SHORT G_M000_IG16
 
G_M000_IG15:
       lea      rdi, [rbp-0x1F0]
       mov      esi, 469
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG16:
       mov      edi, 25
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, gword ptr [rbp-0x60]
       mov      rsi, gword ptr [rbp-0x48]
       mov      edi, 7
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       mov      gword ptr [rbp-0x1E0], rax
       jmp      G_M000_IG07
 
G_M000_IG17:
       mov      eax, dword ptr [rbp-0x1F0]
       dec      eax
       mov      dword ptr [rbp-0x1F0], eax
       cmp      dword ptr [rbp-0x1F0], 0
       jg       SHORT G_M000_IG19
 
G_M000_IG18:
       lea      rdi, [rbp-0x1F0]
       mov      esi, 496
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG19:
       cmp      qword ptr [rbp-0x180], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x184], eax
       mov      rax, qword ptr [rbp-0x170]
       cmp      rax, qword ptr [rbp-0x178]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x188], eax
       mov      rax, qword ptr [rbp-0x170]
       cmp      rax, qword ptr [rbp-0x178]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x18C], eax
       mov      eax, dword ptr [rbp-0x184]
       and      eax, dword ptr [rbp-0x188]
       movzx    rax, al
       mov      dword ptr [rbp-0x190], eax
       cmp      dword ptr [rbp-0x184], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x194], eax
       mov      eax, dword ptr [rbp-0x194]
       and      eax, dword ptr [rbp-0x18C]
       movzx    rax, al
       mov      dword ptr [rbp-0x198], eax
       mov      eax, dword ptr [rbp-0x190]
       or       eax, dword ptr [rbp-0x198]
       movzx    rax, al
       mov      dword ptr [rbp-0x19C], eax
       cmp      dword ptr [rbp-0x19C], 0
       jne      SHORT G_M000_IG20
       jmp      G_M000_IG21
 
G_M000_IG20:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x170]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0x1A8], rax
       mov      rax, gword ptr [rbp-0x1A8]
       mov      gword ptr [rbp-0x78], rax
       mov      edi, 23
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x58]
       mov      rsi, gword ptr [rbp-0x78]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x1B0], rax
       mov      rdi, gword ptr [rbp-0x38]
       mov      rsi, gword ptr [rbp-0x78]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x1B8], rax
       mov      rdi, gword ptr [rbp-0x1B8]
       mov      rsi, gword ptr [rbp-0x68]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x1C0], rax
       mov      rdx, gword ptr [rbp-0x1B0]
       mov      rcx, gword ptr [rbp-0x1C0]
       mov      edi, 8
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x1C8], rax
       mov      rdx, gword ptr [rbp-0x70]
       mov      rcx, gword ptr [rbp-0x1C8]
       mov      edi, 9
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x1D0], rax
       mov      rax, gword ptr [rbp-0x1D0]
       mov      gword ptr [rbp-0x70], rax
       mov      rax, qword ptr [rbp-0x170]
       add      rax, qword ptr [rbp-0x180]
       jo       G_M000_IG26
       mov      qword ptr [rbp-0x170], rax
       jmp      G_M000_IG17
 
G_M000_IG21:
       mov      eax, dword ptr [rbp-0x1F0]
       dec      eax
       mov      dword ptr [rbp-0x1F0], eax
       cmp      dword ptr [rbp-0x1F0], 0
       jg       SHORT G_M000_IG23
 
G_M000_IG22:
       lea      rdi, [rbp-0x1F0]
       mov      esi, 670
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG23:
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, gword ptr [rbp-0x70]
       mov      rsi, gword ptr [rbp-0x60]
       mov      edi, 10
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       mov      gword ptr [rbp-0x1D8], rax
       jmp      G_M000_IG13
 
G_M000_IG24:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x80]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x1E8]
 
G_M000_IG25:
       add      rsp, 496
       pop      rbp
       ret      
 
G_M000_IG26:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1906

; Assembly listing for method RyuPy.Generated.Module:m0_matmul(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x1f0
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 46 inlinees with PGO data; 61 single block inlinees; 64 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 304
       mov      qword ptr [rsp+0x328], r15
       mov      qword ptr [rsp+0x320], r14
       mov      qword ptr [rsp+0x318], r13
       mov      qword ptr [rsp+0x310], r12
       mov      qword ptr [rsp+0x308], rbx
       lea      rbp, [rsp+0x130]
       xor      eax, eax
       mov      qword ptr [rbp-0xC8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       mov      rbx, gword ptr [rbp+0x1C8]
       mov      r12, gword ptr [rbp+0x190]
       mov      r13, qword ptr [rbp+0x90]
       mov      rax, qword ptr [rbp+0x88]
       mov      rcx, qword ptr [rbp+0x80]
 
G_M000_IG02:
       mov      rsi, gword ptr [rbp+0x1A8]
       mov      r15, gword ptr [rbp+0x1C0]
       jmp      G_M000_IG62
 
G_M000_IG03:
       test     r14, r14
       jl       G_M000_IG160
       mov      rdi, r14
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG159
       test     r14, r14
       jl       G_M000_IG160
       mov      rdi, r14
       mov      r12, gword ptr [rax+8*rdi+0x10]
       jmp      SHORT G_M000_IG08
 
G_M000_IG04:
       xor      edi, edi
       jmp      G_M000_IG45
 
G_M000_IG05:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x118], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r14
       call     CORINFO_HELP_ASSIGN_REF
       mov      r14, gword ptr [rbp-0x118]
       mov      dword ptr [r14+0x10], r12d
       mov      r12, r14
       jmp      G_M000_IG47
 
G_M000_IG06:
       lea      r14, [r12+0x05]
       cmp      r14, 261
       jbe      G_M000_IG03
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
       mov      r12, rax
 
G_M000_IG08:
       jmp      G_M000_IG47
 
G_M000_IG09:
       mov      r15, gword ptr [rbp+0x1C0]
       jmp      G_M000_IG52
 
G_M000_IG10:
       mov      rsi, qword ptr [r14+0x08]
       test     rax, rax
       je       G_M000_IG55
 
G_M000_IG11:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rax], rdx
       jne      G_M000_IG32
 
G_M000_IG12:
       mov      r14, qword ptr [rax+0x08]
       lea      rdi, [rbp-0x60]
       mov      rdx, rsi
       mulx     rcx, rax, r14
       mov      qword ptr [rdi], rax
       mov      rax, qword ptr [rbp-0x60]
       mov      qword ptr [rbp-0x58], rax
       mov      rdi, rsi
       sar      rdi, 63
       and      rdi, r14
       sub      rcx, rdi
       mov      rdi, r14
       sar      rdi, 63
       and      rdi, rsi
       sub      rcx, rdi
       mov      rdi, rax
       sar      rdi, 63
       cmp      rcx, rdi
       je       G_M000_IG28
 
G_M000_IG13:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x50], xmm0
       lea      rdi, [rbp-0x50]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x130], rax
       mov      ecx, dword ptr [rbp-0x48]
       mov      dword ptr [rbp-0xDC], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       mov      rsi, r14
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r14, gword ptr [rbp-0x70]
       mov      r9d, dword ptr [rbp-0x68]
       mov      rax, gword ptr [rbp-0x130]
       test     rax, rax
       jne      G_M000_IG21
 
G_M000_IG14:
       test     r14, r14
       jne      G_M000_IG118
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       movsxd   rsi, dword ptr [rbp-0xDC]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x80]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r14, gword ptr [rbp-0x80]
       mov      eax, dword ptr [rbp-0x78]
 
G_M000_IG15:
       test     r14, r14
       jne      G_M000_IG119
 
G_M000_IG16:
       test     r14, r14
       jne      G_M000_IG128
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      SHORT G_M000_IG26
 
G_M000_IG17:
       mov      edi, -1
 
G_M000_IG18:
       test     edi, edi
       mov      dword ptr [rbp-0xE0], eax
       jg       SHORT G_M000_IG27
 
G_M000_IG19:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x108], rax
       mov      rdi, r14
       mov      esi, dword ptr [rbp-0xE0]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r14, gword ptr [rbp-0x108]
       mov      qword ptr [r14+0x08], rax
 
G_M000_IG20:
       jmp      G_M000_IG56
 
G_M000_IG21:
       lea      rdi, bword ptr [rax+0x10]
       mov      esi, dword ptr [rax+0x08]
 
G_M000_IG22:
       test     r14, r14
       je       SHORT G_M000_IG25
 
G_M000_IG23:
       lea      rcx, bword ptr [r14+0x10]
       mov      r8d, dword ptr [r14+0x08]
 
G_M000_IG24:
       mov      edx, dword ptr [rbp-0xDC]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      r14, rax
       mov      eax, edx
       jmp      G_M000_IG15
 
G_M000_IG25:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG24
 
G_M000_IG26:
       xor      edi, edi
       jmp      SHORT G_M000_IG18
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x100], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r14
       call     CORINFO_HELP_ASSIGN_REF
       mov      r14, gword ptr [rbp-0x100]
       mov      edi, dword ptr [rbp-0xE0]
       mov      dword ptr [r14+0x10], edi
       jmp      SHORT G_M000_IG20
 
G_M000_IG28:
       lea      r14, [rax+0x05]
       cmp      r14, 261
       jbe      SHORT G_M000_IG31
 
G_M000_IG29:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, qword ptr [rbp-0x58]
       mov      qword ptr [rax+0x08], r14
       mov      r14, rax
 
G_M000_IG30:
       jmp      G_M000_IG20
 
G_M000_IG31:
       test     r14, r14
       jl       G_M000_IG160
       mov      rdi, r14
       mov      rax, 0xD1FFAB1E
       mov      r14, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG159
       mov      r14, gword ptr [r14+8*rdi+0x10]
       jmp      SHORT G_M000_IG30
 
G_M000_IG32:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG55
 
G_M000_IG33:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x88], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x88]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG56
 
G_M000_IG34:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     rax, rax
       je       G_M000_IG55
 
G_M000_IG35:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG37
 
G_M000_IG36:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG56
 
G_M000_IG37:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG55
 
G_M000_IG38:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG56
 
G_M000_IG39:
       mov      rsi, qword ptr [r12+0x08]
       test     r14, r14
       je       G_M000_IG60
 
G_M000_IG40:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG65
 
G_M000_IG41:
       mov      r14, qword ptr [r14+0x08]
       lea      r12, [rsi+r14]
       mov      rdi, rsi
       xor      rdi, r12
       mov      rax, r14
       xor      rax, r12
       and      rdi, rax
       jge      G_M000_IG06
 
G_M000_IG42:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xB8], xmm0
       lea      rdi, [rbp-0xB8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0xB8]
       mov      eax, dword ptr [rbp-0xB0]
       mov      dword ptr [rbp-0xE4], eax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xC8], xmm0
       lea      rdi, [rbp-0xC8]
       mov      rsi, r14
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, r12
       mov      esi, dword ptr [rbp-0xE4]
       mov      rdx, gword ptr [rbp-0xC8]
       mov      rcx, qword ptr [rbp-0xC0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      r14, rax
       mov      r12d, edx
       test     r14, r14
       jne      G_M000_IG136
 
G_M000_IG43:
       test     r14, r14
       jne      G_M000_IG145
       movsxd   rdi, r12d
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jge      G_M000_IG04
 
G_M000_IG44:
       mov      edi, -1
 
G_M000_IG45:
       test     edi, edi
       jg       G_M000_IG05
 
G_M000_IG46:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x120], rax
       mov      rdi, r14
       mov      esi, r12d
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r12, gword ptr [rbp-0x120]
       mov      qword ptr [r12+0x08], rax
 
G_M000_IG47:
       jmp      G_M000_IG61
 
G_M000_IG48:
       mov      gword ptr [rbp+0x1A8], rsi
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      qword ptr [r14+0x08], r13
       mov      rsi, gword ptr [rbp+0x1A8]
 
G_M000_IG49:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG52
 
G_M000_IG50:
       mov      gword ptr [rbp+0x1C0], r15
       mov      r11, 0xD1FFAB1E
       mov      r11, gword ptr [r11]
       mov      r15, r11
       dec      edi
       mov      r10d, edi
       cmp      r10d, dword ptr [r15+0x08]
       jae      G_M000_IG159
       cmp      dword ptr [r15+4*r10+0x10], 23
       je       G_M000_IG09
 
G_M000_IG51:
       cmp      edi, dword ptr [r11+0x08]
       jae      G_M000_IG159
       mov      edi, edi
       mov      dword ptr [r11+4*rdi+0x10], 23
       mov      r15, gword ptr [rbp+0x1C0]
 
G_M000_IG52:
       mov      gword ptr [rbp+0x1A8], rsi
       mov      rdi, rsi
       mov      rsi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x50], rax
       mov      rdi, rbx
       mov      rsi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdi, rax
       mov      rsi, gword ptr [rbp+0x198]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      r14, gword ptr [rbp+0x50]
       test     r14, r14
       je       SHORT G_M000_IG55
 
G_M000_IG53:
       mov      rcx, qword ptr [r14]
       mov      rdx, 0xD1FFAB1E
       cmp      rcx, rdx
       je       G_M000_IG10
 
G_M000_IG54:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       je       G_M000_IG34
 
G_M000_IG55:
       mov      rdx, rax
       mov      rsi, r14
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      r14, rax
 
G_M000_IG56:
       test     r12, r12
       je       SHORT G_M000_IG60
 
G_M000_IG57:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       je       G_M000_IG39
 
G_M000_IG58:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       je       G_M000_IG67
 
G_M000_IG59:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r12], rsi
       je       G_M000_IG72
 
G_M000_IG60:
       mov      rdx, r14
       mov      rsi, r12
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r12, rax
 
G_M000_IG61:
       mov      r14, qword ptr [rbp+0x80]
       add      r13, r14
       jo       G_M000_IG160
       mov      rcx, r14
       mov      rax, qword ptr [rbp+0x88]
       mov      rsi, gword ptr [rbp+0x1A8]
 
G_M000_IG62:
       mov      qword ptr [rbp+0x80], rcx
       test     rcx, rcx
       setg     dil
       movzx    rdi, dil
       cmp      r13, rax
       setl     r14b
       movzx    r14, r14b
       and      r14d, edi
       mov      qword ptr [rbp+0x88], rax
       cmp      r13, rax
       setg     r11b
       movzx    r11, r11b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r11d
       or       edi, r14d
       je       G_M000_IG82
 
G_M000_IG63:
       lea      r14, [r13+0x05]
       cmp      r14, 261
       ja       G_M000_IG48
 
G_M000_IG64:
       test     r14, r14
       jl       G_M000_IG160
       mov      rdi, r14
       mov      r11, 0xD1FFAB1E
       mov      r14, gword ptr [r11]
       cmp      rdi, 262
       jae      G_M000_IG159
       mov      r14, gword ptr [r14+8*rdi+0x10]
       jmp      G_M000_IG49
 
G_M000_IG65:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG60
 
G_M000_IG66:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xD0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xD0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xA8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG61
 
G_M000_IG67:
       vmovsd   xmm0, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm0
       test     r14, r14
       je       G_M000_IG60
 
G_M000_IG68:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      SHORT G_M000_IG70
 
G_M000_IG69:
       vmovsd   xmm1, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x98]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG61
 
G_M000_IG70:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG60
 
G_M000_IG71:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0xD8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x98]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xD8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG61
 
G_M000_IG72:
       mov      rsi, r12
       mov      gword ptr [rbp-0x110], rsi
       test     r14, r14
       je       G_M000_IG60
 
G_M000_IG73:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG60
 
G_M000_IG74:
       cmp      dword ptr [rsi+0x08], 0
       je       SHORT G_M000_IG80
 
G_M000_IG75:
       cmp      dword ptr [r14+0x08], 0
       je       SHORT G_M000_IG79
 
G_M000_IG76:
       mov      r12d, dword ptr [rsi+0x08]
       mov      edi, r12d
       add      edi, dword ptr [r14+0x08]
       js       G_M000_IG117
 
G_M000_IG77:
       movsxd   rsi, edi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x128], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rsi, gword ptr [rbp-0x110]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, r12d
       mov      r12, gword ptr [rbp-0x128]
       lea      rdi, bword ptr [r12+2*rdi+0x0C]
       mov      edx, dword ptr [r14+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r14+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       nop      
 
G_M000_IG78:
       jmp      G_M000_IG61
 
G_M000_IG79:
       mov      r12, rsi
       jmp      SHORT G_M000_IG78
 
G_M000_IG80:
       cmp      dword ptr [r14+0x08], 0
       je       G_M000_IG153
 
G_M000_IG81:
       mov      r12, r14
       jmp      SHORT G_M000_IG78
 
G_M000_IG82:
       mov      gword ptr [rbp+0x1A8], rsi
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r13, gword ptr [rdi]
       mov      r14, gword ptr [r13+0x60]
       test     r14, r14
       je       SHORT G_M000_IG84
 
G_M000_IG83:
       cmp      dword ptr [r14+0x08], 1
       je       SHORT G_M000_IG85
 
G_M000_IG84:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r14, rax
       lea      rdi, bword ptr [r13+0x60]
       mov      rsi, r14
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG85:
       mov      rdi, r14
       mov      rdx, r12
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, r14
       mov      rsi, gword ptr [rbp+0x1A0]
       mov      edi, 10
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
       mov      r14, qword ptr [rbp+0xF0]
       mov      r12, qword ptr [rbp+0xE0]
       add      r14, r12
       jo       G_M000_IG160
 
G_M000_IG86:
       test     r12, r12
       setg     dil
       movzx    rdi, dil
       mov      rax, qword ptr [rbp+0xE8]
       cmp      r14, rax
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       cmp      r14, rax
       setg     cl
       movzx    rcx, cl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, ecx
       or       edi, esi
       je       G_M000_IG96
 
G_M000_IG87:
       lea      r13, [r14+0x05]
       cmp      r13, 261
       jbe      G_M000_IG116
 
G_M000_IG88:
       mov      qword ptr [rbp+0xE8], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r14
       mov      rsi, rax
 
G_M000_IG89:
       mov      r13, rsi
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG92
 
G_M000_IG90:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       lea      esi, [rdi-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      G_M000_IG159
       cmp      dword ptr [rdx+4*r8+0x10], 21
       je       SHORT G_M000_IG92
 
G_M000_IG91:
       cmp      esi, dword ptr [rcx+0x08]
       jae      G_M000_IG159
       mov      edx, esi
       mov      dword ptr [rcx+4*rdx+0x10], 21
 
G_M000_IG92:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      gword ptr [rbp+0x190], rcx
       test     edi, edi
       jle      SHORT G_M000_IG95
 
G_M000_IG93:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rsi, rdx
       dec      edi
       mov      r8d, edi
       cmp      r8d, dword ptr [rsi+0x08]
       jae      G_M000_IG159
       cmp      dword ptr [rsi+4*r8+0x10], 22
       je       SHORT G_M000_IG95
 
G_M000_IG94:
       cmp      edi, dword ptr [rdx+0x08]
       jae      G_M000_IG159
       mov      edi, edi
       mov      dword ptr [rdx+4*rdi+0x10], 22
 
G_M000_IG95:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0xA0], rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp+0xA0]
       mov      edx, 1
       mov      gword ptr [rbp+0x198], r13
       mov      qword ptr [rbp+0xF0], r14
       mov      qword ptr [rbp+0xE0], r12
       mov      r13, rcx
       mov      rcx, rdx
       mov      rsi, gword ptr [rbp+0x1A8]
       mov      r12, gword ptr [rbp+0x190]
       jmp      G_M000_IG62
 
G_M000_IG96:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG99
 
G_M000_IG97:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       dec      edi
       mov      ecx, dword ptr [rax+0x08]
       cmp      edi, ecx
       jae      G_M000_IG159
       cmp      dword ptr [rax+4*rdi+0x10], 25
       je       SHORT G_M000_IG99
 
G_M000_IG98:
       mov      dword ptr [rsi+4*rdi+0x10], 25
 
G_M000_IG99:
       mov      r14, gword ptr [r13+0x48]
       test     r14, r14
       je       SHORT G_M000_IG101
 
G_M000_IG100:
       cmp      dword ptr [r14+0x08], 1
       je       SHORT G_M000_IG102
 
G_M000_IG101:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r14, rax
       lea      rdi, bword ptr [r13+0x48]
       mov      rsi, r14
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG102:
       mov      rdi, r14
       mov      rdx, gword ptr [rbp+0x1A0]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, r14
       mov      rsi, gword ptr [rbp+0x1B8]
       mov      edi, 7
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
       mov      r14, qword ptr [rbp+0x158]
       mov      r12, qword ptr [rbp+0x148]
       add      r14, r12
       jo       G_M000_IG160
       test     r12, r12
       setg     dil
       movzx    rdi, dil
       mov      rax, qword ptr [rbp+0x150]
       cmp      r14, rax
       setl     cl
       movzx    rcx, cl
       and      ecx, edi
       mov      qword ptr [rbp+0x150], rax
       cmp      r14, rax
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, ecx
       je       G_M000_IG154
 
G_M000_IG103:
       lea      rcx, [r14+0x05]
       cmp      rcx, 261
       jbe      G_M000_IG115
 
G_M000_IG104:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r14
 
G_M000_IG105:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG108
 
G_M000_IG106:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      r8d, dword ptr [rdx+0x08]
       cmp      edi, r8d
       jae      G_M000_IG159
       cmp      dword ptr [rdx+4*rdi+0x10], 18
       je       SHORT G_M000_IG108
 
G_M000_IG107:
       mov      dword ptr [rcx+4*rdi+0x10], 18
 
G_M000_IG108:
       mov      rdi, gword ptr [rbp+0x1D0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x1A8], rax
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG111
 
G_M000_IG109:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      esi, dword ptr [rdx+0x08]
       cmp      edi, esi
       jae      G_M000_IG159
       cmp      dword ptr [rdx+4*rdi+0x10], 19
       je       SHORT G_M000_IG111
 
G_M000_IG110:
       mov      dword ptr [rcx+4*rdi+0x10], 19
 
G_M000_IG111:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xF0], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xF8], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, rax
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       mov      rdx, gword ptr [rbp-0xF0]
       lea      rdi, bword ptr [rdx+0x08]
       mov      rsi, gword ptr [rbp-0xF8]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdx, gword ptr [rbp-0xF0]
       mov      gword ptr [rbp+0x1A0], rdx
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG114
 
G_M000_IG112:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      esi, dword ptr [rdx+0x08]
       cmp      edi, esi
       jae      G_M000_IG159
       cmp      dword ptr [rdx+4*rdi+0x10], 20
       je       SHORT G_M000_IG114
 
G_M000_IG113:
       mov      dword ptr [rcx+4*rdi+0x10], 20
 
G_M000_IG114:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x100], rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rdi, qword ptr [rbp+0x100]
       mov      rcx, rdi
       mov      qword ptr [rbp+0xE8], rax
       mov      edx, 1
       mov      qword ptr [rbp+0x158], r14
       mov      qword ptr [rbp+0x148], r12
       mov      r12, rdx
       mov      r14, rcx
       jmp      G_M000_IG86
 
G_M000_IG115:
       test     rcx, rcx
       jl       G_M000_IG160
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG159
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       jmp      G_M000_IG105
 
G_M000_IG116:
       test     r13, r13
       jl       G_M000_IG160
       mov      rsi, r13
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG159
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       mov      qword ptr [rbp+0xE8], rax
       jmp      G_M000_IG89
 
G_M000_IG117:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG118:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG22
 
G_M000_IG119:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG120
       mov      edi, dword ptr [r14+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG121
 
G_M000_IG120:
       mov      ecx, eax
       jmp      SHORT G_M000_IG127
 
G_M000_IG121:
       cmp      edi, 2
       je       SHORT G_M000_IG122
       cmp      dword ptr [r14+0x08], 0
       jbe      G_M000_IG159
       mov      edi, dword ptr [r14+0x10]
       jmp      SHORT G_M000_IG123
 
G_M000_IG122:
       mov      edi, dword ptr [r14+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r14+0x10]
       or       rdi, rcx
 
G_M000_IG123:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG124
       mov      ecx, -1
       jmp      SHORT G_M000_IG126
 
G_M000_IG124:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG125
       mov      ecx, 1
       jmp      SHORT G_M000_IG126
 
G_M000_IG125:
       xor      ecx, ecx
 
G_M000_IG126:
       imul     ecx, eax
 
G_M000_IG127:
       test     ecx, ecx
       jge      G_M000_IG16
       mov      dword ptr [rbp-0xE0], eax
       jmp      G_M000_IG27
 
G_M000_IG128:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG129
       mov      edi, dword ptr [r14+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG130
 
G_M000_IG129:
       mov      dword ptr [rbp-0xE0], eax
       mov      edi, eax
       mov      eax, dword ptr [rbp-0xE0]
       jmp      G_M000_IG18
 
G_M000_IG130:
       cmp      edi, 2
       je       SHORT G_M000_IG131
       cmp      dword ptr [r14+0x08], 0
       jbe      G_M000_IG159
       mov      edi, dword ptr [r14+0x10]
       jmp      SHORT G_M000_IG132
 
G_M000_IG131:
       mov      edi, dword ptr [r14+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r14+0x10]
       or       rdi, rcx
 
G_M000_IG132:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG133
       mov      ecx, -1
       jmp      SHORT G_M000_IG135
 
G_M000_IG133:
       test     rdi, rdi
       jge      SHORT G_M000_IG134
       mov      ecx, 1
       jmp      SHORT G_M000_IG135
 
G_M000_IG134:
       xor      ecx, ecx
 
G_M000_IG135:
       mov      edi, eax
       imul     edi, ecx
       jmp      G_M000_IG18
 
G_M000_IG136:
       movsxd   rdi, r12d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG137
       mov      edi, dword ptr [r14+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG138
 
G_M000_IG137:
       mov      eax, r12d
       jmp      SHORT G_M000_IG144
 
G_M000_IG138:
       cmp      edi, 2
       je       SHORT G_M000_IG139
       cmp      dword ptr [r14+0x08], 0
       jbe      G_M000_IG159
       mov      edi, dword ptr [r14+0x10]
       jmp      SHORT G_M000_IG140
 
G_M000_IG139:
       mov      edi, dword ptr [r14+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r14+0x10]
       or       rdi, rax
 
G_M000_IG140:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG141
       mov      eax, -1
       jmp      SHORT G_M000_IG143
 
G_M000_IG141:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jbe      SHORT G_M000_IG142
       mov      eax, 1
       jmp      SHORT G_M000_IG143
 
G_M000_IG142:
       xor      eax, eax
 
G_M000_IG143:
       imul     eax, r12d
 
G_M000_IG144:
       test     eax, eax
       jl       G_M000_IG05
       jmp      G_M000_IG43
 
G_M000_IG145:
       movsxd   rdi, r12d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG146
       mov      edi, dword ptr [r14+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG147
 
G_M000_IG146:
       mov      edi, r12d
       jmp      G_M000_IG45
 
G_M000_IG147:
       cmp      edi, 2
       je       SHORT G_M000_IG148
       cmp      dword ptr [r14+0x08], 0
       jbe      G_M000_IG159
       mov      edi, dword ptr [r14+0x10]
       jmp      SHORT G_M000_IG149
 
G_M000_IG148:
       mov      edi, dword ptr [r14+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r14+0x10]
       or       rdi, rax
 
G_M000_IG149:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG150
       mov      eax, -1
       jmp      SHORT G_M000_IG152
 
G_M000_IG150:
       test     rdi, rdi
       jge      SHORT G_M000_IG151
       mov      eax, 1
       jmp      SHORT G_M000_IG152
 
G_M000_IG151:
       xor      eax, eax
 
G_M000_IG152:
       mov      edi, r12d
       imul     edi, eax
       jmp      G_M000_IG45
 
G_M000_IG153:
       mov      r12, 0xD1FFAB1E
       jmp      G_M000_IG78
 
G_M000_IG154:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG157
 
G_M000_IG155:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      eax, dword ptr [(reloc)]
       dec      eax
       cmp      eax, dword ptr [rdi+0x08]
       jae      SHORT G_M000_IG159
       cmp      dword ptr [rdi+4*rax+0x10], 26
       je       SHORT G_M000_IG157
 
G_M000_IG156:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      eax, dword ptr [(reloc)]
       dec      eax
       cmp      eax, dword ptr [rdi+0x08]
       jae      SHORT G_M000_IG159
       mov      dword ptr [rdi+4*rax+0x10], 26
 
G_M000_IG157:
       mov      rdi, qword ptr [rbp+0x180]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp+0x1B8]
 
G_M000_IG158:
       add      rsp, 776
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG159:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG160:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 4240

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_matmul(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; optimized using Synthesized PGO
; rsp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 100
; No PGO data

G_M000_IG01:
       push     rax
       mov      rax, rsi
 
G_M000_IG02:
       test     edx, edx
       je       SHORT G_M000_IG04
       mov      rdi, gword ptr [rax]
       cmp      edx, 1
       jbe      SHORT G_M000_IG04
       mov      rsi, gword ptr [rax+0x08]
       cmp      edx, 2
       jbe      SHORT G_M000_IG04
       mov      rdx, gword ptr [rax+0x10]
       call     [RyuPy.Generated.Module:m0_matmul(System.Object,System.Object,System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 47

