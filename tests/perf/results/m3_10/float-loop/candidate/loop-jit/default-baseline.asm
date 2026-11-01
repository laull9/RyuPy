; Assembly listing for method RyuPy.Generated.Module:m0_accumulate(System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 448
       lea      rbp, [rsp+0x1C0]
       xor      eax, eax
       mov      qword ptr [rbp-0x1B8], rax
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -384
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x20], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x10], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      gword ptr [rbp-0x30], rdi
 
G_M000_IG02:
       mov      dword ptr [rbp-0x1B0], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x50], rax
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x58], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x60], rax
       mov      rdi, gword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x68], rax
       mov      rdi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x70], rax
       mov      rax, qword ptr [rbp-0x68]
       mov      qword ptr [rbp-0x78], rax
       mov      rax, qword ptr [rbp-0x70]
       mov      qword ptr [rbp-0x80], rax
       mov      qword ptr [rbp-0x88], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 78
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       cmp      qword ptr [rbp-0x88], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x8C], eax
       mov      rax, qword ptr [rbp-0x78]
       cmp      rax, qword ptr [rbp-0x80]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x90], eax
       mov      rax, qword ptr [rbp-0x78]
       cmp      rax, qword ptr [rbp-0x80]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x94], eax
       mov      eax, dword ptr [rbp-0x8C]
       and      eax, dword ptr [rbp-0x90]
       movzx    rax, al
       mov      dword ptr [rbp-0x98], eax
       cmp      dword ptr [rbp-0x8C], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x9C], eax
       mov      eax, dword ptr [rbp-0x9C]
       and      eax, dword ptr [rbp-0x94]
       movzx    rax, al
       mov      dword ptr [rbp-0xA0], eax
       mov      eax, dword ptr [rbp-0x98]
       or       eax, dword ptr [rbp-0xA0]
       movzx    rax, al
       mov      dword ptr [rbp-0xA4], eax
       cmp      dword ptr [rbp-0xA4], 0
       jne      SHORT G_M000_IG06
       jmp      G_M000_IG08
 
G_M000_IG06:
       mov      edi, 9
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xB8], rax
       mov      rax, gword ptr [rbp-0xB8]
       mov      gword ptr [rbp-0x40], rax
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xC0], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x198], rax
       mov      rdi, gword ptr [rbp-0x198]
       mov      rdx, gword ptr [rbp-0x30]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x1A0], rax
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x1B8], rax
       mov      rcx, gword ptr [rbp-0x1B8]
       mov      rsi, gword ptr [rbp-0x198]
       mov      rdx, gword ptr [rbp-0x1A0]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:CallBuiltin(System.String,System.Object[],System.Object[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0xC8], rax
       mov      rdi, gword ptr [rbp-0xC0]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0xD0], rax
       mov      rdi, gword ptr [rbp-0xC8]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0xD8], rax
       mov      rax, qword ptr [rbp-0xD0]
       mov      qword ptr [rbp-0xE0], rax
       mov      rax, qword ptr [rbp-0xD8]
       mov      qword ptr [rbp-0xE8], rax
       mov      qword ptr [rbp-0xF0], 1
       jmp      SHORT G_M000_IG09
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x78]
       add      rax, qword ptr [rbp-0x88]
       jo       G_M000_IG34
       mov      qword ptr [rbp-0x78], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      edi, 12
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0x140], rax
       jmp      G_M000_IG16
 
G_M000_IG09:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG11
 
G_M000_IG10:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 285
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG11:
       cmp      qword ptr [rbp-0xF0], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0xF4], eax
       mov      rax, qword ptr [rbp-0xE0]
       cmp      rax, qword ptr [rbp-0xE8]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0xF8], eax
       mov      rax, qword ptr [rbp-0xE0]
       cmp      rax, qword ptr [rbp-0xE8]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0xFC], eax
       mov      eax, dword ptr [rbp-0xF4]
       and      eax, dword ptr [rbp-0xF8]
       movzx    rax, al
       mov      dword ptr [rbp-0x100], eax
       cmp      dword ptr [rbp-0xF4], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x104], eax
       mov      eax, dword ptr [rbp-0x104]
       and      eax, dword ptr [rbp-0xFC]
       movzx    rax, al
       mov      dword ptr [rbp-0x108], eax
       mov      eax, dword ptr [rbp-0x100]
       or       eax, dword ptr [rbp-0x108]
       movzx    rax, al
       mov      dword ptr [rbp-0x10C], eax
       cmp      dword ptr [rbp-0x10C], 0
       jne      SHORT G_M000_IG14
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG13
 
G_M000_IG12:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 351
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG13:
       jmp      G_M000_IG07
 
G_M000_IG14:
       mov      rdi, qword ptr [rbp-0xE0]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0x118], rax
       mov      rax, gword ptr [rbp-0x118]
       mov      gword ptr [rbp-0x48], rax
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x48]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x120], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x128], rax
       mov      rax, gword ptr [rbp-0x120]
       mov      gword ptr [rbp-0x148], rax
       mov      rax, gword ptr [rbp-0x128]
       mov      gword ptr [rbp-0x158], rax
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0x168], rax
       jmp      G_M000_IG31
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0xE0]
       add      rax, qword ptr [rbp-0xF0]
       jo       G_M000_IG34
       mov      qword ptr [rbp-0xE0], rax
       jmp      G_M000_IG09
 
G_M000_IG16:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x50]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x140]
 
G_M000_IG17:
       add      rsp, 448
       pop      rbp
       ret      
 
G_M000_IG18:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG20
 
G_M000_IG19:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 428
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG20:
       mov      rax, gword ptr [rbp-0x138]
       mov      gword ptr [rbp-0x40], rax
       jmp      G_M000_IG15
 
G_M000_IG21:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG23
 
G_M000_IG22:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 436
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG23:
       mov      rdx, gword ptr [rbp-0x120]
       mov      rcx, gword ptr [rbp-0x128]
       xor      edi, edi
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x130], rax
       mov      rdx, gword ptr [rbp-0x40]
       mov      rcx, gword ptr [rbp-0x130]
       mov      edi, 1
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x138], rax
       jmp      G_M000_IG18
 
G_M000_IG24:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x148]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x150], xmm0
       mov      rsi, gword ptr [rbp-0x158]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x160], xmm0
       mov      rsi, gword ptr [rbp-0x168]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x170], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x150]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x160]
       vmovsd   qword ptr [rbp-0x178], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x170]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rbp-0x180], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x1A8], rax
       mov      rax, gword ptr [rbp-0x1A8]
       vmovsd   xmm0, qword ptr [rbp-0x180]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x1A8]
       mov      gword ptr [rbp-0x138], rax
       jmp      G_M000_IG18
 
G_M000_IG25:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG27
 
G_M000_IG26:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 525
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG27:
       mov      rsi, gword ptr [rbp-0x168]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x184], eax
       cmp      dword ptr [rbp-0x184], 0
       jne      G_M000_IG24
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG21
 
G_M000_IG28:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG30
 
G_M000_IG29:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 552
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG30:
       mov      rsi, gword ptr [rbp-0x158]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x188], eax
       cmp      dword ptr [rbp-0x188], 0
       jne      G_M000_IG25
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG21
 
G_M000_IG31:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG33
 
G_M000_IG32:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 579
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG33:
       mov      rsi, gword ptr [rbp-0x148]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x18C], eax
       cmp      dword ptr [rbp-0x18C], 0
       jne      G_M000_IG28
       jmp      G_M000_IG21
 
G_M000_IG34:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1917

; Assembly listing for method RyuPy.Generated.Module:m0_accumulate(System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x20d
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 2
; 16 inlinees with PGO data; 26 single block inlinees; 39 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 288
       mov      qword ptr [rsp+0x2E8], r15
       mov      qword ptr [rsp+0x2E0], r14
       mov      qword ptr [rsp+0x2D8], r13
       mov      qword ptr [rsp+0x2D0], r12
       mov      qword ptr [rsp+0x2C8], rbx
       lea      rbp, [rsp+0x120]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xE0], zmm8
       vmovdqu32 zmmword ptr [rbp-0xA0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x70], zmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, gword ptr [rbp+0x1A0]
       mov      r14, gword ptr [rbp+0x190]
       mov      r15, qword ptr [rbp+0xF0]
       mov      r12, qword ptr [rbp+0xE8]
       mov      rsi, gword ptr [rbp+0xB0]
       mov      rdx, gword ptr [rbp+0xA8]
       mov      r13, gword ptr [rbp+0x88]
       mov      r8, gword ptr [rbp+0x68]
 
G_M000_IG02:
       test     r8, r8
       je       G_M000_IG68
 
G_M000_IG03:
       mov      r10, 0xD1FFAB1E
       mov      gword ptr [rbp+0x68], r8
       cmp      qword ptr [r8], r10
       jne      G_M000_IG68
 
G_M000_IG04:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r13], rsi
       je       SHORT G_M000_IG06
 
G_M000_IG05:
       mov      rsi, r13
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
 
G_M000_IG06:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp+0x80], xmm0
       mov      rsi, 0xD1FFAB1E
       mov      rcx, gword ptr [rbp+0x78]
       cmp      qword ptr [rcx], rsi
       je       SHORT G_M000_IG08
 
G_M000_IG07:
       mov      gword ptr [rbp+0x78], rcx
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rcx, gword ptr [rbp+0x78]
 
G_M000_IG08:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   xmm0, qword ptr [rbp+0x80]
       vmulsd   xmm0, xmm0, xmm1
       mov      r8, gword ptr [rbp+0x68]
       vaddsd   xmm0, xmm0, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp+0x50], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG57
 
G_M000_IG09:
       lea      rdi, bword ptr [rax+0x10]
       mov      esi, dword ptr [rax+0x08]
 
G_M000_IG10:
       test     r13, r13
       je       SHORT G_M000_IG13
 
G_M000_IG11:
       lea      rcx, bword ptr [r13+0x10]
       mov      r8d, dword ptr [r13+0x08]
 
G_M000_IG12:
       mov      edx, dword ptr [rbp-0xF4]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      G_M000_IG75
 
G_M000_IG13:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG12
 
G_M000_IG14:
       lea      r13, [r8+0x05]
       cmp      r13, 261
       jbe      SHORT G_M000_IG17
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, qword ptr [rbp-0x70]
       mov      qword ptr [rax+0x08], r13
       mov      rdx, rax
 
G_M000_IG16:
       jmp      G_M000_IG76
 
G_M000_IG17:
       test     r13, r13
       jl       G_M000_IG98
       mov      rdx, r13
       mov      rcx, 0xD1FFAB1E
       mov      r13, gword ptr [rcx]
       cmp      rdx, 262
       jae      G_M000_IG97
       mov      rdx, gword ptr [r13+8*rdx+0x10]
       jmp      SHORT G_M000_IG16
 
G_M000_IG18:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rdx], r8
       jne      G_M000_IG26
 
G_M000_IG19:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x58], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0xA0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xA0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG27
 
G_M000_IG20:
       mov      rcx, 0xD1FFAB1E
       cmp      r13, rcx
       jne      G_M000_IG26
 
G_M000_IG21:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm0
       test     rdx, rdx
       je       G_M000_IG26
 
G_M000_IG22:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdx], rcx
       jne      SHORT G_M000_IG24
 
G_M000_IG23:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      SHORT G_M000_IG27
 
G_M000_IG24:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdx], rcx
       jne      SHORT G_M000_IG26
 
G_M000_IG25:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0xA8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      SHORT G_M000_IG27
 
G_M000_IG26:
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      r13, rax
 
G_M000_IG27:
       test     r14, r14
       je       G_M000_IG56
 
G_M000_IG28:
       mov      rax, qword ptr [r14]
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG40
 
G_M000_IG29:
       mov      rsi, qword ptr [r14+0x08]
       test     r13, r13
       je       G_M000_IG56
 
G_M000_IG30:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG38
 
G_M000_IG31:
       mov      r13, qword ptr [r13+0x08]
       lea      r14, [rsi+r13]
       mov      rdi, rsi
       xor      rdi, r14
       mov      rax, r13
       xor      rax, r14
       and      rdi, rax
       jge      SHORT G_M000_IG34
 
G_M000_IG32:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xD0], xmm0
       lea      rdi, [rbp-0xD0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r14, gword ptr [rbp-0xD0]
       mov      eax, dword ptr [rbp-0xC8]
       mov      dword ptr [rbp-0xF8], eax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xE0], xmm0
       lea      rdi, [rbp-0xE0]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, r14
       mov      esi, dword ptr [rbp-0xF8]
       mov      rdx, gword ptr [rbp-0xE0]
       mov      rcx, qword ptr [rbp-0xD8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG33:
       jmp      G_M000_IG57
 
G_M000_IG34:
       lea      r13, [r14+0x05]
       cmp      r13, 261
       jbe      SHORT G_M000_IG37
 
G_M000_IG35:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r14
 
G_M000_IG36:
       jmp      SHORT G_M000_IG33
 
G_M000_IG37:
       test     r13, r13
       jl       G_M000_IG98
       mov      rax, r13
       mov      rdi, 0xD1FFAB1E
       mov      r13, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG97
       mov      rax, gword ptr [r13+8*rax+0x10]
       jmp      SHORT G_M000_IG36
 
G_M000_IG38:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG56
 
G_M000_IG39:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xC0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xC0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG57
 
G_M000_IG40:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG46
 
G_M000_IG41:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm0
       test     r13, r13
       je       G_M000_IG56
 
G_M000_IG42:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      SHORT G_M000_IG44
 
G_M000_IG43:
       vmovsd   xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xB8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xB0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xB8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG57
 
G_M000_IG44:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG56
 
G_M000_IG45:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xF0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xB0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG57
 
G_M000_IG46:
       mov      rsi, 0xD1FFAB1E
       cmp      rax, rsi
       jne      G_M000_IG56
 
G_M000_IG47:
       mov      rsi, r14
       mov      gword ptr [rbp-0x108], rsi
       test     r13, r13
       je       G_M000_IG56
 
G_M000_IG48:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG56
 
G_M000_IG49:
       cmp      dword ptr [rsi+0x08], 0
       je       SHORT G_M000_IG54
 
G_M000_IG50:
       cmp      dword ptr [r13+0x08], 0
       je       SHORT G_M000_IG53
 
G_M000_IG51:
       mov      r14d, dword ptr [rsi+0x08]
       mov      edi, r14d
       add      edi, dword ptr [r13+0x08]
       js       G_M000_IG96
       movsxd   rsi, edi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x110], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rsi, gword ptr [rbp-0x108]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, r14d
       mov      r14, gword ptr [rbp-0x110]
       lea      rdi, bword ptr [r14+2*rdi+0x0C]
       mov      edx, dword ptr [r13+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r13+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rax, r14
 
G_M000_IG52:
       jmp      SHORT G_M000_IG57
 
G_M000_IG53:
       mov      rax, rsi
       jmp      SHORT G_M000_IG52
 
G_M000_IG54:
       cmp      dword ptr [r13+0x08], 0
       je       G_M000_IG95
 
G_M000_IG55:
       mov      rax, r13
       jmp      SHORT G_M000_IG52
 
G_M000_IG56:
       mov      rdx, r13
       mov      rsi, r14
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
 
G_M000_IG57:
       mov      r14, rax
       mov      r13, qword ptr [rbp+0xE0]
       add      r15, r13
       jo       G_M000_IG98
 
G_M000_IG58:
       test     r13, r13
       setg     dil
       movzx    rdi, dil
       cmp      r15, r12
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       cmp      r15, r12
       setg     al
       movzx    rax, al
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, eax
       or       edi, esi
       je       G_M000_IG77
 
G_M000_IG59:
       lea      rax, [r15+0x05]
       cmp      rax, 261
       jbe      G_M000_IG85
 
G_M000_IG60:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r15
 
G_M000_IG61:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG64
 
G_M000_IG62:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rcx, rax
       dec      edi
       mov      edx, edi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG97
       cmp      dword ptr [rcx+4*rdx+0x10], 11
       je       SHORT G_M000_IG64
 
G_M000_IG63:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG97
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 11
 
G_M000_IG64:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rsi, rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       mov      rax, rsi
       mov      rcx, rdx
       mov      r8, r14
       test     rax, rax
       mov      qword ptr [rbp+0xE0], r13
       mov      gword ptr [rbp+0x78], rcx
       je       SHORT G_M000_IG68
 
G_M000_IG65:
       mov      r13, rax
       mov      r9, 0xD1FFAB1E
       cmp      qword ptr [r13], r9
       jne      SHORT G_M000_IG68
 
G_M000_IG66:
       mov      rcx, gword ptr [rbp+0x78]
       test     rcx, rcx
       mov      gword ptr [rbp+0x78], rcx
       je       SHORT G_M000_IG68
 
G_M000_IG67:
       mov      r9, 0xD1FFAB1E
       mov      rcx, gword ptr [rbp+0x78]
       cmp      qword ptr [rcx], r9
       mov      gword ptr [rbp+0x78], rcx
       je       G_M000_IG02
 
G_M000_IG68:
       test     rsi, rsi
       je       G_M000_IG26
 
G_M000_IG69:
       mov      r13, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      r13, rcx
       jne      G_M000_IG20
 
G_M000_IG70:
       mov      rcx, qword ptr [rsi+0x08]
       test     rdx, rdx
       je       G_M000_IG26
 
G_M000_IG71:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rdx], r8
       jne      G_M000_IG18
 
G_M000_IG72:
       mov      r13, qword ptr [rdx+0x08]
       lea      rsi, [rbp-0x78]
       mov      rdx, rcx
       mulx     rdx, r8, r13
       mov      qword ptr [rsi], r8
       mov      r8, qword ptr [rbp-0x78]
       mov      qword ptr [rbp-0x70], r8
       mov      rsi, rcx
       sar      rsi, 63
       and      rsi, r13
       sub      rdx, rsi
       mov      rsi, r13
       sar      rsi, 63
       and      rsi, rcx
       sub      rdx, rsi
       mov      rsi, r8
       sar      rsi, 63
       cmp      rdx, rsi
       je       G_M000_IG14
 
G_M000_IG73:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x68], xmm0
       lea      rdi, [rbp-0x68]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x68]
       mov      gword ptr [rbp-0x118], rax
       mov      ecx, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0xF4], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x88], xmm0
       lea      rdi, [rbp-0x88]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r13, gword ptr [rbp-0x88]
       mov      r9d, dword ptr [rbp-0x80]
       mov      rax, gword ptr [rbp-0x118]
       test     rax, rax
       jne      G_M000_IG09
 
G_M000_IG74:
       test     r13, r13
       jne      G_M000_IG94
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x98], xmm0
       movsxd   rsi, dword ptr [rbp-0xF4]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x98]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x98]
       mov      esi, dword ptr [rbp-0x90]
 
G_M000_IG75:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG76:
       mov      r13, rdx
       jmp      G_M000_IG27
 
G_M000_IG77:
       mov      r13, qword ptr [rbp+0x158]
       mov      r12, qword ptr [rbp+0x148]
       add      r13, r12
       jo       G_M000_IG98
       test     r12, r12
       setg     dil
       movzx    rdi, dil
       mov      r15, qword ptr [rbp+0x150]
       cmp      r13, r15
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       mov      qword ptr [rbp+0x158], r13
       cmp      r13, r15
       setg     al
       movzx    rax, al
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, eax
       or       edi, esi
       je       G_M000_IG86
 
G_M000_IG78:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG81
 
G_M000_IG79:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       lea      ecx, [rdi-0x01]
       mov      edx, ecx
       mov      r8d, dword ptr [rax+0x08]
       cmp      edx, r8d
       jae      G_M000_IG97
       mov      r8d, edx
       cmp      dword ptr [rax+4*r8+0x10], 9
       je       SHORT G_M000_IG81
 
G_M000_IG80:
       mov      eax, ecx
       mov      dword ptr [rsi+4*rax+0x10], 9
 
G_M000_IG81:
       mov      rsi, 0xD1FFAB1E
       mov      r14, gword ptr [rsi]
       test     edi, edi
       jle      SHORT G_M000_IG84
 
G_M000_IG82:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       lea      ecx, [rdi-0x01]
       mov      edi, ecx
       mov      r8d, dword ptr [rax+0x08]
       cmp      edi, r8d
       jae      G_M000_IG97
       cmp      dword ptr [rax+4*rdi+0x10], 10
       je       SHORT G_M000_IG84
 
G_M000_IG83:
       mov      rdi, rsi
       mov      esi, ecx
       mov      dword ptr [rdi+4*rsi+0x10], 10
 
G_M000_IG84:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp+0x110], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x100], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x40]
       mov      rsi, gword ptr [rbp-0x100]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdx, gword ptr [rbp-0x38]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp+0x108], rax
       mov      rdi, gword ptr [rbp+0x110]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x100], rax
       mov      rdi, gword ptr [rbp+0x108]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rdi, qword ptr [rbp+0x100]
       mov      rcx, rdi
       mov      edx, 1
       mov      qword ptr [rbp+0x150], r15
       mov      qword ptr [rbp+0x148], r12
       mov      r12, rax
       mov      r13, rdx
       mov      r15, rcx
       jmp      G_M000_IG58
 
G_M000_IG85:
       test     rax, rax
       jl       G_M000_IG98
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG97
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       jmp      G_M000_IG61
 
G_M000_IG86:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG89
 
G_M000_IG87:
       mov      rax, 0xD1FFAB1E
       mov      rsi, gword ptr [rax]
       mov      rax, rsi
       lea      ecx, [rdi-0x01]
       mov      edx, ecx
       mov      r8d, dword ptr [rax+0x08]
       cmp      edx, r8d
       jae      SHORT G_M000_IG97
       cmp      dword ptr [rax+4*rdx+0x10], 12
       je       SHORT G_M000_IG89
 
G_M000_IG88:
       mov      rax, rsi
       mov      ecx, ecx
       mov      dword ptr [rax+4*rcx+0x10], 12
 
G_M000_IG89:
       mov      rbx, qword ptr [rbp+0x180]
       test     rbx, rbx
       jle      SHORT G_M000_IG92
 
G_M000_IG90:
       movsxd   rax, edi
       cmp      rbx, rax
       jg       SHORT G_M000_IG92
 
G_M000_IG91:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG92:
       mov      rax, r14
 
G_M000_IG93:
       add      rsp, 712
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG94:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG10
 
G_M000_IG95:
       mov      rax, 0xD1FFAB1E
       jmp      G_M000_IG52
 
G_M000_IG96:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG97:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG98:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2739

; Assembly listing for method RyuPy.Generated.Module:m0_accumulate(System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x1b4
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 3
; 40 inlinees with PGO data; 42 single block inlinees; 43 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 368
       mov      qword ptr [rsp+0x338], r15
       mov      qword ptr [rsp+0x330], r14
       mov      qword ptr [rsp+0x328], r13
       mov      qword ptr [rsp+0x320], r12
       mov      qword ptr [rsp+0x318], rbx
       lea      rbp, [rsp+0x170]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xF0], zmm8
       vmovdqu32 zmmword ptr [rbp-0xB0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x70], zmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, gword ptr [rbp+0x1A0]
       mov      r14, gword ptr [rbp+0x190]
       mov      r15, qword ptr [rbp+0xF0]
       mov      r13, qword ptr [rbp+0xE8]
       mov      r12, qword ptr [rbp+0xE0]
       mov      rsi, gword ptr [rbp+0xB0]
       mov      rdx, gword ptr [rbp+0xA8]
 
G_M000_IG02:
       test     rsi, rsi
       je       G_M000_IG35
 
G_M000_IG03:
       mov      r10, qword ptr [rsi]
       mov      rdi, 0xD1FFAB1E
       cmp      r10, rdi
       jne      G_M000_IG29
 
G_M000_IG04:
       mov      r10, qword ptr [rsi+0x08]
       test     rdx, rdx
       je       G_M000_IG35
 
G_M000_IG05:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG27
 
G_M000_IG06:
       mov      r11, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x60], r11
       lea      rdi, [rbp-0x80]
       mov      rdx, r10
       mulx     rdx, rsi, r11
       mov      qword ptr [rdi], rsi
       mov      rsi, qword ptr [rbp-0x80]
       mov      qword ptr [rbp-0x78], rsi
       mov      rdi, r10
       sar      rdi, 63
       and      rdi, r11
       sub      rdx, rdi
       mov      rdi, r11
       sar      rdi, 63
       and      rdi, r10
       sub      rdx, rdi
       mov      rdi, rsi
       sar      rdi, 63
       cmp      rdx, rdi
       je       G_M000_IG23
 
G_M000_IG07:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       mov      rsi, r10
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x70]
       mov      gword ptr [rbp-0x158], rax
       mov      ecx, dword ptr [rbp-0x68]
       mov      dword ptr [rbp-0x108], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x90], xmm0
       lea      rdi, [rbp-0x90]
       mov      rsi, qword ptr [rbp-0x60]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x90]
       mov      r9d, dword ptr [rbp-0x88]
       mov      rcx, gword ptr [rbp-0x158]
       test     rcx, rcx
       jne      G_M000_IG16
 
G_M000_IG08:
       test     rax, rax
       jne      G_M000_IG103
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xA0], xmm0
       movsxd   rsi, dword ptr [rbp-0x108]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xA0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xA0]
       mov      r10d, dword ptr [rbp-0x98]
 
G_M000_IG09:
       test     rax, rax
       jne      G_M000_IG104
 
G_M000_IG10:
       test     rax, rax
       jne      G_M000_IG113
 
G_M000_IG11:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      G_M000_IG21
 
G_M000_IG12:
       mov      edi, -1
 
G_M000_IG13:
       test     edi, edi
       mov      gword ptr [rbp-0x160], rax
       mov      dword ptr [rbp-0x10C], r10d
       jg       SHORT G_M000_IG22
 
G_M000_IG14:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x130], rax
       mov      rdi, gword ptr [rbp-0x160]
       mov      esi, dword ptr [rbp-0x10C]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rdx, gword ptr [rbp-0x130]
       mov      qword ptr [rdx+0x08], rax
 
G_M000_IG15:
       mov      rax, rdx
       jmp      G_M000_IG36
 
G_M000_IG16:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG17:
       test     rax, rax
       je       SHORT G_M000_IG20
 
G_M000_IG18:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG19:
       mov      edx, dword ptr [rbp-0x108]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      r10d, ecx
       jmp      G_M000_IG09
 
G_M000_IG20:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG19
 
G_M000_IG21:
       xor      edi, edi
       jmp      G_M000_IG13
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      gword ptr [rbp-0x128], rdx
       lea      rdi, bword ptr [rdx+0x08]
       mov      rsi, gword ptr [rbp-0x160]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdx, gword ptr [rbp-0x128]
       mov      edi, dword ptr [rbp-0x10C]
       mov      dword ptr [rdx+0x10], edi
       jmp      SHORT G_M000_IG15
 
G_M000_IG23:
       lea      rdx, [rsi+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG26
 
G_M000_IG24:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x78]
       mov      qword ptr [rax+0x08], rdi
       mov      rdx, rax
 
G_M000_IG25:
       jmp      G_M000_IG15
 
G_M000_IG26:
       test     rdx, rdx
       jl       G_M000_IG141
       mov      rdi, rdx
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG140
       mov      rdx, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG25
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG35
 
G_M000_IG28:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x58], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, r10
       vmovsd   qword ptr [rbp-0xA8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xA8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       mov      rax, rdx
       jmp      G_M000_IG36
 
G_M000_IG29:
       mov      rdi, 0xD1FFAB1E
       cmp      r10, rdi
       jne      G_M000_IG35
 
G_M000_IG30:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm0
       test     rdx, rdx
       je       SHORT G_M000_IG35
 
G_M000_IG31:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG33
 
G_M000_IG32:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG36
 
G_M000_IG33:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG35
 
G_M000_IG34:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG36
 
G_M000_IG35:
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG36:
       test     r14, r14
       je       G_M000_IG71
 
G_M000_IG37:
       mov      rcx, qword ptr [r14]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG55
 
G_M000_IG38:
       mov      rsi, qword ptr [r14+0x08]
       test     rax, rax
       je       G_M000_IG71
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG53
 
G_M000_IG40:
       mov      r14, qword ptr [rax+0x08]
       lea      rax, [rsi+r14]
       mov      qword ptr [rbp-0xD0], rax
       mov      rdi, rsi
       xor      rdi, rax
       mov      rcx, r14
       xor      rcx, rax
       and      rdi, rcx
       jge      G_M000_IG49
 
G_M000_IG41:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xE0], xmm0
       lea      rdi, [rbp-0xE0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xE0]
       mov      gword ptr [rbp-0x168], rax
       mov      ecx, dword ptr [rbp-0xD8]
       mov      dword ptr [rbp-0x110], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xF0], xmm0
       lea      rdi, [rbp-0xF0]
       mov      rsi, r14
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x168]
       mov      esi, dword ptr [rbp-0x110]
       mov      rdx, gword ptr [rbp-0xF0]
       mov      rcx, qword ptr [rbp-0xE8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      r14, rax
       mov      eax, edx
       mov      dword ptr [rbp-0x114], edx
       test     r14, r14
       jne      G_M000_IG121
 
G_M000_IG42:
       test     r14, r14
       jne      G_M000_IG130
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      SHORT G_M000_IG47
 
G_M000_IG43:
       mov      edi, -1
 
G_M000_IG44:
       test     edi, edi
       jg       SHORT G_M000_IG48
 
G_M000_IG45:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x148], rax
       mov      rdi, r14
       mov      esi, dword ptr [rbp-0x114]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r14, gword ptr [rbp-0x148]
       mov      qword ptr [r14+0x08], rax
 
G_M000_IG46:
       jmp      G_M000_IG72
 
G_M000_IG47:
       xor      edi, edi
       jmp      SHORT G_M000_IG44
 
G_M000_IG48:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x140], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r14
       call     CORINFO_HELP_ASSIGN_REF
       mov      r14, gword ptr [rbp-0x140]
       mov      edi, dword ptr [rbp-0x114]
       mov      dword ptr [r14+0x10], edi
       jmp      SHORT G_M000_IG46
 
G_M000_IG49:
       lea      r14, [rax+0x05]
       cmp      r14, 261
       jbe      SHORT G_M000_IG52
 
G_M000_IG50:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, qword ptr [rbp-0xD0]
       mov      qword ptr [rax+0x08], r14
       mov      r14, rax
 
G_M000_IG51:
       jmp      SHORT G_M000_IG46
 
G_M000_IG52:
       test     r14, r14
       jl       G_M000_IG141
       mov      rdi, r14
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG140
       mov      r14, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG51
 
G_M000_IG53:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG71
 
G_M000_IG54:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xC8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xF8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xF8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xC8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG72
 
G_M000_IG55:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG61
 
G_M000_IG56:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0xB8], xmm0
       test     rax, rax
       je       G_M000_IG71
 
G_M000_IG57:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG59
 
G_M000_IG58:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xC0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xB8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xC0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG72
 
G_M000_IG59:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG71
 
G_M000_IG60:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x100], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xB8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x100]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG72
 
G_M000_IG61:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      G_M000_IG71
 
G_M000_IG62:
       mov      rsi, r14
       mov      gword ptr [rbp-0x138], rsi
       test     rax, rax
       je       G_M000_IG71
 
G_M000_IG63:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG71
 
G_M000_IG64:
       mov      r14, rax
       cmp      dword ptr [rsi+0x08], 0
       je       G_M000_IG69
 
G_M000_IG65:
       cmp      dword ptr [r14+0x08], 0
       je       SHORT G_M000_IG68
 
G_M000_IG66:
       mov      eax, dword ptr [rsi+0x08]
       mov      dword ptr [rbp-0x104], eax
       mov      edi, eax
       add      edi, dword ptr [r14+0x08]
       js       G_M000_IG139
       movsxd   rsi, edi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x150], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rsi, gword ptr [rbp-0x138]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0x104]
       mov      rax, gword ptr [rbp-0x150]
       lea      rdi, bword ptr [rax+2*rdi+0x0C]
       mov      edx, dword ptr [r14+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r14+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      r14, gword ptr [rbp-0x150]
 
G_M000_IG67:
       jmp      SHORT G_M000_IG72
 
G_M000_IG68:
       mov      r14, rsi
       jmp      SHORT G_M000_IG67
 
G_M000_IG69:
       cmp      dword ptr [r14+0x08], 0
       je       G_M000_IG138
 
G_M000_IG70:
       jmp      SHORT G_M000_IG67
 
G_M000_IG71:
       mov      rdx, rax
       mov      rsi, r14
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r14, rax
 
G_M000_IG72:
       add      r15, r12
       jo       G_M000_IG141
 
G_M000_IG73:
       test     r12, r12
       setg     dil
       movzx    rdi, dil
       cmp      r15, r13
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       cmp      r15, r13
       setg     al
       movzx    rax, al
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, eax
       or       edi, esi
       je       G_M000_IG86
 
G_M000_IG74:
       lea      rax, [r15+0x05]
       cmp      rax, 261
       jbe      G_M000_IG94
 
G_M000_IG75:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r15
 
G_M000_IG76:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG79
 
G_M000_IG77:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rcx, rax
       dec      edi
       mov      edx, edi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG140
       cmp      dword ptr [rcx+4*rdx+0x10], 11
       je       SHORT G_M000_IG79
 
G_M000_IG78:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG140
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 11
 
G_M000_IG79:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       test     rsi, rsi
       je       G_M000_IG02
 
G_M000_IG80:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG02
 
G_M000_IG81:
       test     rdx, rdx
       je       G_M000_IG02
 
G_M000_IG82:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG02
 
G_M000_IG83:
       test     r14, r14
       je       G_M000_IG02
 
G_M000_IG84:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG02
 
G_M000_IG85:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp+0x50], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       vmovsd   xmm0, qword ptr [rbp+0x50]
       vmovsd   qword ptr [r14+0x08], xmm0
       jmp      G_M000_IG72
 
G_M000_IG86:
       mov      r12, qword ptr [rbp+0x158]
       mov      r13, qword ptr [rbp+0x148]
       add      r12, r13
       jo       G_M000_IG141
       mov      qword ptr [rbp+0x148], r13
       test     r13, r13
       setg     dil
       movzx    rdi, dil
       mov      r15, qword ptr [rbp+0x150]
       cmp      r12, r15
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       mov      qword ptr [rbp+0x158], r12
       mov      qword ptr [rbp+0x150], r15
       cmp      r12, r15
       setg     al
       movzx    rax, al
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, eax
       or       edi, esi
       je       G_M000_IG95
 
G_M000_IG87:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG90
 
G_M000_IG88:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       lea      ecx, [rdi-0x01]
       mov      edx, ecx
       cmp      edx, dword ptr [rax+0x08]
       jae      G_M000_IG140
       cmp      dword ptr [rax+4*rdx+0x10], 9
       je       SHORT G_M000_IG90
 
G_M000_IG89:
       cmp      ecx, dword ptr [rsi+0x08]
       jae      G_M000_IG140
       mov      eax, ecx
       mov      dword ptr [rsi+4*rax+0x10], 9
 
G_M000_IG90:
       mov      rsi, 0xD1FFAB1E
       mov      r14, gword ptr [rsi]
       test     edi, edi
       jle      SHORT G_M000_IG93
 
G_M000_IG91:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       lea      ecx, [rdi-0x01]
       mov      edi, ecx
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG140
       cmp      dword ptr [rax+4*rdi+0x10], 10
       je       SHORT G_M000_IG93
 
G_M000_IG92:
       mov      rdi, rsi
       mov      esi, ecx
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG140
       mov      dword ptr [rdi+4*rsi+0x10], 10
 
G_M000_IG93:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp+0x110], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x120], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x40]
       mov      rsi, gword ptr [rbp-0x120]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdx, gword ptr [rbp-0x38]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp+0x108], rax
       mov      rdi, gword ptr [rbp+0x110]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x100], rax
       mov      rdi, gword ptr [rbp+0x108]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rdi, qword ptr [rbp+0x100]
       mov      rcx, rdi
       mov      edx, 1
       mov      r12, rdx
       mov      r13, rax
       mov      r15, rcx
       jmp      G_M000_IG73
 
G_M000_IG94:
       test     rax, rax
       jl       G_M000_IG141
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG140
       mov      rsi, gword ptr [rdx+8*rsi+0x10]
       jmp      G_M000_IG76
 
G_M000_IG95:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG98
 
G_M000_IG96:
       mov      rax, 0xD1FFAB1E
       mov      rsi, gword ptr [rax]
       mov      rax, rsi
       lea      ecx, [rdi-0x01]
       mov      edx, ecx
       cmp      edx, dword ptr [rax+0x08]
       jae      G_M000_IG140
       cmp      dword ptr [rax+4*rdx+0x10], 12
       je       SHORT G_M000_IG98
 
G_M000_IG97:
       mov      rax, rsi
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG140
       mov      ecx, ecx
       mov      dword ptr [rax+4*rcx+0x10], 12
 
G_M000_IG98:
       mov      rbx, qword ptr [rbp+0x180]
       test     rbx, rbx
       jle      SHORT G_M000_IG101
 
G_M000_IG99:
       movsxd   rax, edi
       cmp      rbx, rax
       jg       SHORT G_M000_IG101
 
G_M000_IG100:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG101:
       mov      rax, r14
 
G_M000_IG102:
       add      rsp, 792
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG103:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG17
 
G_M000_IG104:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG105
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG106
 
G_M000_IG105:
       mov      ecx, r10d
       jmp      SHORT G_M000_IG112
 
G_M000_IG106:
       cmp      edi, 2
       je       SHORT G_M000_IG107
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG140
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG108
 
G_M000_IG107:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [rax+0x10]
       or       rdi, rcx
 
G_M000_IG108:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG109
       mov      ecx, -1
       jmp      SHORT G_M000_IG111
 
G_M000_IG109:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG110
       mov      ecx, 1
       jmp      SHORT G_M000_IG111
 
G_M000_IG110:
       xor      ecx, ecx
 
G_M000_IG111:
       imul     ecx, r10d
 
G_M000_IG112:
       test     ecx, ecx
       jge      G_M000_IG10
       mov      gword ptr [rbp-0x160], rax
       mov      dword ptr [rbp-0x10C], r10d
       jmp      G_M000_IG22
 
G_M000_IG113:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG114
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG115
 
G_M000_IG114:
       mov      dword ptr [rbp-0x10C], r10d
       mov      edi, r10d
       mov      r10d, dword ptr [rbp-0x10C]
       jmp      G_M000_IG13
 
G_M000_IG115:
       cmp      edi, 2
       je       SHORT G_M000_IG116
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG140
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG117
 
G_M000_IG116:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      gword ptr [rbp-0x160], rax
       mov      ecx, dword ptr [rax+0x10]
       or       rdi, rcx
       mov      rax, gword ptr [rbp-0x160]
 
G_M000_IG117:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG118
       mov      ecx, -1
       jmp      SHORT G_M000_IG120
 
G_M000_IG118:
       test     rdi, rdi
       jge      SHORT G_M000_IG119
       mov      ecx, 1
       jmp      SHORT G_M000_IG120
 
G_M000_IG119:
       xor      ecx, ecx
 
G_M000_IG120:
       mov      edi, r10d
       imul     edi, ecx
       jmp      G_M000_IG13
 
G_M000_IG121:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG122
       mov      edi, dword ptr [r14+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG123
 
G_M000_IG122:
       mov      ecx, eax
       jmp      SHORT G_M000_IG129
 
G_M000_IG123:
       cmp      edi, 2
       je       SHORT G_M000_IG124
       cmp      dword ptr [r14+0x08], 0
       jbe      G_M000_IG140
       mov      edi, dword ptr [r14+0x10]
       jmp      SHORT G_M000_IG125
 
G_M000_IG124:
       mov      edi, dword ptr [r14+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r14+0x10]
       or       rdi, rcx
 
G_M000_IG125:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG126
       mov      ecx, -1
       jmp      SHORT G_M000_IG128
 
G_M000_IG126:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG127
       mov      ecx, 1
       jmp      SHORT G_M000_IG128
 
G_M000_IG127:
       xor      ecx, ecx
 
G_M000_IG128:
       imul     ecx, eax
 
G_M000_IG129:
       test     ecx, ecx
       jge      G_M000_IG42
       jmp      G_M000_IG48
 
G_M000_IG130:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG131
       mov      edi, dword ptr [r14+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG132
 
G_M000_IG131:
       mov      edi, eax
       mov      eax, dword ptr [rbp-0x114]
       jmp      G_M000_IG44
 
G_M000_IG132:
       cmp      edi, 2
       je       SHORT G_M000_IG133
       cmp      dword ptr [r14+0x08], 0
       jbe      SHORT G_M000_IG140
       mov      edi, dword ptr [r14+0x10]
       jmp      SHORT G_M000_IG134
 
G_M000_IG133:
       mov      edi, dword ptr [r14+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r14+0x10]
       or       rdi, rcx
 
G_M000_IG134:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG135
       mov      ecx, -1
       jmp      SHORT G_M000_IG137
 
G_M000_IG135:
       test     rdi, rdi
       jge      SHORT G_M000_IG136
       mov      ecx, 1
       jmp      SHORT G_M000_IG137
 
G_M000_IG136:
       xor      ecx, ecx
 
G_M000_IG137:
       mov      edi, eax
       imul     edi, ecx
       jmp      G_M000_IG44
 
G_M000_IG138:
       mov      r14, 0xD1FFAB1E
       jmp      G_M000_IG67
 
G_M000_IG139:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG140:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG141:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 3516

; Assembly listing for method RyuPy.Generated.Module:m0_repeated(System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 512
       lea      rbp, [rsp+0x200]
       xor      eax, eax
       mov      qword ptr [rbp-0x1F8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x1F0], xmm8
       mov      rax, -432
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x20], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x10], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      gword ptr [rbp-0x30], rdi
 
G_M000_IG02:
       mov      dword ptr [rbp-0x1E8], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x60], rax
       mov      edi, 16
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      rdi, rax
       call     [RyuPy.Runtime.PyOps:MakeList(System.Object[]):System.Object]
       mov      gword ptr [rbp-0x68], rax
       mov      rax, gword ptr [rbp-0x68]
       mov      gword ptr [rbp-0x38], rax
       mov      edi, 17
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x70], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x1C0], rax
       mov      rdi, gword ptr [rbp-0x1C0]
       mov      rdx, gword ptr [rbp-0x30]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x1C8], rax
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x1F0], rax
       mov      rcx, gword ptr [rbp-0x1F0]
       mov      rsi, gword ptr [rbp-0x1C0]
       mov      rdx, gword ptr [rbp-0x1C8]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:CallBuiltin(System.String,System.Object[],System.Object[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x78], rax
       mov      rdi, gword ptr [rbp-0x70]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x80], rax
       mov      rdi, gword ptr [rbp-0x78]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x88], rax
       mov      rax, qword ptr [rbp-0x80]
       mov      qword ptr [rbp-0x90], rax
       mov      rax, qword ptr [rbp-0x88]
       mov      qword ptr [rbp-0x98], rax
       mov      qword ptr [rbp-0xA0], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x1E8]
       dec      eax
       mov      dword ptr [rbp-0x1E8], eax
       cmp      dword ptr [rbp-0x1E8], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x1E8]
       mov      esi, 131
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       cmp      qword ptr [rbp-0xA0], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0xA4], eax
       mov      rax, qword ptr [rbp-0x90]
       cmp      rax, qword ptr [rbp-0x98]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0xA8], eax
       mov      rax, qword ptr [rbp-0x90]
       cmp      rax, qword ptr [rbp-0x98]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0xAC], eax
       mov      eax, dword ptr [rbp-0xA4]
       and      eax, dword ptr [rbp-0xA8]
       movzx    rax, al
       mov      dword ptr [rbp-0xB0], eax
       cmp      dword ptr [rbp-0xA4], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0xB4], eax
       mov      eax, dword ptr [rbp-0xB4]
       and      eax, dword ptr [rbp-0xAC]
       movzx    rax, al
       mov      dword ptr [rbp-0xB8], eax
       mov      eax, dword ptr [rbp-0xB0]
       or       eax, dword ptr [rbp-0xB8]
       movzx    rax, al
       mov      dword ptr [rbp-0xBC], eax
       cmp      dword ptr [rbp-0xBC], 0
       jne      SHORT G_M000_IG06
       jmp      G_M000_IG08
 
G_M000_IG06:
       mov      rdi, qword ptr [rbp-0x90]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0xC8], rax
       mov      rax, gword ptr [rbp-0xC8]
       mov      gword ptr [rbp-0x40], rax
       mov      edi, 18
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x40]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xD0], rax
       mov      rax, gword ptr [rbp-0xD0]
       mov      gword ptr [rbp-0x48], rax
       mov      edi, 19
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xD8], rax
       mov      rax, gword ptr [rbp-0xD8]
       mov      gword ptr [rbp-0x50], rax
       mov      edi, 20
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xE0], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x1D0], rax
       mov      rdi, gword ptr [rbp-0x1D0]
       mov      rdx, gword ptr [rbp-0x48]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x1D8], rax
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x1F8], rax
       mov      rcx, gword ptr [rbp-0x1F8]
       mov      rsi, gword ptr [rbp-0x1D0]
       mov      rdx, gword ptr [rbp-0x1D8]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:CallBuiltin(System.String,System.Object[],System.Object[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0xE8], rax
       mov      rdi, gword ptr [rbp-0xE0]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0xF0], rax
       mov      rdi, gword ptr [rbp-0xE8]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0xF8], rax
       mov      rax, qword ptr [rbp-0xF0]
       mov      qword ptr [rbp-0x100], rax
       mov      rax, qword ptr [rbp-0xF8]
       mov      qword ptr [rbp-0x108], rax
       mov      qword ptr [rbp-0x110], 1
       jmp      SHORT G_M000_IG09
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x90]
       add      rax, qword ptr [rbp-0xA0]
       jo       G_M000_IG35
       mov      qword ptr [rbp-0x90], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      edi, 23
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0x168], rax
       jmp      G_M000_IG17
 
G_M000_IG09:
       mov      eax, dword ptr [rbp-0x1E8]
       dec      eax
       mov      dword ptr [rbp-0x1E8], eax
       cmp      dword ptr [rbp-0x1E8], 0
       jg       SHORT G_M000_IG11
 
G_M000_IG10:
       lea      rdi, [rbp-0x1E8]
       mov      esi, 369
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG11:
       cmp      qword ptr [rbp-0x110], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x114], eax
       mov      rax, qword ptr [rbp-0x100]
       cmp      rax, qword ptr [rbp-0x108]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x118], eax
       mov      rax, qword ptr [rbp-0x100]
       cmp      rax, qword ptr [rbp-0x108]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x11C], eax
       mov      eax, dword ptr [rbp-0x114]
       and      eax, dword ptr [rbp-0x118]
       movzx    rax, al
       mov      dword ptr [rbp-0x120], eax
       cmp      dword ptr [rbp-0x114], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x124], eax
       mov      eax, dword ptr [rbp-0x124]
       and      eax, dword ptr [rbp-0x11C]
       movzx    rax, al
       mov      dword ptr [rbp-0x128], eax
       mov      eax, dword ptr [rbp-0x120]
       or       eax, dword ptr [rbp-0x128]
       movzx    rax, al
       mov      dword ptr [rbp-0x12C], eax
       cmp      dword ptr [rbp-0x12C], 0
       jne      SHORT G_M000_IG12
       jmp      G_M000_IG14
 
G_M000_IG12:
       mov      rdi, qword ptr [rbp-0x100]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0x138], rax
       mov      rax, gword ptr [rbp-0x138]
       mov      gword ptr [rbp-0x58], rax
       mov      edi, 21
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x48]
       mov      rsi, gword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x140], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x148], rax
       mov      rax, gword ptr [rbp-0x140]
       mov      gword ptr [rbp-0x170], rax
       mov      rax, gword ptr [rbp-0x148]
       mov      gword ptr [rbp-0x180], rax
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x190], rax
       jmp      G_M000_IG32
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x100]
       add      rax, qword ptr [rbp-0x110]
       jo       G_M000_IG35
       mov      qword ptr [rbp-0x100], rax
       jmp      G_M000_IG09
 
G_M000_IG14:
       mov      eax, dword ptr [rbp-0x1E8]
       dec      eax
       mov      dword ptr [rbp-0x1E8], eax
       cmp      dword ptr [rbp-0x1E8], 0
       jg       SHORT G_M000_IG16
 
G_M000_IG15:
       lea      rdi, [rbp-0x1E8]
       mov      esi, 505
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG16:
       mov      edi, 22
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, gword ptr [rbp-0x50]
       mov      rsi, gword ptr [rbp-0x38]
       mov      edi, 2
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       mov      gword ptr [rbp-0x160], rax
       jmp      G_M000_IG07
 
G_M000_IG17:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x168]
 
G_M000_IG18:
       add      rsp, 512
       pop      rbp
       ret      
 
G_M000_IG19:
       mov      eax, dword ptr [rbp-0x1E8]
       dec      eax
       mov      dword ptr [rbp-0x1E8], eax
       cmp      dword ptr [rbp-0x1E8], 0
       jg       SHORT G_M000_IG21
 
G_M000_IG20:
       lea      rdi, [rbp-0x1E8]
       mov      esi, 542
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG21:
       mov      rax, gword ptr [rbp-0x158]
       mov      gword ptr [rbp-0x50], rax
       jmp      G_M000_IG13
 
G_M000_IG22:
       mov      eax, dword ptr [rbp-0x1E8]
       dec      eax
       mov      dword ptr [rbp-0x1E8], eax
       cmp      dword ptr [rbp-0x1E8], 0
       jg       SHORT G_M000_IG24
 
G_M000_IG23:
       lea      rdi, [rbp-0x1E8]
       mov      esi, 550
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG24:
       mov      rdx, gword ptr [rbp-0x140]
       mov      rcx, gword ptr [rbp-0x148]
       mov      edi, 3
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x150], rax
       mov      rdx, gword ptr [rbp-0x50]
       mov      rcx, gword ptr [rbp-0x150]
       mov      edi, 4
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x158], rax
       jmp      G_M000_IG19
 
G_M000_IG25:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x170]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x178], xmm0
       mov      rsi, gword ptr [rbp-0x180]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x188], xmm0
       mov      rsi, gword ptr [rbp-0x190]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x198], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x178]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x188]
       vmovsd   qword ptr [rbp-0x1A0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x198]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x1A0]
       vmovsd   qword ptr [rbp-0x1A8], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x1E0], rax
       mov      rax, gword ptr [rbp-0x1E0]
       vmovsd   xmm0, qword ptr [rbp-0x1A8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x1E0]
       mov      gword ptr [rbp-0x158], rax
       jmp      G_M000_IG19
 
G_M000_IG26:
       mov      eax, dword ptr [rbp-0x1E8]
       dec      eax
       mov      dword ptr [rbp-0x1E8], eax
       cmp      dword ptr [rbp-0x1E8], 0
       jg       SHORT G_M000_IG28
 
G_M000_IG27:
       lea      rdi, [rbp-0x1E8]
       mov      esi, 639
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG28:
       mov      rsi, gword ptr [rbp-0x190]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x1AC], eax
       cmp      dword ptr [rbp-0x1AC], 0
       jne      G_M000_IG25
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG22
 
G_M000_IG29:
       mov      eax, dword ptr [rbp-0x1E8]
       dec      eax
       mov      dword ptr [rbp-0x1E8], eax
       cmp      dword ptr [rbp-0x1E8], 0
       jg       SHORT G_M000_IG31
 
G_M000_IG30:
       lea      rdi, [rbp-0x1E8]
       mov      esi, 666
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG31:
       mov      rsi, gword ptr [rbp-0x180]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jne      G_M000_IG26
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG22
 
G_M000_IG32:
       mov      eax, dword ptr [rbp-0x1E8]
       dec      eax
       mov      dword ptr [rbp-0x1E8], eax
       cmp      dword ptr [rbp-0x1E8], 0
       jg       SHORT G_M000_IG34
 
G_M000_IG33:
       lea      rdi, [rbp-0x1E8]
       mov      esi, 693
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG34:
       mov      rsi, gword ptr [rbp-0x170]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x1B4], eax
       cmp      dword ptr [rbp-0x1B4], 0
       jne      G_M000_IG29
       jmp      G_M000_IG22
 
G_M000_IG35:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2217

; Assembly listing for method RyuPy.Generated.Module:m0_repeated(System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x27f
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 40 inlinees with PGO data; 54 single block inlinees; 54 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 320
       mov      qword ptr [rsp+0x348], r15
       mov      qword ptr [rsp+0x340], r14
       mov      qword ptr [rsp+0x338], r13
       mov      qword ptr [rsp+0x330], r12
       mov      qword ptr [rsp+0x328], rbx
       lea      rbp, [rsp+0x140]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xE0], zmm8
       vmovdqu32 zmmword ptr [rbp-0xA0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x70], zmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, gword ptr [rbp+0x1E0]
       mov      r14, gword ptr [rbp+0x1C0]
       mov      r15, qword ptr [rbp+0x110]
       mov      r12, qword ptr [rbp+0x108]
       mov      rsi, gword ptr [rbp+0xD0]
       mov      rdx, gword ptr [rbp+0xC8]
       mov      r13, gword ptr [rbp+0xA0]
       mov      r8, gword ptr [rbp+0x80]
 
G_M000_IG02:
       test     r8, r8
       je       G_M000_IG69
 
G_M000_IG03:
       mov      r11, 0xD1FFAB1E
       mov      gword ptr [rbp+0x80], r8
       cmp      qword ptr [r8], r11
       jne      G_M000_IG69
 
G_M000_IG04:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r13], rsi
       je       SHORT G_M000_IG06
 
G_M000_IG05:
       mov      rsi, r13
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
 
G_M000_IG06:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp+0x98], xmm0
       mov      rsi, 0xD1FFAB1E
       mov      rcx, gword ptr [rbp+0x90]
       cmp      qword ptr [rcx], rsi
       je       SHORT G_M000_IG08
 
G_M000_IG07:
       mov      gword ptr [rbp+0x90], rcx
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rcx, gword ptr [rbp+0x90]
 
G_M000_IG08:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   xmm0, qword ptr [rbp+0x98]
       vmulsd   xmm0, xmm0, xmm1
       mov      r8, gword ptr [rbp+0x80]
       vaddsd   xmm0, xmm0, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp+0x68], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x68]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG58
 
G_M000_IG09:
       lea      r13, [r8+0x05]
       cmp      r13, 261
       jbe      SHORT G_M000_IG12
 
G_M000_IG10:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, qword ptr [rbp-0x70]
       mov      qword ptr [rax+0x08], r13
       mov      r13, rax
 
G_M000_IG11:
       jmp      G_M000_IG81
 
G_M000_IG12:
       test     r13, r13
       jl       G_M000_IG156
       mov      rcx, r13
       mov      rdx, 0xD1FFAB1E
       mov      r13, gword ptr [rdx]
       cmp      rcx, 262
       jae      G_M000_IG155
       mov      r13, gword ptr [r13+8*rcx+0x10]
       jmp      SHORT G_M000_IG11
 
G_M000_IG13:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rdx], r8
       jne      G_M000_IG21
 
G_M000_IG14:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x58], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0xA0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xA0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG22
 
G_M000_IG15:
       mov      rcx, 0xD1FFAB1E
       cmp      r13, rcx
       jne      G_M000_IG21
 
G_M000_IG16:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm0
       test     rdx, rdx
       je       G_M000_IG21
 
G_M000_IG17:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdx], rcx
       jne      SHORT G_M000_IG19
 
G_M000_IG18:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      SHORT G_M000_IG22
 
G_M000_IG19:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdx], rcx
       jne      SHORT G_M000_IG21
 
G_M000_IG20:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0xA8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      SHORT G_M000_IG22
 
G_M000_IG21:
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      r13, rax
 
G_M000_IG22:
       test     r14, r14
       je       G_M000_IG57
 
G_M000_IG23:
       mov      rax, qword ptr [r14]
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG41
 
G_M000_IG24:
       mov      rsi, qword ptr [r14+0x08]
       test     r13, r13
       je       G_M000_IG57
 
G_M000_IG25:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG39
 
G_M000_IG26:
       mov      r13, qword ptr [r13+0x08]
       lea      r14, [rsi+r13]
       mov      rdi, rsi
       xor      rdi, r14
       mov      rax, r13
       xor      rax, r14
       and      rdi, rax
       jge      G_M000_IG35
 
G_M000_IG27:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xD0], xmm0
       lea      rdi, [rbp-0xD0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r14, gword ptr [rbp-0xD0]
       mov      eax, dword ptr [rbp-0xC8]
       mov      dword ptr [rbp-0xFC], eax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xE0], xmm0
       lea      rdi, [rbp-0xE0]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, r14
       mov      esi, dword ptr [rbp-0xFC]
       mov      rdx, gword ptr [rbp-0xE0]
       mov      rcx, qword ptr [rbp-0xD8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      r14, rax
       mov      r13d, edx
       test     r14, r14
       jne      G_M000_IG128
 
G_M000_IG28:
       test     r14, r14
       jne      G_M000_IG137
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jge      SHORT G_M000_IG33
 
G_M000_IG29:
       mov      edi, -1
 
G_M000_IG30:
       test     edi, edi
       jg       SHORT G_M000_IG34
 
G_M000_IG31:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x130], rax
       mov      rdi, r14
       mov      esi, r13d
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r13, gword ptr [rbp-0x130]
       mov      qword ptr [r13+0x08], rax
 
G_M000_IG32:
       mov      rax, r13
       jmp      G_M000_IG58
 
G_M000_IG33:
       xor      edi, edi
       jmp      SHORT G_M000_IG30
 
G_M000_IG34:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x128], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r14
       call     CORINFO_HELP_ASSIGN_REF
       mov      r14, gword ptr [rbp-0x128]
       mov      dword ptr [r14+0x10], r13d
       mov      r13, r14
       jmp      SHORT G_M000_IG32
 
G_M000_IG35:
       lea      r13, [r14+0x05]
       cmp      r13, 261
       jbe      SHORT G_M000_IG38
 
G_M000_IG36:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r14
 
G_M000_IG37:
       mov      r13, rax
       jmp      SHORT G_M000_IG32
 
G_M000_IG38:
       test     r13, r13
       jl       G_M000_IG156
       mov      rax, r13
       mov      rdi, 0xD1FFAB1E
       mov      r13, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG155
       mov      rax, gword ptr [r13+8*rax+0x10]
       jmp      SHORT G_M000_IG37
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG57
 
G_M000_IG40:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xC0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xC0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       mov      rax, r13
       jmp      G_M000_IG58
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG47
 
G_M000_IG42:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm0
       test     r13, r13
       je       G_M000_IG57
 
G_M000_IG43:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      SHORT G_M000_IG45
 
G_M000_IG44:
       vmovsd   xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xB8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xB0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xB8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       mov      rax, r13
       jmp      G_M000_IG58
 
G_M000_IG45:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG57
 
G_M000_IG46:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xF0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xB0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       mov      rax, r13
       jmp      G_M000_IG58
 
G_M000_IG47:
       mov      rsi, 0xD1FFAB1E
       cmp      rax, rsi
       jne      G_M000_IG57
 
G_M000_IG48:
       mov      rsi, r14
       mov      gword ptr [rbp-0x120], rsi
       test     r13, r13
       je       G_M000_IG57
 
G_M000_IG49:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG57
 
G_M000_IG50:
       cmp      dword ptr [rsi+0x08], 0
       je       SHORT G_M000_IG55
 
G_M000_IG51:
       cmp      dword ptr [r13+0x08], 0
       je       SHORT G_M000_IG54
 
G_M000_IG52:
       mov      r14d, dword ptr [rsi+0x08]
       mov      edi, r14d
       add      edi, dword ptr [r13+0x08]
       js       G_M000_IG154
       movsxd   rsi, edi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x138], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rsi, gword ptr [rbp-0x120]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, r14d
       mov      r14, gword ptr [rbp-0x138]
       lea      rdi, bword ptr [r14+2*rdi+0x0C]
       mov      edx, dword ptr [r13+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r13+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      r13, r14
 
G_M000_IG53:
       mov      rax, r13
       jmp      SHORT G_M000_IG58
 
G_M000_IG54:
       mov      r13, rsi
       jmp      SHORT G_M000_IG53
 
G_M000_IG55:
       cmp      dword ptr [r13+0x08], 0
       je       G_M000_IG145
 
G_M000_IG56:
       jmp      SHORT G_M000_IG53
 
G_M000_IG57:
       mov      rdx, r13
       mov      rsi, r14
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
 
G_M000_IG58:
       mov      r14, rax
       mov      r13, qword ptr [rbp+0x100]
       add      r15, r13
       jo       G_M000_IG156
 
G_M000_IG59:
       test     r13, r13
       setg     dil
       movzx    rdi, dil
       cmp      r15, r12
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       cmp      r15, r12
       setg     al
       movzx    rax, al
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, eax
       or       edi, esi
       je       G_M000_IG89
 
G_M000_IG60:
       lea      rax, [r15+0x05]
       cmp      rax, 261
       jbe      G_M000_IG109
 
G_M000_IG61:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r15
 
G_M000_IG62:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG65
 
G_M000_IG63:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rcx, rax
       dec      edi
       mov      edx, edi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG155
       cmp      dword ptr [rcx+4*rdx+0x10], 21
       je       SHORT G_M000_IG65
 
G_M000_IG64:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG155
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 21
 
G_M000_IG65:
       mov      rdi, gword ptr [rbp+0x1C8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rsi, rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       mov      rax, rsi
       mov      rcx, rdx
       mov      r8, r14
       test     rax, rax
       mov      qword ptr [rbp+0x100], r13
       mov      gword ptr [rbp+0x90], rcx
       je       SHORT G_M000_IG69
 
G_M000_IG66:
       mov      r13, rax
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      SHORT G_M000_IG69
 
G_M000_IG67:
       mov      rcx, gword ptr [rbp+0x90]
       test     rcx, rcx
       mov      gword ptr [rbp+0x90], rcx
       je       SHORT G_M000_IG69
 
G_M000_IG68:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rbp+0x90]
       cmp      qword ptr [rcx], rdi
       mov      gword ptr [rbp+0x90], rcx
       je       G_M000_IG02
 
G_M000_IG69:
       test     rsi, rsi
       je       G_M000_IG21
 
G_M000_IG70:
       mov      r13, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      r13, rcx
       jne      G_M000_IG15
 
G_M000_IG71:
       mov      rcx, qword ptr [rsi+0x08]
       test     rdx, rdx
       je       G_M000_IG21
 
G_M000_IG72:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rdx], r8
       jne      G_M000_IG13
 
G_M000_IG73:
       mov      r13, qword ptr [rdx+0x08]
       lea      rsi, [rbp-0x78]
       mov      rdx, rcx
       mulx     rdx, r8, r13
       mov      qword ptr [rsi], r8
       mov      r8, qword ptr [rbp-0x78]
       mov      qword ptr [rbp-0x70], r8
       mov      rsi, rcx
       sar      rsi, 63
       and      rsi, r13
       sub      rdx, rsi
       mov      rsi, r13
       sar      rsi, 63
       and      rsi, rcx
       sub      rdx, rsi
       mov      rsi, r8
       sar      rsi, 63
       cmp      rdx, rsi
       je       G_M000_IG09
 
G_M000_IG74:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x68], xmm0
       lea      rdi, [rbp-0x68]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x68]
       mov      gword ptr [rbp-0x140], rax
       mov      ecx, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0xF4], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x88], xmm0
       lea      rdi, [rbp-0x88]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r13, gword ptr [rbp-0x88]
       mov      r9d, dword ptr [rbp-0x80]
       mov      rax, gword ptr [rbp-0x140]
       test     rax, rax
       jne      G_M000_IG82
 
G_M000_IG75:
       test     r13, r13
       jne      G_M000_IG110
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x98], xmm0
       movsxd   rsi, dword ptr [rbp-0xF4]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x98]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r13, gword ptr [rbp-0x98]
       mov      eax, dword ptr [rbp-0x90]
 
G_M000_IG76:
       test     r13, r13
       jne      G_M000_IG111
 
G_M000_IG77:
       test     r13, r13
       jne      G_M000_IG120
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      SHORT G_M000_IG87
 
G_M000_IG78:
       mov      edi, -1
 
G_M000_IG79:
       test     edi, edi
       mov      dword ptr [rbp-0xF8], eax
       jg       SHORT G_M000_IG88
 
G_M000_IG80:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x118], rax
       mov      rdi, r13
       mov      esi, dword ptr [rbp-0xF8]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r13, gword ptr [rbp-0x118]
       mov      qword ptr [r13+0x08], rax
 
G_M000_IG81:
       jmp      G_M000_IG22
 
G_M000_IG82:
       lea      rdi, bword ptr [rax+0x10]
       mov      esi, dword ptr [rax+0x08]
 
G_M000_IG83:
       test     r13, r13
       je       SHORT G_M000_IG86
 
G_M000_IG84:
       lea      rcx, bword ptr [r13+0x10]
       mov      r8d, dword ptr [r13+0x08]
 
G_M000_IG85:
       mov      edx, dword ptr [rbp-0xF4]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      r13, rax
       mov      eax, edx
       jmp      G_M000_IG76
 
G_M000_IG86:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG85
 
G_M000_IG87:
       xor      edi, edi
       jmp      SHORT G_M000_IG79
 
G_M000_IG88:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x110], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r13
       call     CORINFO_HELP_ASSIGN_REF
       mov      r13, gword ptr [rbp-0x110]
       mov      edi, dword ptr [rbp-0xF8]
       mov      dword ptr [r13+0x10], edi
       jmp      SHORT G_M000_IG81
 
G_M000_IG89:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG92
 
G_M000_IG90:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       dec      edi
       mov      ecx, edi
       mov      edx, dword ptr [rax+0x08]
       cmp      ecx, edx
       jae      G_M000_IG155
       cmp      dword ptr [rax+4*rcx+0x10], 22
       je       SHORT G_M000_IG92
 
G_M000_IG91:
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 22
 
G_M000_IG92:
       mov      rdi, 0xD1FFAB1E
       mov      r15, gword ptr [rdi]
       mov      r12, gword ptr [r15+0x20]
       test     r12, r12
       je       SHORT G_M000_IG94
 
G_M000_IG93:
       cmp      dword ptr [r12+0x08], 1
       je       SHORT G_M000_IG95
 
G_M000_IG94:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r12, rax
       lea      rdi, bword ptr [r15+0x20]
       mov      rsi, r12
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG95:
       mov      rdi, r12
       mov      rdx, r14
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, r12
       mov      rsi, gword ptr [rbp+0x1D8]
       mov      edi, 2
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
       mov      r15, qword ptr [rbp+0x180]
       mov      r14, qword ptr [rbp+0x170]
       add      r15, r14
       jo       G_M000_IG156
       mov      qword ptr [rbp+0x170], r14
       test     r14, r14
       setg     dil
       movzx    rdi, dil
       mov      r12, qword ptr [rbp+0x178]
       cmp      r15, r12
       setl     al
       movzx    rax, al
       and      eax, edi
       cmp      r15, r12
       setg     cl
       movzx    rcx, cl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, ecx
       or       edi, eax
       je       G_M000_IG146
 
G_M000_IG96:
       lea      r13, [r15+0x05]
       cmp      r13, 261
       jbe      G_M000_IG108
 
G_M000_IG97:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r15
 
G_M000_IG98:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG101
 
G_M000_IG99:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rcx, rax
       dec      edi
       mov      edx, edi
       mov      r8d, dword ptr [rcx+0x08]
       cmp      edx, r8d
       jae      G_M000_IG155
       cmp      dword ptr [rcx+4*rdx+0x10], 18
       je       SHORT G_M000_IG101
 
G_M000_IG100:
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 18
 
G_M000_IG101:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      r13, rax
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG104
 
G_M000_IG102:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       lea      ecx, [rdi-0x01]
       mov      edx, ecx
       mov      r8d, dword ptr [rax+0x08]
       cmp      edx, r8d
       jae      G_M000_IG155
       mov      r8d, edx
       cmp      dword ptr [rax+4*r8+0x10], 19
       je       SHORT G_M000_IG104
 
G_M000_IG103:
       mov      eax, ecx
       mov      dword ptr [rsi+4*rax+0x10], 19
 
G_M000_IG104:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      gword ptr [rbp+0x1C0], rsi
       test     edi, edi
       jle      SHORT G_M000_IG107
 
G_M000_IG105:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rcx, rsi
       dec      edi
       mov      edx, edi
       mov      r8d, dword ptr [rcx+0x08]
       cmp      edx, r8d
       jae      G_M000_IG155
       cmp      dword ptr [rcx+4*rdx+0x10], 20
       je       SHORT G_M000_IG107
 
G_M000_IG106:
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 20
 
G_M000_IG107:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp+0x130], rcx
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x108], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, r13
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x40]
       mov      rsi, gword ptr [rbp-0x108]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdx, gword ptr [rbp-0x38]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp+0x128], rax
       mov      rdi, gword ptr [rbp+0x130]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x120], rax
       mov      rdi, gword ptr [rbp+0x128]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rdi, qword ptr [rbp+0x120]
       mov      rcx, rdi
       mov      edx, 1
       mov      gword ptr [rbp+0x1C8], r13
       mov      qword ptr [rbp+0x180], r15
       mov      qword ptr [rbp+0x178], r12
       mov      r12, rax
       mov      r13, rdx
       mov      r15, rcx
       mov      r14, gword ptr [rbp+0x1C0]
       jmp      G_M000_IG59
 
G_M000_IG108:
       test     r13, r13
       jl       G_M000_IG156
       mov      rsi, r13
       mov      rdi, 0xD1FFAB1E
       mov      r13, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG155
       mov      rsi, gword ptr [r13+8*rsi+0x10]
       jmp      G_M000_IG98
 
G_M000_IG109:
       test     rax, rax
       jl       G_M000_IG156
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG155
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       jmp      G_M000_IG62
 
G_M000_IG110:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG83
 
G_M000_IG111:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG112
       mov      edi, dword ptr [r13+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG113
 
G_M000_IG112:
       mov      ecx, eax
       jmp      SHORT G_M000_IG119
 
G_M000_IG113:
       cmp      edi, 2
       je       SHORT G_M000_IG114
       cmp      dword ptr [r13+0x08], 0
       jbe      G_M000_IG155
       mov      edi, dword ptr [r13+0x10]
       jmp      SHORT G_M000_IG115
 
G_M000_IG114:
       mov      edi, dword ptr [r13+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r13+0x10]
       or       rdi, rcx
 
G_M000_IG115:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG116
       mov      ecx, -1
       jmp      SHORT G_M000_IG118
 
G_M000_IG116:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG117
       mov      ecx, 1
       jmp      SHORT G_M000_IG118
 
G_M000_IG117:
       xor      ecx, ecx
 
G_M000_IG118:
       imul     ecx, eax
 
G_M000_IG119:
       test     ecx, ecx
       jge      G_M000_IG77
       mov      dword ptr [rbp-0xF8], eax
       jmp      G_M000_IG88
 
G_M000_IG120:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG121
       mov      edi, dword ptr [r13+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG122
 
G_M000_IG121:
       mov      dword ptr [rbp-0xF8], eax
       mov      edi, eax
       mov      eax, dword ptr [rbp-0xF8]
       jmp      G_M000_IG79
 
G_M000_IG122:
       cmp      edi, 2
       je       SHORT G_M000_IG123
       cmp      dword ptr [r13+0x08], 0
       jbe      G_M000_IG155
       mov      edi, dword ptr [r13+0x10]
       jmp      SHORT G_M000_IG124
 
G_M000_IG123:
       mov      edi, dword ptr [r13+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r13+0x10]
       or       rdi, rcx
 
G_M000_IG124:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG125
       mov      ecx, -1
       jmp      SHORT G_M000_IG127
 
G_M000_IG125:
       test     rdi, rdi
       jge      SHORT G_M000_IG126
       mov      ecx, 1
       jmp      SHORT G_M000_IG127
 
G_M000_IG126:
       xor      ecx, ecx
 
G_M000_IG127:
       mov      edi, eax
       imul     edi, ecx
       jmp      G_M000_IG79
 
G_M000_IG128:
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG129
       mov      edi, dword ptr [r14+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG130
 
G_M000_IG129:
       mov      eax, r13d
       jmp      SHORT G_M000_IG136
 
G_M000_IG130:
       cmp      edi, 2
       je       SHORT G_M000_IG131
       cmp      dword ptr [r14+0x08], 0
       jbe      G_M000_IG155
       mov      edi, dword ptr [r14+0x10]
       jmp      SHORT G_M000_IG132
 
G_M000_IG131:
       mov      edi, dword ptr [r14+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r14+0x10]
       or       rdi, rax
 
G_M000_IG132:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG133
       mov      eax, -1
       jmp      SHORT G_M000_IG135
 
G_M000_IG133:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jbe      SHORT G_M000_IG134
       mov      eax, 1
       jmp      SHORT G_M000_IG135
 
G_M000_IG134:
       xor      eax, eax
 
G_M000_IG135:
       imul     eax, r13d
 
G_M000_IG136:
       test     eax, eax
       jl       G_M000_IG34
       jmp      G_M000_IG28
 
G_M000_IG137:
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG138
       mov      edi, dword ptr [r14+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG139
 
G_M000_IG138:
       mov      edi, r13d
       jmp      G_M000_IG30
 
G_M000_IG139:
       cmp      edi, 2
       je       SHORT G_M000_IG140
       cmp      dword ptr [r14+0x08], 0
       jbe      G_M000_IG155
       mov      edi, dword ptr [r14+0x10]
       jmp      SHORT G_M000_IG141
 
G_M000_IG140:
       mov      edi, dword ptr [r14+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r14+0x10]
       or       rdi, rax
 
G_M000_IG141:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG142
       mov      eax, -1
       jmp      SHORT G_M000_IG144
 
G_M000_IG142:
       test     rdi, rdi
       jge      SHORT G_M000_IG143
       mov      eax, 1
       jmp      SHORT G_M000_IG144
 
G_M000_IG143:
       xor      eax, eax
 
G_M000_IG144:
       mov      edi, r13d
       imul     edi, eax
       jmp      G_M000_IG30
 
G_M000_IG145:
       mov      r13, 0xD1FFAB1E
       jmp      G_M000_IG53
 
G_M000_IG146:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG149
 
G_M000_IG147:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      ecx, dword ptr [(reloc)]
       dec      ecx
       cmp      ecx, dword ptr [rax+0x08]
       jae      SHORT G_M000_IG155
       cmp      dword ptr [rax+4*rcx+0x10], 23
       je       SHORT G_M000_IG149
 
G_M000_IG148:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      ecx, dword ptr [(reloc)]
       dec      ecx
       cmp      ecx, dword ptr [rax+0x08]
       jae      SHORT G_M000_IG155
       mov      dword ptr [rax+4*rcx+0x10], 23
 
G_M000_IG149:
       mov      rbx, qword ptr [rbp+0x1B0]
       test     rbx, rbx
       jle      SHORT G_M000_IG152
 
G_M000_IG150:
       movsxd   rax, dword ptr [(reloc)]
       cmp      rbx, rax
       jg       SHORT G_M000_IG152
 
G_M000_IG151:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG152:
       mov      rax, gword ptr [rbp+0x1D8]
 
G_M000_IG153:
       add      rsp, 808
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG154:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG155:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG156:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 3969

; Assembly listing for method RyuPy.Generated.Module:m0_accumulate(System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x1ac
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 3
; 40 inlinees with PGO data; 42 single block inlinees; 43 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 352
       mov      qword ptr [rsp+0x328], r15
       mov      qword ptr [rsp+0x320], r14
       mov      qword ptr [rsp+0x318], r13
       mov      qword ptr [rsp+0x310], r12
       mov      qword ptr [rsp+0x308], rbx
       lea      rbp, [rsp+0x160]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xF0], zmm8
       vmovdqu32 zmmword ptr [rbp-0xB0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x70], zmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, gword ptr [rbp+0x1A0]
       mov      rdi, qword ptr [rbp+0x158]
       mov      rax, qword ptr [rbp+0x150]
       mov      rcx, qword ptr [rbp+0x148]
       mov      r15, qword ptr [rbp+0xF0]
       mov      r14, qword ptr [rbp+0xE8]
       mov      r13, qword ptr [rbp+0xE0]
       mov      r12, gword ptr [rbp+0x98]
 
G_M000_IG02:
       jmp      G_M000_IG57
 
G_M000_IG03:
       test     r12, r12
       jl       G_M000_IG142
       mov      rdi, r12
       mov      rax, 0xD1FFAB1E
       mov      r8, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG141
       mov      r12, gword ptr [r8+8*rdi+0x10]
       jmp      G_M000_IG54
 
G_M000_IG04:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG65
 
G_M000_IG05:
       test     rdx, rdx
       je       SHORT G_M000_IG10
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG10
 
G_M000_IG07:
       test     r12, r12
       je       SHORT G_M000_IG10
 
G_M000_IG08:
       mov      rcx, qword ptr [r12]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      SHORT G_M000_IG10
 
G_M000_IG09:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp+0x50], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       vmovsd   xmm0, qword ptr [rbp+0x50]
       vmovsd   qword ptr [r12+0x08], xmm0
       mov      rax, qword ptr [rbp+0x150]
       mov      rcx, qword ptr [rbp+0x148]
       mov      rdi, qword ptr [rbp+0x158]
       jmp      G_M000_IG57
 
G_M000_IG10:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG35
 
G_M000_IG11:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG66
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG33
 
G_M000_IG13:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x60], rax
       lea      rdi, [rbp-0x80]
       mov      rdx, rsi
       mulx     rdx, rcx, rax
       mov      qword ptr [rdi], rcx
       mov      rcx, qword ptr [rbp-0x80]
       mov      qword ptr [rbp-0x78], rcx
       mov      rdi, rsi
       sar      rdi, 63
       and      rdi, rax
       sub      rdx, rdi
       mov      rdi, rax
       sar      rdi, 63
       and      rdi, rsi
       sub      rdx, rdi
       mov      rdi, rcx
       sar      rdi, 63
       cmp      rdx, rdi
       je       G_M000_IG29
 
G_M000_IG14:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x70]
       mov      gword ptr [rbp-0x150], rax
       mov      ecx, dword ptr [rbp-0x68]
       mov      dword ptr [rbp-0x108], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x90], xmm0
       lea      rdi, [rbp-0x90]
       mov      rsi, qword ptr [rbp-0x60]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x90]
       mov      r9d, dword ptr [rbp-0x88]
       mov      rcx, gword ptr [rbp-0x150]
       test     rcx, rcx
       jne      G_M000_IG22
 
G_M000_IG15:
       test     rax, rax
       jne      G_M000_IG104
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xA0], xmm0
       movsxd   rsi, dword ptr [rbp-0x108]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xA0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xA0]
       mov      r10d, dword ptr [rbp-0x98]
 
G_M000_IG16:
       test     rax, rax
       jne      G_M000_IG105
 
G_M000_IG17:
       test     rax, rax
       jne      G_M000_IG114
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      G_M000_IG27
 
G_M000_IG18:
       mov      edi, -1
 
G_M000_IG19:
       test     edi, edi
       mov      gword ptr [rbp-0x158], rax
       mov      dword ptr [rbp-0x10C], r10d
       jg       SHORT G_M000_IG28
 
G_M000_IG20:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x128], rax
       mov      rdi, gword ptr [rbp-0x158]
       mov      esi, dword ptr [rbp-0x10C]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rdx, gword ptr [rbp-0x128]
       mov      qword ptr [rdx+0x08], rax
 
G_M000_IG21:
       mov      rax, rdx
       jmp      G_M000_IG67
 
G_M000_IG22:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG23:
       test     rax, rax
       je       SHORT G_M000_IG26
 
G_M000_IG24:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG25:
       mov      edx, dword ptr [rbp-0x108]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      r10d, ecx
       jmp      G_M000_IG16
 
G_M000_IG26:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG25
 
G_M000_IG27:
       xor      edi, edi
       jmp      G_M000_IG19
 
G_M000_IG28:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      gword ptr [rbp-0x120], rdx
       lea      rdi, bword ptr [rdx+0x08]
       mov      rsi, gword ptr [rbp-0x158]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdx, gword ptr [rbp-0x120]
       mov      edi, dword ptr [rbp-0x10C]
       mov      dword ptr [rdx+0x10], edi
       jmp      SHORT G_M000_IG21
 
G_M000_IG29:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG32
 
G_M000_IG30:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x78]
       mov      qword ptr [rax+0x08], rdi
       mov      rdx, rax
 
G_M000_IG31:
       jmp      G_M000_IG21
 
G_M000_IG32:
       test     rax, rax
       jl       G_M000_IG142
       mov      rdx, rax
       mov      rdi, 0xD1FFAB1E
       mov      r8, gword ptr [rdi]
       cmp      rdx, 262
       jae      G_M000_IG141
       mov      rdx, gword ptr [r8+8*rdx+0x10]
       jmp      SHORT G_M000_IG31
 
G_M000_IG33:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG66
 
G_M000_IG34:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x58], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xA8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xA8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG67
 
G_M000_IG35:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG66
 
G_M000_IG36:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm0
       test     rdx, rdx
       je       G_M000_IG66
 
G_M000_IG37:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG39
 
G_M000_IG38:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG67
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG66
 
G_M000_IG40:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG67
 
G_M000_IG41:
       mov      rsi, qword ptr [r12+0x08]
       test     rax, rax
       je       G_M000_IG55
 
G_M000_IG42:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG97
 
G_M000_IG43:
       mov      r12, qword ptr [rax+0x08]
       lea      rax, [rsi+r12]
       mov      qword ptr [rbp-0xD0], rax
       mov      rdi, rsi
       xor      rdi, rax
       mov      rcx, r12
       xor      rcx, rax
       and      rdi, rcx
       jge      G_M000_IG52
 
G_M000_IG44:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xE0], xmm0
       lea      rdi, [rbp-0xE0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xE0]
       mov      gword ptr [rbp-0x160], rax
       mov      ecx, dword ptr [rbp-0xD8]
       mov      dword ptr [rbp-0x110], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xF0], xmm0
       lea      rdi, [rbp-0xF0]
       mov      rsi, r12
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x160]
       mov      esi, dword ptr [rbp-0x110]
       mov      rdx, gword ptr [rbp-0xF0]
       mov      rcx, qword ptr [rbp-0xE8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      r12, rax
       mov      eax, edx
       mov      dword ptr [rbp-0x114], edx
       test     r12, r12
       jne      G_M000_IG122
 
G_M000_IG45:
       test     r12, r12
       jne      G_M000_IG131
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      SHORT G_M000_IG50
 
G_M000_IG46:
       mov      edi, -1
 
G_M000_IG47:
       test     edi, edi
       jg       SHORT G_M000_IG51
 
G_M000_IG48:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x140], rax
       mov      rdi, r12
       mov      esi, dword ptr [rbp-0x114]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r12, gword ptr [rbp-0x140]
       mov      qword ptr [r12+0x08], rax
 
G_M000_IG49:
       jmp      G_M000_IG56
 
G_M000_IG50:
       xor      edi, edi
       jmp      SHORT G_M000_IG47
 
G_M000_IG51:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x138], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r12
       call     CORINFO_HELP_ASSIGN_REF
       mov      r12, gword ptr [rbp-0x138]
       mov      edi, dword ptr [rbp-0x114]
       mov      dword ptr [r12+0x10], edi
       jmp      SHORT G_M000_IG49
 
G_M000_IG52:
       lea      r12, [rax+0x05]
       cmp      r12, 261
       jbe      G_M000_IG03
 
G_M000_IG53:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, qword ptr [rbp-0xD0]
       mov      qword ptr [rax+0x08], r12
       mov      r12, rax
 
G_M000_IG54:
       jmp      SHORT G_M000_IG49
 
G_M000_IG55:
       mov      rdx, rax
       mov      rsi, r12
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r12, rax
 
G_M000_IG56:
       mov      rax, qword ptr [rbp+0x150]
       mov      rcx, qword ptr [rbp+0x148]
       mov      rdi, qword ptr [rbp+0x158]
 
G_M000_IG57:
       add      r15, r13
       jo       G_M000_IG142
 
G_M000_IG58:
       test     r13, r13
       setg     sil
       movzx    rsi, sil
       cmp      r15, r14
       setl     r8b
       movzx    r8, r8b
       and      r8d, esi
       cmp      r15, r14
       setg     r9b
       movzx    r9, r9b
       test     esi, esi
       sete     sil
       movzx    rsi, sil
       and      esi, r9d
       or       esi, r8d
       je       G_M000_IG88
 
G_M000_IG59:
       lea      rsi, [r15+0x05]
       cmp      rsi, 261
       jbe      G_M000_IG96
 
G_M000_IG60:
       mov      qword ptr [rbp+0x158], rdi
       mov      qword ptr [rbp+0x150], rax
       mov      qword ptr [rbp+0x148], rcx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r15
 
G_M000_IG61:
       mov      r8d, dword ptr [(reloc)]
       test     r8d, r8d
       jle      SHORT G_M000_IG64
 
G_M000_IG62:
       mov      r9, 0xD1FFAB1E
       mov      r9, gword ptr [r9]
       mov      r10, r9
       dec      r8d
       mov      r11d, r8d
       cmp      r11d, dword ptr [r10+0x08]
       jae      G_M000_IG141
       cmp      dword ptr [r10+4*r11+0x10], 11
       je       SHORT G_M000_IG64
 
G_M000_IG63:
       cmp      r8d, dword ptr [r9+0x08]
       jae      G_M000_IG141
       mov      r8d, r8d
       mov      dword ptr [r9+4*r8+0x10], 11
 
G_M000_IG64:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       test     rax, rax
       jne      G_M000_IG04
 
G_M000_IG65:
       test     rax, rax
       jne      G_M000_IG10
 
G_M000_IG66:
       mov      rsi, rax
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG67:
       test     r12, r12
       je       G_M000_IG55
 
G_M000_IG68:
       mov      rcx, qword ptr [r12]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       je       G_M000_IG41
 
G_M000_IG69:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       je       G_M000_IG99
 
G_M000_IG70:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      G_M000_IG55
 
G_M000_IG71:
       mov      rsi, r12
       mov      gword ptr [rbp-0x130], rsi
       test     rax, rax
       je       G_M000_IG55
 
G_M000_IG72:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG55
 
G_M000_IG73:
       mov      r12, rax
       cmp      dword ptr [rsi+0x08], 0
       je       G_M000_IG78
 
G_M000_IG74:
       cmp      dword ptr [r12+0x08], 0
       je       G_M000_IG77
 
G_M000_IG75:
       mov      eax, dword ptr [rsi+0x08]
       mov      dword ptr [rbp-0x104], eax
       mov      edi, eax
       add      edi, dword ptr [r12+0x08]
       js       G_M000_IG140
       movsxd   rsi, edi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x148], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rsi, gword ptr [rbp-0x130]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0x104]
       mov      rax, gword ptr [rbp-0x148]
       lea      rdi, bword ptr [rax+2*rdi+0x0C]
       mov      edx, dword ptr [r12+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r12+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      r12, gword ptr [rbp-0x148]
 
G_M000_IG76:
       jmp      G_M000_IG56
 
G_M000_IG77:
       mov      r12, rsi
       jmp      SHORT G_M000_IG76
 
G_M000_IG78:
       cmp      dword ptr [r12+0x08], 0
       je       G_M000_IG139
 
G_M000_IG79:
       jmp      SHORT G_M000_IG76
 
G_M000_IG80:
       mov      esi, dword ptr [(reloc)]
       test     esi, esi
       jle      SHORT G_M000_IG83
 
G_M000_IG81:
       mov      rax, 0xD1FFAB1E
       mov      r8, gword ptr [rax]
       mov      rax, r8
       lea      r10d, [rsi-0x01]
       mov      ecx, r10d
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG141
       cmp      dword ptr [rax+4*rcx+0x10], 12
       je       SHORT G_M000_IG83
 
G_M000_IG82:
       mov      rax, r8
       mov      ecx, r10d
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG141
       mov      dword ptr [rax+4*rcx+0x10], 12
 
G_M000_IG83:
       mov      rdx, qword ptr [rbp+0x180]
       test     rdx, rdx
       jle      SHORT G_M000_IG86
 
G_M000_IG84:
       movsxd   rax, esi
       cmp      rdx, rax
       jg       SHORT G_M000_IG86
 
G_M000_IG85:
       dec      edx
       mov      dword ptr [(reloc)], edx
 
G_M000_IG86:
       mov      rax, r12
 
G_M000_IG87:
       add      rsp, 776
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG88:
       add      rdi, rcx
       jo       G_M000_IG142
       mov      qword ptr [rbp+0x148], rcx
       test     rcx, rcx
       setg     sil
       movzx    rsi, sil
       cmp      rdi, rax
       setl     r8b
       movzx    r8, r8b
       and      r8d, esi
       mov      qword ptr [rbp+0x158], rdi
       mov      qword ptr [rbp+0x150], rax
       cmp      rdi, rax
       setg     r9b
       movzx    r9, r9b
       test     esi, esi
       sete     sil
       movzx    rsi, sil
       and      esi, r9d
       or       esi, r8d
       je       G_M000_IG80
 
G_M000_IG89:
       mov      esi, dword ptr [(reloc)]
       test     esi, esi
       jle      SHORT G_M000_IG92
 
G_M000_IG90:
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      r9, r8
       lea      r10d, [rsi-0x01]
       mov      r11d, r10d
       cmp      r11d, dword ptr [r9+0x08]
       jae      G_M000_IG141
       cmp      dword ptr [r9+4*r11+0x10], 9
       je       SHORT G_M000_IG92
 
G_M000_IG91:
       cmp      r10d, dword ptr [r8+0x08]
       jae      G_M000_IG141
       mov      r9d, r10d
       mov      dword ptr [r8+4*r9+0x10], 9
 
G_M000_IG92:
       mov      r8, 0xD1FFAB1E
       mov      r12, gword ptr [r8]
       test     esi, esi
       jle      SHORT G_M000_IG95
 
G_M000_IG93:
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      r9, r8
       lea      r10d, [rsi-0x01]
       mov      esi, r10d
       cmp      esi, dword ptr [r9+0x08]
       jae      G_M000_IG141
       cmp      dword ptr [r9+4*rsi+0x10], 10
       je       SHORT G_M000_IG95
 
G_M000_IG94:
       mov      rsi, r8
       mov      r8d, r10d
       cmp      r8d, dword ptr [rsi+0x08]
       jae      G_M000_IG141
       mov      dword ptr [rsi+4*r8+0x10], 10
 
G_M000_IG95:
       mov      rsi, 0xD1FFAB1E
       mov      r15, gword ptr [rsi]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r14, rax
       lea      rdi, bword ptr [r14+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x40]
       mov      rsi, r14
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdx, gword ptr [rbp-0x38]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      r13, rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r15, rax
       mov      rdi, r13
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r14, rax
       mov      r13d, 1
       mov      rax, qword ptr [rbp+0x150]
       mov      rcx, qword ptr [rbp+0x148]
       mov      rdi, qword ptr [rbp+0x158]
       jmp      G_M000_IG58
 
G_M000_IG96:
       test     rsi, rsi
       jl       G_M000_IG142
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       cmp      rsi, 262
       jae      G_M000_IG141
       mov      rsi, gword ptr [r8+8*rsi+0x10]
       mov      qword ptr [rbp+0x158], rdi
       mov      qword ptr [rbp+0x150], rax
       mov      qword ptr [rbp+0x148], rcx
       jmp      G_M000_IG61
 
G_M000_IG97:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG55
 
G_M000_IG98:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xC8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xF8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xF8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xC8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG56
 
G_M000_IG99:
       vmovsd   xmm0, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0xB8], xmm0
       test     rax, rax
       je       G_M000_IG55
 
G_M000_IG100:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG102
 
G_M000_IG101:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xC0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xB8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xC0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG56
 
G_M000_IG102:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG55
 
G_M000_IG103:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x100], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xB8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x100]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG56
 
G_M000_IG104:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG23
 
G_M000_IG105:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG106
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG107
 
G_M000_IG106:
       mov      ecx, r10d
       jmp      SHORT G_M000_IG113
 
G_M000_IG107:
       cmp      edi, 2
       je       SHORT G_M000_IG108
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG141
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG109
 
G_M000_IG108:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [rax+0x10]
       or       rdi, rcx
 
G_M000_IG109:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG110
       mov      ecx, -1
       jmp      SHORT G_M000_IG112
 
G_M000_IG110:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG111
       mov      ecx, 1
       jmp      SHORT G_M000_IG112
 
G_M000_IG111:
       xor      ecx, ecx
 
G_M000_IG112:
       imul     ecx, r10d
 
G_M000_IG113:
       test     ecx, ecx
       jge      G_M000_IG17
       mov      gword ptr [rbp-0x158], rax
       mov      dword ptr [rbp-0x10C], r10d
       jmp      G_M000_IG28
 
G_M000_IG114:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG115
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG116
 
G_M000_IG115:
       mov      dword ptr [rbp-0x10C], r10d
       mov      edi, r10d
       mov      r10d, dword ptr [rbp-0x10C]
       jmp      G_M000_IG19
 
G_M000_IG116:
       cmp      edi, 2
       je       SHORT G_M000_IG117
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG141
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG118
 
G_M000_IG117:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      gword ptr [rbp-0x158], rax
       mov      ecx, dword ptr [rax+0x10]
       or       rdi, rcx
       mov      rax, gword ptr [rbp-0x158]
 
G_M000_IG118:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG119
       mov      ecx, -1
       jmp      SHORT G_M000_IG121
 
G_M000_IG119:
       test     rdi, rdi
       jge      SHORT G_M000_IG120
       mov      ecx, 1
       jmp      SHORT G_M000_IG121
 
G_M000_IG120:
       xor      ecx, ecx
 
G_M000_IG121:
       mov      edi, r10d
       imul     edi, ecx
       jmp      G_M000_IG19
 
G_M000_IG122:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG123
       mov      edi, dword ptr [r12+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG124
 
G_M000_IG123:
       mov      ecx, eax
       jmp      SHORT G_M000_IG130
 
G_M000_IG124:
       cmp      edi, 2
       je       SHORT G_M000_IG125
       cmp      dword ptr [r12+0x08], 0
       jbe      G_M000_IG141
       mov      edi, dword ptr [r12+0x10]
       jmp      SHORT G_M000_IG126
 
G_M000_IG125:
       mov      edi, dword ptr [r12+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r12+0x10]
       or       rdi, rcx
 
G_M000_IG126:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG127
       mov      ecx, -1
       jmp      SHORT G_M000_IG129
 
G_M000_IG127:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG128
       mov      ecx, 1
       jmp      SHORT G_M000_IG129
 
G_M000_IG128:
       xor      ecx, ecx
 
G_M000_IG129:
       imul     ecx, eax
 
G_M000_IG130:
       test     ecx, ecx
       jge      G_M000_IG45
       jmp      G_M000_IG51
 
G_M000_IG131:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG132
       mov      edi, dword ptr [r12+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG133
 
G_M000_IG132:
       mov      edi, eax
       mov      eax, dword ptr [rbp-0x114]
       jmp      G_M000_IG47
 
G_M000_IG133:
       cmp      edi, 2
       je       SHORT G_M000_IG134
       cmp      dword ptr [r12+0x08], 0
       jbe      SHORT G_M000_IG141
       mov      edi, dword ptr [r12+0x10]
       jmp      SHORT G_M000_IG135
 
G_M000_IG134:
       mov      edi, dword ptr [r12+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r12+0x10]
       or       rdi, rcx
 
G_M000_IG135:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG136
       mov      ecx, -1
       jmp      SHORT G_M000_IG138
 
G_M000_IG136:
       test     rdi, rdi
       jge      SHORT G_M000_IG137
       mov      ecx, 1
       jmp      SHORT G_M000_IG138
 
G_M000_IG137:
       xor      ecx, ecx
 
G_M000_IG138:
       mov      edi, eax
       imul     edi, ecx
       jmp      G_M000_IG47
 
G_M000_IG139:
       mov      r12, 0xD1FFAB1E
       jmp      G_M000_IG76
 
G_M000_IG140:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG141:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG142:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 3625

; Assembly listing for method RyuPy.Generated.Module:m0_accumulate(System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 448
       lea      rbp, [rsp+0x1C0]
       xor      eax, eax
       mov      qword ptr [rbp-0x1B8], rax
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -384
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x20], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x10], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      gword ptr [rbp-0x30], rdi
 
G_M000_IG02:
       mov      dword ptr [rbp-0x1B0], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x50], rax
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x58], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x60], rax
       mov      rdi, gword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x68], rax
       mov      rdi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x70], rax
       mov      rax, qword ptr [rbp-0x68]
       mov      qword ptr [rbp-0x78], rax
       mov      rax, qword ptr [rbp-0x70]
       mov      qword ptr [rbp-0x80], rax
       mov      qword ptr [rbp-0x88], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 78
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       cmp      qword ptr [rbp-0x88], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x8C], eax
       mov      rax, qword ptr [rbp-0x78]
       cmp      rax, qword ptr [rbp-0x80]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x90], eax
       mov      rax, qword ptr [rbp-0x78]
       cmp      rax, qword ptr [rbp-0x80]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x94], eax
       mov      eax, dword ptr [rbp-0x8C]
       and      eax, dword ptr [rbp-0x90]
       movzx    rax, al
       mov      dword ptr [rbp-0x98], eax
       cmp      dword ptr [rbp-0x8C], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x9C], eax
       mov      eax, dword ptr [rbp-0x9C]
       and      eax, dword ptr [rbp-0x94]
       movzx    rax, al
       mov      dword ptr [rbp-0xA0], eax
       mov      eax, dword ptr [rbp-0x98]
       or       eax, dword ptr [rbp-0xA0]
       movzx    rax, al
       mov      dword ptr [rbp-0xA4], eax
       cmp      dword ptr [rbp-0xA4], 0
       jne      SHORT G_M000_IG06
       jmp      G_M000_IG08
 
G_M000_IG06:
       mov      edi, 9
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xB8], rax
       mov      rax, gword ptr [rbp-0xB8]
       mov      gword ptr [rbp-0x40], rax
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xC0], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x198], rax
       mov      rdi, gword ptr [rbp-0x198]
       mov      rdx, gword ptr [rbp-0x30]
       xor      esi, esi
       call     CORINFO_HELP_ARRADDR_ST
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x1A0], rax
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x1B8], rax
       mov      rcx, gword ptr [rbp-0x1B8]
       mov      rsi, gword ptr [rbp-0x198]
       mov      rdx, gword ptr [rbp-0x1A0]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:CallBuiltin(System.String,System.Object[],System.Object[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0xC8], rax
       mov      rdi, gword ptr [rbp-0xC0]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0xD0], rax
       mov      rdi, gword ptr [rbp-0xC8]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0xD8], rax
       mov      rax, qword ptr [rbp-0xD0]
       mov      qword ptr [rbp-0xE0], rax
       mov      rax, qword ptr [rbp-0xD8]
       mov      qword ptr [rbp-0xE8], rax
       mov      qword ptr [rbp-0xF0], 1
       jmp      SHORT G_M000_IG09
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x78]
       add      rax, qword ptr [rbp-0x88]
       jo       G_M000_IG34
       mov      qword ptr [rbp-0x78], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      edi, 12
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0x140], rax
       jmp      G_M000_IG16
 
G_M000_IG09:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG11
 
G_M000_IG10:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 285
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG11:
       cmp      qword ptr [rbp-0xF0], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0xF4], eax
       mov      rax, qword ptr [rbp-0xE0]
       cmp      rax, qword ptr [rbp-0xE8]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0xF8], eax
       mov      rax, qword ptr [rbp-0xE0]
       cmp      rax, qword ptr [rbp-0xE8]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0xFC], eax
       mov      eax, dword ptr [rbp-0xF4]
       and      eax, dword ptr [rbp-0xF8]
       movzx    rax, al
       mov      dword ptr [rbp-0x100], eax
       cmp      dword ptr [rbp-0xF4], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x104], eax
       mov      eax, dword ptr [rbp-0x104]
       and      eax, dword ptr [rbp-0xFC]
       movzx    rax, al
       mov      dword ptr [rbp-0x108], eax
       mov      eax, dword ptr [rbp-0x100]
       or       eax, dword ptr [rbp-0x108]
       movzx    rax, al
       mov      dword ptr [rbp-0x10C], eax
       cmp      dword ptr [rbp-0x10C], 0
       jne      SHORT G_M000_IG14
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG13
 
G_M000_IG12:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 351
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG13:
       jmp      G_M000_IG07
 
G_M000_IG14:
       mov      rdi, qword ptr [rbp-0xE0]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0x118], rax
       mov      rax, gword ptr [rbp-0x118]
       mov      gword ptr [rbp-0x48], rax
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x48]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x120], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x128], rax
       mov      rax, gword ptr [rbp-0x120]
       mov      gword ptr [rbp-0x148], rax
       mov      rax, gword ptr [rbp-0x128]
       mov      gword ptr [rbp-0x158], rax
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0x168], rax
       jmp      G_M000_IG31
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0xE0]
       add      rax, qword ptr [rbp-0xF0]
       jo       G_M000_IG34
       mov      qword ptr [rbp-0xE0], rax
       jmp      G_M000_IG09
 
G_M000_IG16:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x50]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x140]
 
G_M000_IG17:
       add      rsp, 448
       pop      rbp
       ret      
 
G_M000_IG18:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG20
 
G_M000_IG19:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 428
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG20:
       mov      rax, gword ptr [rbp-0x138]
       mov      gword ptr [rbp-0x40], rax
       jmp      G_M000_IG15
 
G_M000_IG21:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG23
 
G_M000_IG22:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 436
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG23:
       mov      rdx, gword ptr [rbp-0x120]
       mov      rcx, gword ptr [rbp-0x128]
       xor      edi, edi
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x130], rax
       mov      rdx, gword ptr [rbp-0x40]
       mov      rcx, gword ptr [rbp-0x130]
       mov      edi, 1
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x138], rax
       jmp      G_M000_IG18
 
G_M000_IG24:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x148]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x150], xmm0
       mov      rsi, gword ptr [rbp-0x158]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x160], xmm0
       mov      rsi, gword ptr [rbp-0x168]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x170], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x150]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x160]
       vmovsd   qword ptr [rbp-0x178], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x170]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rbp-0x180], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x1A8], rax
       mov      rax, gword ptr [rbp-0x1A8]
       vmovsd   xmm0, qword ptr [rbp-0x180]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x1A8]
       mov      gword ptr [rbp-0x138], rax
       jmp      G_M000_IG18
 
G_M000_IG25:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG27
 
G_M000_IG26:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 525
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG27:
       mov      rsi, gword ptr [rbp-0x168]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_ISINSTANCEOFCLASS
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x184], eax
       cmp      dword ptr [rbp-0x184], 0
       jne      G_M000_IG24
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG21
 
G_M000_IG28:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG30
 
G_M000_IG29:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 552
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG30:
       mov      rsi, gword ptr [rbp-0x158]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_ISINSTANCEOFCLASS
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x188], eax
       cmp      dword ptr [rbp-0x188], 0
       jne      G_M000_IG25
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG21
 
G_M000_IG31:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG33
 
G_M000_IG32:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 579
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG33:
       mov      rsi, gword ptr [rbp-0x148]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_ISINSTANCEOFCLASS
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x18C], eax
       cmp      dword ptr [rbp-0x18C], 0
       jne      G_M000_IG28
       jmp      G_M000_IG21
 
G_M000_IG34:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1910

; Assembly listing for method RyuPy.Generated.Module:m0_accumulate(System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x243
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 9
; 25 inlinees with PGO data; 6 single block inlinees; 4 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 256
       mov      qword ptr [rsp+0x2C8], r15
       mov      qword ptr [rsp+0x2C0], r14
       mov      qword ptr [rsp+0x2B8], r13
       mov      qword ptr [rsp+0x2B0], r12
       mov      qword ptr [rsp+0x2A8], rbx
       lea      rbp, [rsp+0x100]
       xor      eax, eax
       mov      qword ptr [rbp-0xC8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       vmovdqa  xmmword ptr [rbp-0x40], xmm8
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, gword ptr [rbp+0x1A0]
       mov      r14, gword ptr [rbp+0x190]
       mov      r11, qword ptr [rbp+0x148]
       mov      r15, qword ptr [rbp+0xF0]
       mov      r13, qword ptr [rbp+0xE8]
       mov      r12, qword ptr [rbp+0xE0]
       mov      rsi, gword ptr [rbp+0xB0]
       mov      rdx, gword ptr [rbp+0xA8]
       mov      rax, gword ptr [rbp+0x88]
       mov      rcx, gword ptr [rbp+0x78]
       mov      r8, gword ptr [rbp+0x68]
 
G_M000_IG02:
       mov      qword ptr [rbp+0x148], r11
       test     r11, r11
       setg     dil
       movzx    rdi, dil
       mov      dword ptr [rbp-0xE4], edi
       jmp      G_M000_IG58
 
G_M000_IG03:
       test     rsi, rsi
       je       G_M000_IG25
 
G_M000_IG04:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG16
 
G_M000_IG05:
       mov      rax, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x58], rax
       test     rdx, rdx
       je       G_M000_IG25
 
G_M000_IG06:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdx], rcx
       jne      G_M000_IG14
 
G_M000_IG07:
       mov      rcx, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x68], rcx
       xor      edx, edx
       mov      qword ptr [rbp-0x70], rdx
       lea      rdx, [rbp-0x70]
       mov      rdi, rax
       mov      rsi, rcx
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x70]
       sar      rdi, 63
       cmp      rax, rdi
       je       SHORT G_M000_IG10
 
G_M000_IG08:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       lea      rdi, [rbp-0x80]
       mov      rsi, qword ptr [rbp-0x58]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x80]
       mov      gword ptr [rbp-0xF8], rax
       mov      ecx, dword ptr [rbp-0x78]
       mov      dword ptr [rbp-0xDC], ecx
       mov      rdi, qword ptr [rbp-0x68]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, gword ptr [rbp-0xF8]
       mov      esi, dword ptr [rbp-0xDC]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG09:
       jmp      G_M000_IG26
 
G_M000_IG10:
       mov      rax, qword ptr [rbp-0x70]
       mov      qword ptr [rbp-0x88], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG13
 
G_M000_IG11:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x88]
       mov      qword ptr [rax+0x08], rdi
       mov      rsi, rax
 
G_M000_IG12:
       jmp      SHORT G_M000_IG09
 
G_M000_IG13:
       test     rcx, rcx
       jl       G_M000_IG83
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG82
       mov      rsi, gword ptr [rax+8*rsi+0x10]
       jmp      SHORT G_M000_IG12
 
G_M000_IG14:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdx], rcx
       jne      G_M000_IG25
 
G_M000_IG15:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x60], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x90], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x90]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x60]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG26
 
G_M000_IG16:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG25
 
G_M000_IG17:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm0
       test     rdx, rdx
       je       SHORT G_M000_IG20
 
G_M000_IG18:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdx], rax
       jne      SHORT G_M000_IG20
 
G_M000_IG19:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG26
 
G_M000_IG20:
       mov      rax, rdx
       test     rax, rax
       je       SHORT G_M000_IG23
 
G_M000_IG21:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       SHORT G_M000_IG23
 
G_M000_IG22:
       xor      rax, rax
 
G_M000_IG23:
       test     rax, rax
       je       SHORT G_M000_IG25
 
G_M000_IG24:
       mov      rsi, rdx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x98], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x98]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG26
 
G_M000_IG25:
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG26:
       test     r14, r14
       je       G_M000_IG49
 
G_M000_IG27:
       mov      rax, qword ptr [r14]
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG36
 
G_M000_IG28:
       mov      rax, qword ptr [r14+0x08]
       test     rsi, rsi
       je       G_M000_IG49
 
G_M000_IG29:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG34
 
G_M000_IG30:
       mov      r14, qword ptr [rsi+0x08]
       lea      rcx, [rax+r14]
       mov      qword ptr [rbp-0xB8], rcx
       mov      rdi, rax
       xor      rdi, rcx
       mov      rdx, r14
       xor      rdx, rcx
       and      rdi, rdx
       jl       G_M000_IG81
       lea      r14, [rcx+0x05]
       cmp      r14, 261
       jbe      SHORT G_M000_IG33
 
G_M000_IG31:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, qword ptr [rbp-0xB8]
       mov      qword ptr [rax+0x08], r14
 
G_M000_IG32:
       jmp      G_M000_IG50
 
G_M000_IG33:
       test     r14, r14
       jl       G_M000_IG83
       mov      rax, r14
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG82
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG32
 
G_M000_IG34:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG49
 
G_M000_IG35:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0xD0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xD0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG50
 
G_M000_IG36:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG45
 
G_M000_IG37:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       test     rsi, rsi
       je       SHORT G_M000_IG40
 
G_M000_IG38:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG40
 
G_M000_IG39:
       vmovsd   xmm1, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xA8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG50
 
G_M000_IG40:
       mov      rdi, rsi
       test     rdi, rdi
       je       SHORT G_M000_IG43
 
G_M000_IG41:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       je       SHORT G_M000_IG43
 
G_M000_IG42:
       xor      rdi, rdi
 
G_M000_IG43:
       test     rdi, rdi
       je       SHORT G_M000_IG49
 
G_M000_IG44:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0xD8], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xD8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG50
 
G_M000_IG45:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG49
 
G_M000_IG46:
       mov      rdi, r14
       test     rsi, rsi
       je       SHORT G_M000_IG49
 
G_M000_IG47:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      SHORT G_M000_IG49
 
G_M000_IG48:
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG50
 
G_M000_IG49:
       mov      rdx, rsi
       mov      rsi, r14
       mov      edi, 257
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG50:
       mov      r14, rax
       add      r15, r12
       jo       G_M000_IG83
 
G_M000_IG51:
       test     r12, r12
       setg     dil
       movzx    rdi, dil
       cmp      r15, r13
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       cmp      r15, r13
       setg     al
       movzx    rax, al
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, eax
       or       edi, esi
       je       G_M000_IG65
 
G_M000_IG52:
       lea      rax, [r15+0x05]
       cmp      rax, 261
       jbe      G_M000_IG73
 
G_M000_IG53:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r15
 
G_M000_IG54:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG57
 
G_M000_IG55:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       dec      edi
       mov      ecx, edi
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG82
       cmp      dword ptr [rax+4*rcx+0x10], 11
       je       SHORT G_M000_IG57
 
G_M000_IG56:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG82
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 11
 
G_M000_IG57:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rsi, rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       mov      rax, rsi
       mov      rcx, rdx
       mov      r8, r14
 
G_M000_IG58:
       test     rax, rax
       je       G_M000_IG03
 
G_M000_IG59:
       mov      r11, 0xD1FFAB1E
       cmp      qword ptr [rax], r11
       jne      G_M000_IG03
 
G_M000_IG60:
       test     rcx, rcx
       je       G_M000_IG03
 
G_M000_IG61:
       mov      r11, 0xD1FFAB1E
       cmp      qword ptr [rcx], r11
       jne      G_M000_IG03
 
G_M000_IG62:
       test     r8, r8
       je       G_M000_IG03
 
G_M000_IG63:
       mov      r11, 0xD1FFAB1E
       cmp      qword ptr [r8], r11
       jne      G_M000_IG03
 
G_M000_IG64:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp+0x50], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG50
 
G_M000_IG65:
       mov      r12, qword ptr [rbp+0x158]
       mov      r13, qword ptr [rbp+0x148]
       add      r12, r13
       jo       G_M000_IG83
       mov      r15d, dword ptr [rbp-0xE4]
       mov      edi, r15d
       mov      rax, qword ptr [rbp+0x150]
       cmp      r12, rax
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       mov      qword ptr [rbp+0x150], rax
       cmp      r12, rax
       setg     cl
       movzx    rcx, cl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, ecx
       or       edi, esi
       je       G_M000_IG74
 
G_M000_IG66:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG69
 
G_M000_IG67:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       lea      ecx, [rdi-0x01]
       mov      edx, ecx
       cmp      edx, dword ptr [rsi+0x08]
       jae      G_M000_IG82
       cmp      dword ptr [rsi+4*rdx+0x10], 9
       je       SHORT G_M000_IG69
 
G_M000_IG68:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      ecx, dword ptr [rsi+0x08]
       jae      G_M000_IG82
       mov      ecx, ecx
       mov      dword ptr [rsi+4*rcx+0x10], 9
 
G_M000_IG69:
       mov      rsi, 0xD1FFAB1E
       mov      r14, gword ptr [rsi]
       test     edi, edi
       jle      SHORT G_M000_IG72
 
G_M000_IG70:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       lea      ecx, [rdi-0x01]
       mov      edi, ecx
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG82
       cmp      dword ptr [rsi+4*rdi+0x10], 10
       je       SHORT G_M000_IG72
 
G_M000_IG71:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, ecx
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG82
       mov      dword ptr [rdi+4*rsi+0x10], 10
 
G_M000_IG72:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp+0x110], rcx
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0xF0], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x40]
       mov      rsi, gword ptr [rbp-0xF0]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdx, gword ptr [rbp-0x38]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp+0x108], rax
       mov      rdi, gword ptr [rbp+0x110]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x100], rax
       mov      rdi, gword ptr [rbp+0x108]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rdi, qword ptr [rbp+0x100]
       mov      rcx, rdi
       mov      edx, 1
       mov      qword ptr [rbp+0x158], r12
       mov      qword ptr [rbp+0x148], r13
       mov      r12, rdx
       mov      r13, rax
       mov      r15, rcx
       jmp      G_M000_IG51
 
G_M000_IG73:
       test     rax, rax
       jl       G_M000_IG83
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG82
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       jmp      G_M000_IG54
 
G_M000_IG74:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG77
 
G_M000_IG75:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       lea      ecx, [rdi-0x01]
       mov      edx, ecx
       cmp      edx, dword ptr [rax+0x08]
       jae      G_M000_IG82
       cmp      dword ptr [rax+4*rdx+0x10], 12
       je       SHORT G_M000_IG77
 
G_M000_IG76:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG82
       mov      ecx, ecx
       mov      dword ptr [rax+4*rcx+0x10], 12
 
G_M000_IG77:
       mov      rbx, qword ptr [rbp+0x180]
       test     rbx, rbx
       jle      SHORT G_M000_IG79
 
G_M000_IG78:
       movsxd   rax, edi
       cmp      rbx, rax
       jg       SHORT G_M000_IG79
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG79:
       mov      rax, r14
 
G_M000_IG80:
       add      rsp, 680
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG81:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xC8], xmm0
       lea      rdi, [rbp-0xC8]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xC8]
       mov      gword ptr [rbp-0x100], rax
       mov      ecx, dword ptr [rbp-0xC0]
       mov      dword ptr [rbp-0xE0], ecx
       mov      rdi, r14
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, gword ptr [rbp-0x100]
       mov      esi, dword ptr [rbp-0xE0]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       jmp      G_M000_IG32
 
G_M000_IG82:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG83:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2423

