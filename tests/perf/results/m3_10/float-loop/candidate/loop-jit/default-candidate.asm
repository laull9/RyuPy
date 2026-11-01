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
       xor      eax, eax
       mov      qword ptr [rbp-0x148], rax
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x14C], eax
       cmp      dword ptr [rbp-0x14C], 0
       jne      G_M000_IG36
       jmp      SHORT G_M000_IG09
 
G_M000_IG07:
       cmp      gword ptr [rbp-0x40], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x150], eax
       mov      eax, dword ptr [rbp-0x14C]
       and      eax, dword ptr [rbp-0x150]
       movzx    rax, al
       mov      dword ptr [rbp-0x154], eax
       cmp      dword ptr [rbp-0x154], 0
       jne      G_M000_IG40
       jmp      G_M000_IG37
 
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
       mov      esi, 330
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
       mov      esi, 396
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
       mov      gword ptr [rbp-0x160], rax
       mov      rax, gword ptr [rbp-0x128]
       mov      gword ptr [rbp-0x170], rax
       cmp      dword ptr [rbp-0x14C], 0
       jne      G_M000_IG33
       jmp      G_M000_IG26
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0xE0]
       add      rax, qword ptr [rbp-0xF0]
       jo       G_M000_IG41
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
       mov      esi, 477
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG20:
       jmp      G_M000_IG15
 
G_M000_IG21:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x148], xmm0
       jmp      SHORT G_M000_IG18
 
G_M000_IG22:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG24
 
G_M000_IG23:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 495
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG24:
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
       mov      rax, gword ptr [rbp-0x138]
       mov      gword ptr [rbp-0x40], rax
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x14C], eax
       cmp      dword ptr [rbp-0x14C], 0
       jne      G_M000_IG21
       jmp      G_M000_IG18
 
G_M000_IG25:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x1A8], rax
       mov      rax, gword ptr [rbp-0x1A8]
       vmovsd   xmm0, qword ptr [rbp-0x148]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x1A8]
       mov      gword ptr [rbp-0x40], rax
       jmp      G_M000_IG22
 
G_M000_IG26:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG28
 
G_M000_IG27:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 566
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG28:
       cmp      gword ptr [rbp-0x40], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x150], eax
       mov      eax, dword ptr [rbp-0x14C]
       and      eax, dword ptr [rbp-0x150]
       movzx    rax, al
       mov      dword ptr [rbp-0x154], eax
       cmp      dword ptr [rbp-0x154], 0
       jne      G_M000_IG25
       jmp      G_M000_IG22
 
G_M000_IG29:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x160]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x168], xmm0
       mov      rsi, gword ptr [rbp-0x170]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x178], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x168]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rbp-0x180], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x148]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x180]
       vmovsd   qword ptr [rbp-0x188], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x188]
       vmovsd   qword ptr [rbp-0x148], xmm0
       xor      rax, rax
       mov      gword ptr [rbp-0x40], rax
       jmp      G_M000_IG18
 
G_M000_IG30:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG32
 
G_M000_IG31:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 634
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG32:
       mov      rsi, gword ptr [rbp-0x170]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x18C], eax
       cmp      dword ptr [rbp-0x18C], 0
       jne      G_M000_IG29
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG26
 
G_M000_IG33:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG35
 
G_M000_IG34:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 661
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG35:
       mov      rsi, gword ptr [rbp-0x160]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x190], eax
       cmp      dword ptr [rbp-0x190], 0
       jne      G_M000_IG30
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG26
 
G_M000_IG36:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x148], xmm0
       jmp      G_M000_IG09
 
G_M000_IG37:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG39
 
G_M000_IG38:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 701
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x78]
       add      rax, qword ptr [rbp-0x88]
       jo       SHORT G_M000_IG41
       mov      qword ptr [rbp-0x78], rax
       jmp      G_M000_IG03
 
G_M000_IG40:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x1A8], rax
       mov      rax, gword ptr [rbp-0x1A8]
       vmovsd   xmm0, qword ptr [rbp-0x148]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x1A8]
       mov      gword ptr [rbp-0x40], rax
       jmp      G_M000_IG37
 
G_M000_IG41:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2290

; Assembly listing for method RyuPy.Generated.Module:m0_accumulate(System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x27a
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 2
; 16 inlinees with PGO data; 26 single block inlinees; 39 inlinees without PGO data

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
       mov      qword ptr [rbp-0xD8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xD0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, gword ptr [rbp+0x1A0]
       mov      r15, gword ptr [rbp+0x190]
       mov      r14, qword ptr [rbp+0xF0]
       mov      r13, gword ptr [rbp+0xB0]
       mov      r12, gword ptr [rbp+0xA8]
       mov      rcx, gword ptr [rbp+0x70]
       mov      r9, gword ptr [rbp+0x60]
 
G_M000_IG02:
       test     r9, r9
       je       G_M000_IG26
 
G_M000_IG03:
       mov      r11, 0xD1FFAB1E
       mov      gword ptr [rbp+0x60], r9
       cmp      qword ptr [r9], r11
       jne      G_M000_IG26
 
G_M000_IG04:
       mov      r11, 0xD1FFAB1E
       cmp      qword ptr [rcx], r11
       je       SHORT G_M000_IG06
 
G_M000_IG05:
       mov      gword ptr [rbp+0x70], rcx
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rcx, gword ptr [rbp+0x70]
 
G_M000_IG06:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       mov      r9, gword ptr [rbp+0x60]
       vmulsd   xmm1, xmm1, qword ptr [r9+0x08]
       vaddsd   xmm0, xmm1, qword ptr [rbp+0x88]
       vmovsd   qword ptr [rbp+0x88], xmm0
       xor      r15, r15
       mov      r13d, dword ptr [rbp+0x84]
       jmp      G_M000_IG16
 
G_M000_IG07:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       je       G_M000_IG48
 
G_M000_IG08:
       mov      rdx, r12
       mov      rsi, r13
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      r12, rax
 
G_M000_IG09:
       test     r15, r15
       je       SHORT G_M000_IG13
 
G_M000_IG10:
       mov      r13, qword ptr [r15]
       mov      rdi, 0xD1FFAB1E
       cmp      r13, rdi
       je       G_M000_IG53
 
G_M000_IG11:
       mov      rdi, 0xD1FFAB1E
       cmp      r13, rdi
       je       G_M000_IG64
 
G_M000_IG12:
       mov      rsi, 0xD1FFAB1E
       cmp      r13, rsi
       je       G_M000_IG69
 
G_M000_IG13:
       mov      rdx, r12
       mov      rsi, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r15, rax
 
G_M000_IG14:
       test     r15, r15
       jne      G_M000_IG78
 
G_M000_IG15:
       xor      r13d, r13d
 
G_M000_IG16:
       mov      r12, qword ptr [rbp+0xE0]
       add      r14, r12
       jo       G_M000_IG108
 
G_M000_IG17:
       test     r12, r12
       setg     dil
       movzx    rdi, dil
       mov      rax, qword ptr [rbp+0xE8]
       cmp      r14, rax
       setl     cl
       movzx    rcx, cl
       and      ecx, edi
       cmp      r14, rax
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, ecx
       je       G_M000_IG81
 
G_M000_IG18:
       lea      rcx, [r14+0x05]
       cmp      rcx, 261
       jbe      G_M000_IG94
 
G_M000_IG19:
       mov      qword ptr [rbp+0xE8], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r14
 
G_M000_IG20:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG23
 
G_M000_IG21:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      r8d, edi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      G_M000_IG107
       cmp      dword ptr [rdx+4*r8+0x10], 11
       je       SHORT G_M000_IG23
 
G_M000_IG22:
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG107
       mov      edi, edi
       mov      dword ptr [rcx+4*rdi+0x10], 11
 
G_M000_IG23:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rax
       mov      r9, rcx
       test     r13d, r13d
       mov      dword ptr [rbp+0x84], r13d
       mov      qword ptr [rbp+0xE0], r12
       mov      r12, rcx
       mov      r13, rax
       je       SHORT G_M000_IG26
 
G_M000_IG24:
       mov      rcx, rdx
       test     rcx, rcx
       je       SHORT G_M000_IG26
 
G_M000_IG25:
       mov      r10, 0xD1FFAB1E
       cmp      qword ptr [rcx], r10
       je       G_M000_IG02
 
G_M000_IG26:
       test     r15, r15
       sete     cl
       movzx    rcx, cl
       test     dword ptr [rbp+0x84], ecx
       je       SHORT G_M000_IG28
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       vmovsd   xmm0, qword ptr [rbp+0x88]
       vmovsd   qword ptr [r15+0x08], xmm0
 
G_M000_IG28:
       test     r13, r13
       je       G_M000_IG08
 
G_M000_IG29:
       mov      rax, qword ptr [r13]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG07
 
G_M000_IG30:
       mov      rax, qword ptr [r13+0x08]
       test     r12, r12
       je       G_M000_IG08
 
G_M000_IG31:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [r12], rcx
       jne      G_M000_IG46
 
G_M000_IG32:
       mov      r12, qword ptr [r12+0x08]
       lea      rcx, [rbp-0x70]
       mov      rdx, rax
       mulx     rdx, r9, r12
       mov      qword ptr [rcx], r9
       mov      r13, qword ptr [rbp-0x70]
       mov      rcx, rax
       sar      rcx, 63
       and      rcx, r12
       sub      rdx, rcx
       mov      rcx, r12
       sar      rcx, 63
       and      rcx, rax
       sub      rdx, rcx
       mov      rcx, r13
       sar      rcx, 63
       cmp      rdx, rcx
       je       G_M000_IG42
 
G_M000_IG33:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x68], xmm1
       lea      rdi, [rbp-0x68]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r13, gword ptr [rbp-0x68]
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0xEC], eax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       lea      rdi, [rbp-0x80]
       mov      rsi, r12
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0x80]
       mov      r9d, dword ptr [rbp-0x78]
       test     r13, r13
       jne      SHORT G_M000_IG37
 
G_M000_IG34:
       test     r12, r12
       jne      G_M000_IG104
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x90], xmm0
       movsxd   rsi, dword ptr [rbp-0xEC]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x90]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x90]
       mov      esi, dword ptr [rbp-0x88]
 
G_M000_IG35:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG36:
       mov      r12, rdx
       jmp      G_M000_IG09
 
G_M000_IG37:
       lea      rdi, bword ptr [r13+0x10]
       mov      esi, dword ptr [r13+0x08]
 
G_M000_IG38:
       test     r12, r12
       je       SHORT G_M000_IG41
 
G_M000_IG39:
       lea      rcx, bword ptr [r12+0x10]
       mov      r8d, dword ptr [r12+0x08]
 
G_M000_IG40:
       mov      edx, dword ptr [rbp-0xEC]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG35
 
G_M000_IG41:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG40
 
G_M000_IG42:
       lea      r12, [r13+0x05]
       cmp      r12, 261
       jbe      SHORT G_M000_IG45
 
G_M000_IG43:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r13
       mov      rdx, rax
 
G_M000_IG44:
       jmp      SHORT G_M000_IG36
 
G_M000_IG45:
       test     r12, r12
       jl       G_M000_IG108
       mov      rdx, r12
       mov      rax, 0xD1FFAB1E
       mov      r13, gword ptr [rax]
       cmp      rdx, 262
       jae      G_M000_IG107
       mov      rdx, gword ptr [r13+8*rdx+0x10]
       jmp      SHORT G_M000_IG44
 
G_M000_IG46:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [r12], rcx
       jne      G_M000_IG08
 
G_M000_IG47:
       vmovsd   xmm1, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0x58], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rax
       vmovsd   qword ptr [rbp-0x98], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x98]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG09
 
G_M000_IG48:
       vmovsd   xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm1
       test     r12, r12
       je       G_M000_IG08
 
G_M000_IG49:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r12], rax
       jne      SHORT G_M000_IG51
 
G_M000_IG50:
       vmovsd   xmm2, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG09
 
G_M000_IG51:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r12], rax
       jne      G_M000_IG08
 
G_M000_IG52:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG09
 
G_M000_IG53:
       mov      rsi, qword ptr [r15+0x08]
       test     r12, r12
       je       G_M000_IG13
 
G_M000_IG54:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG62
 
G_M000_IG55:
       mov      r12, qword ptr [r12+0x08]
       lea      r13, [rsi+r12]
       mov      rdi, rsi
       xor      rdi, r13
       mov      rax, r12
       xor      rax, r13
       and      rdi, rax
       jge      SHORT G_M000_IG58
 
G_M000_IG56:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xC8], xmm0
       lea      rdi, [rbp-0xC8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r13, gword ptr [rbp-0xC8]
       mov      r15d, dword ptr [rbp-0xC0]
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xD8], xmm0
       lea      rdi, [rbp-0xD8]
       mov      rsi, r12
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, r13
       mov      esi, r15d
       mov      rdx, gword ptr [rbp-0xD8]
       mov      rcx, qword ptr [rbp-0xD0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG57:
       mov      r15, rax
       jmp      G_M000_IG14
 
G_M000_IG58:
       lea      r15, [r13+0x05]
       cmp      r15, 261
       jbe      SHORT G_M000_IG61
 
G_M000_IG59:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r13
 
G_M000_IG60:
       jmp      SHORT G_M000_IG57
 
G_M000_IG61:
       test     r15, r15
       jl       G_M000_IG108
       mov      rax, r15
       mov      rdi, 0xD1FFAB1E
       mov      r13, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG107
       mov      rax, gword ptr [r13+8*rax+0x10]
       jmp      SHORT G_M000_IG60
 
G_M000_IG62:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG13
 
G_M000_IG63:
       vmovsd   xmm0, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0xB8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xE0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xB8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG14
 
G_M000_IG64:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm0
       test     r12, r12
       je       G_M000_IG13
 
G_M000_IG65:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      SHORT G_M000_IG67
 
G_M000_IG66:
       vmovsd   xmm1, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG14
 
G_M000_IG67:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG13
 
G_M000_IG68:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xE8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG14
 
G_M000_IG69:
       mov      r13, r15
       test     r12, r12
       je       G_M000_IG13
 
G_M000_IG70:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r12], rsi
       jne      G_M000_IG13
 
G_M000_IG71:
       cmp      dword ptr [r13+0x08], 0
       je       SHORT G_M000_IG76
 
G_M000_IG72:
       cmp      dword ptr [r12+0x08], 0
       je       SHORT G_M000_IG75
 
G_M000_IG73:
       mov      r15d, dword ptr [r13+0x08]
       mov      esi, r15d
       add      esi, dword ptr [r12+0x08]
       js       G_M000_IG106
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x100], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [r13+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r13+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, r15d
       mov      r15, gword ptr [rbp-0x100]
       lea      rdi, bword ptr [r15+2*rdi+0x0C]
       mov      edx, dword ptr [r12+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r12+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       nop      
 
G_M000_IG74:
       jmp      G_M000_IG14
 
G_M000_IG75:
       mov      r15, r13
       jmp      SHORT G_M000_IG74
 
G_M000_IG76:
       cmp      dword ptr [r12+0x08], 0
       je       G_M000_IG105
 
G_M000_IG77:
       mov      r15, r12
       jmp      SHORT G_M000_IG74
 
G_M000_IG78:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG80
 
G_M000_IG79:
       mov      r13d, 1
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp+0x88], xmm0
       jmp      G_M000_IG16
 
G_M000_IG80:
       xor      r13d, r13d
       jmp      G_M000_IG16
 
G_M000_IG81:
       test     r15, r15
       sete     dil
       movzx    rdi, dil
       test     r13d, edi
       jne      G_M000_IG103
 
G_M000_IG82:
       mov      r13, qword ptr [rbp+0x158]
       mov      r12, qword ptr [rbp+0x148]
       add      r13, r12
       jo       G_M000_IG108
       test     r12, r12
       setg     dil
       movzx    rdi, dil
       mov      r14, qword ptr [rbp+0x150]
       cmp      r13, r14
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       mov      qword ptr [rbp+0x158], r13
       cmp      r13, r14
       setg     al
       movzx    rax, al
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, eax
       or       edi, esi
       je       G_M000_IG95
 
G_M000_IG83:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG86
 
G_M000_IG84:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       lea      ecx, [rdi-0x01]
       mov      edx, ecx
       mov      r8d, dword ptr [rax+0x08]
       cmp      edx, r8d
       jae      G_M000_IG107
       mov      r8d, edx
       cmp      dword ptr [rax+4*r8+0x10], 9
       je       SHORT G_M000_IG86
 
G_M000_IG85:
       mov      eax, ecx
       mov      dword ptr [rsi+4*rax+0x10], 9
 
G_M000_IG86:
       mov      rsi, 0xD1FFAB1E
       mov      r15, gword ptr [rsi]
       test     edi, edi
       jle      SHORT G_M000_IG89
 
G_M000_IG87:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       lea      ecx, [rdi-0x01]
       mov      edi, ecx
       mov      r8d, dword ptr [rax+0x08]
       cmp      edi, r8d
       jae      G_M000_IG107
       cmp      dword ptr [rax+4*rdi+0x10], 10
       je       SHORT G_M000_IG89
 
G_M000_IG88:
       mov      rdi, rsi
       mov      esi, ecx
       mov      dword ptr [rdi+4*rsi+0x10], 10
 
G_M000_IG89:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp+0x110], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0xF8], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x40]
       mov      rsi, gword ptr [rbp-0xF8]
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
       mov      qword ptr [rbp+0xE8], rax
       mov      edx, 1
       vxorps   xmm0, xmm0, xmm0
       test     r15, r15
       je       SHORT G_M000_IG93
 
G_M000_IG90:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG92
 
G_M000_IG91:
       mov      esi, 1
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp+0x88], xmm0
       mov      qword ptr [rbp+0x150], r14
       mov      qword ptr [rbp+0x148], r12
       mov      r12, rdx
       mov      r13d, esi
       mov      r14, rcx
       jmp      G_M000_IG17
 
G_M000_IG92:
       xor      esi, esi
       mov      qword ptr [rbp+0x150], r14
       mov      qword ptr [rbp+0x148], r12
       vmovsd   qword ptr [rbp+0x88], xmm0
       mov      r12, rdx
       mov      r13d, esi
       mov      r14, rcx
       jmp      G_M000_IG17
 
G_M000_IG93:
       xor      esi, esi
       mov      qword ptr [rbp+0x150], r14
       mov      qword ptr [rbp+0x148], r12
       vmovsd   qword ptr [rbp+0x88], xmm0
       mov      r12, rdx
       mov      r13d, esi
       mov      r14, rcx
       jmp      G_M000_IG17
 
G_M000_IG94:
       test     rcx, rcx
       jl       G_M000_IG108
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG107
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       mov      qword ptr [rbp+0xE8], rax
       jmp      G_M000_IG20
 
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
       mov      r8d, dword ptr [rax+0x08]
       cmp      edx, r8d
       jae      G_M000_IG107
       cmp      dword ptr [rax+4*rdx+0x10], 12
       je       SHORT G_M000_IG98
 
G_M000_IG97:
       mov      rax, rsi
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
       mov      rax, r15
 
G_M000_IG102:
       add      rsp, 680
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG103:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       vmovsd   xmm0, qword ptr [rbp+0x88]
       vmovsd   qword ptr [r15+0x08], xmm0
       jmp      G_M000_IG82
 
G_M000_IG104:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG38
 
G_M000_IG105:
       mov      r15, 0xD1FFAB1E
       jmp      G_M000_IG74
 
G_M000_IG106:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG107:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG108:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2935

; Assembly listing for method RyuPy.Generated.Module:m0_accumulate(System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x1ef
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 3
; 46 inlinees with PGO data; 46 single block inlinees; 44 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 272
       mov      qword ptr [rsp+0x2D8], r15
       mov      qword ptr [rsp+0x2D0], r14
       mov      qword ptr [rsp+0x2C8], r13
       mov      qword ptr [rsp+0x2C0], r12
       mov      qword ptr [rsp+0x2B8], rbx
       lea      rbp, [rsp+0x110]
       xor      eax, eax
       mov      qword ptr [rbp-0xD8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xD0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, gword ptr [rbp+0x1A0]
       mov      r15, gword ptr [rbp+0x190]
       mov      r14, qword ptr [rbp+0xF0]
       mov      r13, gword ptr [rbp+0xB0]
       mov      r12, gword ptr [rbp+0xA8]
 
G_M000_IG02:
       jmp      G_M000_IG75
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       vmovsd   xmm0, qword ptr [rbp+0x88]
       vmovsd   qword ptr [r15+0x08], xmm0
       vmovsd   qword ptr [rbp+0x88], xmm0
       jmp      G_M000_IG75
 
G_M000_IG04:
       lea      rdi, bword ptr [r13+0x10]
       mov      esi, dword ptr [r13+0x08]
 
G_M000_IG05:
       test     r12, r12
       je       SHORT G_M000_IG08
 
G_M000_IG06:
       lea      rcx, bword ptr [r12+0x10]
       mov      r8d, dword ptr [r12+0x08]
 
G_M000_IG07:
       mov      edx, dword ptr [rbp-0xEC]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      r12, rax
       mov      r13d, edx
       jmp      G_M000_IG82
 
G_M000_IG08:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG07
 
G_M000_IG09:
       xor      edi, edi
       jmp      G_M000_IG85
 
G_M000_IG10:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x100], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r12
       call     CORINFO_HELP_ASSIGN_REF
       mov      r12, gword ptr [rbp-0x100]
       mov      dword ptr [r12+0x10], r13d
       mov      r13, r12
       jmp      G_M000_IG87
 
G_M000_IG11:
       lea      r12, [r13+0x05]
       cmp      r12, 261
       jbe      SHORT G_M000_IG14
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r13
       mov      r13, rax
 
G_M000_IG13:
       jmp      G_M000_IG87
 
G_M000_IG14:
       test     r12, r12
       jl       G_M000_IG153
       mov      rdi, r12
       mov      rdx, 0xD1FFAB1E
       mov      r12, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG152
       mov      r13, gword ptr [r12+8*rdi+0x10]
       jmp      SHORT G_M000_IG13
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG23
 
G_M000_IG16:
       vmovsd   xmm1, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0x58], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rsi
       vmovsd   qword ptr [rbp-0x98], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x98]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG24
 
G_M000_IG17:
       mov      rdi, 0xD1FFAB1E
       cmp      rsi, rdi
       jne      SHORT G_M000_IG23
 
G_M000_IG18:
       vmovsd   xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm1
       test     r12, r12
       je       SHORT G_M000_IG23
 
G_M000_IG19:
       cmp      qword ptr [r12], rdi
       jne      SHORT G_M000_IG21
 
G_M000_IG20:
       vmovsd   xmm2, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      SHORT G_M000_IG24
 
G_M000_IG21:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [r12], rdx
       jne      SHORT G_M000_IG23
 
G_M000_IG22:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      SHORT G_M000_IG24
 
G_M000_IG23:
       mov      rdx, r12
       mov      rsi, r13
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      r13, rax
 
G_M000_IG24:
       test     r15, r15
       je       G_M000_IG59
 
G_M000_IG25:
       mov      r12, qword ptr [r15]
       mov      rdi, 0xD1FFAB1E
       cmp      r12, rdi
       jne      G_M000_IG43
 
G_M000_IG26:
       mov      rsi, qword ptr [r15+0x08]
       test     r13, r13
       je       G_M000_IG59
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG41
 
G_M000_IG28:
       mov      r13, qword ptr [r13+0x08]
       lea      r12, [rsi+r13]
       mov      rdi, rsi
       xor      rdi, r12
       mov      rax, r13
       xor      rax, r12
       and      rdi, rax
       jge      G_M000_IG37
 
G_M000_IG29:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xC8], xmm0
       lea      rdi, [rbp-0xC8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0xC8]
       mov      r15d, dword ptr [rbp-0xC0]
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xD8], xmm0
       lea      rdi, [rbp-0xD8]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, r12
       mov      esi, r15d
       mov      rdx, gword ptr [rbp-0xD8]
       mov      rcx, qword ptr [rbp-0xD0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      r15, rax
       mov      r13d, edx
       test     r15, r15
       jne      G_M000_IG133
 
G_M000_IG30:
       test     r15, r15
       jne      G_M000_IG142
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jge      SHORT G_M000_IG35
 
G_M000_IG31:
       mov      edi, -1
 
G_M000_IG32:
       test     edi, edi
       jg       SHORT G_M000_IG36
 
G_M000_IG33:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       mov      rdi, r15
       mov      esi, r13d
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      qword ptr [r12+0x08], rax
 
G_M000_IG34:
       mov      r15, r12
       jmp      G_M000_IG60
 
G_M000_IG35:
       xor      edi, edi
       jmp      SHORT G_M000_IG32
 
G_M000_IG36:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       lea      rdi, bword ptr [r12+0x08]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
       mov      dword ptr [r12+0x10], r13d
       jmp      SHORT G_M000_IG34
 
G_M000_IG37:
       lea      r15, [r12+0x05]
       cmp      r15, 261
       jbe      SHORT G_M000_IG40
 
G_M000_IG38:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
       mov      r12, rax
 
G_M000_IG39:
       jmp      SHORT G_M000_IG34
 
G_M000_IG40:
       test     r15, r15
       jl       G_M000_IG153
       mov      rdi, r15
       mov      rax, 0xD1FFAB1E
       mov      r12, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG152
       mov      r12, gword ptr [r12+8*rdi+0x10]
       jmp      SHORT G_M000_IG39
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG59
 
G_M000_IG42:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xB8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xE0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xB8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG60
 
G_M000_IG43:
       mov      rdi, 0xD1FFAB1E
       cmp      r12, rdi
       jne      G_M000_IG49
 
G_M000_IG44:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm0
       test     r13, r13
       je       G_M000_IG59
 
G_M000_IG45:
       cmp      qword ptr [r13], rdi
       jne      SHORT G_M000_IG47
 
G_M000_IG46:
       vmovsd   xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG60
 
G_M000_IG47:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r13], rax
       jne      G_M000_IG59
 
G_M000_IG48:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xE8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xE8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG60
 
G_M000_IG49:
       mov      rsi, 0xD1FFAB1E
       cmp      r12, rsi
       jne      G_M000_IG59
 
G_M000_IG50:
       mov      r12, r15
       test     r13, r13
       je       G_M000_IG59
 
G_M000_IG51:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r13], rsi
       jne      G_M000_IG59
 
G_M000_IG52:
       cmp      dword ptr [r12+0x08], 0
       je       SHORT G_M000_IG57
 
G_M000_IG53:
       cmp      dword ptr [r13+0x08], 0
       je       SHORT G_M000_IG56
 
G_M000_IG54:
       mov      r15d, dword ptr [r12+0x08]
       mov      esi, r15d
       add      esi, dword ptr [r13+0x08]
       js       G_M000_IG151
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x110], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [r12+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r12+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, r15d
       mov      r15, gword ptr [rbp-0x110]
       lea      rdi, bword ptr [r15+2*rdi+0x0C]
       mov      edx, dword ptr [r13+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r13+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       nop      
 
G_M000_IG55:
       jmp      SHORT G_M000_IG60
 
G_M000_IG56:
       mov      r15, r12
       jmp      SHORT G_M000_IG55
 
G_M000_IG57:
       cmp      dword ptr [r13+0x08], 0
       je       G_M000_IG150
 
G_M000_IG58:
       mov      r15, r13
       jmp      SHORT G_M000_IG55
 
G_M000_IG59:
       mov      rdx, r13
       mov      rsi, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r15, rax
 
G_M000_IG60:
       test     r15, r15
       je       SHORT G_M000_IG64
 
G_M000_IG61:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG63
 
G_M000_IG62:
       mov      r13d, 1
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp+0x88], xmm0
       jmp      SHORT G_M000_IG65
 
G_M000_IG63:
       xor      r13d, r13d
       jmp      SHORT G_M000_IG65
 
G_M000_IG64:
       xor      r13d, r13d
 
G_M000_IG65:
       mov      r12, qword ptr [rbp+0xE0]
       add      r14, r12
       jo       G_M000_IG153
 
G_M000_IG66:
       test     r12, r12
       setg     dil
       movzx    rdi, dil
       mov      rax, qword ptr [rbp+0xE8]
       cmp      r14, rax
       setl     cl
       movzx    rcx, cl
       and      ecx, edi
       cmp      r14, rax
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, ecx
       je       G_M000_IG88
 
G_M000_IG67:
       lea      rcx, [r14+0x05]
       cmp      rcx, 261
       jbe      G_M000_IG102
 
G_M000_IG68:
       mov      qword ptr [rbp+0xE8], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r14
 
G_M000_IG69:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG72
 
G_M000_IG70:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      r8d, edi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      G_M000_IG152
       cmp      dword ptr [rdx+4*r8+0x10], 11
       je       SHORT G_M000_IG72
 
G_M000_IG71:
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG152
       mov      edi, edi
       mov      dword ptr [rcx+4*rdi+0x10], 11
 
G_M000_IG72:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rcx, rdi
       test     r13d, r13d
       je       SHORT G_M000_IG74
 
G_M000_IG73:
       test     rax, rax
       jne      G_M000_IG103
 
G_M000_IG74:
       test     r15, r15
       sete     dil
       movzx    rdi, dil
       test     r13d, edi
       mov      qword ptr [rbp+0xE0], r12
       mov      r12, rcx
       mov      r13, rax
       jne      G_M000_IG03
 
G_M000_IG75:
       test     r13, r13
       je       G_M000_IG23
 
G_M000_IG76:
       mov      rsi, qword ptr [r13]
       mov      rdx, 0xD1FFAB1E
       cmp      rsi, rdx
       jne      G_M000_IG17
 
G_M000_IG77:
       mov      rsi, qword ptr [r13+0x08]
       test     r12, r12
       je       G_M000_IG23
 
G_M000_IG78:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [r12], rdx
       jne      G_M000_IG15
 
G_M000_IG79:
       mov      r12, qword ptr [r12+0x08]
       lea      rdi, [rbp-0x70]
       mov      rdx, rsi
       mulx     rdx, r13, r12
       mov      qword ptr [rdi], r13
       mov      r13, qword ptr [rbp-0x70]
       mov      rdi, rsi
       sar      rdi, 63
       and      rdi, r12
       sub      rdx, rdi
       mov      rdi, r12
       sar      rdi, 63
       and      rdi, rsi
       sub      rdx, rdi
       mov      rdi, r13
       sar      rdi, 63
       cmp      rdx, rdi
       je       G_M000_IG11
 
G_M000_IG80:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x68], xmm1
       lea      rdi, [rbp-0x68]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r13, gword ptr [rbp-0x68]
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0xEC], eax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       lea      rdi, [rbp-0x80]
       mov      rsi, r12
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0x80]
       mov      r9d, dword ptr [rbp-0x78]
       test     r13, r13
       jne      G_M000_IG04
 
G_M000_IG81:
       test     r12, r12
       jne      G_M000_IG115
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x90], xmm0
       movsxd   rsi, dword ptr [rbp-0xEC]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x90]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0x90]
       mov      r13d, dword ptr [rbp-0x88]
 
G_M000_IG82:
       test     r12, r12
       jne      G_M000_IG116
 
G_M000_IG83:
       test     r12, r12
       jne      G_M000_IG125
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jge      G_M000_IG09
 
G_M000_IG84:
       mov      edi, -1
 
G_M000_IG85:
       test     edi, edi
       jg       G_M000_IG10
 
G_M000_IG86:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x108], rax
       mov      rdi, r12
       mov      esi, r13d
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r13, gword ptr [rbp-0x108]
       mov      qword ptr [r13+0x08], rax
 
G_M000_IG87:
       jmp      G_M000_IG24
 
G_M000_IG88:
       test     r15, r15
       sete     dil
       movzx    rdi, dil
       test     r13d, edi
       jne      G_M000_IG98
 
G_M000_IG89:
       mov      r13, qword ptr [rbp+0x158]
       mov      r12, qword ptr [rbp+0x148]
       add      r13, r12
       jo       G_M000_IG153
       mov      qword ptr [rbp+0x148], r12
       test     r12, r12
       setg     dil
       movzx    rdi, dil
       mov      r14, qword ptr [rbp+0x150]
       cmp      r13, r14
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       mov      qword ptr [rbp+0x158], r13
       mov      qword ptr [rbp+0x150], r14
       cmp      r13, r14
       setg     al
       movzx    rax, al
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, eax
       or       edi, esi
       je       G_M000_IG107
 
G_M000_IG90:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG93
 
G_M000_IG91:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       lea      ecx, [rdi-0x01]
       mov      edx, ecx
       cmp      edx, dword ptr [rax+0x08]
       jae      G_M000_IG152
       cmp      dword ptr [rax+4*rdx+0x10], 9
       je       SHORT G_M000_IG93
 
G_M000_IG92:
       cmp      ecx, dword ptr [rsi+0x08]
       jae      G_M000_IG152
       mov      eax, ecx
       mov      dword ptr [rsi+4*rax+0x10], 9
 
G_M000_IG93:
       mov      rsi, 0xD1FFAB1E
       mov      r15, gword ptr [rsi]
       test     edi, edi
       jle      SHORT G_M000_IG96
 
G_M000_IG94:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       lea      ecx, [rdi-0x01]
       mov      edi, ecx
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG152
       cmp      dword ptr [rax+4*rdi+0x10], 10
       je       SHORT G_M000_IG96
 
G_M000_IG95:
       mov      rdi, rsi
       mov      esi, ecx
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG152
       mov      dword ptr [rdi+4*rsi+0x10], 10
 
G_M000_IG96:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp+0x110], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0xF8], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x40]
       mov      rsi, gword ptr [rbp-0xF8]
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
       mov      qword ptr [rbp+0xE8], rax
       mov      qword ptr [rbp+0xE0], 1
       vxorps   xmm0, xmm0, xmm0
       test     r15, r15
       jne      SHORT G_M000_IG99
 
G_M000_IG97:
       xor      esi, esi
       vmovsd   qword ptr [rbp+0x88], xmm0
       mov      r13d, esi
       mov      r14, rcx
       mov      r12, qword ptr [rbp+0xE0]
       jmp      G_M000_IG66
 
G_M000_IG98:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       vmovsd   xmm0, qword ptr [rbp+0x88]
       vmovsd   qword ptr [r15+0x08], xmm0
       jmp      G_M000_IG89
 
G_M000_IG99:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG101
 
G_M000_IG100:
       mov      esi, 1
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp+0x88], xmm0
       mov      r13d, esi
       mov      r14, rcx
       mov      r12, qword ptr [rbp+0xE0]
       jmp      G_M000_IG66
 
G_M000_IG101:
       xor      esi, esi
       vmovsd   qword ptr [rbp+0x88], xmm0
       mov      r13d, esi
       mov      r14, rcx
       mov      r12, qword ptr [rbp+0xE0]
       jmp      G_M000_IG66
 
G_M000_IG102:
       test     rcx, rcx
       jl       G_M000_IG153
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG152
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       mov      qword ptr [rbp+0xE8], rax
       jmp      G_M000_IG69
 
G_M000_IG103:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG74
 
G_M000_IG104:
       test     rcx, rcx
       je       G_M000_IG74
 
G_M000_IG105:
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG74
 
G_M000_IG106:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmulsd   xmm0, xmm0, qword ptr [rcx+0x08]
       vaddsd   xmm0, xmm0, qword ptr [rbp+0x88]
       vmovsd   qword ptr [rbp+0x88], xmm0
       xor      r15, r15
       mov      qword ptr [rbp+0xE0], r12
       jmp      G_M000_IG65
 
G_M000_IG107:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG110
 
G_M000_IG108:
       mov      rax, 0xD1FFAB1E
       mov      rsi, gword ptr [rax]
       mov      rax, rsi
       lea      ecx, [rdi-0x01]
       mov      edx, ecx
       cmp      edx, dword ptr [rax+0x08]
       jae      G_M000_IG152
       cmp      dword ptr [rax+4*rdx+0x10], 12
       je       SHORT G_M000_IG110
 
G_M000_IG109:
       mov      rax, rsi
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG152
       mov      ecx, ecx
       mov      dword ptr [rax+4*rcx+0x10], 12
 
G_M000_IG110:
       mov      rbx, qword ptr [rbp+0x180]
       test     rbx, rbx
       jle      SHORT G_M000_IG113
 
G_M000_IG111:
       movsxd   rax, edi
       cmp      rbx, rax
       jg       SHORT G_M000_IG113
 
G_M000_IG112:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG113:
       mov      rax, r15
 
G_M000_IG114:
       add      rsp, 696
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG115:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG05
 
G_M000_IG116:
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG117
       mov      edi, dword ptr [r12+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG118
 
G_M000_IG117:
       mov      eax, r13d
       jmp      SHORT G_M000_IG124
 
G_M000_IG118:
       cmp      edi, 2
       je       SHORT G_M000_IG119
       cmp      dword ptr [r12+0x08], 0
       jbe      G_M000_IG152
       mov      edi, dword ptr [r12+0x10]
       jmp      SHORT G_M000_IG120
 
G_M000_IG119:
       mov      edi, dword ptr [r12+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r12+0x10]
       or       rdi, rax
 
G_M000_IG120:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG121
       mov      eax, -1
       jmp      SHORT G_M000_IG123
 
G_M000_IG121:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jbe      SHORT G_M000_IG122
       mov      eax, 1
       jmp      SHORT G_M000_IG123
 
G_M000_IG122:
       xor      eax, eax
 
G_M000_IG123:
       imul     eax, r13d
 
G_M000_IG124:
       test     eax, eax
       jl       G_M000_IG10
       jmp      G_M000_IG83
 
G_M000_IG125:
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG126
       mov      edi, dword ptr [r12+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG127
 
G_M000_IG126:
       mov      edi, r13d
       jmp      G_M000_IG85
 
G_M000_IG127:
       cmp      edi, 2
       je       SHORT G_M000_IG128
       cmp      dword ptr [r12+0x08], 0
       jbe      G_M000_IG152
       mov      edi, dword ptr [r12+0x10]
       jmp      SHORT G_M000_IG129
 
G_M000_IG128:
       mov      edi, dword ptr [r12+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r12+0x10]
       or       rdi, rax
 
G_M000_IG129:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG130
       mov      eax, -1
       jmp      SHORT G_M000_IG132
 
G_M000_IG130:
       test     rdi, rdi
       jge      SHORT G_M000_IG131
       mov      eax, 1
       jmp      SHORT G_M000_IG132
 
G_M000_IG131:
       xor      eax, eax
 
G_M000_IG132:
       mov      edi, r13d
       imul     edi, eax
       jmp      G_M000_IG85
 
G_M000_IG133:
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG134
       mov      edi, dword ptr [r15+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG135
 
G_M000_IG134:
       mov      eax, r13d
       jmp      SHORT G_M000_IG141
 
G_M000_IG135:
       cmp      edi, 2
       je       SHORT G_M000_IG136
       cmp      dword ptr [r15+0x08], 0
       jbe      G_M000_IG152
       mov      edi, dword ptr [r15+0x10]
       jmp      SHORT G_M000_IG137
 
G_M000_IG136:
       mov      edi, dword ptr [r15+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r15+0x10]
       or       rdi, rax
 
G_M000_IG137:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG138
       mov      eax, -1
       jmp      SHORT G_M000_IG140
 
G_M000_IG138:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jbe      SHORT G_M000_IG139
       mov      eax, 1
       jmp      SHORT G_M000_IG140
 
G_M000_IG139:
       xor      eax, eax
 
G_M000_IG140:
       imul     eax, r13d
 
G_M000_IG141:
       test     eax, eax
       jl       G_M000_IG36
       jmp      G_M000_IG30
 
G_M000_IG142:
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG143
       mov      edi, dword ptr [r15+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG144
 
G_M000_IG143:
       mov      edi, r13d
       jmp      G_M000_IG32
 
G_M000_IG144:
       cmp      edi, 2
       je       SHORT G_M000_IG145
       cmp      dword ptr [r15+0x08], 0
       jbe      SHORT G_M000_IG152
       mov      edi, dword ptr [r15+0x10]
       jmp      SHORT G_M000_IG146
 
G_M000_IG145:
       mov      edi, dword ptr [r15+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r15+0x10]
       or       rdi, rax
 
G_M000_IG146:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG147
       mov      eax, -1
       jmp      SHORT G_M000_IG149
 
G_M000_IG147:
       test     rdi, rdi
       jge      SHORT G_M000_IG148
       mov      eax, 1
       jmp      SHORT G_M000_IG149
 
G_M000_IG148:
       xor      eax, eax
 
G_M000_IG149:
       mov      edi, r13d
       imul     edi, eax
       jmp      G_M000_IG32
 
G_M000_IG150:
       mov      r15, 0xD1FFAB1E
       jmp      G_M000_IG55
 
G_M000_IG151:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG152:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG153:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 3556

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
       jmp      G_M000_IG09
 
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
       xor      eax, eax
       mov      qword ptr [rbp-0x170], rax
       mov      rsi, gword ptr [rbp-0x50]
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x174], eax
       cmp      dword ptr [rbp-0x174], 0
       jne      G_M000_IG36
       jmp      SHORT G_M000_IG10
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x90]
       add      rax, qword ptr [rbp-0xA0]
       jo       G_M000_IG41
       mov      qword ptr [rbp-0x90], rax
       jmp      G_M000_IG03
 
G_M000_IG09:
       mov      edi, 23
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0x168], rax
       jmp      G_M000_IG16
 
G_M000_IG10:
       mov      eax, dword ptr [rbp-0x1E8]
       dec      eax
       mov      dword ptr [rbp-0x1E8], eax
       cmp      dword ptr [rbp-0x1E8], 0
       jg       SHORT G_M000_IG12
 
G_M000_IG11:
       lea      rdi, [rbp-0x1E8]
       mov      esi, 401
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG12:
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
       jne      SHORT G_M000_IG13
       jmp      G_M000_IG15
 
G_M000_IG13:
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
       mov      gword ptr [rbp-0x188], rax
       mov      rax, gword ptr [rbp-0x148]
       mov      gword ptr [rbp-0x198], rax
       cmp      dword ptr [rbp-0x174], 0
       jne      G_M000_IG33
       jmp      G_M000_IG26
 
G_M000_IG14:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x100]
       add      rax, qword ptr [rbp-0x110]
       jo       G_M000_IG41
       mov      qword ptr [rbp-0x100], rax
       jmp      G_M000_IG10
 
G_M000_IG15:
       cmp      gword ptr [rbp-0x50], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x178], eax
       mov      eax, dword ptr [rbp-0x174]
       and      eax, dword ptr [rbp-0x178]
       movzx    rax, al
       mov      dword ptr [rbp-0x17C], eax
       cmp      dword ptr [rbp-0x17C], 0
       jne      G_M000_IG40
       jmp      G_M000_IG37
 
G_M000_IG16:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x168]
 
G_M000_IG17:
       add      rsp, 512
       pop      rbp
       ret      
 
G_M000_IG18:
       mov      eax, dword ptr [rbp-0x1E8]
       dec      eax
       mov      dword ptr [rbp-0x1E8], eax
       cmp      dword ptr [rbp-0x1E8], 0
       jg       SHORT G_M000_IG20
 
G_M000_IG19:
       lea      rdi, [rbp-0x1E8]
       mov      esi, 576
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG20:
       jmp      G_M000_IG14
 
G_M000_IG21:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x50]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x170], xmm0
       jmp      SHORT G_M000_IG18
 
G_M000_IG22:
       mov      eax, dword ptr [rbp-0x1E8]
       dec      eax
       mov      dword ptr [rbp-0x1E8], eax
       cmp      dword ptr [rbp-0x1E8], 0
       jg       SHORT G_M000_IG24
 
G_M000_IG23:
       lea      rdi, [rbp-0x1E8]
       mov      esi, 594
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG24:
       mov      rdx, gword ptr [rbp-0x140]
       mov      rcx, gword ptr [rbp-0x148]
       mov      edi, 2
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x150], rax
       mov      rdx, gword ptr [rbp-0x50]
       mov      rcx, gword ptr [rbp-0x150]
       mov      edi, 3
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x158], rax
       mov      rax, gword ptr [rbp-0x158]
       mov      gword ptr [rbp-0x50], rax
       mov      rsi, gword ptr [rbp-0x50]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x174], eax
       cmp      dword ptr [rbp-0x174], 0
       jne      G_M000_IG21
       jmp      G_M000_IG18
 
G_M000_IG25:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x1E0], rax
       mov      rax, gword ptr [rbp-0x1E0]
       vmovsd   xmm0, qword ptr [rbp-0x170]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x1E0]
       mov      gword ptr [rbp-0x50], rax
       jmp      G_M000_IG22
 
G_M000_IG26:
       mov      eax, dword ptr [rbp-0x1E8]
       dec      eax
       mov      dword ptr [rbp-0x1E8], eax
       cmp      dword ptr [rbp-0x1E8], 0
       jg       SHORT G_M000_IG28
 
G_M000_IG27:
       lea      rdi, [rbp-0x1E8]
       mov      esi, 665
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG28:
       cmp      gword ptr [rbp-0x50], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x178], eax
       mov      eax, dword ptr [rbp-0x174]
       and      eax, dword ptr [rbp-0x178]
       movzx    rax, al
       mov      dword ptr [rbp-0x17C], eax
       cmp      dword ptr [rbp-0x17C], 0
       jne      G_M000_IG25
       jmp      G_M000_IG22
 
G_M000_IG29:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x188]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x190], xmm0
       mov      rsi, gword ptr [rbp-0x198]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x1A0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x190]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x1A0]
       vmovsd   qword ptr [rbp-0x1A8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x170]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x1A8]
       vmovsd   qword ptr [rbp-0x1B0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1B0]
       vmovsd   qword ptr [rbp-0x170], xmm0
       xor      rax, rax
       mov      gword ptr [rbp-0x50], rax
       jmp      G_M000_IG18
 
G_M000_IG30:
       mov      eax, dword ptr [rbp-0x1E8]
       dec      eax
       mov      dword ptr [rbp-0x1E8], eax
       cmp      dword ptr [rbp-0x1E8], 0
       jg       SHORT G_M000_IG32
 
G_M000_IG31:
       lea      rdi, [rbp-0x1E8]
       mov      esi, 733
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG32:
       mov      rsi, gword ptr [rbp-0x198]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x1B4], eax
       cmp      dword ptr [rbp-0x1B4], 0
       jne      G_M000_IG29
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG26
 
G_M000_IG33:
       mov      eax, dword ptr [rbp-0x1E8]
       dec      eax
       mov      dword ptr [rbp-0x1E8], eax
       cmp      dword ptr [rbp-0x1E8], 0
       jg       SHORT G_M000_IG35
 
G_M000_IG34:
       lea      rdi, [rbp-0x1E8]
       mov      esi, 760
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG35:
       mov      rsi, gword ptr [rbp-0x188]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x1B8], eax
       cmp      dword ptr [rbp-0x1B8], 0
       jne      G_M000_IG30
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG26
 
G_M000_IG36:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x50]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x170], xmm0
       jmp      G_M000_IG10
 
G_M000_IG37:
       mov      eax, dword ptr [rbp-0x1E8]
       dec      eax
       mov      dword ptr [rbp-0x1E8], eax
       cmp      dword ptr [rbp-0x1E8], 0
       jg       SHORT G_M000_IG39
 
G_M000_IG38:
       lea      rdi, [rbp-0x1E8]
       mov      esi, 800
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG39:
       mov      edi, 22
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, gword ptr [rbp-0x50]
       mov      rsi, gword ptr [rbp-0x38]
       mov      edi, 4
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       mov      gword ptr [rbp-0x160], rax
       jmp      G_M000_IG08
 
G_M000_IG40:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x1E0], rax
       mov      rax, gword ptr [rbp-0x1E0]
       vmovsd   xmm0, qword ptr [rbp-0x170]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x1E0]
       mov      gword ptr [rbp-0x50], rax
       jmp      G_M000_IG37
 
G_M000_IG41:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2554

; Assembly listing for method RyuPy.Generated.Module:m0_repeated(System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x2dd
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 46 inlinees with PGO data; 58 single block inlinees; 55 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 272
       mov      qword ptr [rsp+0x318], r15
       mov      qword ptr [rsp+0x310], r14
       mov      qword ptr [rsp+0x308], r13
       mov      qword ptr [rsp+0x300], r12
       mov      qword ptr [rsp+0x2F8], rbx
       lea      rbp, [rsp+0x110]
       xor      eax, eax
       mov      qword ptr [rbp-0xD8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xD0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, gword ptr [rbp+0x1E0]
       mov      r15, gword ptr [rbp+0x1C0]
       mov      r14, qword ptr [rbp+0x110]
       mov      r13, gword ptr [rbp+0xD0]
       mov      r12, gword ptr [rbp+0xC8]
       mov      rcx, gword ptr [rbp+0x88]
       mov      r9, gword ptr [rbp+0x78]
 
G_M000_IG02:
       test     r9, r9
       je       G_M000_IG26
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       mov      gword ptr [rbp+0x78], r9
       cmp      qword ptr [r9], rdi
       jne      G_M000_IG26
 
G_M000_IG04:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       SHORT G_M000_IG06
 
G_M000_IG05:
       mov      gword ptr [rbp+0x88], rcx
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rcx, gword ptr [rbp+0x88]
 
G_M000_IG06:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       mov      r9, gword ptr [rbp+0x78]
       vmulsd   xmm1, xmm1, qword ptr [r9+0x08]
       vaddsd   xmm0, xmm1, qword ptr [rbp+0xA0]
       vmovsd   qword ptr [rbp+0xA0], xmm0
       xor      r15, r15
       mov      r13d, dword ptr [rbp+0x9C]
       jmp      G_M000_IG16
 
G_M000_IG07:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       je       G_M000_IG54
 
G_M000_IG08:
       mov      rdx, r12
       mov      rsi, r13
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      r13, rax
 
G_M000_IG09:
       test     r15, r15
       je       SHORT G_M000_IG13
 
G_M000_IG10:
       mov      r12, qword ptr [r15]
       mov      rdi, 0xD1FFAB1E
       cmp      r12, rdi
       je       G_M000_IG59
 
G_M000_IG11:
       mov      rdi, 0xD1FFAB1E
       cmp      r12, rdi
       je       G_M000_IG76
 
G_M000_IG12:
       mov      rsi, 0xD1FFAB1E
       cmp      r12, rsi
       je       G_M000_IG81
 
G_M000_IG13:
       mov      rdx, r13
       mov      rsi, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r15, rax
 
G_M000_IG14:
       test     r15, r15
       jne      G_M000_IG90
 
G_M000_IG15:
       xor      r13d, r13d
 
G_M000_IG16:
       mov      r12, qword ptr [rbp+0x100]
       add      r14, r12
       jo       G_M000_IG166
 
G_M000_IG17:
       test     r12, r12
       setg     dil
       movzx    rdi, dil
       mov      rax, qword ptr [rbp+0x108]
       cmp      r14, rax
       setl     cl
       movzx    rcx, cl
       and      ecx, edi
       cmp      r14, rax
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, ecx
       je       G_M000_IG93
 
G_M000_IG18:
       lea      rcx, [r14+0x05]
       cmp      rcx, 261
       jbe      G_M000_IG119
 
G_M000_IG19:
       mov      qword ptr [rbp+0x108], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r14
 
G_M000_IG20:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG23
 
G_M000_IG21:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      r8d, edi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      G_M000_IG165
       cmp      dword ptr [rdx+4*r8+0x10], 21
       je       SHORT G_M000_IG23
 
G_M000_IG22:
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG165
       mov      edi, edi
       mov      dword ptr [rcx+4*rdi+0x10], 21
 
G_M000_IG23:
       mov      rdi, gword ptr [rbp+0x1C8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rax
       mov      r9, rcx
       test     r13d, r13d
       mov      dword ptr [rbp+0x9C], r13d
       mov      qword ptr [rbp+0x100], r12
       mov      r12, rcx
       mov      r13, rax
       je       SHORT G_M000_IG26
 
G_M000_IG24:
       mov      rcx, rdx
       test     rcx, rcx
       je       SHORT G_M000_IG26
 
G_M000_IG25:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rsi
       je       G_M000_IG02
 
G_M000_IG26:
       test     r15, r15
       sete     cl
       movzx    rcx, cl
       test     dword ptr [rbp+0x9C], ecx
       je       SHORT G_M000_IG28
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       vmovsd   qword ptr [r15+0x08], xmm0
 
G_M000_IG28:
       test     r13, r13
       je       G_M000_IG08
 
G_M000_IG29:
       mov      rax, qword ptr [r13]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG07
 
G_M000_IG30:
       mov      rax, qword ptr [r13+0x08]
       test     r12, r12
       je       G_M000_IG08
 
G_M000_IG31:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [r12], rcx
       jne      G_M000_IG52
 
G_M000_IG32:
       mov      r12, qword ptr [r12+0x08]
       lea      rcx, [rbp-0x70]
       mov      rdx, rax
       mulx     rdx, r9, r12
       mov      qword ptr [rcx], r9
       mov      r13, qword ptr [rbp-0x70]
       mov      rcx, rax
       sar      rcx, 63
       and      rcx, r12
       sub      rdx, rcx
       mov      rcx, r12
       sar      rcx, 63
       and      rcx, rax
       sub      rdx, rcx
       mov      rcx, r13
       sar      rcx, 63
       cmp      rdx, rcx
       je       G_M000_IG48
 
G_M000_IG33:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x68], xmm1
       lea      rdi, [rbp-0x68]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r13, gword ptr [rbp-0x68]
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0xEC], eax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       lea      rdi, [rbp-0x80]
       mov      rsi, r12
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0x80]
       mov      r9d, dword ptr [rbp-0x78]
       test     r13, r13
       jne      G_M000_IG41
 
G_M000_IG34:
       test     r12, r12
       jne      G_M000_IG120
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x90], xmm0
       movsxd   rsi, dword ptr [rbp-0xEC]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x90]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0x90]
       mov      r13d, dword ptr [rbp-0x88]
 
G_M000_IG35:
       test     r12, r12
       jne      G_M000_IG121
 
G_M000_IG36:
       test     r12, r12
       jne      G_M000_IG130
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jge      SHORT G_M000_IG46
 
G_M000_IG37:
       mov      edi, -1
 
G_M000_IG38:
       test     edi, edi
       jg       SHORT G_M000_IG47
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x108], rax
       mov      rdi, r12
       mov      esi, r13d
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r13, gword ptr [rbp-0x108]
       mov      qword ptr [r13+0x08], rax
 
G_M000_IG40:
       jmp      G_M000_IG09
 
G_M000_IG41:
       lea      rdi, bword ptr [r13+0x10]
       mov      esi, dword ptr [r13+0x08]
 
G_M000_IG42:
       test     r12, r12
       je       SHORT G_M000_IG45
 
G_M000_IG43:
       lea      rcx, bword ptr [r12+0x10]
       mov      r8d, dword ptr [r12+0x08]
 
G_M000_IG44:
       mov      edx, dword ptr [rbp-0xEC]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      r12, rax
       mov      r13d, edx
       jmp      G_M000_IG35
 
G_M000_IG45:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG44
 
G_M000_IG46:
       xor      edi, edi
       jmp      SHORT G_M000_IG38
 
G_M000_IG47:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x100], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r12
       call     CORINFO_HELP_ASSIGN_REF
       mov      r12, gword ptr [rbp-0x100]
       mov      dword ptr [r12+0x10], r13d
       mov      r13, r12
       jmp      SHORT G_M000_IG40
 
G_M000_IG48:
       lea      r12, [r13+0x05]
       cmp      r12, 261
       jbe      SHORT G_M000_IG51
 
G_M000_IG49:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r13
       mov      r13, rax
 
G_M000_IG50:
       jmp      G_M000_IG40
 
G_M000_IG51:
       test     r12, r12
       jl       G_M000_IG166
       mov      rax, r12
       mov      rcx, 0xD1FFAB1E
       mov      r12, gword ptr [rcx]
       cmp      rax, 262
       jae      G_M000_IG165
       mov      r13, gword ptr [r12+8*rax+0x10]
       jmp      SHORT G_M000_IG50
 
G_M000_IG52:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [r12], rcx
       jne      G_M000_IG08
 
G_M000_IG53:
       vmovsd   xmm1, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0x58], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rax
       vmovsd   qword ptr [rbp-0x98], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x98]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG09
 
G_M000_IG54:
       vmovsd   xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm1
       test     r12, r12
       je       G_M000_IG08
 
G_M000_IG55:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r12], rax
       jne      SHORT G_M000_IG57
 
G_M000_IG56:
       vmovsd   xmm2, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG09
 
G_M000_IG57:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r12], rax
       jne      G_M000_IG08
 
G_M000_IG58:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG09
 
G_M000_IG59:
       mov      rsi, qword ptr [r15+0x08]
       test     r13, r13
       je       G_M000_IG13
 
G_M000_IG60:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG74
 
G_M000_IG61:
       mov      r13, qword ptr [r13+0x08]
       lea      r12, [rsi+r13]
       mov      rdi, rsi
       xor      rdi, r12
       mov      rax, r13
       xor      rax, r12
       and      rdi, rax
       jge      G_M000_IG70
 
G_M000_IG62:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xC8], xmm0
       lea      rdi, [rbp-0xC8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0xC8]
       mov      r15d, dword ptr [rbp-0xC0]
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xD8], xmm0
       lea      rdi, [rbp-0xD8]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, r12
       mov      esi, r15d
       mov      rdx, gword ptr [rbp-0xD8]
       mov      rcx, qword ptr [rbp-0xD0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      r15, rax
       mov      r13d, edx
       test     r15, r15
       jne      G_M000_IG138
 
G_M000_IG63:
       test     r15, r15
       jne      G_M000_IG147
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jge      SHORT G_M000_IG68
 
G_M000_IG64:
       mov      edi, -1
 
G_M000_IG65:
       test     edi, edi
       jg       SHORT G_M000_IG69
 
G_M000_IG66:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       mov      rdi, r15
       mov      esi, r13d
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      qword ptr [r12+0x08], rax
 
G_M000_IG67:
       mov      r15, r12
       jmp      G_M000_IG14
 
G_M000_IG68:
       xor      edi, edi
       jmp      SHORT G_M000_IG65
 
G_M000_IG69:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       lea      rdi, bword ptr [r12+0x08]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
       mov      dword ptr [r12+0x10], r13d
       jmp      SHORT G_M000_IG67
 
G_M000_IG70:
       lea      r15, [r12+0x05]
       cmp      r15, 261
       jbe      SHORT G_M000_IG73
 
G_M000_IG71:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
       mov      r12, rax
 
G_M000_IG72:
       jmp      SHORT G_M000_IG67
 
G_M000_IG73:
       test     r15, r15
       jl       G_M000_IG166
       mov      rdi, r15
       mov      rax, 0xD1FFAB1E
       mov      r12, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG165
       mov      r12, gword ptr [r12+8*rdi+0x10]
       jmp      SHORT G_M000_IG72
 
G_M000_IG74:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG13
 
G_M000_IG75:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xB8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xE0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xB8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG14
 
G_M000_IG76:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm0
       test     r13, r13
       je       G_M000_IG13
 
G_M000_IG77:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      SHORT G_M000_IG79
 
G_M000_IG78:
       vmovsd   xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG14
 
G_M000_IG79:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG13
 
G_M000_IG80:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xE8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG14
 
G_M000_IG81:
       mov      r12, r15
       test     r13, r13
       je       G_M000_IG13
 
G_M000_IG82:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r13], rsi
       jne      G_M000_IG13
 
G_M000_IG83:
       cmp      dword ptr [r12+0x08], 0
       je       SHORT G_M000_IG88
 
G_M000_IG84:
       cmp      dword ptr [r13+0x08], 0
       je       SHORT G_M000_IG87
 
G_M000_IG85:
       mov      r15d, dword ptr [r12+0x08]
       mov      esi, r15d
       add      esi, dword ptr [r13+0x08]
       js       G_M000_IG164
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x110], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [r12+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r12+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, r15d
       mov      r15, gword ptr [rbp-0x110]
       lea      rdi, bword ptr [r15+2*rdi+0x0C]
       mov      edx, dword ptr [r13+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r13+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       nop      
 
G_M000_IG86:
       jmp      G_M000_IG14
 
G_M000_IG87:
       mov      r15, r12
       jmp      SHORT G_M000_IG86
 
G_M000_IG88:
       cmp      dword ptr [r13+0x08], 0
       je       G_M000_IG155
 
G_M000_IG89:
       mov      r15, r13
       jmp      SHORT G_M000_IG86
 
G_M000_IG90:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG92
 
G_M000_IG91:
       mov      r13d, 1
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp+0xA0], xmm0
       jmp      G_M000_IG16
 
G_M000_IG92:
       xor      r13d, r13d
       jmp      G_M000_IG16
 
G_M000_IG93:
       test     r15, r15
       sete     dil
       movzx    rdi, dil
       test     r13d, edi
       je       SHORT G_M000_IG95
 
G_M000_IG94:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       vmovsd   qword ptr [r15+0x08], xmm0
 
G_M000_IG95:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG98
 
G_M000_IG96:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       dec      edi
       mov      ecx, edi
       mov      edx, dword ptr [rax+0x08]
       cmp      ecx, edx
       jae      G_M000_IG165
       cmp      dword ptr [rax+4*rcx+0x10], 22
       je       SHORT G_M000_IG98
 
G_M000_IG97:
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 22
 
G_M000_IG98:
       mov      rdi, 0xD1FFAB1E
       mov      r14, gword ptr [rdi]
       mov      r12, gword ptr [r14+0x30]
       test     r12, r12
       je       SHORT G_M000_IG100
 
G_M000_IG99:
       cmp      dword ptr [r12+0x08], 1
       je       SHORT G_M000_IG101
 
G_M000_IG100:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r12, rax
       lea      rdi, bword ptr [r14+0x30]
       mov      rsi, r12
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG101:
       mov      rdi, r12
       mov      rdx, r15
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, r12
       mov      rsi, gword ptr [rbp+0x1D8]
       mov      edi, 4
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
       mov      r15, qword ptr [rbp+0x180]
       mov      r14, qword ptr [rbp+0x170]
       add      r15, r14
       jo       G_M000_IG166
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
       je       G_M000_IG156
 
G_M000_IG102:
       lea      r13, [r15+0x05]
       cmp      r13, 261
       jbe      G_M000_IG115
 
G_M000_IG103:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r15
 
G_M000_IG104:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG107
 
G_M000_IG105:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rcx, rax
       dec      edi
       mov      edx, edi
       mov      r8d, dword ptr [rcx+0x08]
       cmp      edx, r8d
       jae      G_M000_IG165
       cmp      dword ptr [rcx+4*rdx+0x10], 18
       je       SHORT G_M000_IG107
 
G_M000_IG106:
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 18
 
G_M000_IG107:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      r13, rax
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG110
 
G_M000_IG108:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       lea      ecx, [rdi-0x01]
       mov      edx, ecx
       mov      r8d, dword ptr [rax+0x08]
       cmp      edx, r8d
       jae      G_M000_IG165
       mov      r8d, edx
       cmp      dword ptr [rax+4*r8+0x10], 19
       je       SHORT G_M000_IG110
 
G_M000_IG109:
       mov      eax, ecx
       mov      dword ptr [rsi+4*rax+0x10], 19
 
G_M000_IG110:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      gword ptr [rbp+0x1C0], rsi
       test     edi, edi
       jle      SHORT G_M000_IG113
 
G_M000_IG111:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rcx, rsi
       dec      edi
       mov      edx, edi
       mov      r8d, dword ptr [rcx+0x08]
       cmp      edx, r8d
       jae      G_M000_IG165
       cmp      dword ptr [rcx+4*rdx+0x10], 20
       je       SHORT G_M000_IG113
 
G_M000_IG112:
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 20
 
G_M000_IG113:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp+0x130], rcx
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0xF8], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, r13
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x40]
       mov      rsi, gword ptr [rbp-0xF8]
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
       mov      qword ptr [rbp+0x108], rax
       mov      edx, 1
       vxorps   xmm0, xmm0, xmm0
       mov      rsi, gword ptr [rbp+0x1C0]
       test     rsi, rsi
       jne      SHORT G_M000_IG116
 
G_M000_IG114:
       xor      r8d, r8d
       mov      gword ptr [rbp+0x1C8], r13
       mov      qword ptr [rbp+0x180], r15
       mov      qword ptr [rbp+0x178], r12
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      r12, rdx
       mov      r13d, r8d
       mov      r14, rcx
       mov      r15, rsi
       jmp      G_M000_IG17
 
G_M000_IG115:
       test     r13, r13
       jl       G_M000_IG166
       mov      rsi, r13
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG165
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       jmp      G_M000_IG104
 
G_M000_IG116:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG118
 
G_M000_IG117:
       mov      r8d, 1
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      gword ptr [rbp+0x1C8], r13
       mov      qword ptr [rbp+0x180], r15
       mov      qword ptr [rbp+0x178], r12
       mov      r12, rdx
       mov      r13d, r8d
       mov      r14, rcx
       mov      r15, rsi
       jmp      G_M000_IG17
 
G_M000_IG118:
       xor      r8d, r8d
       mov      gword ptr [rbp+0x1C8], r13
       mov      qword ptr [rbp+0x180], r15
       mov      qword ptr [rbp+0x178], r12
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      r12, rdx
       mov      r13d, r8d
       mov      r14, rcx
       mov      r15, rsi
       jmp      G_M000_IG17
 
G_M000_IG119:
       test     rcx, rcx
       jl       G_M000_IG166
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG165
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       mov      qword ptr [rbp+0x108], rax
       jmp      G_M000_IG20
 
G_M000_IG120:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG42
 
G_M000_IG121:
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG122
       mov      edi, dword ptr [r12+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG123
 
G_M000_IG122:
       mov      eax, r13d
       jmp      SHORT G_M000_IG129
 
G_M000_IG123:
       cmp      edi, 2
       je       SHORT G_M000_IG124
       cmp      dword ptr [r12+0x08], 0
       jbe      G_M000_IG165
       mov      edi, dword ptr [r12+0x10]
       jmp      SHORT G_M000_IG125
 
G_M000_IG124:
       mov      edi, dword ptr [r12+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r12+0x10]
       or       rdi, rax
 
G_M000_IG125:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG126
       mov      eax, -1
       jmp      SHORT G_M000_IG128
 
G_M000_IG126:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jbe      SHORT G_M000_IG127
       mov      eax, 1
       jmp      SHORT G_M000_IG128
 
G_M000_IG127:
       xor      eax, eax
 
G_M000_IG128:
       imul     eax, r13d
 
G_M000_IG129:
       test     eax, eax
       jl       G_M000_IG47
       jmp      G_M000_IG36
 
G_M000_IG130:
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG131
       mov      edi, dword ptr [r12+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG132
 
G_M000_IG131:
       mov      edi, r13d
       jmp      G_M000_IG38
 
G_M000_IG132:
       cmp      edi, 2
       je       SHORT G_M000_IG133
       cmp      dword ptr [r12+0x08], 0
       jbe      G_M000_IG165
       mov      edi, dword ptr [r12+0x10]
       jmp      SHORT G_M000_IG134
 
G_M000_IG133:
       mov      edi, dword ptr [r12+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r12+0x10]
       or       rdi, rax
 
G_M000_IG134:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG135
       mov      eax, -1
       jmp      SHORT G_M000_IG137
 
G_M000_IG135:
       test     rdi, rdi
       jge      SHORT G_M000_IG136
       mov      eax, 1
       jmp      SHORT G_M000_IG137
 
G_M000_IG136:
       xor      eax, eax
 
G_M000_IG137:
       mov      edi, r13d
       imul     edi, eax
       jmp      G_M000_IG38
 
G_M000_IG138:
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG139
       mov      edi, dword ptr [r15+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG140
 
G_M000_IG139:
       mov      eax, r13d
       jmp      SHORT G_M000_IG146
 
G_M000_IG140:
       cmp      edi, 2
       je       SHORT G_M000_IG141
       cmp      dword ptr [r15+0x08], 0
       jbe      G_M000_IG165
       mov      edi, dword ptr [r15+0x10]
       jmp      SHORT G_M000_IG142
 
G_M000_IG141:
       mov      edi, dword ptr [r15+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r15+0x10]
       or       rdi, rax
 
G_M000_IG142:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG143
       mov      eax, -1
       jmp      SHORT G_M000_IG145
 
G_M000_IG143:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jbe      SHORT G_M000_IG144
       mov      eax, 1
       jmp      SHORT G_M000_IG145
 
G_M000_IG144:
       xor      eax, eax
 
G_M000_IG145:
       imul     eax, r13d
 
G_M000_IG146:
       test     eax, eax
       jl       G_M000_IG69
       jmp      G_M000_IG63
 
G_M000_IG147:
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG148
       mov      edi, dword ptr [r15+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG149
 
G_M000_IG148:
       mov      edi, r13d
       jmp      G_M000_IG65
 
G_M000_IG149:
       cmp      edi, 2
       je       SHORT G_M000_IG150
       cmp      dword ptr [r15+0x08], 0
       jbe      G_M000_IG165
       mov      edi, dword ptr [r15+0x10]
       jmp      SHORT G_M000_IG151
 
G_M000_IG150:
       mov      edi, dword ptr [r15+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r15+0x10]
       or       rdi, rax
 
G_M000_IG151:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG152
       mov      eax, -1
       jmp      SHORT G_M000_IG154
 
G_M000_IG152:
       test     rdi, rdi
       jge      SHORT G_M000_IG153
       mov      eax, 1
       jmp      SHORT G_M000_IG154
 
G_M000_IG153:
       xor      eax, eax
 
G_M000_IG154:
       mov      edi, r13d
       imul     edi, eax
       jmp      G_M000_IG65
 
G_M000_IG155:
       mov      r15, 0xD1FFAB1E
       jmp      G_M000_IG86
 
G_M000_IG156:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG159
 
G_M000_IG157:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      ecx, dword ptr [(reloc)]
       dec      ecx
       cmp      ecx, dword ptr [rax+0x08]
       jae      SHORT G_M000_IG165
       cmp      dword ptr [rax+4*rcx+0x10], 23
       je       SHORT G_M000_IG159
 
G_M000_IG158:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      ecx, dword ptr [(reloc)]
       dec      ecx
       cmp      ecx, dword ptr [rax+0x08]
       jae      SHORT G_M000_IG165
       mov      dword ptr [rax+4*rcx+0x10], 23
 
G_M000_IG159:
       mov      rbx, qword ptr [rbp+0x1B0]
       test     rbx, rbx
       jle      SHORT G_M000_IG162
 
G_M000_IG160:
       movsxd   rax, dword ptr [(reloc)]
       cmp      rbx, rax
       jg       SHORT G_M000_IG162
 
G_M000_IG161:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG162:
       mov      rax, gword ptr [rbp+0x1D8]
 
G_M000_IG163:
       add      rsp, 760
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG164:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG165:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG166:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 4103

; Assembly listing for method RyuPy.Generated.Module:m0_accumulate(System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x236
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 3
; 46 inlinees with PGO data; 46 single block inlinees; 44 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 272
       mov      qword ptr [rsp+0x2D8], r15
       mov      qword ptr [rsp+0x2D0], r14
       mov      qword ptr [rsp+0x2C8], r13
       mov      qword ptr [rsp+0x2C0], r12
       mov      qword ptr [rsp+0x2B8], rbx
       lea      rbp, [rsp+0x110]
       xor      eax, eax
       mov      qword ptr [rbp-0xD8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xD0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, gword ptr [rbp+0x1A0]
       mov      r15, gword ptr [rbp+0x190]
       mov      r14, qword ptr [rbp+0xF0]
       mov      r13, gword ptr [rbp+0xB0]
       mov      r12d, dword ptr [rbp+0x84]
 
G_M000_IG02:
       test     r15, r15
       sete     dil
       movzx    rdi, dil
       test     r12d, edi
       jne      G_M000_IG29
 
G_M000_IG03:
       test     r13, r13
       je       SHORT G_M000_IG07
 
G_M000_IG04:
       mov      r12, qword ptr [r13]
       mov      rdi, 0xD1FFAB1E
       cmp      r12, rdi
       je       G_M000_IG30
 
G_M000_IG05:
       mov      rdi, 0xD1FFAB1E
       cmp      r12, rdi
       je       G_M000_IG54
 
G_M000_IG06:
       mov      rax, gword ptr [rbp+0xA8]
       jmp      SHORT G_M000_IG08
 
G_M000_IG07:
       mov      rax, gword ptr [rbp+0xA8]
 
G_M000_IG08:
       mov      rdx, rax
       mov      rsi, r13
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      r13, rax
 
G_M000_IG09:
       test     r15, r15
       je       SHORT G_M000_IG13
 
G_M000_IG10:
       mov      r12, qword ptr [r15]
       mov      rdi, 0xD1FFAB1E
       cmp      r12, rdi
       je       G_M000_IG59
 
G_M000_IG11:
       mov      rdi, 0xD1FFAB1E
       cmp      r12, rdi
       je       G_M000_IG76
 
G_M000_IG12:
       mov      rsi, 0xD1FFAB1E
       cmp      r12, rsi
       je       G_M000_IG81
 
G_M000_IG13:
       mov      rdx, r13
       mov      rsi, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r15, rax
 
G_M000_IG14:
       test     r15, r15
       jne      G_M000_IG90
 
G_M000_IG15:
       xor      r12d, r12d
 
G_M000_IG16:
       mov      r13, qword ptr [rbp+0xE0]
       add      r14, r13
       jo       G_M000_IG154
 
G_M000_IG17:
       test     r13, r13
       setg     dil
       movzx    rdi, dil
       mov      rax, qword ptr [rbp+0xE8]
       cmp      r14, rax
       setl     cl
       movzx    rcx, cl
       and      ecx, edi
       cmp      r14, rax
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, ecx
       je       G_M000_IG93
 
G_M000_IG18:
       lea      rcx, [r14+0x05]
       cmp      rcx, 261
       jbe      G_M000_IG107
 
G_M000_IG19:
       mov      qword ptr [rbp+0xE8], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r14
 
G_M000_IG20:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG23
 
G_M000_IG21:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      r8d, edi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      G_M000_IG153
       cmp      dword ptr [rdx+4*r8+0x10], 11
       je       SHORT G_M000_IG23
 
G_M000_IG22:
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG153
       mov      edi, edi
       mov      dword ptr [rcx+4*rdi+0x10], 11
 
G_M000_IG23:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       test     r12d, r12d
       mov      gword ptr [rbp+0xA8], rcx
       mov      qword ptr [rbp+0xE0], r13
       mov      r13, rax
       je       G_M000_IG02
 
G_M000_IG24:
       test     r13, r13
       je       G_M000_IG02
 
G_M000_IG25:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG02
 
G_M000_IG26:
       mov      rax, gword ptr [rbp+0xA8]
       test     rax, rax
       mov      gword ptr [rbp+0xA8], rax
       je       G_M000_IG02
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rbp+0xA8]
       cmp      qword ptr [rax], rdi
       mov      gword ptr [rbp+0xA8], rax
       jne      G_M000_IG02
 
G_M000_IG28:
       vmovsd   xmm1, qword ptr [r13+0x08]
       mov      rax, gword ptr [rbp+0xA8]
       vmulsd   xmm1, xmm1, qword ptr [rax+0x08]
       vaddsd   xmm0, xmm1, qword ptr [rbp+0x88]
       xor      r15, r15
       vmovsd   qword ptr [rbp+0x88], xmm0
       jmp      G_M000_IG16
 
G_M000_IG29:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       vmovsd   xmm0, qword ptr [rbp+0x88]
       vmovsd   qword ptr [r15+0x08], xmm0
       jmp      G_M000_IG03
 
G_M000_IG30:
       mov      r11, qword ptr [r13+0x08]
       mov      rax, gword ptr [rbp+0xA8]
       test     rax, rax
       je       G_M000_IG08
 
G_M000_IG31:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG52
 
G_M000_IG32:
       mov      r12, qword ptr [rax+0x08]
       lea      rdi, [rbp-0x70]
       mov      rdx, r11
       mulx     rdx, rax, r12
       mov      qword ptr [rdi], rax
       mov      r13, qword ptr [rbp-0x70]
       mov      rdi, r11
       sar      rdi, 63
       and      rdi, r12
       sub      rdx, rdi
       mov      rdi, r12
       sar      rdi, 63
       and      rdi, r11
       sub      rdx, rdi
       mov      rdi, r13
       sar      rdi, 63
       cmp      rdx, rdi
       je       G_M000_IG48
 
G_M000_IG33:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x68], xmm1
       lea      rdi, [rbp-0x68]
       mov      rsi, r11
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r13, gword ptr [rbp-0x68]
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0xEC], eax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       lea      rdi, [rbp-0x80]
       mov      rsi, r12
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0x80]
       mov      r9d, dword ptr [rbp-0x78]
       test     r13, r13
       jne      G_M000_IG41
 
G_M000_IG34:
       test     r12, r12
       jne      G_M000_IG116
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x90], xmm0
       movsxd   rsi, dword ptr [rbp-0xEC]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x90]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0x90]
       mov      r13d, dword ptr [rbp-0x88]
 
G_M000_IG35:
       test     r12, r12
       jne      G_M000_IG117
 
G_M000_IG36:
       test     r12, r12
       jne      G_M000_IG126
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jge      SHORT G_M000_IG46
 
G_M000_IG37:
       mov      edi, -1
 
G_M000_IG38:
       test     edi, edi
       jg       SHORT G_M000_IG47
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x108], rax
       mov      rdi, r12
       mov      esi, r13d
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r13, gword ptr [rbp-0x108]
       mov      qword ptr [r13+0x08], rax
 
G_M000_IG40:
       jmp      G_M000_IG09
 
G_M000_IG41:
       lea      rdi, bword ptr [r13+0x10]
       mov      esi, dword ptr [r13+0x08]
 
G_M000_IG42:
       test     r12, r12
       je       SHORT G_M000_IG45
 
G_M000_IG43:
       lea      rcx, bword ptr [r12+0x10]
       mov      r8d, dword ptr [r12+0x08]
 
G_M000_IG44:
       mov      edx, dword ptr [rbp-0xEC]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      r12, rax
       mov      r13d, edx
       jmp      G_M000_IG35
 
G_M000_IG45:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG44
 
G_M000_IG46:
       xor      edi, edi
       jmp      SHORT G_M000_IG38
 
G_M000_IG47:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x100], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r12
       call     CORINFO_HELP_ASSIGN_REF
       mov      r12, gword ptr [rbp-0x100]
       mov      dword ptr [r12+0x10], r13d
       mov      r13, r12
       jmp      SHORT G_M000_IG40
 
G_M000_IG48:
       lea      r12, [r13+0x05]
       cmp      r12, 261
       jbe      SHORT G_M000_IG51
 
G_M000_IG49:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r13
       mov      r13, rax
 
G_M000_IG50:
       jmp      G_M000_IG40
 
G_M000_IG51:
       test     r12, r12
       jl       G_M000_IG154
       mov      rdi, r12
       mov      rax, 0xD1FFAB1E
       mov      r12, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG153
       mov      r13, gword ptr [r12+8*rdi+0x10]
       jmp      SHORT G_M000_IG50
 
G_M000_IG52:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG08
 
G_M000_IG53:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x58], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, r11
       vmovsd   qword ptr [rbp-0x98], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x98]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG09
 
G_M000_IG54:
       vmovsd   xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm1
       mov      rax, gword ptr [rbp+0xA8]
       test     rax, rax
       je       G_M000_IG08
 
G_M000_IG55:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG57
 
G_M000_IG56:
       vmovsd   xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG09
 
G_M000_IG57:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG08
 
G_M000_IG58:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG09
 
G_M000_IG59:
       mov      rsi, qword ptr [r15+0x08]
       test     r13, r13
       je       G_M000_IG13
 
G_M000_IG60:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG74
 
G_M000_IG61:
       mov      r13, qword ptr [r13+0x08]
       lea      r12, [rsi+r13]
       mov      rdi, rsi
       xor      rdi, r12
       mov      rax, r13
       xor      rax, r12
       and      rdi, rax
       jge      G_M000_IG70
 
G_M000_IG62:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xC8], xmm0
       lea      rdi, [rbp-0xC8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0xC8]
       mov      r15d, dword ptr [rbp-0xC0]
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xD8], xmm0
       lea      rdi, [rbp-0xD8]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, r12
       mov      esi, r15d
       mov      rdx, gword ptr [rbp-0xD8]
       mov      rcx, qword ptr [rbp-0xD0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      r15, rax
       mov      r13d, edx
       test     r15, r15
       jne      G_M000_IG134
 
G_M000_IG63:
       test     r15, r15
       jne      G_M000_IG143
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jge      SHORT G_M000_IG68
 
G_M000_IG64:
       mov      edi, -1
 
G_M000_IG65:
       test     edi, edi
       jg       SHORT G_M000_IG69
 
G_M000_IG66:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       mov      rdi, r15
       mov      esi, r13d
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      qword ptr [r12+0x08], rax
 
G_M000_IG67:
       mov      r15, r12
       jmp      G_M000_IG14
 
G_M000_IG68:
       xor      edi, edi
       jmp      SHORT G_M000_IG65
 
G_M000_IG69:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       lea      rdi, bword ptr [r12+0x08]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
       mov      dword ptr [r12+0x10], r13d
       jmp      SHORT G_M000_IG67
 
G_M000_IG70:
       lea      r15, [r12+0x05]
       cmp      r15, 261
       jbe      SHORT G_M000_IG73
 
G_M000_IG71:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
       mov      r12, rax
 
G_M000_IG72:
       jmp      SHORT G_M000_IG67
 
G_M000_IG73:
       test     r15, r15
       jl       G_M000_IG154
       mov      rdi, r15
       mov      rax, 0xD1FFAB1E
       mov      r12, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG153
       mov      r12, gword ptr [r12+8*rdi+0x10]
       jmp      SHORT G_M000_IG72
 
G_M000_IG74:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG13
 
G_M000_IG75:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xB8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xE0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xB8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG14
 
G_M000_IG76:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm0
       test     r13, r13
       je       G_M000_IG13
 
G_M000_IG77:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      SHORT G_M000_IG79
 
G_M000_IG78:
       vmovsd   xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG14
 
G_M000_IG79:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG13
 
G_M000_IG80:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xE8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG14
 
G_M000_IG81:
       mov      r12, r15
       test     r13, r13
       je       G_M000_IG13
 
G_M000_IG82:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r13], rsi
       jne      G_M000_IG13
 
G_M000_IG83:
       cmp      dword ptr [r12+0x08], 0
       je       SHORT G_M000_IG88
 
G_M000_IG84:
       cmp      dword ptr [r13+0x08], 0
       je       SHORT G_M000_IG87
 
G_M000_IG85:
       mov      r15d, dword ptr [r12+0x08]
       mov      esi, r15d
       add      esi, dword ptr [r13+0x08]
       js       G_M000_IG152
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x110], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [r12+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r12+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, r15d
       mov      r15, gword ptr [rbp-0x110]
       lea      rdi, bword ptr [r15+2*rdi+0x0C]
       mov      edx, dword ptr [r13+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r13+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       nop      
 
G_M000_IG86:
       jmp      G_M000_IG14
 
G_M000_IG87:
       mov      r15, r12
       jmp      SHORT G_M000_IG86
 
G_M000_IG88:
       cmp      dword ptr [r13+0x08], 0
       je       G_M000_IG151
 
G_M000_IG89:
       mov      r15, r13
       jmp      SHORT G_M000_IG86
 
G_M000_IG90:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG92
 
G_M000_IG91:
       mov      r12d, 1
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp+0x88], xmm0
       jmp      G_M000_IG16
 
G_M000_IG92:
       xor      r12d, r12d
       jmp      G_M000_IG16
 
G_M000_IG93:
       test     r15, r15
       sete     dil
       movzx    rdi, dil
       test     r12d, edi
       jne      G_M000_IG103
 
G_M000_IG94:
       mov      r12, qword ptr [rbp+0x158]
       mov      r13, qword ptr [rbp+0x148]
       add      r12, r13
       jo       G_M000_IG154
       test     r13, r13
       setg     dil
       movzx    rdi, dil
       mov      r14, qword ptr [rbp+0x150]
       cmp      r12, r14
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       cmp      r12, r14
       setg     al
       movzx    rax, al
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, eax
       or       edi, esi
       je       G_M000_IG108
 
G_M000_IG95:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG98
 
G_M000_IG96:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       lea      ecx, [rdi-0x01]
       mov      edx, ecx
       cmp      edx, dword ptr [rax+0x08]
       jae      G_M000_IG153
       cmp      dword ptr [rax+4*rdx+0x10], 9
       je       SHORT G_M000_IG98
 
G_M000_IG97:
       cmp      ecx, dword ptr [rsi+0x08]
       jae      G_M000_IG153
       mov      eax, ecx
       mov      dword ptr [rsi+4*rax+0x10], 9
 
G_M000_IG98:
       mov      rsi, 0xD1FFAB1E
       mov      r15, gword ptr [rsi]
       test     edi, edi
       jle      SHORT G_M000_IG101
 
G_M000_IG99:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       lea      ecx, [rdi-0x01]
       mov      edi, ecx
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG153
       cmp      dword ptr [rax+4*rdi+0x10], 10
       je       SHORT G_M000_IG101
 
G_M000_IG100:
       mov      rdi, rsi
       mov      esi, ecx
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG153
       mov      dword ptr [rdi+4*rsi+0x10], 10
 
G_M000_IG101:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp+0x110], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0xF8], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x40]
       mov      rsi, gword ptr [rbp-0xF8]
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
       mov      qword ptr [rbp+0xE8], rax
       mov      edx, 1
       vxorps   xmm0, xmm0, xmm0
       test     r15, r15
       jne      SHORT G_M000_IG104
 
G_M000_IG102:
       xor      esi, esi
       mov      qword ptr [rbp+0x158], r12
       mov      qword ptr [rbp+0x150], r14
       mov      qword ptr [rbp+0x148], r13
       vmovsd   qword ptr [rbp+0x88], xmm0
       mov      r12d, esi
       mov      r13, rdx
       mov      r14, rcx
       jmp      G_M000_IG17
 
G_M000_IG103:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       vmovsd   xmm0, qword ptr [rbp+0x88]
       vmovsd   qword ptr [r15+0x08], xmm0
       jmp      G_M000_IG94
 
G_M000_IG104:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG106
 
G_M000_IG105:
       mov      esi, 1
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp+0x88], xmm0
       mov      qword ptr [rbp+0x158], r12
       mov      qword ptr [rbp+0x150], r14
       mov      qword ptr [rbp+0x148], r13
       mov      r12d, esi
       mov      r13, rdx
       mov      r14, rcx
       jmp      G_M000_IG17
 
G_M000_IG106:
       xor      esi, esi
       mov      qword ptr [rbp+0x158], r12
       mov      qword ptr [rbp+0x150], r14
       mov      qword ptr [rbp+0x148], r13
       vmovsd   qword ptr [rbp+0x88], xmm0
       mov      r12d, esi
       mov      r13, rdx
       mov      r14, rcx
       jmp      G_M000_IG17
 
G_M000_IG107:
       test     rcx, rcx
       jl       G_M000_IG154
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG153
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       mov      qword ptr [rbp+0xE8], rax
       jmp      G_M000_IG20
 
G_M000_IG108:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG111
 
G_M000_IG109:
       mov      rax, 0xD1FFAB1E
       mov      rsi, gword ptr [rax]
       mov      rax, rsi
       lea      ecx, [rdi-0x01]
       mov      edx, ecx
       cmp      edx, dword ptr [rax+0x08]
       jae      G_M000_IG153
       cmp      dword ptr [rax+4*rdx+0x10], 12
       je       SHORT G_M000_IG111
 
G_M000_IG110:
       mov      rax, rsi
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG153
       mov      ecx, ecx
       mov      dword ptr [rax+4*rcx+0x10], 12
 
G_M000_IG111:
       mov      rbx, qword ptr [rbp+0x180]
       test     rbx, rbx
       jle      SHORT G_M000_IG114
 
G_M000_IG112:
       movsxd   rax, edi
       cmp      rbx, rax
       jg       SHORT G_M000_IG114
 
G_M000_IG113:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG114:
       mov      rax, r15
 
G_M000_IG115:
       add      rsp, 696
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG116:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG42
 
G_M000_IG117:
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG118
       mov      edi, dword ptr [r12+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG119
 
G_M000_IG118:
       mov      eax, r13d
       jmp      SHORT G_M000_IG125
 
G_M000_IG119:
       cmp      edi, 2
       je       SHORT G_M000_IG120
       cmp      dword ptr [r12+0x08], 0
       jbe      G_M000_IG153
       mov      edi, dword ptr [r12+0x10]
       jmp      SHORT G_M000_IG121
 
G_M000_IG120:
       mov      edi, dword ptr [r12+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r12+0x10]
       or       rdi, rax
 
G_M000_IG121:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG122
       mov      eax, -1
       jmp      SHORT G_M000_IG124
 
G_M000_IG122:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jbe      SHORT G_M000_IG123
       mov      eax, 1
       jmp      SHORT G_M000_IG124
 
G_M000_IG123:
       xor      eax, eax
 
G_M000_IG124:
       imul     eax, r13d
 
G_M000_IG125:
       test     eax, eax
       jl       G_M000_IG47
       jmp      G_M000_IG36
 
G_M000_IG126:
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG127
       mov      edi, dword ptr [r12+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG128
 
G_M000_IG127:
       mov      edi, r13d
       jmp      G_M000_IG38
 
G_M000_IG128:
       cmp      edi, 2
       je       SHORT G_M000_IG129
       cmp      dword ptr [r12+0x08], 0
       jbe      G_M000_IG153
       mov      edi, dword ptr [r12+0x10]
       jmp      SHORT G_M000_IG130
 
G_M000_IG129:
       mov      edi, dword ptr [r12+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r12+0x10]
       or       rdi, rax
 
G_M000_IG130:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG131
       mov      eax, -1
       jmp      SHORT G_M000_IG133
 
G_M000_IG131:
       test     rdi, rdi
       jge      SHORT G_M000_IG132
       mov      eax, 1
       jmp      SHORT G_M000_IG133
 
G_M000_IG132:
       xor      eax, eax
 
G_M000_IG133:
       mov      edi, r13d
       imul     edi, eax
       jmp      G_M000_IG38
 
G_M000_IG134:
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG135
       mov      edi, dword ptr [r15+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG136
 
G_M000_IG135:
       mov      eax, r13d
       jmp      SHORT G_M000_IG142
 
G_M000_IG136:
       cmp      edi, 2
       je       SHORT G_M000_IG137
       cmp      dword ptr [r15+0x08], 0
       jbe      G_M000_IG153
       mov      edi, dword ptr [r15+0x10]
       jmp      SHORT G_M000_IG138
 
G_M000_IG137:
       mov      edi, dword ptr [r15+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r15+0x10]
       or       rdi, rax
 
G_M000_IG138:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG139
       mov      eax, -1
       jmp      SHORT G_M000_IG141
 
G_M000_IG139:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jbe      SHORT G_M000_IG140
       mov      eax, 1
       jmp      SHORT G_M000_IG141
 
G_M000_IG140:
       xor      eax, eax
 
G_M000_IG141:
       imul     eax, r13d
 
G_M000_IG142:
       test     eax, eax
       jl       G_M000_IG69
       jmp      G_M000_IG63
 
G_M000_IG143:
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG144
       mov      edi, dword ptr [r15+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG145
 
G_M000_IG144:
       mov      edi, r13d
       jmp      G_M000_IG65
 
G_M000_IG145:
       cmp      edi, 2
       je       SHORT G_M000_IG146
       cmp      dword ptr [r15+0x08], 0
       jbe      SHORT G_M000_IG153
       mov      edi, dword ptr [r15+0x10]
       jmp      SHORT G_M000_IG147
 
G_M000_IG146:
       mov      edi, dword ptr [r15+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r15+0x10]
       or       rdi, rax
 
G_M000_IG147:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG148
       mov      eax, -1
       jmp      SHORT G_M000_IG150
 
G_M000_IG148:
       test     rdi, rdi
       jge      SHORT G_M000_IG149
       mov      eax, 1
       jmp      SHORT G_M000_IG150
 
G_M000_IG149:
       xor      eax, eax
 
G_M000_IG150:
       mov      edi, r13d
       imul     edi, eax
       jmp      G_M000_IG65
 
G_M000_IG151:
       mov      r15, 0xD1FFAB1E
       jmp      G_M000_IG86
 
G_M000_IG152:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG153:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG154:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 3725

; Assembly listing for method RyuPy.Generated.Module:m0_accumulate(System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x295
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 3
; 46 inlinees with PGO data; 46 single block inlinees; 44 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 272
       mov      qword ptr [rsp+0x2D8], r15
       mov      qword ptr [rsp+0x2D0], r14
       mov      qword ptr [rsp+0x2C8], r13
       mov      qword ptr [rsp+0x2C0], r12
       mov      qword ptr [rsp+0x2B8], rbx
       lea      rbp, [rsp+0x110]
       xor      eax, eax
       mov      qword ptr [rbp-0xD8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xD0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, gword ptr [rbp+0x1A0]
       mov      r15, gword ptr [rbp+0x190]
       mov      r14, qword ptr [rbp+0xF0]
       mov      r13, gword ptr [rbp+0xB0]
       mov      r12, gword ptr [rbp+0xA8]
       vmovsd   xmm0, qword ptr [rbp+0x88]
       mov      eax, dword ptr [rbp+0x84]
       mov      rcx, gword ptr [rbp+0x70]
       mov      r8, gword ptr [rbp+0x60]
 
G_M000_IG02:
       jmp      G_M000_IG21
 
G_M000_IG03:
       vmovsd   qword ptr [rbp+0x88], xmm0
 
G_M000_IG04:
       mov      rdx, r12
       mov      rsi, r13
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      r13, rax
 
G_M000_IG05:
       test     r15, r15
       je       SHORT G_M000_IG09
 
G_M000_IG06:
       mov      r12, qword ptr [r15]
       mov      rdi, 0xD1FFAB1E
       cmp      r12, rdi
       je       G_M000_IG63
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       cmp      r12, rdi
       je       G_M000_IG80
 
G_M000_IG08:
       mov      rsi, 0xD1FFAB1E
       cmp      r12, rsi
       je       G_M000_IG85
 
G_M000_IG09:
       mov      rdx, r13
       mov      rsi, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r15, rax
 
G_M000_IG10:
       test     r15, r15
       jne      G_M000_IG94
 
G_M000_IG11:
       xor      r13d, r13d
 
G_M000_IG12:
       mov      r12, qword ptr [rbp+0xE0]
       add      r14, r12
       jo       G_M000_IG158
 
G_M000_IG13:
       test     r12, r12
       setg     dil
       movzx    rdi, dil
       mov      rax, qword ptr [rbp+0xE8]
       cmp      r14, rax
       setl     cl
       movzx    rcx, cl
       and      ecx, edi
       cmp      r14, rax
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, ecx
       je       G_M000_IG97
 
G_M000_IG14:
       lea      rcx, [r14+0x05]
       cmp      rcx, 261
       jbe      G_M000_IG110
 
G_M000_IG15:
       mov      qword ptr [rbp+0xE8], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r14
 
G_M000_IG16:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG19
 
G_M000_IG17:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      r8d, edi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      G_M000_IG157
       cmp      dword ptr [rdx+4*r8+0x10], 11
       je       SHORT G_M000_IG19
 
G_M000_IG18:
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG157
       mov      edi, edi
       mov      dword ptr [rcx+4*rdi+0x10], 11
 
G_M000_IG19:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rax
       mov      r8, rcx
       test     r13d, r13d
       mov      qword ptr [rbp+0xE0], r12
       mov      r12, rcx
       mov      rsi, rax
       mov      eax, r13d
       mov      r13, rsi
       vmovsd   xmm0, qword ptr [rbp+0x88]
       je       SHORT G_M000_IG26
 
G_M000_IG20:
       mov      rcx, rdx
 
G_M000_IG21:
       test     rcx, rcx
       je       SHORT G_M000_IG26
 
G_M000_IG22:
       mov      r10, 0xD1FFAB1E
       cmp      qword ptr [rcx], r10
       jne      SHORT G_M000_IG26
 
G_M000_IG23:
       test     r8, r8
       je       SHORT G_M000_IG26
 
G_M000_IG24:
       mov      r10, 0xD1FFAB1E
       cmp      qword ptr [r8], r10
       jne      SHORT G_M000_IG26
 
G_M000_IG25:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmulsd   xmm1, xmm1, qword ptr [r8+0x08]
       vaddsd   xmm0, xmm1, xmm0
       vmovsd   qword ptr [rbp+0x88], xmm0
       xor      r15, r15
       mov      r13d, eax
       jmp      G_M000_IG12
 
G_M000_IG26:
       test     r15, r15
       sete     cl
       movzx    rcx, cl
       test     eax, ecx
       jne      SHORT G_M000_IG31
 
G_M000_IG27:
       test     r13, r13
       je       G_M000_IG03
 
G_M000_IG28:
       mov      rax, qword ptr [r13]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       je       SHORT G_M000_IG32
 
G_M000_IG29:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       je       G_M000_IG57
 
G_M000_IG30:
       vmovsd   qword ptr [rbp+0x88], xmm0
       jmp      G_M000_IG04
 
G_M000_IG31:
       vmovsd   qword ptr [rbp+0x88], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       vmovsd   xmm0, qword ptr [rbp+0x88]
       vmovsd   qword ptr [r15+0x08], xmm0
       vmovsd   xmm0, qword ptr [rbp+0x88]
       jmp      SHORT G_M000_IG27
 
G_M000_IG32:
       mov      rax, qword ptr [r13+0x08]
       test     r12, r12
       jne      SHORT G_M000_IG34
 
G_M000_IG33:
       vmovsd   qword ptr [rbp+0x88], xmm0
       jmp      G_M000_IG04
 
G_M000_IG34:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [r12], rcx
       jne      G_M000_IG55
 
G_M000_IG35:
       mov      r12, qword ptr [r12+0x08]
       lea      rcx, [rbp-0x70]
       mov      rdx, rax
       mulx     rdx, r8, r12
       mov      qword ptr [rcx], r8
       mov      r13, qword ptr [rbp-0x70]
       mov      rcx, rax
       sar      rcx, 63
       and      rcx, r12
       sub      rdx, rcx
       mov      rcx, r12
       sar      rcx, 63
       and      rcx, rax
       sub      rdx, rcx
       mov      rcx, r13
       sar      rcx, 63
       cmp      rdx, rcx
       je       G_M000_IG51
 
G_M000_IG36:
       vmovsd   qword ptr [rbp+0x88], xmm0
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x68], xmm1
       lea      rdi, [rbp-0x68]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r13, gword ptr [rbp-0x68]
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0xEC], eax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       lea      rdi, [rbp-0x80]
       mov      rsi, r12
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0x80]
       mov      r9d, dword ptr [rbp-0x78]
       test     r13, r13
       jne      G_M000_IG44
 
G_M000_IG37:
       test     r12, r12
       jne      G_M000_IG120
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x90], xmm0
       movsxd   rsi, dword ptr [rbp-0xEC]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x90]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0x90]
       mov      r13d, dword ptr [rbp-0x88]
 
G_M000_IG38:
       test     r12, r12
       jne      G_M000_IG121
 
G_M000_IG39:
       test     r12, r12
       jne      G_M000_IG130
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jge      SHORT G_M000_IG49
 
G_M000_IG40:
       mov      edi, -1
 
G_M000_IG41:
       test     edi, edi
       jg       SHORT G_M000_IG50
 
G_M000_IG42:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x108], rax
       mov      rdi, r12
       mov      esi, r13d
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r13, gword ptr [rbp-0x108]
       mov      qword ptr [r13+0x08], rax
 
G_M000_IG43:
       jmp      G_M000_IG05
 
G_M000_IG44:
       lea      rdi, bword ptr [r13+0x10]
       mov      esi, dword ptr [r13+0x08]
 
G_M000_IG45:
       test     r12, r12
       je       SHORT G_M000_IG48
 
G_M000_IG46:
       lea      rcx, bword ptr [r12+0x10]
       mov      r8d, dword ptr [r12+0x08]
 
G_M000_IG47:
       mov      edx, dword ptr [rbp-0xEC]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      r12, rax
       mov      r13d, edx
       jmp      G_M000_IG38
 
G_M000_IG48:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG47
 
G_M000_IG49:
       xor      edi, edi
       jmp      SHORT G_M000_IG41
 
G_M000_IG50:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x100], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r12
       call     CORINFO_HELP_ASSIGN_REF
       mov      r12, gword ptr [rbp-0x100]
       mov      dword ptr [r12+0x10], r13d
       mov      r13, r12
       jmp      SHORT G_M000_IG43
 
G_M000_IG51:
       lea      r12, [r13+0x05]
       cmp      r12, 261
       jbe      SHORT G_M000_IG54
 
G_M000_IG52:
       vmovsd   qword ptr [rbp+0x88], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r13
       mov      r13, rax
 
G_M000_IG53:
       jmp      G_M000_IG43
 
G_M000_IG54:
       test     r12, r12
       jl       G_M000_IG158
       mov      rax, r12
       mov      rcx, 0xD1FFAB1E
       mov      r12, gword ptr [rcx]
       cmp      rax, 262
       jae      G_M000_IG157
       mov      r13, gword ptr [r12+8*rax+0x10]
       vmovsd   qword ptr [rbp+0x88], xmm0
       jmp      SHORT G_M000_IG53
 
G_M000_IG55:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [r12], rcx
       vmovsd   qword ptr [rbp+0x88], xmm0
       jne      G_M000_IG04
 
G_M000_IG56:
       vmovsd   xmm1, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0x58], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rax
       vmovsd   qword ptr [rbp-0x98], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x98]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG05
 
G_M000_IG57:
       vmovsd   xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm1
       test     r12, r12
       je       G_M000_IG62
 
G_M000_IG58:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r12], rax
       jne      SHORT G_M000_IG60
 
G_M000_IG59:
       vmovsd   qword ptr [rbp+0x88], xmm0
       vmovsd   xmm2, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG05
 
G_M000_IG60:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r12], rax
       vmovsd   qword ptr [rbp+0x88], xmm0
       jne      G_M000_IG04
 
G_M000_IG61:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG05
 
G_M000_IG62:
       vmovsd   qword ptr [rbp+0x88], xmm0
       jmp      G_M000_IG04
 
G_M000_IG63:
       mov      rsi, qword ptr [r15+0x08]
       test     r13, r13
       je       G_M000_IG09
 
G_M000_IG64:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG78
 
G_M000_IG65:
       mov      r13, qword ptr [r13+0x08]
       lea      r12, [rsi+r13]
       mov      rdi, rsi
       xor      rdi, r12
       mov      rax, r13
       xor      rax, r12
       and      rdi, rax
       jge      G_M000_IG74
 
G_M000_IG66:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xC8], xmm0
       lea      rdi, [rbp-0xC8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0xC8]
       mov      r15d, dword ptr [rbp-0xC0]
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xD8], xmm0
       lea      rdi, [rbp-0xD8]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, r12
       mov      esi, r15d
       mov      rdx, gword ptr [rbp-0xD8]
       mov      rcx, qword ptr [rbp-0xD0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      r15, rax
       mov      r13d, edx
       test     r15, r15
       jne      G_M000_IG138
 
G_M000_IG67:
       test     r15, r15
       jne      G_M000_IG147
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jge      SHORT G_M000_IG72
 
G_M000_IG68:
       mov      edi, -1
 
G_M000_IG69:
       test     edi, edi
       jg       SHORT G_M000_IG73
 
G_M000_IG70:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       mov      rdi, r15
       mov      esi, r13d
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      qword ptr [r12+0x08], rax
 
G_M000_IG71:
       mov      r15, r12
       jmp      G_M000_IG10
 
G_M000_IG72:
       xor      edi, edi
       jmp      SHORT G_M000_IG69
 
G_M000_IG73:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       lea      rdi, bword ptr [r12+0x08]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
       mov      dword ptr [r12+0x10], r13d
       jmp      SHORT G_M000_IG71
 
G_M000_IG74:
       lea      r15, [r12+0x05]
       cmp      r15, 261
       jbe      SHORT G_M000_IG77
 
G_M000_IG75:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
       mov      r12, rax
 
G_M000_IG76:
       jmp      SHORT G_M000_IG71
 
G_M000_IG77:
       test     r15, r15
       jl       G_M000_IG158
       mov      rdi, r15
       mov      rax, 0xD1FFAB1E
       mov      r12, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG157
       mov      r12, gword ptr [r12+8*rdi+0x10]
       jmp      SHORT G_M000_IG76
 
G_M000_IG78:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG09
 
G_M000_IG79:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xB8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xE0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xB8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG10
 
G_M000_IG80:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm0
       test     r13, r13
       je       G_M000_IG09
 
G_M000_IG81:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      SHORT G_M000_IG83
 
G_M000_IG82:
       vmovsd   xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG10
 
G_M000_IG83:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG09
 
G_M000_IG84:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xE8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG10
 
G_M000_IG85:
       mov      r12, r15
       test     r13, r13
       je       G_M000_IG09
 
G_M000_IG86:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r13], rsi
       jne      G_M000_IG09
 
G_M000_IG87:
       cmp      dword ptr [r12+0x08], 0
       je       SHORT G_M000_IG92
 
G_M000_IG88:
       cmp      dword ptr [r13+0x08], 0
       je       SHORT G_M000_IG91
 
G_M000_IG89:
       mov      r15d, dword ptr [r12+0x08]
       mov      esi, r15d
       add      esi, dword ptr [r13+0x08]
       js       G_M000_IG156
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x110], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [r12+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r12+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, r15d
       mov      r15, gword ptr [rbp-0x110]
       lea      rdi, bword ptr [r15+2*rdi+0x0C]
       mov      edx, dword ptr [r13+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r13+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       nop      
 
G_M000_IG90:
       jmp      G_M000_IG10
 
G_M000_IG91:
       mov      r15, r12
       jmp      SHORT G_M000_IG90
 
G_M000_IG92:
       cmp      dword ptr [r13+0x08], 0
       je       G_M000_IG155
 
G_M000_IG93:
       mov      r15, r13
       jmp      SHORT G_M000_IG90
 
G_M000_IG94:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG96
 
G_M000_IG95:
       mov      r13d, 1
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp+0x88], xmm0
       jmp      G_M000_IG12
 
G_M000_IG96:
       xor      r13d, r13d
       jmp      G_M000_IG12
 
G_M000_IG97:
       test     r15, r15
       sete     dil
       movzx    rdi, dil
       test     r13d, edi
       jne      G_M000_IG119
 
G_M000_IG98:
       mov      r13, qword ptr [rbp+0x158]
       mov      r12, qword ptr [rbp+0x148]
       add      r13, r12
       jo       G_M000_IG158
       test     r12, r12
       setg     dil
       movzx    rdi, dil
       mov      r14, qword ptr [rbp+0x150]
       cmp      r13, r14
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       cmp      r13, r14
       setg     al
       movzx    rax, al
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, eax
       or       edi, esi
       je       G_M000_IG111
 
G_M000_IG99:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG102
 
G_M000_IG100:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       lea      ecx, [rdi-0x01]
       mov      edx, ecx
       mov      r8d, dword ptr [rax+0x08]
       cmp      edx, r8d
       jae      G_M000_IG157
       mov      r8d, edx
       cmp      dword ptr [rax+4*r8+0x10], 9
       je       SHORT G_M000_IG102
 
G_M000_IG101:
       mov      eax, ecx
       mov      dword ptr [rsi+4*rax+0x10], 9
 
G_M000_IG102:
       mov      rsi, 0xD1FFAB1E
       mov      r15, gword ptr [rsi]
       test     edi, edi
       jle      SHORT G_M000_IG105
 
G_M000_IG103:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       lea      ecx, [rdi-0x01]
       mov      edi, ecx
       mov      r8d, dword ptr [rax+0x08]
       cmp      edi, r8d
       jae      G_M000_IG157
       cmp      dword ptr [rax+4*rdi+0x10], 10
       je       SHORT G_M000_IG105
 
G_M000_IG104:
       mov      rdi, rsi
       mov      esi, ecx
       mov      dword ptr [rdi+4*rsi+0x10], 10
 
G_M000_IG105:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp+0x110], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0xF8], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x40]
       mov      rsi, gword ptr [rbp-0xF8]
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
       mov      qword ptr [rbp+0xE8], rax
       mov      edx, 1
       vxorps   xmm0, xmm0, xmm0
       test     r15, r15
       je       SHORT G_M000_IG109
 
G_M000_IG106:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG108
 
G_M000_IG107:
       mov      esi, 1
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp+0x88], xmm0
       mov      qword ptr [rbp+0x158], r13
       mov      qword ptr [rbp+0x150], r14
       mov      qword ptr [rbp+0x148], r12
       mov      r12, rdx
       mov      r13d, esi
       mov      r14, rcx
       jmp      G_M000_IG13
 
G_M000_IG108:
       xor      esi, esi
       mov      qword ptr [rbp+0x158], r13
       mov      qword ptr [rbp+0x150], r14
       mov      qword ptr [rbp+0x148], r12
       vmovsd   qword ptr [rbp+0x88], xmm0
       mov      r12, rdx
       mov      r13d, esi
       mov      r14, rcx
       jmp      G_M000_IG13
 
G_M000_IG109:
       xor      esi, esi
       mov      qword ptr [rbp+0x158], r13
       mov      qword ptr [rbp+0x150], r14
       mov      qword ptr [rbp+0x148], r12
       vmovsd   qword ptr [rbp+0x88], xmm0
       mov      r12, rdx
       mov      r13d, esi
       mov      r14, rcx
       jmp      G_M000_IG13
 
G_M000_IG110:
       test     rcx, rcx
       jl       G_M000_IG158
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG157
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       mov      qword ptr [rbp+0xE8], rax
       jmp      G_M000_IG16
 
G_M000_IG111:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG114
 
G_M000_IG112:
       mov      rax, 0xD1FFAB1E
       mov      rsi, gword ptr [rax]
       mov      rax, rsi
       lea      ecx, [rdi-0x01]
       mov      edx, ecx
       mov      r8d, dword ptr [rax+0x08]
       cmp      edx, r8d
       jae      G_M000_IG157
       cmp      dword ptr [rax+4*rdx+0x10], 12
       je       SHORT G_M000_IG114
 
G_M000_IG113:
       mov      rax, rsi
       mov      ecx, ecx
       mov      dword ptr [rax+4*rcx+0x10], 12
 
G_M000_IG114:
       mov      rbx, qword ptr [rbp+0x180]
       test     rbx, rbx
       jle      SHORT G_M000_IG117
 
G_M000_IG115:
       movsxd   rax, edi
       cmp      rbx, rax
       jg       SHORT G_M000_IG117
 
G_M000_IG116:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG117:
       mov      rax, r15
 
G_M000_IG118:
       add      rsp, 696
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG119:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       vmovsd   xmm0, qword ptr [rbp+0x88]
       vmovsd   qword ptr [r15+0x08], xmm0
       jmp      G_M000_IG98
 
G_M000_IG120:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG45
 
G_M000_IG121:
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG122
       mov      edi, dword ptr [r12+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG123
 
G_M000_IG122:
       mov      eax, r13d
       jmp      SHORT G_M000_IG129
 
G_M000_IG123:
       cmp      edi, 2
       je       SHORT G_M000_IG124
       cmp      dword ptr [r12+0x08], 0
       jbe      G_M000_IG157
       mov      edi, dword ptr [r12+0x10]
       jmp      SHORT G_M000_IG125
 
G_M000_IG124:
       mov      edi, dword ptr [r12+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r12+0x10]
       or       rdi, rax
 
G_M000_IG125:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG126
       mov      eax, -1
       jmp      SHORT G_M000_IG128
 
G_M000_IG126:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jbe      SHORT G_M000_IG127
       mov      eax, 1
       jmp      SHORT G_M000_IG128
 
G_M000_IG127:
       xor      eax, eax
 
G_M000_IG128:
       imul     eax, r13d
 
G_M000_IG129:
       test     eax, eax
       jl       G_M000_IG50
       jmp      G_M000_IG39
 
G_M000_IG130:
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG131
       mov      edi, dword ptr [r12+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG132
 
G_M000_IG131:
       mov      edi, r13d
       jmp      G_M000_IG41
 
G_M000_IG132:
       cmp      edi, 2
       je       SHORT G_M000_IG133
       cmp      dword ptr [r12+0x08], 0
       jbe      G_M000_IG157
       mov      edi, dword ptr [r12+0x10]
       jmp      SHORT G_M000_IG134
 
G_M000_IG133:
       mov      edi, dword ptr [r12+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r12+0x10]
       or       rdi, rax
 
G_M000_IG134:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG135
       mov      eax, -1
       jmp      SHORT G_M000_IG137
 
G_M000_IG135:
       test     rdi, rdi
       jge      SHORT G_M000_IG136
       mov      eax, 1
       jmp      SHORT G_M000_IG137
 
G_M000_IG136:
       xor      eax, eax
 
G_M000_IG137:
       mov      edi, r13d
       imul     edi, eax
       jmp      G_M000_IG41
 
G_M000_IG138:
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG139
       mov      edi, dword ptr [r15+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG140
 
G_M000_IG139:
       mov      eax, r13d
       jmp      SHORT G_M000_IG146
 
G_M000_IG140:
       cmp      edi, 2
       je       SHORT G_M000_IG141
       cmp      dword ptr [r15+0x08], 0
       jbe      G_M000_IG157
       mov      edi, dword ptr [r15+0x10]
       jmp      SHORT G_M000_IG142
 
G_M000_IG141:
       mov      edi, dword ptr [r15+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r15+0x10]
       or       rdi, rax
 
G_M000_IG142:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG143
       mov      eax, -1
       jmp      SHORT G_M000_IG145
 
G_M000_IG143:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jbe      SHORT G_M000_IG144
       mov      eax, 1
       jmp      SHORT G_M000_IG145
 
G_M000_IG144:
       xor      eax, eax
 
G_M000_IG145:
       imul     eax, r13d
 
G_M000_IG146:
       test     eax, eax
       jl       G_M000_IG73
       jmp      G_M000_IG67
 
G_M000_IG147:
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG148
       mov      edi, dword ptr [r15+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG149
 
G_M000_IG148:
       mov      edi, r13d
       jmp      G_M000_IG69
 
G_M000_IG149:
       cmp      edi, 2
       je       SHORT G_M000_IG150
       cmp      dword ptr [r15+0x08], 0
       jbe      SHORT G_M000_IG157
       mov      edi, dword ptr [r15+0x10]
       jmp      SHORT G_M000_IG151
 
G_M000_IG150:
       mov      edi, dword ptr [r15+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r15+0x10]
       or       rdi, rax
 
G_M000_IG151:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG152
       mov      eax, -1
       jmp      SHORT G_M000_IG154
 
G_M000_IG152:
       test     rdi, rdi
       jge      SHORT G_M000_IG153
       mov      eax, 1
       jmp      SHORT G_M000_IG154
 
G_M000_IG153:
       xor      eax, eax
 
G_M000_IG154:
       mov      edi, r13d
       imul     edi, eax
       jmp      G_M000_IG69
 
G_M000_IG155:
       mov      r15, 0xD1FFAB1E
       jmp      G_M000_IG90
 
G_M000_IG156:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG157:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG158:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 3766

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
       xor      eax, eax
       mov      qword ptr [rbp-0x148], rax
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_ISINSTANCEOFCLASS
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x14C], eax
       cmp      dword ptr [rbp-0x14C], 0
       jne      G_M000_IG36
       jmp      SHORT G_M000_IG09
 
G_M000_IG07:
       cmp      gword ptr [rbp-0x40], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x150], eax
       mov      eax, dword ptr [rbp-0x14C]
       and      eax, dword ptr [rbp-0x150]
       movzx    rax, al
       mov      dword ptr [rbp-0x154], eax
       cmp      dword ptr [rbp-0x154], 0
       jne      G_M000_IG40
       jmp      G_M000_IG37
 
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
       mov      esi, 330
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
       mov      esi, 396
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
       mov      gword ptr [rbp-0x160], rax
       mov      rax, gword ptr [rbp-0x128]
       mov      gword ptr [rbp-0x170], rax
       cmp      dword ptr [rbp-0x14C], 0
       jne      G_M000_IG33
       jmp      G_M000_IG26
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0xE0]
       add      rax, qword ptr [rbp-0xF0]
       jo       G_M000_IG41
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
       mov      esi, 477
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG20:
       jmp      G_M000_IG15
 
G_M000_IG21:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x148], xmm0
       jmp      SHORT G_M000_IG18
 
G_M000_IG22:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG24
 
G_M000_IG23:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 495
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG24:
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
       mov      rax, gword ptr [rbp-0x138]
       mov      gword ptr [rbp-0x40], rax
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_ISINSTANCEOFCLASS
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x14C], eax
       cmp      dword ptr [rbp-0x14C], 0
       jne      G_M000_IG21
       jmp      G_M000_IG18
 
G_M000_IG25:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x1A8], rax
       mov      rax, gword ptr [rbp-0x1A8]
       vmovsd   xmm0, qword ptr [rbp-0x148]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x1A8]
       mov      gword ptr [rbp-0x40], rax
       jmp      G_M000_IG22
 
G_M000_IG26:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG28
 
G_M000_IG27:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 566
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG28:
       cmp      gword ptr [rbp-0x40], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x150], eax
       mov      eax, dword ptr [rbp-0x14C]
       and      eax, dword ptr [rbp-0x150]
       movzx    rax, al
       mov      dword ptr [rbp-0x154], eax
       cmp      dword ptr [rbp-0x154], 0
       jne      G_M000_IG25
       jmp      G_M000_IG22
 
G_M000_IG29:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x160]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x168], xmm0
       mov      rsi, gword ptr [rbp-0x170]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x178], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x168]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rbp-0x180], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x148]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x180]
       vmovsd   qword ptr [rbp-0x188], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x188]
       vmovsd   qword ptr [rbp-0x148], xmm0
       xor      rax, rax
       mov      gword ptr [rbp-0x40], rax
       jmp      G_M000_IG18
 
G_M000_IG30:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG32
 
G_M000_IG31:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 634
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG32:
       mov      rsi, gword ptr [rbp-0x170]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_ISINSTANCEOFCLASS
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x18C], eax
       cmp      dword ptr [rbp-0x18C], 0
       jne      G_M000_IG29
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG26
 
G_M000_IG33:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG35
 
G_M000_IG34:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 661
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG35:
       mov      rsi, gword ptr [rbp-0x160]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_ISINSTANCEOFCLASS
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x190], eax
       cmp      dword ptr [rbp-0x190], 0
       jne      G_M000_IG30
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG26
 
G_M000_IG36:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x148], xmm0
       jmp      G_M000_IG09
 
G_M000_IG37:
       mov      eax, dword ptr [rbp-0x1B0]
       dec      eax
       mov      dword ptr [rbp-0x1B0], eax
       cmp      dword ptr [rbp-0x1B0], 0
       jg       SHORT G_M000_IG39
 
G_M000_IG38:
       lea      rdi, [rbp-0x1B0]
       mov      esi, 701
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x78]
       add      rax, qword ptr [rbp-0x88]
       jo       SHORT G_M000_IG41
       mov      qword ptr [rbp-0x78], rax
       jmp      G_M000_IG03
 
G_M000_IG40:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x1A8], rax
       mov      rax, gword ptr [rbp-0x1A8]
       vmovsd   xmm0, qword ptr [rbp-0x148]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x1A8]
       mov      gword ptr [rbp-0x40], rax
       jmp      G_M000_IG37
 
G_M000_IG41:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2281

; Assembly listing for method RyuPy.Generated.Module:m0_accumulate(System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x14a
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 9
; 25 inlinees with PGO data; 6 single block inlinees; 4 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 208
       mov      qword ptr [rsp+0x298], r15
       mov      qword ptr [rsp+0x290], r14
       mov      qword ptr [rsp+0x288], r13
       mov      qword ptr [rsp+0x280], r12
       mov      qword ptr [rsp+0x278], rbx
       lea      rbp, [rsp+0xD0]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xB0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x70], zmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, gword ptr [rbp+0x1A0]
       mov      r15, gword ptr [rbp+0x190]
       mov      rsi, qword ptr [rbp+0x148]
       mov      r14, qword ptr [rbp+0xF0]
       mov      r12, qword ptr [rbp+0xE8]
       mov      rax, qword ptr [rbp+0xE0]
       mov      r13d, dword ptr [rbp+0x84]
 
G_M000_IG02:
       mov      qword ptr [rbp+0x148], rsi
       test     rsi, rsi
       setg     r9b
       movzx    r9, r9b
       mov      dword ptr [rbp-0xCC], r9d
       jmp      SHORT G_M000_IG09
 
G_M000_IG03:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmulsd   xmm0, xmm0, qword ptr [rcx+0x08]
       vaddsd   xmm0, xmm0, qword ptr [rbp+0x88]
       vmovsd   qword ptr [rbp+0x88], xmm0
       xor      r15, r15
       jmp      SHORT G_M000_IG08
 
G_M000_IG04:
       mov      r13d, 1
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp+0x88], xmm0
       jmp      SHORT G_M000_IG08
 
G_M000_IG05:
       mov      rdx, rsi
       mov      rsi, r15
       mov      edi, 257
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      r15, rax
 
G_M000_IG06:
       test     r15, r15
       jne      G_M000_IG32
 
G_M000_IG07:
       xor      r13d, r13d
 
G_M000_IG08:
       mov      rax, qword ptr [rbp+0xE0]
       add      r14, rax
       jo       G_M000_IG94
 
G_M000_IG09:
       mov      qword ptr [rbp+0xE0], rax
       test     rax, rax
       setg     dil
       movzx    rdi, dil
       cmp      r14, r12
       setl     r10b
       movzx    r10, r10b
       and      r10d, edi
       cmp      r14, r12
       setg     r11b
       movzx    r11, r11b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r11d
       or       edi, r10d
       je       G_M000_IG34
 
G_M000_IG10:
       lea      rdi, [r14+0x05]
       cmp      rdi, 261
       ja       G_M000_IG55
 
G_M000_IG11:
       test     rdi, rdi
       jl       G_M000_IG94
       mov      r10, 0xD1FFAB1E
       mov      r10, gword ptr [r10]
       cmp      rdi, 262
       jae      G_M000_IG93
       mov      r10, gword ptr [r10+8*rdi+0x10]
 
G_M000_IG12:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG15
 
G_M000_IG13:
       mov      r11, 0xD1FFAB1E
       mov      r11, gword ptr [r11]
       dec      edi
       mov      r8d, edi
       cmp      r8d, dword ptr [r11+0x08]
       jae      G_M000_IG93
       cmp      dword ptr [r11+4*r8+0x10], 11
       je       SHORT G_M000_IG15
 
G_M000_IG14:
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       cmp      edi, dword ptr [r8+0x08]
       jae      G_M000_IG93
       mov      edi, edi
       mov      dword ptr [r8+4*rdi+0x10], 11
 
G_M000_IG15:
       mov      rdi, rbx
       mov      rsi, r10
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0xB0], rax
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp+0xA8], rcx
       test     r13d, r13d
       je       SHORT G_M000_IG20
 
G_M000_IG16:
       test     rax, rax
       je       SHORT G_M000_IG20
 
G_M000_IG17:
       mov      rdx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      SHORT G_M000_IG20
 
G_M000_IG18:
       test     rcx, rcx
       je       SHORT G_M000_IG20
 
G_M000_IG19:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG03
 
G_M000_IG20:
       test     r15, r15
       sete     dil
       movzx    rdi, dil
       test     r13d, edi
       jne      G_M000_IG56
 
G_M000_IG21:
       test     rax, rax
       je       SHORT G_M000_IG24
 
G_M000_IG22:
       mov      rdx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       je       G_M000_IG57
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       je       G_M000_IG68
 
G_M000_IG24:
       mov      rdx, rcx
       mov      rsi, rax
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG25:
       test     r15, r15
       je       G_M000_IG05
 
G_M000_IG26:
       mov      r13, qword ptr [r15]
       mov      rdi, 0xD1FFAB1E
       cmp      r13, rdi
       je       G_M000_IG76
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       cmp      r13, rdi
       je       G_M000_IG84
 
G_M000_IG28:
       mov      rdi, 0xD1FFAB1E
       cmp      r13, rdi
       jne      G_M000_IG05
 
G_M000_IG29:
       mov      rdi, r15
       test     rsi, rsi
       je       G_M000_IG05
 
G_M000_IG30:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG05
 
G_M000_IG31:
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      r15, rax
       jmp      G_M000_IG06
 
G_M000_IG32:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       je       G_M000_IG04
 
G_M000_IG33:
       xor      r13d, r13d
       jmp      G_M000_IG08
 
G_M000_IG34:
       test     r15, r15
       sete     dil
       movzx    rdi, dil
       test     r13d, edi
       jne      G_M000_IG44
 
G_M000_IG35:
       mov      rcx, qword ptr [rbp+0x158]
       add      rcx, qword ptr [rbp+0x148]
       jo       G_M000_IG94
       mov      r9d, dword ptr [rbp-0xCC]
       mov      edi, r9d
       mov      rdx, qword ptr [rbp+0x150]
       cmp      rcx, rdx
       setl     al
       movzx    rax, al
       and      eax, edi
       mov      qword ptr [rbp+0x158], rcx
       mov      qword ptr [rbp+0x150], rdx
       cmp      rcx, rdx
       setg     r10b
       movzx    r10, r10b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r10d
       or       edi, eax
       je       G_M000_IG48
 
G_M000_IG36:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG39
 
G_M000_IG37:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       lea      r10d, [rdi-0x01]
       mov      r11d, r10d
       cmp      r11d, dword ptr [rax+0x08]
       jae      G_M000_IG93
       cmp      dword ptr [rax+4*r11+0x10], 9
       je       SHORT G_M000_IG39
 
G_M000_IG38:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      r10d, dword ptr [rax+0x08]
       jae      G_M000_IG93
       mov      r10d, r10d
       mov      dword ptr [rax+4*r10+0x10], 9
 
G_M000_IG39:
       mov      rax, 0xD1FFAB1E
       mov      r15, gword ptr [rax]
       test     edi, edi
       jle      SHORT G_M000_IG42
 
G_M000_IG40:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       lea      r10d, [rdi-0x01]
       mov      edi, r10d
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG93
       cmp      dword ptr [rax+4*rdi+0x10], 10
       je       SHORT G_M000_IG42
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      eax, r10d
       cmp      eax, dword ptr [rdi+0x08]
       jae      G_M000_IG93
       mov      dword ptr [rdi+4*rax+0x10], 10
 
G_M000_IG42:
       mov      rdi, 0xD1FFAB1E
       mov      r14, gword ptr [rdi]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r12, rax
       lea      rdi, bword ptr [r12+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x40]
       mov      rsi, r12
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdx, gword ptr [rbp-0x38]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      r13, rax
       mov      rdi, r14
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r14, rax
       mov      rdi, r13
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      r13d, 1
       vxorps   xmm0, xmm0, xmm0
       test     r15, r15
       jne      SHORT G_M000_IG45
 
G_M000_IG43:
       xor      eax, eax
       vmovsd   qword ptr [rbp+0x88], xmm0
       mov      ecx, eax
       mov      rax, r13
       mov      r13d, ecx
       jmp      G_M000_IG09
 
G_M000_IG44:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       vmovsd   xmm0, qword ptr [rbp+0x88]
       vmovsd   qword ptr [r15+0x08], xmm0
       jmp      G_M000_IG35
 
G_M000_IG45:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG47
 
G_M000_IG46:
       mov      eax, 1
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp+0x88], xmm0
       mov      ecx, eax
       mov      rax, r13
       mov      r13d, ecx
       jmp      G_M000_IG09
 
G_M000_IG47:
       xor      eax, eax
       vmovsd   qword ptr [rbp+0x88], xmm0
       mov      ecx, eax
       mov      rax, r13
       mov      r13d, ecx
       jmp      G_M000_IG09
 
G_M000_IG48:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG51
 
G_M000_IG49:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       lea      r10d, [rdi-0x01]
       mov      ecx, r10d
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG93
       cmp      dword ptr [rax+4*rcx+0x10], 12
       je       SHORT G_M000_IG51
 
G_M000_IG50:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      ecx, r10d
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG93
       mov      dword ptr [rax+4*rcx+0x10], 12
 
G_M000_IG51:
       mov      r8, qword ptr [rbp+0x180]
       test     r8, r8
       jle      SHORT G_M000_IG53
 
G_M000_IG52:
       movsxd   rax, edi
       cmp      r8, rax
       jg       SHORT G_M000_IG53
       dec      r8d
       mov      dword ptr [(reloc)], r8d
 
G_M000_IG53:
       mov      rax, r15
 
G_M000_IG54:
       add      rsp, 632
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG55:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r10, rax
       mov      qword ptr [r10+0x08], r14
       jmp      G_M000_IG12
 
G_M000_IG56:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       vmovsd   xmm0, qword ptr [rbp+0x88]
       vmovsd   qword ptr [r15+0x08], xmm0
       mov      rax, gword ptr [rbp+0xB0]
       mov      rcx, gword ptr [rbp+0xA8]
       jmp      G_M000_IG21
 
G_M000_IG57:
       mov      r13, qword ptr [rax+0x08]
       test     rcx, rcx
       je       G_M000_IG24
 
G_M000_IG58:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       jne      G_M000_IG66
 
G_M000_IG59:
       mov      rax, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x60], rax
       xor      edx, edx
       mov      qword ptr [rbp-0x68], rdx
       lea      rdx, [rbp-0x68]
       mov      rdi, r13
       mov      rsi, rax
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x68]
       sar      rdi, 63
       cmp      rax, rdi
       je       SHORT G_M000_IG62
 
G_M000_IG60:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x78], xmm0
       lea      rdi, [rbp-0x78]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r13, gword ptr [rbp-0x78]
       mov      eax, dword ptr [rbp-0x70]
       mov      dword ptr [rbp-0xC4], eax
       mov      rdi, qword ptr [rbp-0x60]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, r13
       mov      esi, dword ptr [rbp-0xC4]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG61:
       jmp      G_M000_IG25
 
G_M000_IG62:
       mov      r13, qword ptr [rbp-0x68]
       lea      rdi, [r13+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG65
 
G_M000_IG63:
       test     rdi, rdi
       jl       G_M000_IG94
       mov      rax, 0xD1FFAB1E
       mov      r10, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG93
       mov      rsi, gword ptr [r10+8*rdi+0x10]
 
G_M000_IG64:
       jmp      SHORT G_M000_IG61
 
G_M000_IG65:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r13
       jmp      SHORT G_M000_IG64
 
G_M000_IG66:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG24
 
G_M000_IG67:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x58], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, r13
       vmovsd   qword ptr [rbp-0x80], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x80]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG25
 
G_M000_IG68:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm1
       test     rcx, rcx
       je       SHORT G_M000_IG71
 
G_M000_IG69:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      SHORT G_M000_IG71
 
G_M000_IG70:
       vmovsd   xmm2, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG25
 
G_M000_IG71:
       mov      rsi, rcx
       test     rsi, rsi
       je       SHORT G_M000_IG74
 
G_M000_IG72:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG74
 
G_M000_IG73:
       xor      rsi, rsi
 
G_M000_IG74:
       test     rsi, rsi
       je       G_M000_IG24
 
G_M000_IG75:
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x88], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG25
 
G_M000_IG76:
       mov      rax, qword ptr [r15+0x08]
       test     rsi, rsi
       je       G_M000_IG05
 
G_M000_IG77:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG82
 
G_M000_IG78:
       mov      r13, qword ptr [rsi+0x08]
       lea      r15, [rax+r13]
       mov      rdi, rax
       xor      rdi, r15
       mov      rcx, r13
       xor      rcx, r15
       and      rdi, rcx
       jl       G_M000_IG92
       lea      rdi, [r15+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG81
 
G_M000_IG79:
       test     rdi, rdi
       jl       G_M000_IG94
       mov      rax, 0xD1FFAB1E
       mov      r10, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG93
       mov      r13, gword ptr [r10+8*rdi+0x10]
 
G_M000_IG80:
       mov      r15, r13
       jmp      G_M000_IG06
 
G_M000_IG81:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       mov      qword ptr [r13+0x08], r15
       jmp      SHORT G_M000_IG80
 
G_M000_IG82:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG05
 
G_M000_IG83:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0xB8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xB8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG06
 
G_M000_IG84:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm0
       test     rsi, rsi
       je       SHORT G_M000_IG87
 
G_M000_IG85:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG87
 
G_M000_IG86:
       vmovsd   xmm1, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x90]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG06
 
G_M000_IG87:
       mov      rdi, rsi
       test     rdi, rdi
       je       SHORT G_M000_IG90
 
G_M000_IG88:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       je       SHORT G_M000_IG90
 
G_M000_IG89:
       xor      rdi, rdi
 
G_M000_IG90:
       test     rdi, rdi
       je       G_M000_IG05
 
G_M000_IG91:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0xC0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xC0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG06
 
G_M000_IG92:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xB0], xmm0
       lea      rdi, [rbp-0xB0]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r15, gword ptr [rbp-0xB0]
       mov      eax, dword ptr [rbp-0xA8]
       mov      dword ptr [rbp-0xC8], eax
       mov      rdi, r13
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, r15
       mov      esi, dword ptr [rbp-0xC8]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      r13, rax
       jmp      G_M000_IG80
 
G_M000_IG93:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG94:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2589

; Assembly listing for method RyuPy.Generated.Module:m0_accumulate(System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x1dd
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 9
; 25 inlinees with PGO data; 6 single block inlinees; 4 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 208
       mov      qword ptr [rsp+0x298], r15
       mov      qword ptr [rsp+0x290], r14
       mov      qword ptr [rsp+0x288], r13
       mov      qword ptr [rsp+0x280], r12
       mov      qword ptr [rsp+0x278], rbx
       lea      rbp, [rsp+0xD0]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xB0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x70], zmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, gword ptr [rbp+0x1A0]
       mov      r15, gword ptr [rbp+0x190]
       mov      rsi, qword ptr [rbp+0x148]
       mov      r14, qword ptr [rbp+0xF0]
       mov      r12, qword ptr [rbp+0xE8]
       mov      rax, qword ptr [rbp+0xE0]
       mov      r13d, dword ptr [rbp+0x84]
 
G_M000_IG02:
       mov      qword ptr [rbp+0x148], rsi
       test     rsi, rsi
       setg     r9b
       movzx    r9, r9b
       mov      dword ptr [rbp-0xCC], r9d
       jmp      SHORT G_M000_IG06
 
G_M000_IG03:
       mov      rdx, rsi
       mov      rsi, r15
       mov      edi, 257
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      r15, rax
 
G_M000_IG04:
       test     r15, r15
       jne      G_M000_IG27
 
G_M000_IG05:
       xor      r13d, r13d
       mov      rax, qword ptr [rbp+0xE0]
 
G_M000_IG06:
       add      r14, rax
       jo       G_M000_IG96
 
G_M000_IG07:
       test     rax, rax
       setg     dil
       movzx    rdi, dil
       cmp      r14, r12
       setl     r10b
       movzx    r10, r10b
       and      r10d, edi
       cmp      r14, r12
       setg     r11b
       movzx    r11, r11b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r11d
       or       edi, r10d
       je       G_M000_IG37
 
G_M000_IG08:
       lea      rdi, [r14+0x05]
       cmp      rdi, 261
       ja       G_M000_IG51
 
G_M000_IG09:
       test     rdi, rdi
       jl       G_M000_IG96
       mov      r10, 0xD1FFAB1E
       mov      r10, gword ptr [r10]
       cmp      rdi, 262
       jae      G_M000_IG95
       mov      r10, gword ptr [r10+8*rdi+0x10]
 
G_M000_IG10:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      G_M000_IG53
 
G_M000_IG11:
       mov      r11, 0xD1FFAB1E
       mov      r11, gword ptr [r11]
       dec      edi
       mov      r8d, edi
       cmp      r8d, dword ptr [r11+0x08]
       jae      G_M000_IG95
       cmp      dword ptr [r11+4*r8+0x10], 11
       jne      G_M000_IG52
 
G_M000_IG12:
       mov      qword ptr [rbp+0xE0], rax
 
G_M000_IG13:
       mov      rdi, rbx
       mov      rsi, r10
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0xB0], rax
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp+0xA8], rcx
       test     r13d, r13d
       je       SHORT G_M000_IG15
 
G_M000_IG14:
       test     rax, rax
       jne      G_M000_IG54
 
G_M000_IG15:
       test     r15, r15
       sete     dil
       movzx    rdi, dil
       test     r13d, edi
       jne      G_M000_IG58
 
G_M000_IG16:
       test     rax, rax
       je       SHORT G_M000_IG19
 
G_M000_IG17:
       mov      rdx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       je       G_M000_IG59
 
G_M000_IG18:
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       je       G_M000_IG70
 
G_M000_IG19:
       mov      rdx, rcx
       mov      rsi, rax
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG20:
       test     r15, r15
       je       G_M000_IG03
 
G_M000_IG21:
       mov      r13, qword ptr [r15]
       mov      rdi, 0xD1FFAB1E
       cmp      r13, rdi
       je       G_M000_IG78
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       cmp      r13, rdi
       je       G_M000_IG86
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       cmp      r13, rdi
       jne      G_M000_IG03
 
G_M000_IG24:
       mov      rdi, r15
       test     rsi, rsi
       je       G_M000_IG03
 
G_M000_IG25:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG03
 
G_M000_IG26:
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      r15, rax
       jmp      G_M000_IG04
 
G_M000_IG27:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG29
 
G_M000_IG28:
       mov      r13d, 1
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp+0x88], xmm0
       mov      rax, qword ptr [rbp+0xE0]
       jmp      G_M000_IG06
 
G_M000_IG29:
       xor      r13d, r13d
       mov      rax, qword ptr [rbp+0xE0]
       jmp      G_M000_IG06
 
G_M000_IG30:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG33
 
G_M000_IG31:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       lea      r10d, [rdi-0x01]
       mov      ecx, r10d
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG95
       cmp      dword ptr [rax+4*rcx+0x10], 12
       je       SHORT G_M000_IG33
 
G_M000_IG32:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      ecx, r10d
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG95
       mov      dword ptr [rax+4*rcx+0x10], 12
 
G_M000_IG33:
       mov      r8, qword ptr [rbp+0x180]
       test     r8, r8
       jle      SHORT G_M000_IG35
 
G_M000_IG34:
       movsxd   rax, edi
       cmp      r8, rax
       jg       SHORT G_M000_IG35
       dec      r8d
       mov      dword ptr [(reloc)], r8d
 
G_M000_IG35:
       mov      rax, r15
 
G_M000_IG36:
       add      rsp, 632
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG37:
       test     r15, r15
       sete     dil
       movzx    rdi, dil
       test     r13d, edi
       jne      G_M000_IG47
 
G_M000_IG38:
       mov      rcx, qword ptr [rbp+0x158]
       add      rcx, qword ptr [rbp+0x148]
       jo       G_M000_IG96
       mov      r9d, dword ptr [rbp-0xCC]
       mov      edi, r9d
       mov      rdx, qword ptr [rbp+0x150]
       cmp      rcx, rdx
       setl     al
       movzx    rax, al
       and      eax, edi
       mov      qword ptr [rbp+0x158], rcx
       mov      qword ptr [rbp+0x150], rdx
       cmp      rcx, rdx
       setg     r10b
       movzx    r10, r10b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r10d
       or       edi, eax
       je       G_M000_IG30
 
G_M000_IG39:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG42
 
G_M000_IG40:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       lea      r10d, [rdi-0x01]
       mov      r11d, r10d
       cmp      r11d, dword ptr [rax+0x08]
       jae      G_M000_IG95
       cmp      dword ptr [rax+4*r11+0x10], 9
       je       SHORT G_M000_IG42
 
G_M000_IG41:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      r10d, dword ptr [rax+0x08]
       jae      G_M000_IG95
       mov      r10d, r10d
       mov      dword ptr [rax+4*r10+0x10], 9
 
G_M000_IG42:
       mov      rax, 0xD1FFAB1E
       mov      r15, gword ptr [rax]
       test     edi, edi
       jle      SHORT G_M000_IG45
 
G_M000_IG43:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       lea      r10d, [rdi-0x01]
       mov      edi, r10d
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG95
       cmp      dword ptr [rax+4*rdi+0x10], 10
       je       SHORT G_M000_IG45
 
G_M000_IG44:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      eax, r10d
       cmp      eax, dword ptr [rdi+0x08]
       jae      G_M000_IG95
       mov      dword ptr [rdi+4*rax+0x10], 10
 
G_M000_IG45:
       mov      rdi, 0xD1FFAB1E
       mov      r14, gword ptr [rdi]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r12, rax
       lea      rdi, bword ptr [r12+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x40]
       mov      rsi, r12
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdx, gword ptr [rbp-0x38]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      r13, rax
       mov      rdi, r14
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r14, rax
       mov      rdi, r13
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      qword ptr [rbp+0xE0], 1
       vxorps   xmm0, xmm0, xmm0
       test     r15, r15
       jne      SHORT G_M000_IG48
 
G_M000_IG46:
       xor      eax, eax
       vmovsd   qword ptr [rbp+0x88], xmm0
       mov      r13d, eax
       mov      rax, qword ptr [rbp+0xE0]
       jmp      G_M000_IG07
 
G_M000_IG47:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       vmovsd   xmm0, qword ptr [rbp+0x88]
       vmovsd   qword ptr [r15+0x08], xmm0
       jmp      G_M000_IG38
 
G_M000_IG48:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG50
 
G_M000_IG49:
       mov      eax, 1
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp+0x88], xmm0
       mov      r13d, eax
       mov      rax, qword ptr [rbp+0xE0]
       jmp      G_M000_IG07
 
G_M000_IG50:
       xor      eax, eax
       vmovsd   qword ptr [rbp+0x88], xmm0
       mov      r13d, eax
       mov      rax, qword ptr [rbp+0xE0]
       jmp      G_M000_IG07
 
G_M000_IG51:
       mov      qword ptr [rbp+0xE0], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r10, rax
       mov      qword ptr [r10+0x08], r14
       mov      rax, qword ptr [rbp+0xE0]
       jmp      G_M000_IG10
 
G_M000_IG52:
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       cmp      edi, dword ptr [r8+0x08]
       jae      G_M000_IG95
       mov      edi, edi
       mov      dword ptr [r8+4*rdi+0x10], 11
       mov      qword ptr [rbp+0xE0], rax
       jmp      G_M000_IG13
 
G_M000_IG53:
       mov      qword ptr [rbp+0xE0], rax
       jmp      G_M000_IG13
 
G_M000_IG54:
       mov      rdx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      G_M000_IG15
 
G_M000_IG55:
       test     rcx, rcx
       je       G_M000_IG15
 
G_M000_IG56:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG15
 
G_M000_IG57:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmulsd   xmm0, xmm0, qword ptr [rcx+0x08]
       vaddsd   xmm0, xmm0, qword ptr [rbp+0x88]
       vmovsd   qword ptr [rbp+0x88], xmm0
       xor      r15, r15
       mov      rax, qword ptr [rbp+0xE0]
       jmp      G_M000_IG06
 
G_M000_IG58:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       vmovsd   xmm0, qword ptr [rbp+0x88]
       vmovsd   qword ptr [r15+0x08], xmm0
       mov      rax, gword ptr [rbp+0xB0]
       mov      rcx, gword ptr [rbp+0xA8]
       jmp      G_M000_IG16
 
G_M000_IG59:
       mov      r13, qword ptr [rax+0x08]
       test     rcx, rcx
       je       G_M000_IG19
 
G_M000_IG60:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       jne      G_M000_IG68
 
G_M000_IG61:
       mov      rax, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x60], rax
       xor      edx, edx
       mov      qword ptr [rbp-0x68], rdx
       lea      rdx, [rbp-0x68]
       mov      rdi, r13
       mov      rsi, rax
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x68]
       sar      rdi, 63
       cmp      rax, rdi
       je       SHORT G_M000_IG64
 
G_M000_IG62:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x78], xmm0
       lea      rdi, [rbp-0x78]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r13, gword ptr [rbp-0x78]
       mov      eax, dword ptr [rbp-0x70]
       mov      dword ptr [rbp-0xC4], eax
       mov      rdi, qword ptr [rbp-0x60]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, r13
       mov      esi, dword ptr [rbp-0xC4]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG63:
       jmp      G_M000_IG20
 
G_M000_IG64:
       mov      r13, qword ptr [rbp-0x68]
       lea      rdi, [r13+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG67
 
G_M000_IG65:
       test     rdi, rdi
       jl       G_M000_IG96
       mov      rax, 0xD1FFAB1E
       mov      r10, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG95
       mov      rsi, gword ptr [r10+8*rdi+0x10]
 
G_M000_IG66:
       jmp      SHORT G_M000_IG63
 
G_M000_IG67:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r13
       jmp      SHORT G_M000_IG66
 
G_M000_IG68:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG19
 
G_M000_IG69:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x58], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, r13
       vmovsd   qword ptr [rbp-0x80], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x80]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG20
 
G_M000_IG70:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm1
       test     rcx, rcx
       je       SHORT G_M000_IG73
 
G_M000_IG71:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      SHORT G_M000_IG73
 
G_M000_IG72:
       vmovsd   xmm2, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG20
 
G_M000_IG73:
       mov      rsi, rcx
       test     rsi, rsi
       je       SHORT G_M000_IG76
 
G_M000_IG74:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG76
 
G_M000_IG75:
       xor      rsi, rsi
 
G_M000_IG76:
       test     rsi, rsi
       je       G_M000_IG19
 
G_M000_IG77:
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x88], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG20
 
G_M000_IG78:
       mov      rax, qword ptr [r15+0x08]
       test     rsi, rsi
       je       G_M000_IG03
 
G_M000_IG79:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG84
 
G_M000_IG80:
       mov      r13, qword ptr [rsi+0x08]
       lea      r15, [rax+r13]
       mov      rdi, rax
       xor      rdi, r15
       mov      rcx, r13
       xor      rcx, r15
       and      rdi, rcx
       jl       G_M000_IG94
       lea      rdi, [r15+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG83
 
G_M000_IG81:
       test     rdi, rdi
       jl       G_M000_IG96
       mov      rax, 0xD1FFAB1E
       mov      r10, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG95
       mov      r13, gword ptr [r10+8*rdi+0x10]
 
G_M000_IG82:
       mov      r15, r13
       jmp      G_M000_IG04
 
G_M000_IG83:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       mov      qword ptr [r13+0x08], r15
       jmp      SHORT G_M000_IG82
 
G_M000_IG84:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG03
 
G_M000_IG85:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0xB8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xB8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG04
 
G_M000_IG86:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm0
       test     rsi, rsi
       je       SHORT G_M000_IG89
 
G_M000_IG87:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG89
 
G_M000_IG88:
       vmovsd   xmm1, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x90]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG04
 
G_M000_IG89:
       mov      rdi, rsi
       test     rdi, rdi
       je       SHORT G_M000_IG92
 
G_M000_IG90:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       je       SHORT G_M000_IG92
 
G_M000_IG91:
       xor      rdi, rdi
 
G_M000_IG92:
       test     rdi, rdi
       je       G_M000_IG03
 
G_M000_IG93:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0xC0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xC0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG04
 
G_M000_IG94:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xB0], xmm0
       lea      rdi, [rbp-0xB0]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r15, gword ptr [rbp-0xB0]
       mov      eax, dword ptr [rbp-0xA8]
       mov      dword ptr [rbp-0xC8], eax
       mov      rdi, r13
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, r15
       mov      esi, dword ptr [rbp-0xC8]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      r13, rax
       jmp      G_M000_IG82
 
G_M000_IG95:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG96:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2681

