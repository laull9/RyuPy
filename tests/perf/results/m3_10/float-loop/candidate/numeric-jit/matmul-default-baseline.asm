; Assembly listing for method RyuPy.Generated.Module:m0_matmul(System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 592
       lea      rbp, [rsp+0x250]
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x240], xmm8
       vmovdqa32 xmmword ptr [rbp-0x230], xmm8
       mov      rax, -480
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x20], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
 
G_M000_IG02:
       mov      dword ptr [rbp-0x248], 0x3E8
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
       mov      eax, dword ptr [rbp-0x248]
       dec      eax
       mov      dword ptr [rbp-0x248], eax
       cmp      dword ptr [rbp-0x248], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x248]
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
       jo       G_M000_IG43
       mov      qword ptr [rbp-0xA8], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      edi, 26
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x1E8], rax
       jmp      G_M000_IG25
 
G_M000_IG09:
       mov      eax, dword ptr [rbp-0x248]
       dec      eax
       mov      dword ptr [rbp-0x248], eax
       cmp      dword ptr [rbp-0x248], 0
       jg       SHORT G_M000_IG11
 
G_M000_IG10:
       lea      rdi, [rbp-0x248]
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
       jo       G_M000_IG43
       mov      qword ptr [rbp-0x110], rax
       jmp      G_M000_IG09
 
G_M000_IG14:
       mov      eax, dword ptr [rbp-0x248]
       dec      eax
       mov      dword ptr [rbp-0x248], eax
       cmp      dword ptr [rbp-0x248], 0
       jg       SHORT G_M000_IG16
 
G_M000_IG15:
       lea      rdi, [rbp-0x248]
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
       mov      eax, dword ptr [rbp-0x248]
       dec      eax
       mov      dword ptr [rbp-0x248], eax
       cmp      dword ptr [rbp-0x248], 0
       jg       SHORT G_M000_IG19
 
G_M000_IG18:
       lea      rdi, [rbp-0x248]
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
       jmp      G_M000_IG22
 
G_M000_IG20:
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
       mov      rax, gword ptr [rbp-0x1B0]
       mov      gword ptr [rbp-0x1F0], rax
       mov      rax, gword ptr [rbp-0x1C0]
       mov      gword ptr [rbp-0x200], rax
       mov      rax, gword ptr [rbp-0x70]
       mov      gword ptr [rbp-0x210], rax
       jmp      G_M000_IG40
 
G_M000_IG21:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x170]
       add      rax, qword ptr [rbp-0x180]
       jo       G_M000_IG43
       mov      qword ptr [rbp-0x170], rax
       jmp      G_M000_IG17
 
G_M000_IG22:
       mov      eax, dword ptr [rbp-0x248]
       dec      eax
       mov      dword ptr [rbp-0x248], eax
       cmp      dword ptr [rbp-0x248], 0
       jg       SHORT G_M000_IG24
 
G_M000_IG23:
       lea      rdi, [rbp-0x248]
       mov      esi, 647
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG24:
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, gword ptr [rbp-0x70]
       mov      rsi, gword ptr [rbp-0x60]
       mov      edi, 8
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       mov      gword ptr [rbp-0x1D8], rax
       jmp      G_M000_IG13
 
G_M000_IG25:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x80]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x1E8]
 
G_M000_IG26:
       add      rsp, 592
       pop      rbp
       ret      
 
G_M000_IG27:
       mov      eax, dword ptr [rbp-0x248]
       dec      eax
       mov      dword ptr [rbp-0x248], eax
       cmp      dword ptr [rbp-0x248], 0
       jg       SHORT G_M000_IG29
 
G_M000_IG28:
       lea      rdi, [rbp-0x248]
       mov      esi, 685
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG29:
       mov      rax, gword ptr [rbp-0x1D0]
       mov      gword ptr [rbp-0x70], rax
       jmp      G_M000_IG21
 
G_M000_IG30:
       mov      eax, dword ptr [rbp-0x248]
       dec      eax
       mov      dword ptr [rbp-0x248], eax
       cmp      dword ptr [rbp-0x248], 0
       jg       SHORT G_M000_IG32
 
G_M000_IG31:
       lea      rdi, [rbp-0x248]
       mov      esi, 694
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG32:
       mov      rdx, gword ptr [rbp-0x1B0]
       mov      rcx, gword ptr [rbp-0x1C0]
       mov      edi, 9
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x1C8], rax
       mov      rdx, gword ptr [rbp-0x70]
       mov      rcx, gword ptr [rbp-0x1C8]
       mov      edi, 10
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x1D0], rax
       jmp      G_M000_IG27
 
G_M000_IG33:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x1F0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x1F8], xmm0
       mov      rsi, gword ptr [rbp-0x200]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x208], xmm0
       mov      rsi, gword ptr [rbp-0x210]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x218], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1F8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x208]
       vmovsd   qword ptr [rbp-0x220], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x218]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x220]
       vmovsd   qword ptr [rbp-0x228], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x240], rax
       mov      rax, gword ptr [rbp-0x240]
       vmovsd   xmm0, qword ptr [rbp-0x228]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x240]
       mov      gword ptr [rbp-0x1D0], rax
       jmp      G_M000_IG27
 
G_M000_IG34:
       mov      eax, dword ptr [rbp-0x248]
       dec      eax
       mov      dword ptr [rbp-0x248], eax
       cmp      dword ptr [rbp-0x248], 0
       jg       SHORT G_M000_IG36
 
G_M000_IG35:
       lea      rdi, [rbp-0x248]
       mov      esi, 786
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG36:
       mov      rsi, gword ptr [rbp-0x210]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x22C], eax
       cmp      dword ptr [rbp-0x22C], 0
       jne      G_M000_IG33
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG30
 
G_M000_IG37:
       mov      eax, dword ptr [rbp-0x248]
       dec      eax
       mov      dword ptr [rbp-0x248], eax
       cmp      dword ptr [rbp-0x248], 0
       jg       SHORT G_M000_IG39
 
G_M000_IG38:
       lea      rdi, [rbp-0x248]
       mov      esi, 813
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG39:
       mov      rsi, gword ptr [rbp-0x200]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x230], eax
       cmp      dword ptr [rbp-0x230], 0
       jne      G_M000_IG34
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG30
 
G_M000_IG40:
       mov      eax, dword ptr [rbp-0x248]
       dec      eax
       mov      dword ptr [rbp-0x248], eax
       cmp      dword ptr [rbp-0x248], 0
       jg       SHORT G_M000_IG42
 
G_M000_IG41:
       lea      rdi, [rbp-0x248]
       mov      esi, 840
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG42:
       mov      rsi, gword ptr [rbp-0x1F0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x234], eax
       cmp      dword ptr [rbp-0x234], 0
       jne      G_M000_IG37
       jmp      G_M000_IG30
 
G_M000_IG43:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2568

; Assembly listing for method RyuPy.Generated.Module:m0_matmul(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x2ad
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 34 inlinees with PGO data; 53 single block inlinees; 64 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 304
       mov      qword ptr [rsp+0x388], r15
       mov      qword ptr [rsp+0x380], r14
       mov      qword ptr [rsp+0x378], r13
       mov      qword ptr [rsp+0x370], r12
       mov      qword ptr [rsp+0x368], rbx
       lea      rbp, [rsp+0x130]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xD0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       vmovdqa  xmmword ptr [rbp-0x50], xmm8
       mov      rbx, gword ptr [rbp+0x228]
       mov      r15, gword ptr [rbp+0x220]
       mov      rdx, gword ptr [rbp+0x1F8]
       mov      r13, qword ptr [rbp+0xF0]
       mov      r12, qword ptr [rbp+0xE8]
       mov      rax, qword ptr [rbp+0xE0]
       mov      rcx, gword ptr [rbp+0x90]
 
G_M000_IG02:
       jmp      G_M000_IG09
 
G_M000_IG03:
       test     r14, r14
       jl       G_M000_IG154
       mov      rax, r14
       mov      rdi, 0xD1FFAB1E
       mov      rsi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG153
       test     r14, r14
       jl       G_M000_IG154
       mov      rdi, r14
       mov      rax, gword ptr [rsi+8*rdi+0x10]
       jmp      G_M000_IG77
 
G_M000_IG04:
       mov      r14, qword ptr [r14+0x08]
       lea      rax, [rsi+r14]
       mov      qword ptr [rbp-0xB0], rax
       mov      rdi, rsi
       xor      rdi, rax
       mov      rcx, r14
       xor      rcx, rax
       and      rdi, rcx
       jge      G_M000_IG75
 
G_M000_IG05:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xC0], xmm0
       lea      rdi, [rbp-0xC0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xC0]
       mov      gword ptr [rbp-0x130], rax
       mov      ecx, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0xF0], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xD0], xmm0
       lea      rdi, [rbp-0xD0]
       mov      rsi, r14
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x130]
       mov      esi, dword ptr [rbp-0xF0]
       mov      rdx, gword ptr [rbp-0xD0]
       mov      rcx, qword ptr [rbp-0xC8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG06:
       mov      r14, rax
       jmp      SHORT G_M000_IG08
 
G_M000_IG07:
       mov      rdx, r14
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r14, rax
 
G_M000_IG08:
       mov      rcx, r14
       mov      rax, qword ptr [rbp+0xE0]
       mov      rdx, gword ptr [rbp+0x1F8]
 
G_M000_IG09:
       mov      gword ptr [rbp+0x1F0], rcx
       add      r13, rax
       jo       G_M000_IG154
 
G_M000_IG10:
       test     rax, rax
       setg     dil
       movzx    rdi, dil
       cmp      r13, r12
       setl     r14b
       movzx    r14, r14b
       and      r14d, edi
       cmp      r13, r12
       setg     sil
       movzx    rsi, sil
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, esi
       or       edi, r14d
       je       G_M000_IG34
 
G_M000_IG11:
       mov      r8, gword ptr [rbp+0x208]
       lea      r14, [r13+0x05]
       cmp      r14, 261
       jbe      G_M000_IG73
 
G_M000_IG12:
       mov      qword ptr [rbp+0xE0], rax
       mov      gword ptr [rbp+0x1F8], rdx
       mov      gword ptr [rbp+0x208], r8
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      qword ptr [r14+0x08], r13
       mov      r8, gword ptr [rbp+0x208]
 
G_M000_IG13:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG16
 
G_M000_IG14:
       mov      gword ptr [rbp+0x220], r15
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r15, rsi
       dec      edi
       mov      r11d, edi
       cmp      r11d, dword ptr [r15+0x08]
       jae      G_M000_IG153
       cmp      dword ptr [r15+4*r11+0x10], 23
       je       G_M000_IG74
 
G_M000_IG15:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG153
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 23
       mov      r15, gword ptr [rbp+0x220]
 
G_M000_IG16:
       mov      gword ptr [rbp+0x208], r8
       mov      rdi, r8
       mov      rsi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0xB0], rax
       mov      rdi, rbx
       mov      rsi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdi, rax
       mov      rsi, gword ptr [rbp+0x1F8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      r14, gword ptr [rbp+0xB0]
       test     r14, r14
       jne      G_M000_IG78
 
G_M000_IG17:
       test     r14, r14
       jne      G_M000_IG86
 
G_M000_IG18:
       mov      rdx, rax
       mov      rsi, r14
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      r14, rax
 
G_M000_IG19:
       mov      rax, gword ptr [rbp+0x1F0]
       test     rax, rax
       je       G_M000_IG07
 
G_M000_IG20:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG117
 
G_M000_IG21:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG121
 
G_M000_IG22:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG07
 
G_M000_IG23:
       mov      rsi, rax
       mov      gword ptr [rbp-0x118], rsi
       test     r14, r14
       je       G_M000_IG07
 
G_M000_IG24:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG07
 
G_M000_IG25:
       cmp      dword ptr [rsi+0x08], 0
       je       G_M000_IG31
 
G_M000_IG26:
       cmp      dword ptr [r14+0x08], 0
       je       SHORT G_M000_IG30
 
G_M000_IG27:
       mov      eax, dword ptr [rsi+0x08]
       mov      dword ptr [rbp-0xE4], eax
       mov      edi, eax
       add      edi, dword ptr [r14+0x08]
       js       SHORT G_M000_IG33
 
G_M000_IG28:
       movsxd   rsi, edi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x120], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rsi, gword ptr [rbp-0x118]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0xE4]
       mov      rax, gword ptr [rbp-0x120]
       lea      rdi, bword ptr [rax+2*rdi+0x0C]
       mov      edx, dword ptr [r14+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r14+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      r14, gword ptr [rbp-0x120]
 
G_M000_IG29:
       jmp      G_M000_IG08
 
G_M000_IG30:
       mov      r14, rsi
       jmp      SHORT G_M000_IG29
 
G_M000_IG31:
       cmp      dword ptr [r14+0x08], 0
       je       G_M000_IG144
 
G_M000_IG32:
       jmp      SHORT G_M000_IG29
 
G_M000_IG33:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG34:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG37
 
G_M000_IG35:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       dec      edi
       mov      edx, edi
       cmp      edx, dword ptr [rax+0x08]
       jae      G_M000_IG153
       cmp      dword ptr [rax+4*rdx+0x10], 24
       je       SHORT G_M000_IG37
 
G_M000_IG36:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG153
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 24
 
G_M000_IG37:
       mov      rdi, 0xD1FFAB1E
       mov      r13, gword ptr [rdi]
       mov      r12, gword ptr [r13+0x50]
       test     r12, r12
       je       SHORT G_M000_IG39
 
G_M000_IG38:
       cmp      dword ptr [r12+0x08], 1
       je       SHORT G_M000_IG40
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r12, rax
       lea      rdi, bword ptr [r13+0x50]
       mov      rsi, r12
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG40:
       mov      rdi, r12
       mov      rdx, gword ptr [rbp+0x1F0]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, r12
       mov      rsi, gword ptr [rbp+0x200]
       mov      edi, 8
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
       mov      r14, qword ptr [rbp+0x150]
       mov      r12, qword ptr [rbp+0x140]
       add      r14, r12
       jo       G_M000_IG154
 
G_M000_IG41:
       test     r12, r12
       setg     dil
       movzx    rdi, dil
       mov      rax, qword ptr [rbp+0x148]
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
       je       G_M000_IG51
 
G_M000_IG42:
       lea      r13, [r14+0x05]
       cmp      r13, 261
       jbe      G_M000_IG72
 
G_M000_IG43:
       mov      qword ptr [rbp+0x148], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rbp+0x150], r14
       mov      qword ptr [rax+0x08], r14
       mov      rdx, rax
 
G_M000_IG44:
       mov      gword ptr [rbp+0x1F8], rdx
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG47
 
G_M000_IG45:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       lea      esi, [rdi-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      G_M000_IG153
       cmp      dword ptr [rdx+4*r8+0x10], 21
       je       SHORT G_M000_IG47
 
G_M000_IG46:
       cmp      esi, dword ptr [rcx+0x08]
       jae      G_M000_IG153
       mov      edx, esi
       mov      dword ptr [rcx+4*rdx+0x10], 21
 
G_M000_IG47:
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      gword ptr [rbp+0x1F0], rdx
       test     edi, edi
       jle      SHORT G_M000_IG50
 
G_M000_IG48:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rsi, rdx
       dec      edi
       mov      r8d, edi
       cmp      r8d, dword ptr [rsi+0x08]
       jae      G_M000_IG153
       cmp      dword ptr [rsi+4*r8+0x10], 22
       je       SHORT G_M000_IG50
 
G_M000_IG49:
       cmp      edi, dword ptr [rdx+0x08]
       jae      G_M000_IG153
       mov      edi, edi
       mov      dword ptr [rdx+4*rdi+0x10], 22
 
G_M000_IG50:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x100], rax
       mov      gword ptr [rbp+0x220], r15
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rdi, qword ptr [rbp+0x100]
       mov      rcx, rdi
       mov      qword ptr [rbp+0xE0], 1
       mov      qword ptr [rbp+0x140], r12
       mov      r12, rax
       mov      r13, rcx
       mov      rax, qword ptr [rbp+0xE0]
       mov      rdx, gword ptr [rbp+0x1F8]
       mov      r15, gword ptr [rbp+0x220]
       jmp      G_M000_IG10
 
G_M000_IG51:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG54
 
G_M000_IG52:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       dec      edi
       mov      ecx, dword ptr [rax+0x08]
       cmp      edi, ecx
       jae      G_M000_IG153
       cmp      dword ptr [rax+4*rdi+0x10], 25
       je       SHORT G_M000_IG54
 
G_M000_IG53:
       mov      dword ptr [rsi+4*rdi+0x10], 25
 
G_M000_IG54:
       mov      r14, gword ptr [r13+0x48]
       test     r14, r14
       je       SHORT G_M000_IG56
 
G_M000_IG55:
       cmp      dword ptr [r14+0x08], 1
       je       SHORT G_M000_IG57
 
G_M000_IG56:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r14, rax
       lea      rdi, bword ptr [r13+0x48]
       mov      rsi, r14
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG57:
       mov      rdi, r14
       mov      rdx, gword ptr [rbp+0x200]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, r14
       mov      rsi, gword ptr [rbp+0x218]
       mov      edi, 7
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
       mov      r14, qword ptr [rbp+0x1B8]
       mov      r12, qword ptr [rbp+0x1A8]
       add      r14, r12
       jo       G_M000_IG154
       test     r12, r12
       setg     dil
       movzx    rdi, dil
       mov      rax, qword ptr [rbp+0x1B0]
       cmp      r14, rax
       setl     cl
       movzx    rcx, cl
       and      ecx, edi
       mov      qword ptr [rbp+0x1B0], rax
       cmp      r14, rax
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, ecx
       je       G_M000_IG145
 
G_M000_IG58:
       lea      rcx, [r14+0x05]
       cmp      rcx, 261
       jbe      G_M000_IG70
 
G_M000_IG59:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r14
 
G_M000_IG60:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG63
 
G_M000_IG61:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      r8d, dword ptr [rdx+0x08]
       cmp      edi, r8d
       jae      G_M000_IG153
       cmp      dword ptr [rdx+4*rdi+0x10], 18
       je       SHORT G_M000_IG63
 
G_M000_IG62:
       mov      dword ptr [rcx+4*rdi+0x10], 18
 
G_M000_IG63:
       mov      rdi, gword ptr [rbp+0x230]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x208], rax
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG66
 
G_M000_IG64:
       mov      rax, gword ptr [rbp+0x208]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      esi, dword ptr [rdx+0x08]
       cmp      edi, esi
       jae      G_M000_IG153
       cmp      dword ptr [rdx+4*rdi+0x10], 19
       je       G_M000_IG71
 
G_M000_IG65:
       mov      dword ptr [rcx+4*rdi+0x10], 19
       mov      gword ptr [rbp+0x208], rax
 
G_M000_IG66:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xF8], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x100], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, rax
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       mov      rdx, gword ptr [rbp-0xF8]
       lea      rdi, bword ptr [rdx+0x08]
       mov      rsi, gword ptr [rbp-0x100]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdx, gword ptr [rbp-0xF8]
       mov      gword ptr [rbp+0x200], rdx
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG69
 
G_M000_IG67:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      esi, dword ptr [rdx+0x08]
       cmp      edi, esi
       jae      G_M000_IG153
       cmp      dword ptr [rdx+4*rdi+0x10], 20
       je       SHORT G_M000_IG69
 
G_M000_IG68:
       mov      dword ptr [rcx+4*rdi+0x10], 20
 
G_M000_IG69:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x160], rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rdi, qword ptr [rbp+0x160]
       mov      rcx, rdi
       mov      qword ptr [rbp+0x148], rax
       mov      qword ptr [rbp+0x140], 1
       mov      qword ptr [rbp+0x1B8], r14
       mov      qword ptr [rbp+0x1A8], r12
       mov      r14, rcx
       mov      r12, qword ptr [rbp+0x140]
       jmp      G_M000_IG41
 
G_M000_IG70:
       test     rcx, rcx
       jl       G_M000_IG154
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG153
       mov      rsi, gword ptr [rcx+8*rsi+0x10]
       jmp      G_M000_IG60
 
G_M000_IG71:
       mov      gword ptr [rbp+0x208], rax
       jmp      G_M000_IG66
 
G_M000_IG72:
       test     r13, r13
       jl       G_M000_IG154
       mov      rdx, r13
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rdx, 262
       jae      G_M000_IG153
       mov      rdx, gword ptr [rcx+8*rdx+0x10]
       mov      qword ptr [rbp+0x150], r14
       mov      qword ptr [rbp+0x148], rax
       jmp      G_M000_IG44
 
G_M000_IG73:
       test     r14, r14
       jl       G_M000_IG154
       mov      rdi, r14
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG153
       mov      r14, gword ptr [rsi+8*rdi+0x10]
       mov      qword ptr [rbp+0xE0], rax
       mov      gword ptr [rbp+0x1F8], rdx
       jmp      G_M000_IG13
 
G_M000_IG74:
       mov      r15, gword ptr [rbp+0x220]
       jmp      G_M000_IG16
 
G_M000_IG75:
       lea      r14, [rax+0x05]
       cmp      r14, 261
       jbe      G_M000_IG03
 
G_M000_IG76:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, qword ptr [rbp-0xB0]
       mov      qword ptr [rax+0x08], r14
 
G_M000_IG77:
       jmp      G_M000_IG06
 
G_M000_IG78:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG17
 
G_M000_IG79:
       test     rax, rax
       je       G_M000_IG86
 
G_M000_IG80:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG86
 
G_M000_IG81:
       mov      rcx, gword ptr [rbp+0x1F0]
       test     rcx, rcx
       je       SHORT G_M000_IG85
 
G_M000_IG82:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       SHORT G_M000_IG84
 
G_M000_IG83:
       mov      gword ptr [rbp+0x1F0], rcx
       jmp      SHORT G_M000_IG86
 
G_M000_IG84:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp+0x38], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rcx, rax
       vmovsd   xmm0, qword ptr [rbp+0x38]
       vmovsd   qword ptr [rcx+0x08], xmm0
       mov      r14, rcx
       mov      rcx, r14
       mov      rax, qword ptr [rbp+0xE0]
       mov      rdx, gword ptr [rbp+0x1F8]
       jmp      G_M000_IG09
 
G_M000_IG85:
       mov      gword ptr [rbp+0x1F0], rcx
 
G_M000_IG86:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [r14], rdx
       jne      G_M000_IG111
 
G_M000_IG87:
       mov      rsi, qword ptr [r14+0x08]
       test     rax, rax
       je       G_M000_IG18
 
G_M000_IG88:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rax], rdx
       jne      G_M000_IG109
 
G_M000_IG89:
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
       je       G_M000_IG105
 
G_M000_IG90:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x50], xmm0
       lea      rdi, [rbp-0x50]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x128], rax
       mov      ecx, dword ptr [rbp-0x48]
       mov      dword ptr [rbp-0xE8], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       mov      rsi, r14
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r14, gword ptr [rbp-0x70]
       mov      r9d, dword ptr [rbp-0x68]
       mov      rax, gword ptr [rbp-0x128]
       test     rax, rax
       jne      G_M000_IG98
 
G_M000_IG91:
       test     r14, r14
       jne      G_M000_IG126
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       movsxd   rsi, dword ptr [rbp-0xE8]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x80]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r14, gword ptr [rbp-0x80]
       mov      eax, dword ptr [rbp-0x78]
 
G_M000_IG92:
       test     r14, r14
       jne      G_M000_IG127
 
G_M000_IG93:
       test     r14, r14
       jne      G_M000_IG136
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      SHORT G_M000_IG103
 
G_M000_IG94:
       mov      edi, -1
 
G_M000_IG95:
       test     edi, edi
       mov      dword ptr [rbp-0xEC], eax
       jg       SHORT G_M000_IG104
 
G_M000_IG96:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x110], rax
       mov      rdi, r14
       mov      esi, dword ptr [rbp-0xEC]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r14, gword ptr [rbp-0x110]
       mov      qword ptr [r14+0x08], rax
 
G_M000_IG97:
       jmp      G_M000_IG19
 
G_M000_IG98:
       lea      rdi, bword ptr [rax+0x10]
       mov      esi, dword ptr [rax+0x08]
 
G_M000_IG99:
       test     r14, r14
       je       SHORT G_M000_IG102
 
G_M000_IG100:
       lea      rcx, bword ptr [r14+0x10]
       mov      r8d, dword ptr [r14+0x08]
 
G_M000_IG101:
       mov      edx, dword ptr [rbp-0xE8]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      r14, rax
       mov      eax, edx
       jmp      G_M000_IG92
 
G_M000_IG102:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG101
 
G_M000_IG103:
       xor      edi, edi
       jmp      SHORT G_M000_IG95
 
G_M000_IG104:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x108], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r14
       call     CORINFO_HELP_ASSIGN_REF
       mov      r14, gword ptr [rbp-0x108]
       mov      edi, dword ptr [rbp-0xEC]
       mov      dword ptr [r14+0x10], edi
       jmp      SHORT G_M000_IG97
 
G_M000_IG105:
       lea      r14, [rax+0x05]
       cmp      r14, 261
       jbe      SHORT G_M000_IG108
 
G_M000_IG106:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, qword ptr [rbp-0x58]
       mov      qword ptr [rax+0x08], r14
       mov      r14, rax
 
G_M000_IG107:
       jmp      G_M000_IG97
 
G_M000_IG108:
       test     r14, r14
       jl       G_M000_IG154
       mov      rdi, r14
       mov      rax, 0xD1FFAB1E
       mov      rsi, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG153
       mov      r14, gword ptr [rsi+8*rdi+0x10]
       jmp      SHORT G_M000_IG107
 
G_M000_IG109:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG18
 
G_M000_IG110:
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
       jmp      G_M000_IG19
 
G_M000_IG111:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG18
 
G_M000_IG112:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     rax, rax
       je       G_M000_IG18
 
G_M000_IG113:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG115
 
G_M000_IG114:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG19
 
G_M000_IG115:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG18
 
G_M000_IG116:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG19
 
G_M000_IG117:
       mov      rsi, qword ptr [rax+0x08]
       test     r14, r14
       je       G_M000_IG07
 
G_M000_IG118:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       je       G_M000_IG04
 
G_M000_IG119:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG07
 
G_M000_IG120:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xD8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xD8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xA8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG08
 
G_M000_IG121:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm0
       test     r14, r14
       je       G_M000_IG07
 
G_M000_IG122:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      SHORT G_M000_IG124
 
G_M000_IG123:
       vmovsd   xmm1, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x98]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG08
 
G_M000_IG124:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG07
 
G_M000_IG125:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0xE0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x98]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xE0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG08
 
G_M000_IG126:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG99
 
G_M000_IG127:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG128
       mov      edi, dword ptr [r14+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG129
 
G_M000_IG128:
       mov      ecx, eax
       jmp      SHORT G_M000_IG135
 
G_M000_IG129:
       cmp      edi, 2
       je       SHORT G_M000_IG130
       cmp      dword ptr [r14+0x08], 0
       jbe      G_M000_IG153
       mov      edi, dword ptr [r14+0x10]
       jmp      SHORT G_M000_IG131
 
G_M000_IG130:
       mov      edi, dword ptr [r14+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r14+0x10]
       or       rdi, rcx
 
G_M000_IG131:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG132
       mov      ecx, -1
       jmp      SHORT G_M000_IG134
 
G_M000_IG132:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG133
       mov      ecx, 1
       jmp      SHORT G_M000_IG134
 
G_M000_IG133:
       xor      ecx, ecx
 
G_M000_IG134:
       imul     ecx, eax
 
G_M000_IG135:
       test     ecx, ecx
       jge      G_M000_IG93
       mov      dword ptr [rbp-0xEC], eax
       jmp      G_M000_IG104
 
G_M000_IG136:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG137
       mov      edi, dword ptr [r14+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG138
 
G_M000_IG137:
       mov      dword ptr [rbp-0xEC], eax
       mov      edi, eax
       mov      eax, dword ptr [rbp-0xEC]
       jmp      G_M000_IG95
 
G_M000_IG138:
       cmp      edi, 2
       je       SHORT G_M000_IG139
       cmp      dword ptr [r14+0x08], 0
       jbe      G_M000_IG153
       mov      edi, dword ptr [r14+0x10]
       jmp      SHORT G_M000_IG140
 
G_M000_IG139:
       mov      edi, dword ptr [r14+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r14+0x10]
       or       rdi, rcx
 
G_M000_IG140:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG141
       mov      ecx, -1
       jmp      SHORT G_M000_IG143
 
G_M000_IG141:
       test     rdi, rdi
       jge      SHORT G_M000_IG142
       mov      ecx, 1
       jmp      SHORT G_M000_IG143
 
G_M000_IG142:
       xor      ecx, ecx
 
G_M000_IG143:
       mov      edi, eax
       imul     edi, ecx
       jmp      G_M000_IG95
 
G_M000_IG144:
       mov      r14, 0xD1FFAB1E
       jmp      G_M000_IG29
 
G_M000_IG145:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG148
 
G_M000_IG146:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      ecx, dword ptr [(reloc)]
       dec      ecx
       cmp      ecx, dword ptr [rax+0x08]
       jae      SHORT G_M000_IG153
       cmp      dword ptr [rax+4*rcx+0x10], 26
       je       SHORT G_M000_IG148
 
G_M000_IG147:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      ecx, dword ptr [(reloc)]
       dec      ecx
       cmp      ecx, dword ptr [rax+0x08]
       jae      SHORT G_M000_IG153
       mov      dword ptr [rax+4*rcx+0x10], 26
 
G_M000_IG148:
       mov      rbx, qword ptr [rbp+0x1E0]
       test     rbx, rbx
       jle      SHORT G_M000_IG151
 
G_M000_IG149:
       movsxd   rax, dword ptr [(reloc)]
       cmp      rbx, rax
       jg       SHORT G_M000_IG151
 
G_M000_IG150:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG151:
       mov      rax, gword ptr [rbp+0x218]
 
G_M000_IG152:
       add      rsp, 872
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG153:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG154:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 4209

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

