; Assembly listing for method RyuPy.Generated.Module:m0_matmul(System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 608
       lea      rbp, [rsp+0x260]
       xor      eax, eax
       mov      qword ptr [rbp-0x258], rax
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -528
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x20], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
 
G_M000_IG02:
       mov      dword ptr [rbp-0x260], 0x3E8
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
       mov      eax, dword ptr [rbp-0x260]
       dec      eax
       mov      dword ptr [rbp-0x260], eax
       cmp      dword ptr [rbp-0x260], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x260]
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
       call     [RyuPy.Runtime.PyNumericListOps:Create():System.Object]
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
       jo       G_M000_IG65
       mov      qword ptr [rbp-0xA8], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      edi, 26
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x1E8], rax
       jmp      G_M000_IG23
 
G_M000_IG09:
       mov      eax, dword ptr [rbp-0x260]
       dec      eax
       mov      dword ptr [rbp-0x260], eax
       cmp      dword ptr [rbp-0x260], 0
       jg       SHORT G_M000_IG11
 
G_M000_IG10:
       lea      rdi, [rbp-0x260]
       mov      esi, 295
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
       xor      eax, eax
       mov      qword ptr [rbp-0x1F0], rax
       mov      rsi, gword ptr [rbp-0x70]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x1F4], eax
       cmp      dword ptr [rbp-0x1F4], 0
       jne      G_M000_IG59
       jmp      G_M000_IG17
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x110]
       add      rax, qword ptr [rbp-0x120]
       jo       G_M000_IG65
       mov      qword ptr [rbp-0x110], rax
       jmp      G_M000_IG09
 
G_M000_IG14:
       mov      eax, dword ptr [rbp-0x260]
       dec      eax
       mov      dword ptr [rbp-0x260], eax
       cmp      dword ptr [rbp-0x260], 0
       jg       SHORT G_M000_IG16
 
G_M000_IG15:
       lea      rdi, [rbp-0x260]
       mov      esi, 497
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
       mov      eax, dword ptr [rbp-0x260]
       dec      eax
       mov      dword ptr [rbp-0x260], eax
       cmp      dword ptr [rbp-0x260], 0
       jg       SHORT G_M000_IG19
 
G_M000_IG18:
       lea      rdi, [rbp-0x260]
       mov      esi, 524
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
       jmp      SHORT G_M000_IG22
 
G_M000_IG20:
       mov      rdi, qword ptr [rbp-0x170]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0x1A8], rax
       mov      rax, gword ptr [rbp-0x1A8]
       mov      gword ptr [rbp-0x78], rax
       mov      edi, 23
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       cmp      dword ptr [rbp-0x1F4], 0
       jne      G_M000_IG56
       jmp      G_M000_IG46
 
G_M000_IG21:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x170]
       add      rax, qword ptr [rbp-0x180]
       jo       G_M000_IG65
       mov      qword ptr [rbp-0x170], rax
       jmp      G_M000_IG17
 
G_M000_IG22:
       cmp      gword ptr [rbp-0x70], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x1F8], eax
       mov      eax, dword ptr [rbp-0x1F4]
       and      eax, dword ptr [rbp-0x1F8]
       movzx    rax, al
       mov      dword ptr [rbp-0x1FC], eax
       cmp      dword ptr [rbp-0x1FC], 0
       jne      G_M000_IG64
       jmp      G_M000_IG63
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x80]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x1E8]
 
G_M000_IG24:
       add      rsp, 608
       pop      rbp
       ret      
 
G_M000_IG25:
       mov      eax, dword ptr [rbp-0x260]
       dec      eax
       mov      dword ptr [rbp-0x260], eax
       cmp      dword ptr [rbp-0x260], 0
       jg       SHORT G_M000_IG27
 
G_M000_IG26:
       lea      rdi, [rbp-0x260]
       mov      esi, 675
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG27:
       jmp      G_M000_IG21
 
G_M000_IG28:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x70]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x1F0], xmm0
       jmp      SHORT G_M000_IG25
 
G_M000_IG29:
       mov      eax, dword ptr [rbp-0x260]
       dec      eax
       mov      dword ptr [rbp-0x260], eax
       cmp      dword ptr [rbp-0x260], 0
       jg       SHORT G_M000_IG31
 
G_M000_IG30:
       lea      rdi, [rbp-0x260]
       mov      esi, 694
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG31:
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
       mov      rsi, gword ptr [rbp-0x70]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x1F4], eax
       cmp      dword ptr [rbp-0x1F4], 0
       jne      G_M000_IG28
       jmp      G_M000_IG25
 
G_M000_IG32:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x258], rax
       mov      rax, gword ptr [rbp-0x258]
       vmovsd   xmm0, qword ptr [rbp-0x1F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x258]
       mov      gword ptr [rbp-0x70], rax
       jmp      G_M000_IG29
 
G_M000_IG33:
       mov      eax, dword ptr [rbp-0x260]
       dec      eax
       mov      dword ptr [rbp-0x260], eax
       cmp      dword ptr [rbp-0x260], 0
       jg       SHORT G_M000_IG35
 
G_M000_IG34:
       lea      rdi, [rbp-0x260]
       mov      esi, 770
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG35:
       cmp      gword ptr [rbp-0x70], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x1F8], eax
       mov      eax, dword ptr [rbp-0x1F4]
       and      eax, dword ptr [rbp-0x1F8]
       movzx    rax, al
       mov      dword ptr [rbp-0x1FC], eax
       cmp      dword ptr [rbp-0x1FC], 0
       jne      G_M000_IG32
       jmp      G_M000_IG29
 
G_M000_IG36:
       mov      eax, dword ptr [rbp-0x260]
       dec      eax
       mov      dword ptr [rbp-0x260], eax
       cmp      dword ptr [rbp-0x260], 0
       jg       SHORT G_M000_IG38
 
G_M000_IG37:
       lea      rdi, [rbp-0x260]
       mov      esi, 796
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG38:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       vmovsd   xmm0, qword ptr [rbp-0x210]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x220]
       vmovsd   qword ptr [rbp-0x228], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x1F0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x228]
       vmovsd   qword ptr [rbp-0x230], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x230]
       vmovsd   qword ptr [rbp-0x1F0], xmm0
       xor      rax, rax
       mov      gword ptr [rbp-0x70], rax
       jmp      G_M000_IG25
 
G_M000_IG39:
       mov      rsi, gword ptr [rbp-0x208]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x210], xmm0
       mov      rsi, gword ptr [rbp-0x218]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x220], xmm0
       jmp      G_M000_IG36
 
G_M000_IG40:
       mov      eax, dword ptr [rbp-0x260]
       dec      eax
       mov      dword ptr [rbp-0x260], eax
       cmp      dword ptr [rbp-0x260], 0
       jg       SHORT G_M000_IG42
 
G_M000_IG41:
       lea      rdi, [rbp-0x260]
       mov      esi, 845
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG42:
       mov      rsi, gword ptr [rbp-0x218]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x234], eax
       cmp      dword ptr [rbp-0x234], 0
       jne      G_M000_IG39
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG33
 
G_M000_IG43:
       mov      eax, dword ptr [rbp-0x260]
       dec      eax
       mov      dword ptr [rbp-0x260], eax
       cmp      dword ptr [rbp-0x260], 0
       jg       SHORT G_M000_IG45
 
G_M000_IG44:
       lea      rdi, [rbp-0x260]
       mov      esi, 872
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG45:
       mov      rsi, gword ptr [rbp-0x208]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x238], eax
       cmp      dword ptr [rbp-0x238], 0
       jne      G_M000_IG40
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG33
 
G_M000_IG46:
       mov      eax, dword ptr [rbp-0x260]
       dec      eax
       mov      dword ptr [rbp-0x260], eax
       cmp      dword ptr [rbp-0x260], 0
       jg       SHORT G_M000_IG48
 
G_M000_IG47:
       lea      rdi, [rbp-0x260]
       mov      esi, 899
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG48:
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
       mov      gword ptr [rbp-0x208], rax
       mov      rax, gword ptr [rbp-0x1C0]
       mov      gword ptr [rbp-0x218], rax
       cmp      dword ptr [rbp-0x1F4], 0
       jne      G_M000_IG43
       jmp      G_M000_IG33
 
G_M000_IG49:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0x58]
       mov      rsi, gword ptr [rbp-0x78]
       call     [RyuPy.Runtime.PyNumericListOps:ReadCheckedF64(System.Object,System.Object):double]
       vmovsd   qword ptr [rbp-0x210], xmm0
       mov      rdi, gword ptr [rbp-0x248]
       mov      rsi, gword ptr [rbp-0x68]
       call     [RyuPy.Runtime.PyNumericListOps:ReadCheckedF64(System.Object,System.Object):double]
       vmovsd   qword ptr [rbp-0x220], xmm0
       jmp      G_M000_IG36
 
G_M000_IG50:
       mov      eax, dword ptr [rbp-0x260]
       dec      eax
       mov      dword ptr [rbp-0x260], eax
       cmp      dword ptr [rbp-0x260], 0
       jg       SHORT G_M000_IG52
 
G_M000_IG51:
       lea      rdi, [rbp-0x260]
       mov      esi, 976
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG52:
       mov      rdi, gword ptr [rbp-0x248]
       mov      rsi, gword ptr [rbp-0x68]
       call     [RyuPy.Runtime.PyNumericListOps:CanReadF64(System.Object,System.Object):bool]
       mov      dword ptr [rbp-0x24C], eax
       cmp      dword ptr [rbp-0x24C], 0
       jne      G_M000_IG49
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG46
 
G_M000_IG53:
       mov      eax, dword ptr [rbp-0x260]
       dec      eax
       mov      dword ptr [rbp-0x260], eax
       cmp      dword ptr [rbp-0x260], 0
       jg       SHORT G_M000_IG55
 
G_M000_IG54:
       lea      rdi, [rbp-0x260]
       mov      esi, 999
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG55:
       mov      rdi, gword ptr [rbp-0x38]
       mov      rsi, gword ptr [rbp-0x78]
       call     [RyuPy.Runtime.PyNumericListOps:PeekList(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x248], rax
       cmp      gword ptr [rbp-0x248], 0
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x240], eax
       cmp      dword ptr [rbp-0x240], 0
       jne      G_M000_IG50
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG46
 
G_M000_IG56:
       mov      eax, dword ptr [rbp-0x260]
       dec      eax
       mov      dword ptr [rbp-0x260], eax
       cmp      dword ptr [rbp-0x260], 0
       jg       SHORT G_M000_IG58
 
G_M000_IG57:
       lea      rdi, [rbp-0x260]
       mov      esi, 0x407
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG58:
       mov      rdi, gword ptr [rbp-0x58]
       mov      rsi, gword ptr [rbp-0x78]
       call     [RyuPy.Runtime.PyNumericListOps:CanReadF64(System.Object,System.Object):bool]
       mov      dword ptr [rbp-0x23C], eax
       cmp      dword ptr [rbp-0x23C], 0
       jne      G_M000_IG53
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG46
 
G_M000_IG59:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x70]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x1F0], xmm0
       jmp      G_M000_IG17
 
G_M000_IG60:
       mov      eax, dword ptr [rbp-0x260]
       dec      eax
       mov      dword ptr [rbp-0x260], eax
       cmp      dword ptr [rbp-0x260], 0
       jg       SHORT G_M000_IG62
 
G_M000_IG61:
       lea      rdi, [rbp-0x260]
       mov      esi, 0x42B
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG62:
       jmp      G_M000_IG13
 
G_M000_IG63:
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, gword ptr [rbp-0x70]
       mov      rsi, gword ptr [rbp-0x60]
       mov      edi, 10
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       mov      gword ptr [rbp-0x1D8], rax
       jmp      SHORT G_M000_IG60
 
G_M000_IG64:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x60]
       vmovsd   xmm0, qword ptr [rbp-0x1F0]
       call     [RyuPy.Runtime.PyNumericListOps:AppendF64(System.Object,double):System.Object]
       mov      gword ptr [rbp-0x1D8], rax
       jmp      G_M000_IG60
 
G_M000_IG65:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 3344

; Assembly listing for method RyuPy.Runtime.PyNumericListOps:CanReadF64(System.Object,System.Object):bool (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 48
       lea      rbp, [rsp+0x30]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x30], ymm8
       mov      gword ptr [rbp-0x08], rdi
       mov      gword ptr [rbp-0x10], rsi
 
G_M000_IG02:
       mov      rsi, gword ptr [rbp-0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       mov      gword ptr [rbp-0x18], rax
       cmp      gword ptr [rbp-0x18], 0
       je       SHORT G_M000_IG04
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
       lea      rdx, [rbp-0x28]
       mov      rdi, gword ptr [rbp-0x18]
       mov      rsi, qword ptr [rbp-0x20]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyList:TryNumericPosition(long,byref):bool:this]
       test     eax, eax
       je       SHORT G_M000_IG04
       lea      rdx, [rbp-0x30]
       mov      rdi, gword ptr [rbp-0x18]
       mov      esi, dword ptr [rbp-0x28]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyList:TryReadF64(int,byref):bool:this]
       nop      
 
G_M000_IG03:
       add      rsp, 48
       pop      rbp
       ret      
 
G_M000_IG04:
       xor      eax, eax
 
G_M000_IG05:
       add      rsp, 48
       pop      rbp
       ret      
 
; Total bytes of code 169

; Assembly listing for method RyuPy.Runtime.PyNumericListOps:ReadCheckedF64(System.Object,System.Object):double (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 32
       lea      rbp, [rsp+0x20]
       xor      eax, eax
       mov      qword ptr [rbp-0x18], rax
       mov      gword ptr [rbp-0x08], rdi
       mov      gword ptr [rbp-0x10], rsi
 
G_M000_IG02:
       mov      rsi, gword ptr [rbp-0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_CHKCASTCLASS]
       mov      gword ptr [rbp-0x18], rax
       mov      rsi, gword ptr [rbp-0x10]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x20], rax
       mov      rdi, gword ptr [rbp-0x18]
       mov      rsi, qword ptr [rbp-0x20]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyList:ReadCheckedNumeric(long):double:this]
       nop      
 
G_M000_IG03:
       add      rsp, 32
       pop      rbp
       ret      
 
; Total bytes of code 98

; Assembly listing for method RyuPy.Generated.Module:m0_matmul(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x3e7
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 1
; 40 inlinees with PGO data; 65 single block inlinees; 83 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 416
       mov      qword ptr [rsp+0x408], r15
       mov      qword ptr [rsp+0x400], r14
       mov      qword ptr [rsp+0x3F8], r13
       mov      qword ptr [rsp+0x3F0], r12
       mov      qword ptr [rsp+0x3E8], rbx
       lea      rbp, [rsp+0x1A0]
       xor      eax, eax
       mov      qword ptr [rbp-0xD8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xD0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       mov      qword ptr [rbp-0x50], rax
       mov      rbx, gword ptr [rbp+0x238]
       mov      r15, gword ptr [rbp+0x230]
       mov      rdx, gword ptr [rbp+0x218]
       mov      r10, gword ptr [rbp+0x208]
       mov      r13, gword ptr [rbp+0x200]
       mov      rcx, gword ptr [rbp+0x1F8]
       mov      r12, qword ptr [rbp+0x100]
 
G_M000_IG02:
       mov      r14, gword ptr [rbp+0x240]
       jmp      G_M000_IG167
 
G_M000_IG03:
       test     rax, rax
       je       SHORT G_M000_IG08
 
G_M000_IG04:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG06
 
G_M000_IG05:
       jmp      SHORT G_M000_IG09
 
G_M000_IG06:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   xmm2, qword ptr [rax+0x08]
 
G_M000_IG07:
       vmulsd   xmm0, xmm1, xmm2
       vaddsd   xmm0, xmm0, qword ptr [rbp+0x80]
       vmovsd   qword ptr [rbp+0x80], xmm0
       xor      r13, r13
       mov      dword ptr [rbp+0x7C], ecx
       jmp      G_M000_IG189
 
G_M000_IG08:
 
G_M000_IG09:
       test     r13, r13
       sete     dil
       movzx    rdi, dil
       test     ecx, edi
       jne      G_M000_IG64
 
G_M000_IG10:
       mov      rdx, gword ptr [rbp+0xC0]
       test     rdx, rdx
       je       G_M000_IG181
 
G_M000_IG11:
       mov      rsi, qword ptr [rdx]
       mov      rdi, 0xD1FFAB1E
       cmp      rsi, rdi
       je       G_M000_IG65
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       cmp      rsi, rdi
       je       G_M000_IG90
 
G_M000_IG13:
       jmp      G_M000_IG181
 
G_M000_IG14:
       mov      esi, dword ptr [rsi+0x10]
       mov      dword ptr [rbp-0x12C], esi
       mov      esi, dword ptr [rbp-0x12C]
       jmp      G_M000_IG173
 
G_M000_IG15:
       mov      dword ptr [rbp-0x12C], esi
       movsxd   rsi, esi
       add      rsi, qword ptr [rbp-0x118]
       mov      qword ptr [rbp-0x128], rsi
       jmp      G_M000_IG175
 
G_M000_IG16:
       mov      rsi, qword ptr [rbp-0x128]
       mov      dword ptr [rbp-0x11C], esi
       mov      rsi, gword ptr [rbx+0x08]
       test     rsi, rsi
       je       SHORT G_M000_IG19
 
G_M000_IG17:
       mov      edi, dword ptr [rbp-0x11C]
       cmp      edi, dword ptr [rsi+0x10]
       jae      G_M000_IG264
       mov      rsi, gword ptr [rsi+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG265
       mov      rdi, gword ptr [rsi+8*rdi+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG19
 
G_M000_IG18:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       G_M000_IG250
 
G_M000_IG19:
       xor      rsi, rsi
 
G_M000_IG20:
       jmp      G_M000_IG177
 
G_M000_IG21:
       mov      gword ptr [rbp+0x208], r10
       jmp      G_M000_IG199
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r10], rdi
       jne      G_M000_IG62
 
G_M000_IG23:
       mov      gword ptr [rbp+0x208], r10
       mov      rdi, qword ptr [r10+0x08]
       mov      qword ptr [rbp-0xF8], rdi
       mov      rdi, gword ptr [rsi+0x08]
       test     rdi, rdi
       jne      G_M000_IG44
 
G_M000_IG24:
       mov      rdi, gword ptr [rsi+0x10]
       mov      edi, dword ptr [rdi+0x10]
 
G_M000_IG25:
       cmp      qword ptr [rbp-0xF8], 0
       jl       G_M000_IG45
 
G_M000_IG26:
       mov      dword ptr [rbp-0x10C], edi
       mov      rdi, qword ptr [rbp-0xF8]
       mov      qword ptr [rbp-0x108], rdi
 
G_M000_IG27:
       movsxd   rdi, dword ptr [rbp-0x10C]
       cmp      qword ptr [rbp-0x108], rdi
       jae      G_M000_IG199
 
G_M000_IG28:
       mov      rdi, qword ptr [rbp-0x108]
       mov      dword ptr [rbp-0xFC], edi
       mov      rdi, gword ptr [rsi+0x10]
       test     rdi, rdi
       jne      G_M000_IG46
 
G_M000_IG29:
       mov      gword ptr [rbp+0x240], r14
       mov      gword ptr [rbp-0x178], rsi
       mov      rdi, gword ptr [rsi+0x08]
       mov      r14d, dword ptr [rbp-0xFC]
       cmp      r14d, dword ptr [rdi+0x10]
       jae      G_M000_IG264
       mov      rdi, gword ptr [rdi+0x08]
       cmp      r14d, dword ptr [rdi+0x08]
       jae      G_M000_IG265
       mov      rdi, gword ptr [rdi+8*r14+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG31
 
G_M000_IG30:
       mov      r14, 0xD1FFAB1E
       cmp      qword ptr [rdi], r14
       je       G_M000_IG47
 
G_M000_IG31:
       xor      edi, edi
 
G_M000_IG32:
       test     edi, edi
       je       G_M000_IG249
 
G_M000_IG33:
       mov      gword ptr [rbp+0x218], rdx
       mov      rdi, rdx
       test     rdi, rdi
       je       SHORT G_M000_IG35
 
G_M000_IG34:
       mov      r14, 0xD1FFAB1E
       cmp      qword ptr [rdi], r14
       jne      G_M000_IG248
 
G_M000_IG35:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       SHORT G_M000_IG37
 
G_M000_IG36:
       mov      gword ptr [rbp+0x1F8], rcx
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rcx, gword ptr [rbp+0x1F8]
 
G_M000_IG37:
       mov      rdi, qword ptr [rcx+0x08]
       mov      rdx, gword ptr [rbp+0x218]
       cmp      byte  ptr [rdx], dl
       test     rdi, rdi
       jl       G_M000_IG48
 
G_M000_IG38:
       mov      rcx, gword ptr [rdx+0x10]
       test     rcx, rcx
       je       G_M000_IG52
 
G_M000_IG39:
       cmp      edi, dword ptr [rcx+0x10]
       jae      G_M000_IG264
       mov      rcx, gword ptr [rcx+0x08]
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG265
       mov      edi, edi
       shl      rdi, 4
       lea      rdi, bword ptr [rcx+rdi+0x10]
       mov      rcx, gword ptr [rdi]
       mov      rdi, qword ptr [rdi+0x08]
       test     rcx, rcx
       je       G_M000_IG260
       vmovq    xmm1, rdi
 
G_M000_IG40:
       vmovsd   qword ptr [rbp+0x60], xmm1
       mov      rsi, gword ptr [rbp-0x178]
       mov      r10, gword ptr [rbp+0x208]
       mov      rdi, qword ptr [r10+0x08]
       test     rdi, rdi
       jl       G_M000_IG55
 
G_M000_IG41:
       mov      rcx, gword ptr [rsi+0x10]
       test     rcx, rcx
       je       G_M000_IG59
 
G_M000_IG42:
       cmp      edi, dword ptr [rcx+0x10]
       jae      G_M000_IG264
       mov      rsi, gword ptr [rcx+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG265
       mov      edi, edi
       shl      rdi, 4
       lea      rsi, bword ptr [rsi+rdi+0x10]
       mov      rdi, gword ptr [rsi]
       mov      rsi, qword ptr [rsi+0x08]
       test     rdi, rdi
       je       G_M000_IG261
       vmovq    xmm2, rsi
 
G_M000_IG43:
       mov      gword ptr [rbp+0x218], rdx
       mov      ecx, dword ptr [rbp+0x7C]
       mov      r14, gword ptr [rbp+0x240]
       vmovsd   xmm1, qword ptr [rbp+0x60]
       jmp      G_M000_IG07
 
G_M000_IG44:
       mov      edi, dword ptr [rdi+0x10]
       mov      dword ptr [rbp-0x10C], edi
       mov      edi, dword ptr [rbp-0x10C]
       jmp      G_M000_IG25
 
G_M000_IG45:
       mov      dword ptr [rbp-0x10C], edi
       movsxd   rdi, edi
       add      rdi, qword ptr [rbp-0xF8]
       mov      qword ptr [rbp-0x108], rdi
       jmp      G_M000_IG27
 
G_M000_IG46:
       mov      gword ptr [rbp+0x240], r14
       mov      r14d, dword ptr [rbp-0xFC]
       cmp      r14d, dword ptr [rdi+0x10]
       jae      G_M000_IG264
       mov      rdi, gword ptr [rdi+0x08]
       cmp      r14d, dword ptr [rdi+0x08]
       jae      G_M000_IG265
       shl      r14, 4
       mov      rdi, gword ptr [rdi+r14+0x10]
       test     rdi, rdi
       mov      gword ptr [rbp-0x178], rsi
       je       G_M000_IG262
 
G_M000_IG47:
       mov      edi, 1
       jmp      G_M000_IG32
 
G_M000_IG48:
       mov      rcx, gword ptr [rdx+0x08]
       test     rcx, rcx
       jne      SHORT G_M000_IG51
 
G_M000_IG49:
       mov      rcx, gword ptr [rdx+0x10]
       mov      ecx, dword ptr [rcx+0x10]
 
G_M000_IG50:
       movsxd   rcx, ecx
       add      rdi, rcx
       jmp      G_M000_IG38
 
G_M000_IG51:
       mov      ecx, dword ptr [rcx+0x10]
       jmp      SHORT G_M000_IG50
 
G_M000_IG52:
       mov      gword ptr [rbp+0x218], rdx
       mov      rcx, gword ptr [rdx+0x08]
       cmp      edi, dword ptr [rcx+0x10]
       jae      G_M000_IG264
       mov      rdx, gword ptr [rbp+0x218]
       mov      rcx, gword ptr [rcx+0x08]
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG265
       mov      edi, edi
       mov      r13, gword ptr [rcx+8*rdi+0x10]
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       je       SHORT G_M000_IG54
 
G_M000_IG53:
       mov      gword ptr [rbp+0x218], rdx
       mov      rsi, r13
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rdx, gword ptr [rbp+0x218]
 
G_M000_IG54:
       vmovsd   xmm1, qword ptr [r13+0x08]
       jmp      G_M000_IG40
 
G_M000_IG55:
       mov      rcx, gword ptr [rsi+0x08]
       test     rcx, rcx
       jne      SHORT G_M000_IG58
 
G_M000_IG56:
       mov      rcx, gword ptr [rsi+0x10]
       mov      ecx, dword ptr [rcx+0x10]
 
G_M000_IG57:
       movsxd   rcx, ecx
       add      rdi, rcx
       jmp      G_M000_IG41
 
G_M000_IG58:
       mov      ecx, dword ptr [rcx+0x10]
       jmp      SHORT G_M000_IG57
 
G_M000_IG59:
       mov      rsi, gword ptr [rsi+0x08]
       cmp      edi, dword ptr [rsi+0x10]
       jae      G_M000_IG264
       mov      rsi, gword ptr [rsi+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG265
       mov      edi, edi
       mov      r14, gword ptr [rsi+8*rdi+0x10]
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r14], rsi
       je       SHORT G_M000_IG61
 
G_M000_IG60:
       mov      gword ptr [rbp+0x218], rdx
       mov      rsi, r14
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rdx, gword ptr [rbp+0x218]
 
G_M000_IG61:
       vmovsd   xmm2, qword ptr [r14+0x08]
       jmp      G_M000_IG43
 
G_M000_IG62:
       mov      gword ptr [rbp+0x208], r10
       jmp      G_M000_IG199
 
G_M000_IG63:
       mov      gword ptr [rbp+0x208], r10
       jmp      G_M000_IG199
 
G_M000_IG64:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       vmovsd   xmm0, qword ptr [rbp+0x80]
       vmovsd   qword ptr [r13+0x08], xmm0
       mov      rax, gword ptr [rbp+0xB0]
       jmp      G_M000_IG10
 
G_M000_IG65:
       mov      rsi, qword ptr [rdx+0x08]
       test     rax, rax
       jne      SHORT G_M000_IG67
 
G_M000_IG66:
       jmp      G_M000_IG181
 
G_M000_IG67:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG88
 
G_M000_IG68:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x48], rax
       lea      rdi, [rbp-0x68]
       mov      rdx, rsi
       mulx     rdx, rcx, rax
       mov      qword ptr [rdi], rcx
       mov      rcx, qword ptr [rbp-0x68]
       mov      qword ptr [rbp-0x60], rcx
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
       je       G_M000_IG84
 
G_M000_IG69:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x58], xmm1
       lea      rdi, [rbp-0x58]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x180], rax
       mov      ecx, dword ptr [rbp-0x50]
       mov      dword ptr [rbp-0x130], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x78], xmm0
       lea      rdi, [rbp-0x78]
       mov      rsi, qword ptr [rbp-0x48]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x78]
       mov      r9d, dword ptr [rbp-0x70]
       mov      rcx, gword ptr [rbp-0x180]
       test     rcx, rcx
       jne      G_M000_IG77
 
G_M000_IG70:
       test     rax, rax
       jne      G_M000_IG229
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x88], xmm0
       movsxd   rsi, dword ptr [rbp-0x130]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x88]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x88]
       mov      r10d, dword ptr [rbp-0x80]
 
G_M000_IG71:
       test     rax, rax
       jne      G_M000_IG230
 
G_M000_IG72:
       test     rax, rax
       jne      G_M000_IG239
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      G_M000_IG82
 
G_M000_IG73:
       mov      edi, -1
 
G_M000_IG74:
       test     edi, edi
       mov      gword ptr [rbp-0x188], rax
       mov      dword ptr [rbp-0x134], r10d
       jg       SHORT G_M000_IG83
 
G_M000_IG75:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x160], rax
       mov      rdi, gword ptr [rbp-0x188]
       mov      esi, dword ptr [rbp-0x134]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rdx, gword ptr [rbp-0x160]
       mov      qword ptr [rdx+0x08], rax
 
G_M000_IG76:
       mov      rcx, rdx
       mov      rax, rcx
       jmp      G_M000_IG182
 
G_M000_IG77:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG78:
       test     rax, rax
       je       SHORT G_M000_IG81
 
G_M000_IG79:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG80:
       mov      edx, dword ptr [rbp-0x130]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      r10d, ecx
       jmp      G_M000_IG71
 
G_M000_IG81:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG80
 
G_M000_IG82:
       xor      edi, edi
       jmp      G_M000_IG74
 
G_M000_IG83:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      gword ptr [rbp-0x158], rdx
       lea      rdi, bword ptr [rdx+0x08]
       mov      rsi, gword ptr [rbp-0x188]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdx, gword ptr [rbp-0x158]
       mov      edi, dword ptr [rbp-0x134]
       mov      dword ptr [rdx+0x10], edi
       jmp      SHORT G_M000_IG76
 
G_M000_IG84:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG87
 
G_M000_IG85:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x60]
       mov      qword ptr [rax+0x08], rdi
       mov      rdx, rax
 
G_M000_IG86:
       jmp      G_M000_IG76
 
G_M000_IG87:
       test     rax, rax
       jl       G_M000_IG266
       mov      rdx, rax
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       cmp      rdx, 262
       jae      G_M000_IG265
       mov      rdx, gword ptr [rax+8*rdx+0x10]
       jmp      SHORT G_M000_IG86
 
G_M000_IG88:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG181
 
G_M000_IG89:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rsi
       vmovsd   qword ptr [rbp-0x90], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x90]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rcx, rax
       mov      rax, rcx
       jmp      G_M000_IG182
 
G_M000_IG90:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm1
       test     rax, rax
       je       G_M000_IG181
 
G_M000_IG91:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG93
 
G_M000_IG92:
       vmovsd   xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG182
 
G_M000_IG93:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG181
 
G_M000_IG94:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG182
 
G_M000_IG95:
       mov      rsi, qword ptr [r13+0x08]
       test     rax, rax
       je       G_M000_IG186
 
G_M000_IG96:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG104
 
G_M000_IG97:
       mov      r13, qword ptr [rax+0x08]
       lea      rax, [rsi+r13]
       mov      qword ptr [rbp-0xB8], rax
       mov      rdi, rsi
       xor      rdi, rax
       mov      rcx, r13
       xor      rcx, rax
       and      rdi, rcx
       jge      G_M000_IG100
 
G_M000_IG98:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xC8], xmm0
       lea      rdi, [rbp-0xC8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xC8]
       mov      gword ptr [rbp-0x190], rax
       mov      ecx, dword ptr [rbp-0xC0]
       mov      dword ptr [rbp-0x138], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xD8], xmm0
       lea      rdi, [rbp-0xD8]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x190]
       mov      esi, dword ptr [rbp-0x138]
       mov      rdx, gword ptr [rbp-0xD8]
       mov      rcx, qword ptr [rbp-0xD0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG99:
       mov      r13, rax
       jmp      G_M000_IG187
 
G_M000_IG100:
       lea      r13, [rax+0x05]
       cmp      r13, 261
       jbe      SHORT G_M000_IG103
 
G_M000_IG101:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, qword ptr [rbp-0xB8]
       mov      qword ptr [rax+0x08], r13
 
G_M000_IG102:
       jmp      SHORT G_M000_IG99
 
G_M000_IG103:
       test     r13, r13
       jl       G_M000_IG266
       mov      rax, r13
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG265
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG102
 
G_M000_IG104:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG186
 
G_M000_IG105:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xE0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG187
 
G_M000_IG106:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       test     rax, rax
       je       G_M000_IG186
 
G_M000_IG107:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG109
 
G_M000_IG108:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xA8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG187
 
G_M000_IG109:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG186
 
G_M000_IG110:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xE8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG187
 
G_M000_IG111:
       mov      rsi, r13
       mov      gword ptr [rbp-0x168], rsi
       test     rax, rax
       je       G_M000_IG186
 
G_M000_IG112:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG186
 
G_M000_IG113:
       mov      r13, rax
       cmp      dword ptr [rsi+0x08], 0
       je       G_M000_IG118
 
G_M000_IG114:
       cmp      dword ptr [r13+0x08], 0
       je       SHORT G_M000_IG117
 
G_M000_IG115:
       mov      eax, dword ptr [rsi+0x08]
       mov      dword ptr [rbp-0xEC], eax
       mov      edi, eax
       add      edi, dword ptr [r13+0x08]
       js       G_M000_IG259
       movsxd   rsi, edi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x170], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rsi, gword ptr [rbp-0x168]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0xEC]
       mov      rax, gword ptr [rbp-0x170]
       lea      rdi, bword ptr [rax+2*rdi+0x0C]
       mov      edx, dword ptr [r13+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r13+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      r13, gword ptr [rbp-0x170]
 
G_M000_IG116:
       jmp      G_M000_IG187
 
G_M000_IG117:
       mov      r13, rsi
       jmp      SHORT G_M000_IG116
 
G_M000_IG118:
       cmp      dword ptr [r13+0x08], 0
       je       G_M000_IG247
 
G_M000_IG119:
       jmp      SHORT G_M000_IG116
 
G_M000_IG120:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      SHORT G_M000_IG122
 
G_M000_IG121:
       mov      eax, 1
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp+0x80], xmm0
       mov      dword ptr [rbp+0x7C], eax
       jmp      G_M000_IG189
 
G_M000_IG122:
       xor      eax, eax
       mov      dword ptr [rbp+0x7C], eax
       jmp      G_M000_IG189
 
G_M000_IG123:
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       test     r13, r13
       sete     al
       movzx    rax, al
       test     dword ptr [rbp+0x7C], eax
       je       G_M000_IG145
 
G_M000_IG124:
       mov      r12, gword ptr [rbp+0x210]
       test     r12, r12
       jne      G_M000_IG138
 
G_M000_IG125:
       call     [RyuPy.Runtime.PyNumericListOps:AppendError():System.Object]
 
G_M000_IG126:
       mov      r13, qword ptr [rbp+0x160]
       mov      rax, qword ptr [rbp+0x150]
       add      r13, rax
       jo       G_M000_IG266
 
G_M000_IG127:
       test     rax, rax
       setg     dil
       movzx    rdi, dil
       mov      rcx, qword ptr [rbp+0x158]
       cmp      r13, rcx
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       cmp      r13, rcx
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, esi
       je       G_M000_IG222
 
G_M000_IG128:
       lea      rdx, [r13+0x05]
       cmp      rdx, 261
       jbe      G_M000_IG146
 
G_M000_IG129:
       mov      qword ptr [rbp+0x158], rcx
       mov      qword ptr [rbp+0x150], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r13
       mov      rsi, rax
 
G_M000_IG130:
       mov      gword ptr [rbp+0x208], rsi
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG133
 
G_M000_IG131:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r8, rsi
       lea      r9d, [rdi-0x01]
       mov      r10d, r9d
       cmp      r10d, dword ptr [r8+0x08]
       jae      G_M000_IG265
       cmp      dword ptr [r8+4*r10+0x10], 21
       je       SHORT G_M000_IG133
 
G_M000_IG132:
       cmp      r9d, dword ptr [rsi+0x08]
       jae      G_M000_IG265
       mov      r8d, r9d
       mov      dword ptr [rsi+4*r8+0x10], 21
 
G_M000_IG133:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      gword ptr [rbp+0x200], rsi
       test     edi, edi
       jle      SHORT G_M000_IG136
 
G_M000_IG134:
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      r9, r8
       dec      edi
       mov      r10d, edi
       cmp      r10d, dword ptr [r9+0x08]
       jae      G_M000_IG265
       cmp      dword ptr [r9+4*r10+0x10], 22
       je       SHORT G_M000_IG136
 
G_M000_IG135:
       cmp      edi, dword ptr [r8+0x08]
       jae      G_M000_IG265
       mov      edi, edi
       mov      dword ptr [r8+4*rdi+0x10], 22
 
G_M000_IG136:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x110], rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rdx, rax
       mov      rdi, qword ptr [rbp+0x110]
       mov      rax, rdi
       mov      qword ptr [rbp+0xF8], rdx
       mov      qword ptr [rbp+0xF0], 1
       vxorps   xmm0, xmm0, xmm0
       mov      rsi, gword ptr [rbp+0x200]
       test     rsi, rsi
       jne      G_M000_IG147
 
G_M000_IG137:
       xor      r8d, r8d
       mov      dword ptr [rbp+0x7C], r8d
       mov      qword ptr [rbp+0x160], r13
       mov      gword ptr [rbp+0x210], r12
       vmovsd   qword ptr [rbp+0x80], xmm0
       mov      r12, rax
       mov      r13, rsi
       mov      rcx, qword ptr [rbp+0xF0]
       jmp      G_M000_IG190
 
G_M000_IG138:
       mov      rdi, gword ptr [r12+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG142
 
G_M000_IG139:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r13, qword ptr [rbp+0x80]
       inc      dword ptr [rdi+0x14]
       mov      rax, gword ptr [rdi+0x08]
       mov      ecx, dword ptr [rdi+0x10]
       cmp      dword ptr [rax+0x08], ecx
       jbe      SHORT G_M000_IG141
 
G_M000_IG140:
       lea      edx, [rcx+0x01]
       mov      dword ptr [rdi+0x10], edx
       mov      edi, ecx
       shl      rdi, 4
       lea      rax, bword ptr [rax+rdi+0x10]
       mov      bword ptr [rbp-0x198], rax
       mov      rdi, rax
       call     CORINFO_HELP_ASSIGN_REF
       mov      rsi, bword ptr [rbp-0x198]
       mov      qword ptr [rsi+0x08], r13
       jmp      G_M000_IG126
 
G_M000_IG141:
       mov      rdx, r13
       call     [System.Collections.Generic.List`1[RyuPy.Runtime.PyNumericSlot]:AddWithResize(RyuPy.Runtime.PyNumericSlot):this]
       jmp      G_M000_IG126
 
G_M000_IG142:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, gword ptr [r12+0x08]
       vmovsd   xmm0, qword ptr [rbp+0x80]
       vmovsd   qword ptr [rdx+0x08], xmm0
       inc      dword ptr [rdi+0x14]
       mov      rax, gword ptr [rdi+0x08]
       mov      esi, dword ptr [rdi+0x10]
       cmp      dword ptr [rax+0x08], esi
       jbe      SHORT G_M000_IG144
 
G_M000_IG143:
       lea      ecx, [rsi+0x01]
       mov      dword ptr [rdi+0x10], ecx
       mov      esi, esi
       mov      rdi, rax
       call     [CORINFO_HELP_ARRADDR_ST]
       jmp      G_M000_IG126
 
G_M000_IG144:
       mov      rsi, rdx
       call     [System.Collections.Generic.List`1[System.__Canon]:AddWithResize(System.__Canon):this]
       jmp      G_M000_IG126
 
G_M000_IG145:
       mov      r12, gword ptr [rbp+0x210]
       mov      rcx, r13
       mov      rsi, r12
       mov      edi, 10
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       jmp      G_M000_IG126
 
G_M000_IG146:
       test     rdx, rdx
       jl       G_M000_IG266
       mov      rsi, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG265
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       mov      qword ptr [rbp+0x158], rcx
       mov      qword ptr [rbp+0x150], rax
       jmp      G_M000_IG130
 
G_M000_IG147:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG149
 
G_M000_IG148:
       mov      r8d, 1
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp+0x80], xmm0
       mov      dword ptr [rbp+0x7C], r8d
       mov      qword ptr [rbp+0x160], r13
       mov      gword ptr [rbp+0x210], r12
       mov      r12, rax
       mov      r13, rsi
       mov      rcx, qword ptr [rbp+0xF0]
       jmp      G_M000_IG190
 
G_M000_IG149:
       xor      r8d, r8d
       mov      dword ptr [rbp+0x7C], r8d
       mov      qword ptr [rbp+0x160], r13
       mov      gword ptr [rbp+0x210], r12
       vmovsd   qword ptr [rbp+0x80], xmm0
       mov      r12, rax
       mov      r13, rsi
       mov      rcx, qword ptr [rbp+0xF0]
       jmp      G_M000_IG190
 
G_M000_IG150:
       test     rsi, rsi
       jl       G_M000_IG266
       mov      rdi, rsi
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG265
       mov      rsi, gword ptr [rsi+8*rdi+0x10]
       mov      qword ptr [rbp+0xF8], rdx
       mov      qword ptr [rbp+0xF0], rcx
       jmp      G_M000_IG193
 
G_M000_IG151:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       je       SHORT G_M000_IG153
 
G_M000_IG152:
       mov      gword ptr [rbp+0x208], r10
       jmp      G_M000_IG199
 
G_M000_IG153:
       test     rcx, rcx
       je       G_M000_IG207
 
G_M000_IG154:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rsi
       jne      G_M000_IG206
 
G_M000_IG155:
       mov      rsi, qword ptr [rcx+0x08]
       mov      rdi, gword ptr [rdx+0x08]
       test     rdi, rdi
       jne      SHORT G_M000_IG160
 
G_M000_IG156:
       mov      rdi, gword ptr [rdx+0x10]
       mov      edi, dword ptr [rdi+0x10]
 
G_M000_IG157:
       test     rsi, rsi
       jl       SHORT G_M000_IG161
 
G_M000_IG158:
       movsxd   rdi, edi
       cmp      rsi, rdi
       jb       SHORT G_M000_IG162
 
G_M000_IG159:
       mov      gword ptr [rbp+0x208], r10
       jmp      G_M000_IG199
 
G_M000_IG160:
       mov      edi, dword ptr [rdi+0x10]
       jmp      SHORT G_M000_IG157
 
G_M000_IG161:
       mov      gword ptr [rbp+0x240], r14
       movsxd   r14, edi
       add      rsi, r14
       mov      r14, gword ptr [rbp+0x240]
       jmp      SHORT G_M000_IG158
 
G_M000_IG162:
       mov      rdi, gword ptr [rdx+0x10]
       test     rdi, rdi
       jne      G_M000_IG204
 
G_M000_IG163:
       mov      rdi, gword ptr [rdx+0x08]
       cmp      esi, dword ptr [rdi+0x10]
       jae      G_M000_IG264
       mov      rdi, gword ptr [rdi+0x08]
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG265
       mov      esi, esi
       mov      rdi, gword ptr [rdi+8*rsi+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG165
 
G_M000_IG164:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       G_M000_IG205
 
G_M000_IG165:
       xor      edi, edi
 
G_M000_IG166:
       test     edi, edi
       je       G_M000_IG203
 
G_M000_IG167:
       test     rbx, rbx
       je       SHORT G_M000_IG176
 
G_M000_IG168:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      SHORT G_M000_IG176
 
G_M000_IG169:
       test     rcx, rcx
       je       SHORT G_M000_IG176
 
G_M000_IG170:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rsi
       jne      SHORT G_M000_IG176
 
G_M000_IG171:
       mov      rsi, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x118], rsi
       mov      rsi, gword ptr [rbx+0x08]
       test     rsi, rsi
       jne      G_M000_IG14
 
G_M000_IG172:
       mov      rsi, gword ptr [rbx+0x10]
       mov      esi, dword ptr [rsi+0x10]
 
G_M000_IG173:
       cmp      qword ptr [rbp-0x118], 0
       jl       G_M000_IG15
 
G_M000_IG174:
       mov      dword ptr [rbp-0x12C], esi
       mov      rsi, qword ptr [rbp-0x118]
       mov      qword ptr [rbp-0x128], rsi
 
G_M000_IG175:
       movsxd   rsi, dword ptr [rbp-0x12C]
       cmp      qword ptr [rbp-0x128], rsi
       jb       G_M000_IG16
 
G_M000_IG176:
       xor      rsi, rsi
 
G_M000_IG177:
       test     rsi, rsi
       je       G_M000_IG63
 
G_M000_IG178:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG21
 
G_M000_IG179:
       test     r10, r10
       jne      G_M000_IG22
 
G_M000_IG180:
       mov      gword ptr [rbp+0x208], r10
       jmp      G_M000_IG199
 
G_M000_IG181:
       mov      rdx, rax
       mov      rsi, gword ptr [rbp+0xC0]
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG182:
       test     r13, r13
       je       SHORT G_M000_IG186
 
G_M000_IG183:
       mov      rcx, qword ptr [r13]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       je       G_M000_IG95
 
G_M000_IG184:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       je       G_M000_IG106
 
G_M000_IG185:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       je       G_M000_IG111
 
G_M000_IG186:
       mov      rdx, rax
       mov      rsi, r13
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r13, rax
 
G_M000_IG187:
       test     r13, r13
       jne      G_M000_IG120
 
G_M000_IG188:
       xor      eax, eax
       mov      dword ptr [rbp+0x7C], eax
 
G_M000_IG189:
       mov      rcx, qword ptr [rbp+0xF0]
       add      r12, rcx
       jo       G_M000_IG266
 
G_M000_IG190:
       test     rcx, rcx
       setg     dil
       movzx    rdi, dil
       mov      rdx, qword ptr [rbp+0xF8]
       cmp      r12, rdx
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       cmp      r12, rdx
       setg     r8b
       movzx    r8, r8b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r8d
       or       edi, esi
       je       G_M000_IG123
 
G_M000_IG191:
       lea      rsi, [r12+0x05]
       cmp      rsi, 261
       jbe      G_M000_IG150
 
G_M000_IG192:
       mov      qword ptr [rbp+0xF8], rdx
       mov      qword ptr [rbp+0xF0], rcx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
       mov      rsi, rax
 
G_M000_IG193:
       mov      rdi, rsi
       mov      esi, dword ptr [(reloc)]
       test     esi, esi
       jle      SHORT G_M000_IG196
 
G_M000_IG194:
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      r9, r8
       dec      esi
       mov      r10d, esi
       cmp      r10d, dword ptr [r9+0x08]
       jae      G_M000_IG265
       cmp      dword ptr [r9+4*r10+0x10], 23
       je       SHORT G_M000_IG196
 
G_M000_IG195:
       cmp      esi, dword ptr [r8+0x08]
       jae      G_M000_IG265
       mov      esi, esi
       mov      dword ptr [r8+4*rsi+0x10], 23
 
G_M000_IG196:
       mov      eax, dword ptr [rbp+0x7C]
       test     eax, eax
       mov      dword ptr [rbp+0x7C], eax
       mov      rcx, rdi
       mov      rdx, gword ptr [rbp+0x218]
       je       SHORT G_M000_IG199
 
G_M000_IG197:
       mov      r10, gword ptr [rbp+0x208]
       test     rdx, rdx
       jne      G_M000_IG151
 
G_M000_IG198:
       mov      gword ptr [rbp+0x208], r10
 
G_M000_IG199:
       mov      gword ptr [rbp+0x218], rdx
       mov      rdi, rdx
       mov      gword ptr [rbp+0x1F8], rcx
       mov      rsi, rcx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0xC0], rax
       mov      rdi, rbx
       mov      rsi, gword ptr [rbp+0x1F8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdi, rax
       mov      rsi, gword ptr [rbp+0x208]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0xB0], rax
       mov      ecx, dword ptr [rbp+0x7C]
       test     ecx, ecx
       je       G_M000_IG09
 
G_M000_IG200:
       mov      rdx, gword ptr [rbp+0xC0]
       test     rdx, rdx
       je       G_M000_IG09
 
G_M000_IG201:
       mov      rsi, qword ptr [rdx]
       mov      rdi, 0xD1FFAB1E
       cmp      rsi, rdi
       je       G_M000_IG03
 
G_M000_IG202:
       jmp      G_M000_IG09
 
G_M000_IG203:
       mov      gword ptr [rbp+0x208], r10
       jmp      G_M000_IG199
 
G_M000_IG204:
       cmp      esi, dword ptr [rdi+0x10]
       jae      G_M000_IG264
       mov      rdi, gword ptr [rdi+0x08]
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG265
       mov      esi, esi
       shl      rsi, 4
       mov      rdi, gword ptr [rdi+rsi+0x10]
       test     rdi, rdi
       je       G_M000_IG263
 
G_M000_IG205:
       mov      edi, 1
       jmp      G_M000_IG166
 
G_M000_IG206:
       mov      gword ptr [rbp+0x208], r10
       jmp      G_M000_IG199
 
G_M000_IG207:
       mov      gword ptr [rbp+0x208], r10
       jmp      G_M000_IG199
 
G_M000_IG208:
       lea      rcx, [r13+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG210
 
G_M000_IG209:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r13
       jmp      SHORT G_M000_IG211
 
G_M000_IG210:
       test     rcx, rcx
       jl       G_M000_IG266
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG265
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
 
G_M000_IG211:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG214
 
G_M000_IG212:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      r8d, dword ptr [rdx+0x08]
       cmp      edi, r8d
       jae      G_M000_IG265
       cmp      dword ptr [rdx+4*rdi+0x10], 18
       je       SHORT G_M000_IG214
 
G_M000_IG213:
       mov      dword ptr [rcx+4*rdi+0x10], 18
 
G_M000_IG214:
       mov      gword ptr [rbp+0x240], r14
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x218], rax
       mov      ecx, dword ptr [(reloc)]
       mov      dword ptr [rbp-0x13C], ecx
       test     ecx, ecx
       jle      G_M000_IG221
 
G_M000_IG215:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rdx, rdi
       lea      esi, [rcx-0x01]
       mov      r8d, dword ptr [rdx+0x08]
       cmp      esi, r8d
       jae      G_M000_IG265
       cmp      dword ptr [rdx+4*rsi+0x10], 19
       je       SHORT G_M000_IG217
 
G_M000_IG216:
       mov      dword ptr [rdi+4*rsi+0x10], 19
 
G_M000_IG217:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x148], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, 0xD1FFAB1E
       mov      gword ptr [rax+0x08], rdi
       mov      rcx, gword ptr [rbp-0x148]
       lea      rdi, bword ptr [rcx+0x10]
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, gword ptr [rbp-0x148]
       mov      gword ptr [rbp+0x210], rdi
       mov      esi, dword ptr [rbp-0x13C]
       test     esi, esi
       jle      SHORT G_M000_IG220
 
G_M000_IG218:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rcx, rdi
       dec      esi
       mov      edx, esi
       mov      r8d, dword ptr [rcx+0x08]
       cmp      edx, r8d
       jae      G_M000_IG265
       mov      esi, edx
       cmp      dword ptr [rcx+4*rsi+0x10], 20
       je       SHORT G_M000_IG220
 
G_M000_IG219:
       mov      dword ptr [rdi+4*rsi+0x10], 20
 
G_M000_IG220:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x170], rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, rax
       mov      rdi, qword ptr [rbp+0x170]
       mov      rax, rdi
       mov      qword ptr [rbp+0x158], rcx
       mov      qword ptr [rbp+0x150], 1
       mov      qword ptr [rbp+0x1C8], r13
       mov      qword ptr [rbp+0x1B8], r12
       mov      r13, rax
       mov      rax, qword ptr [rbp+0x150]
       mov      r12, gword ptr [rbp+0x210]
       mov      r14, gword ptr [rbp+0x240]
       jmp      G_M000_IG127
 
G_M000_IG221:
       jmp      G_M000_IG217
 
G_M000_IG222:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG225
 
G_M000_IG223:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       dec      edi
       mov      ecx, dword ptr [rax+0x08]
       cmp      edi, ecx
       jae      G_M000_IG265
       cmp      dword ptr [rax+4*rdi+0x10], 25
       je       SHORT G_M000_IG225
 
G_M000_IG224:
       mov      dword ptr [rsi+4*rdi+0x10], 25
 
G_M000_IG225:
       mov      rdi, 0xD1FFAB1E
       mov      r13, gword ptr [rdi]
       mov      rax, gword ptr [r13+0x48]
       test     rax, rax
       je       SHORT G_M000_IG227
 
G_M000_IG226:
       cmp      dword ptr [rax+0x08], 1
       mov      gword ptr [rbp-0x150], rax
       je       SHORT G_M000_IG228
 
G_M000_IG227:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       lea      rdi, bword ptr [r13+0x48]
       mov      gword ptr [rbp-0x150], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG228:
       mov      rdi, gword ptr [rbp-0x150]
       mov      rdx, r12
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, gword ptr [rbp-0x150]
       mov      rsi, gword ptr [rbp+0x228]
       mov      edi, 7
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
       mov      r13, qword ptr [rbp+0x1C8]
       mov      r12, qword ptr [rbp+0x1B8]
       add      r13, r12
       jo       G_M000_IG266
       test     r12, r12
       setg     dil
       movzx    rdi, dil
       mov      rax, qword ptr [rbp+0x1C0]
       cmp      r13, rax
       setl     cl
       movzx    rcx, cl
       and      ecx, edi
       mov      qword ptr [rbp+0x1C0], rax
       cmp      r13, rax
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, ecx
       je       G_M000_IG251
       jmp      G_M000_IG208
 
G_M000_IG229:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG78
 
G_M000_IG230:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG231
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG232
 
G_M000_IG231:
       mov      ecx, r10d
       jmp      SHORT G_M000_IG238
 
G_M000_IG232:
       cmp      edi, 2
       je       SHORT G_M000_IG233
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG265
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG234
 
G_M000_IG233:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [rax+0x10]
       or       rdi, rcx
 
G_M000_IG234:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG235
       mov      ecx, -1
       jmp      SHORT G_M000_IG237
 
G_M000_IG235:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG236
       mov      ecx, 1
       jmp      SHORT G_M000_IG237
 
G_M000_IG236:
       xor      ecx, ecx
 
G_M000_IG237:
       imul     ecx, r10d
 
G_M000_IG238:
       test     ecx, ecx
       jge      G_M000_IG72
       mov      gword ptr [rbp-0x188], rax
       mov      dword ptr [rbp-0x134], r10d
       jmp      G_M000_IG83
 
G_M000_IG239:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG240
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG241
 
G_M000_IG240:
       mov      dword ptr [rbp-0x134], r10d
       mov      edi, r10d
       mov      r10d, dword ptr [rbp-0x134]
       jmp      G_M000_IG74
 
G_M000_IG241:
       cmp      edi, 2
       je       SHORT G_M000_IG242
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG265
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG243
 
G_M000_IG242:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      gword ptr [rbp-0x188], rax
       mov      ecx, dword ptr [rax+0x10]
       or       rdi, rcx
       mov      rax, gword ptr [rbp-0x188]
 
G_M000_IG243:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG244
       mov      ecx, -1
       jmp      SHORT G_M000_IG246
 
G_M000_IG244:
       test     rdi, rdi
       jge      SHORT G_M000_IG245
       mov      ecx, 1
       jmp      SHORT G_M000_IG246
 
G_M000_IG245:
       xor      ecx, ecx
 
G_M000_IG246:
       mov      edi, r10d
       imul     edi, ecx
       jmp      G_M000_IG74
 
G_M000_IG247:
       mov      r13, 0xD1FFAB1E
       jmp      G_M000_IG116
 
G_M000_IG248:
       mov      rdx, gword ptr [rbp+0x218]
       mov      rsi, rdx
       mov      rdi, r14
       call     [CORINFO_HELP_CHKCASTCLASS]
       int3     
 
G_M000_IG249:
       mov      r14, gword ptr [rbp+0x240]
       jmp      G_M000_IG199
 
G_M000_IG250:
       mov      rsi, rdi
       jmp      G_M000_IG20
 
G_M000_IG251:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG254
 
G_M000_IG252:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      ecx, dword ptr [(reloc)]
       dec      ecx
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG265
       cmp      dword ptr [rax+4*rcx+0x10], 26
       je       SHORT G_M000_IG254
 
G_M000_IG253:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      ecx, dword ptr [(reloc)]
       dec      ecx
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG265
       mov      dword ptr [rax+4*rcx+0x10], 26
 
G_M000_IG254:
       mov      rbx, qword ptr [rbp+0x1F0]
       test     rbx, rbx
       jle      SHORT G_M000_IG257
 
G_M000_IG255:
       movsxd   rax, dword ptr [(reloc)]
       cmp      rbx, rax
       jg       SHORT G_M000_IG257
 
G_M000_IG256:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG257:
       mov      rax, gword ptr [rbp+0x228]
 
G_M000_IG258:
       add      rsp, 0x3E8
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG259:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG260:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0xB63B
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyRuntimeError:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG261:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0xB63B
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyRuntimeError:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG262:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0xB63B
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyRuntimeError:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG263:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0xB63B
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyRuntimeError:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG264:
       call     [System.ThrowHelper:ThrowArgumentOutOfRange_IndexMustBeLessException()]
       int3     
 
G_M000_IG265:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG266:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 6467

; Assembly listing for method RyuPy.Generated.Module:m0_matmul(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x31c
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 1
; 40 inlinees with PGO data; 65 single block inlinees; 83 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 336
       mov      qword ptr [rsp+0x3B8], r15
       mov      qword ptr [rsp+0x3B0], r14
       mov      qword ptr [rsp+0x3A8], r13
       mov      qword ptr [rsp+0x3A0], r12
       mov      qword ptr [rsp+0x398], rbx
       lea      rbp, [rsp+0x150]
       xor      eax, eax
       mov      qword ptr [rbp-0xC8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       mov      rbx, gword ptr [rbp+0x238]
       mov      rsi, gword ptr [rbp+0x208]
       mov      r13, qword ptr [rbp+0x100]
       mov      rcx, qword ptr [rbp+0xF8]
       mov      rdx, qword ptr [rbp+0xF0]
       vmovsd   xmm0, qword ptr [rbp+0x80]
       mov      r12d, dword ptr [rbp+0x7C]
       vmovsd   xmm1, qword ptr [rbp+0x60]
       vmovsd   xmm2, qword ptr [rbp+0x50]
 
G_M000_IG02:
       jmp      G_M000_IG88
 
G_M000_IG03:
       mov      rdx, r12
       mov      rsi, r14
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r14, rax
 
G_M000_IG04:
       test     r14, r14
       jne      G_M000_IG166
 
G_M000_IG05:
       xor      r12d, r12d
       mov      rcx, qword ptr [rbp+0xF8]
       mov      rdx, qword ptr [rbp+0xF0]
       mov      rsi, gword ptr [rbp+0x208]
 
G_M000_IG06:
       add      r13, rdx
       jo       G_M000_IG246
 
G_M000_IG07:
       test     rdx, rdx
       setg     dil
       movzx    rdi, dil
       cmp      r13, rcx
       setl     r15b
       movzx    r15, r15b
       and      r15d, edi
       cmp      r13, rcx
       setg     r11b
       movzx    r11, r11b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r11d
       or       edi, r15d
       je       G_M000_IG90
 
G_M000_IG08:
       lea      r11, [r13+0x05]
       cmp      r11, 261
       jbe      G_M000_IG138
 
G_M000_IG09:
       mov      qword ptr [rbp+0xF8], rcx
       mov      qword ptr [rbp+0xF0], rdx
       mov      gword ptr [rbp+0x208], rsi
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r13
 
G_M000_IG10:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG13
 
G_M000_IG11:
       mov      r11, 0xD1FFAB1E
       mov      r11, gword ptr [r11]
       mov      r15, r11
       dec      edi
       mov      r10d, edi
       cmp      r10d, dword ptr [r15+0x08]
       jae      G_M000_IG245
       cmp      dword ptr [r15+4*r10+0x10], 23
       je       SHORT G_M000_IG13
 
G_M000_IG12:
       cmp      edi, dword ptr [r11+0x08]
       jae      G_M000_IG245
       mov      edi, edi
       mov      dword ptr [r11+4*rdi+0x10], 23
 
G_M000_IG13:
       test     r12d, r12d
       je       G_M000_IG144
 
G_M000_IG14:
       mov      r11, gword ptr [rbp+0x218]
       test     r11, r11
       je       G_M000_IG145
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r11], rdi
       jne      G_M000_IG145
 
G_M000_IG16:
       test     rax, rax
       je       G_M000_IG145
 
G_M000_IG17:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG145
 
G_M000_IG18:
       mov      rdi, qword ptr [rax+0x08]
       mov      r15, gword ptr [r11+0x08]
       test     r15, r15
       jne      G_M000_IG39
 
G_M000_IG19:
       mov      r15, gword ptr [r11+0x10]
       mov      r15d, dword ptr [r15+0x10]
 
G_M000_IG20:
       test     rdi, rdi
       jl       G_M000_IG40
 
G_M000_IG21:
       movsxd   r15, r15d
       cmp      rdi, r15
       jae      G_M000_IG145
 
G_M000_IG22:
       mov      r15, gword ptr [r11+0x10]
       test     r15, r15
       jne      G_M000_IG41
 
G_M000_IG23:
       mov      r15, gword ptr [r11+0x08]
       cmp      edi, dword ptr [r15+0x10]
       jae      G_M000_IG89
 
G_M000_IG24:
       mov      r15, gword ptr [r15+0x08]
       cmp      edi, dword ptr [r15+0x08]
       jae      G_M000_IG245
       mov      edi, edi
       mov      rdi, gword ptr [r15+8*rdi+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG26
 
G_M000_IG25:
       mov      r15, 0xD1FFAB1E
       cmp      qword ptr [rdi], r15
       je       G_M000_IG42
 
G_M000_IG26:
       xor      edi, edi
 
G_M000_IG27:
       test     edi, edi
       je       G_M000_IG145
 
G_M000_IG28:
       test     rbx, rbx
       je       SHORT G_M000_IG34
 
G_M000_IG29:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG34
 
G_M000_IG30:
       mov      rdi, qword ptr [rax+0x08]
       mov      r15, gword ptr [rbx+0x08]
       test     r15, r15
       jne      G_M000_IG46
 
G_M000_IG31:
       mov      r15, gword ptr [rbx+0x10]
       mov      r15d, dword ptr [r15+0x10]
 
G_M000_IG32:
       test     rdi, rdi
       jl       G_M000_IG47
 
G_M000_IG33:
       movsxd   r15, r15d
       cmp      rdi, r15
       jb       G_M000_IG43
 
G_M000_IG34:
       xor      r15, r15
 
G_M000_IG35:
       test     r15, r15
       je       G_M000_IG145
 
G_M000_IG36:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG145
 
G_M000_IG37:
       mov      rsi, gword ptr [rbp+0x208]
       test     rsi, rsi
       jne      G_M000_IG51
 
G_M000_IG38:
       mov      gword ptr [rbp+0x208], rsi
       jmp      G_M000_IG145
 
G_M000_IG39:
       mov      r15d, dword ptr [r15+0x10]
       jmp      G_M000_IG20
 
G_M000_IG40:
       movsxd   r10, r15d
       add      rdi, r10
       jmp      G_M000_IG21
 
G_M000_IG41:
       cmp      edi, dword ptr [r15+0x10]
       jae      G_M000_IG89
       mov      r15, gword ptr [r15+0x08]
       cmp      edi, dword ptr [r15+0x08]
       jae      G_M000_IG245
       mov      edi, edi
       shl      rdi, 4
       mov      rdi, gword ptr [r15+rdi+0x10]
       test     rdi, rdi
       je       G_M000_IG244
 
G_M000_IG42:
       mov      edi, 1
       jmp      G_M000_IG27
 
G_M000_IG43:
       mov      r15, gword ptr [rbx+0x08]
       test     r15, r15
       jne      SHORT G_M000_IG48
 
G_M000_IG44:
       xor      r15, r15
 
G_M000_IG45:
       jmp      G_M000_IG35
 
G_M000_IG46:
       mov      r15d, dword ptr [r15+0x10]
       jmp      G_M000_IG32
 
G_M000_IG47:
       mov      dword ptr [rbp-0xF8], r15d
       movsxd   r15, r15d
       add      rdi, r15
       mov      r15d, dword ptr [rbp-0xF8]
       jmp      G_M000_IG33
 
G_M000_IG48:
       cmp      edi, dword ptr [r15+0x10]
       jae      G_M000_IG89
       mov      r15, gword ptr [r15+0x08]
       cmp      edi, dword ptr [r15+0x08]
       jae      G_M000_IG245
       mov      edi, edi
       mov      r15, gword ptr [r15+8*rdi+0x10]
       test     r15, r15
       je       SHORT G_M000_IG44
 
G_M000_IG49:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG44
 
G_M000_IG50:
       jmp      SHORT G_M000_IG45
 
G_M000_IG51:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG143
 
G_M000_IG52:
       mov      gword ptr [rbp+0x208], rsi
       mov      rdi, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0xE0], rdi
       mov      rdi, gword ptr [r15+0x08]
       test     rdi, rdi
       jne      G_M000_IG139
 
G_M000_IG53:
       mov      rdi, gword ptr [r15+0x10]
       mov      edi, dword ptr [rdi+0x10]
 
G_M000_IG54:
       cmp      qword ptr [rbp-0xE0], 0
       jl       G_M000_IG140
 
G_M000_IG55:
       mov      dword ptr [rbp-0xF4], edi
       mov      rdi, qword ptr [rbp-0xE0]
       mov      qword ptr [rbp-0xF0], rdi
 
G_M000_IG56:
       movsxd   rdi, dword ptr [rbp-0xF4]
       cmp      qword ptr [rbp-0xF0], rdi
       jae      G_M000_IG145
 
G_M000_IG57:
       mov      rdi, qword ptr [rbp-0xF0]
       mov      dword ptr [rbp-0xE4], edi
       mov      rdi, gword ptr [r15+0x10]
       test     rdi, rdi
       jne      G_M000_IG141
 
G_M000_IG58:
       mov      rdi, gword ptr [r15+0x08]
       mov      r10d, dword ptr [rbp-0xE4]
       cmp      r10d, dword ptr [rdi+0x10]
       jae      G_M000_IG89
       mov      rdi, gword ptr [rdi+0x08]
       cmp      r10d, dword ptr [rdi+0x08]
       jae      G_M000_IG245
       mov      rdi, gword ptr [rdi+8*r10+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG60
 
G_M000_IG59:
       mov      r10, 0xD1FFAB1E
       cmp      qword ptr [rdi], r10
       je       G_M000_IG142
 
G_M000_IG60:
       xor      edi, edi
 
G_M000_IG61:
       test     edi, edi
       je       G_M000_IG145
 
G_M000_IG62:
       mov      gword ptr [rbp+0x218], r11
       mov      r14, r15
       mov      rdi, qword ptr [rax+0x08]
       test     rdi, rdi
       jl       G_M000_IG70
 
G_M000_IG63:
       mov      rax, gword ptr [rbp+0x218]
       mov      r11, gword ptr [rax+0x10]
       test     r11, r11
       je       G_M000_IG74
 
G_M000_IG64:
       cmp      edi, dword ptr [r11+0x10]
       jae      G_M000_IG89
       mov      r11, gword ptr [r11+0x08]
       cmp      edi, dword ptr [r11+0x08]
       jae      G_M000_IG245
       mov      edi, edi
       shl      rdi, 4
       lea      rdi, bword ptr [r11+rdi+0x10]
       mov      r11, gword ptr [rdi]
       mov      rdi, qword ptr [rdi+0x08]
       test     r11, r11
       je       G_M000_IG241
       vmovq    xmm1, rdi
 
G_M000_IG65:
       vmovsd   qword ptr [rbp+0x60], xmm1
       mov      rsi, gword ptr [rbp+0x208]
       mov      rdi, qword ptr [rsi+0x08]
       test     rdi, rdi
       jl       G_M000_IG77
 
G_M000_IG66:
       mov      r11, gword ptr [r14+0x10]
       test     r11, r11
       je       G_M000_IG81
 
G_M000_IG67:
       cmp      edi, dword ptr [r11+0x10]
       jae      G_M000_IG89
       mov      r11, gword ptr [r11+0x08]
       cmp      edi, dword ptr [r11+0x08]
       jae      G_M000_IG245
       mov      edi, edi
       shl      rdi, 4
       lea      rdi, bword ptr [r11+rdi+0x10]
       mov      r11, gword ptr [rdi]
       mov      rdi, qword ptr [rdi+0x08]
       test     r11, r11
       je       G_M000_IG242
       vmovq    xmm2, rdi
 
G_M000_IG68:
       mov      gword ptr [rbp+0x218], rax
       mov      rcx, qword ptr [rbp+0xF8]
       mov      rdx, qword ptr [rbp+0xF0]
       mov      rsi, gword ptr [rbp+0x208]
       vmovsd   xmm0, qword ptr [rbp+0x80]
       vmovsd   xmm1, qword ptr [rbp+0x60]
       jmp      G_M000_IG88
 
G_M000_IG69:
       xor      r12d, r12d
       mov      rcx, qword ptr [rbp+0xF8]
       mov      rdx, qword ptr [rbp+0xF0]
       mov      rsi, gword ptr [rbp+0x208]
       jmp      G_M000_IG06
 
G_M000_IG70:
       mov      rax, gword ptr [rbp+0x218]
       mov      r11, gword ptr [rax+0x08]
       test     r11, r11
       jne      SHORT G_M000_IG73
 
G_M000_IG71:
       mov      r11, gword ptr [rax+0x10]
       mov      r11d, dword ptr [r11+0x10]
 
G_M000_IG72:
       movsxd   r11, r11d
       add      rdi, r11
       mov      gword ptr [rbp+0x218], rax
       jmp      G_M000_IG63
 
G_M000_IG73:
       mov      r11d, dword ptr [r11+0x10]
       jmp      SHORT G_M000_IG72
 
G_M000_IG74:
       mov      gword ptr [rbp+0x218], rax
       mov      r11, gword ptr [rax+0x08]
       cmp      edi, dword ptr [r11+0x10]
       jae      G_M000_IG89
       mov      r11, gword ptr [r11+0x08]
       cmp      edi, dword ptr [r11+0x08]
       jae      G_M000_IG245
       mov      edi, edi
       mov      r11, gword ptr [r11+8*rdi+0x10]
       mov      gword ptr [rbp-0x140], r11
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r11], rdi
       je       SHORT G_M000_IG76
 
G_M000_IG75:
       mov      rsi, r11
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      r11, gword ptr [rbp-0x140]
 
G_M000_IG76:
       vmovsd   xmm1, qword ptr [r11+0x08]
       mov      rax, gword ptr [rbp+0x218]
       jmp      G_M000_IG65
 
G_M000_IG77:
       mov      r11, gword ptr [r14+0x08]
       test     r11, r11
       jne      SHORT G_M000_IG80
 
G_M000_IG78:
       mov      r11, gword ptr [r14+0x10]
       mov      r11d, dword ptr [r11+0x10]
 
G_M000_IG79:
       movsxd   r11, r11d
       add      rdi, r11
       jmp      G_M000_IG66
 
G_M000_IG80:
       mov      r11d, dword ptr [r11+0x10]
       jmp      SHORT G_M000_IG79
 
G_M000_IG81:
       mov      r11, gword ptr [r14+0x08]
       cmp      edi, dword ptr [r11+0x10]
       jae      G_M000_IG89
       mov      r11, gword ptr [r11+0x08]
       cmp      edi, dword ptr [r11+0x08]
       jae      G_M000_IG245
       mov      edi, edi
       mov      r14, gword ptr [r11+8*rdi+0x10]
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       je       SHORT G_M000_IG83
 
G_M000_IG82:
       mov      gword ptr [rbp+0x218], rax
       mov      rsi, r14
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, gword ptr [rbp+0x218]
 
G_M000_IG83:
       vmovsd   xmm2, qword ptr [r14+0x08]
       jmp      G_M000_IG68
 
G_M000_IG84:
       mov      rdx, qword ptr [rcx]
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      G_M000_IG147
 
G_M000_IG85:
       test     rax, rax
       je       G_M000_IG147
 
G_M000_IG86:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       mov      rax, gword ptr [rbp+0xB0]
       jne      G_M000_IG147
 
G_M000_IG87:
       mov      r14, gword ptr [rbp+0xC0]
       vmovsd   xmm1, qword ptr [r14+0x08]
       vmovsd   xmm2, qword ptr [rax+0x08]
       mov      rcx, qword ptr [rbp+0xF8]
       mov      rdx, qword ptr [rbp+0xF0]
       mov      rsi, gword ptr [rbp+0x208]
       vmovsd   xmm0, qword ptr [rbp+0x80]
 
G_M000_IG88:
       vmulsd   xmm1, xmm1, xmm2
       vaddsd   xmm0, xmm1, xmm0
       vmovsd   qword ptr [rbp+0x80], xmm0
       xor      r14, r14
       jmp      G_M000_IG06
 
G_M000_IG89:
       call     [System.ThrowHelper:ThrowArgumentOutOfRange_IndexMustBeLessException()]
       int3     
 
G_M000_IG90:
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       test     r14, r14
       sete     al
       movzx    rax, al
       test     r12d, eax
       je       G_M000_IG112
 
G_M000_IG91:
       mov      r15, gword ptr [rbp+0x210]
       test     r15, r15
       jne      G_M000_IG105
 
G_M000_IG92:
       call     [RyuPy.Runtime.PyNumericListOps:AppendError():System.Object]
 
G_M000_IG93:
       mov      r14, qword ptr [rbp+0x160]
       mov      r13, qword ptr [rbp+0x150]
       add      r14, r13
       jo       G_M000_IG246
 
G_M000_IG94:
       test     r13, r13
       setg     dil
       movzx    rdi, dil
       mov      r12, qword ptr [rbp+0x158]
       cmp      r14, r12
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       cmp      r14, r12
       setg     al
       movzx    rax, al
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, eax
       or       edi, esi
       je       G_M000_IG113
 
G_M000_IG95:
       lea      rax, [r14+0x05]
       cmp      rax, 261
       jbe      G_M000_IG134
 
G_M000_IG96:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rbp+0x160], r14
       mov      qword ptr [rax+0x08], r14
       mov      rsi, rax
 
G_M000_IG97:
       mov      gword ptr [rbp+0x208], rsi
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG100
 
G_M000_IG98:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       lea      esi, [rdi-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      G_M000_IG245
       cmp      dword ptr [rdx+4*r8+0x10], 21
       je       SHORT G_M000_IG100
 
G_M000_IG99:
       cmp      esi, dword ptr [rcx+0x08]
       jae      G_M000_IG245
       mov      edx, esi
       mov      dword ptr [rcx+4*rdx+0x10], 21
 
G_M000_IG100:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      gword ptr [rbp+0x200], rcx
       test     edi, edi
       jle      SHORT G_M000_IG103
 
G_M000_IG101:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rsi, rdx
       dec      edi
       mov      r8d, edi
       cmp      r8d, dword ptr [rsi+0x08]
       jae      G_M000_IG245
       cmp      dword ptr [rsi+4*r8+0x10], 22
       je       SHORT G_M000_IG103
 
G_M000_IG102:
       cmp      edi, dword ptr [rdx+0x08]
       jae      G_M000_IG245
       mov      edi, edi
       mov      dword ptr [rdx+4*rdi+0x10], 22
 
G_M000_IG103:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x110], rax
       mov      rdi, gword ptr [rbp+0x230]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, rax
       mov      rdi, qword ptr [rbp+0x110]
       mov      rax, rdi
       mov      qword ptr [rbp+0xF8], rcx
       mov      qword ptr [rbp+0xF0], 1
       vxorps   xmm0, xmm0, xmm0
       mov      rsi, gword ptr [rbp+0x200]
       test     rsi, rsi
       jne      G_M000_IG135
 
G_M000_IG104:
       xor      r8d, r8d
       mov      qword ptr [rbp+0x158], r12
       mov      qword ptr [rbp+0x150], r13
       mov      gword ptr [rbp+0x210], r15
       vmovsd   qword ptr [rbp+0x80], xmm0
       mov      r12d, r8d
       mov      r13, rax
       mov      r14, rsi
       mov      rcx, qword ptr [rbp+0xF8]
       mov      rdx, qword ptr [rbp+0xF0]
       mov      rsi, gword ptr [rbp+0x208]
       jmp      G_M000_IG07
 
G_M000_IG105:
       mov      rdi, gword ptr [r15+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG109
 
G_M000_IG106:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r14, qword ptr [rbp+0x80]
       inc      dword ptr [rdi+0x14]
       mov      rax, gword ptr [rdi+0x08]
       mov      ecx, dword ptr [rdi+0x10]
       cmp      dword ptr [rax+0x08], ecx
       jbe      SHORT G_M000_IG108
 
G_M000_IG107:
       lea      edx, [rcx+0x01]
       mov      dword ptr [rdi+0x10], edx
       mov      edi, ecx
       shl      rdi, 4
       lea      r13, bword ptr [rax+rdi+0x10]
       mov      rdi, r13
       call     CORINFO_HELP_ASSIGN_REF
       mov      qword ptr [r13+0x08], r14
       jmp      G_M000_IG93
 
G_M000_IG108:
       mov      rdx, r14
       call     [System.Collections.Generic.List`1[RyuPy.Runtime.PyNumericSlot]:AddWithResize(RyuPy.Runtime.PyNumericSlot):this]
       jmp      G_M000_IG93
 
G_M000_IG109:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, gword ptr [r15+0x08]
       vmovsd   xmm0, qword ptr [rbp+0x80]
       vmovsd   qword ptr [rdx+0x08], xmm0
       inc      dword ptr [rdi+0x14]
       mov      rax, gword ptr [rdi+0x08]
       mov      esi, dword ptr [rdi+0x10]
       cmp      dword ptr [rax+0x08], esi
       jbe      SHORT G_M000_IG111
 
G_M000_IG110:
       lea      ecx, [rsi+0x01]
       mov      dword ptr [rdi+0x10], ecx
       mov      esi, esi
       mov      rdi, rax
       call     [CORINFO_HELP_ARRADDR_ST]
       jmp      G_M000_IG93
 
G_M000_IG111:
       mov      rsi, rdx
       call     [System.Collections.Generic.List`1[System.__Canon]:AddWithResize(System.__Canon):this]
       jmp      G_M000_IG93
 
G_M000_IG112:
       mov      r15, gword ptr [rbp+0x210]
       mov      rcx, r14
       mov      rsi, r15
       mov      edi, 10
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       jmp      G_M000_IG93
 
G_M000_IG113:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG116
 
G_M000_IG114:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       dec      edi
       mov      ecx, edi
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG245
       cmp      dword ptr [rax+4*rcx+0x10], 25
       je       SHORT G_M000_IG116
 
G_M000_IG115:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG245
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 25
 
G_M000_IG116:
       mov      rdi, 0xD1FFAB1E
       mov      r14, gword ptr [rdi]
       mov      r12, gword ptr [r14+0x48]
       test     r12, r12
       je       SHORT G_M000_IG118
 
G_M000_IG117:
       cmp      dword ptr [r12+0x08], 1
       je       SHORT G_M000_IG119
 
G_M000_IG118:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r12, rax
       lea      rdi, bword ptr [r14+0x48]
       mov      rsi, r12
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG119:
       mov      rdi, r12
       mov      rdx, r15
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, r12
       mov      rsi, gword ptr [rbp+0x228]
       mov      edi, 7
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
       mov      r15, qword ptr [rbp+0x1C8]
       mov      r14, qword ptr [rbp+0x1B8]
       add      r15, r14
       jo       G_M000_IG246
       test     r14, r14
       setg     dil
       movzx    rdi, dil
       mov      r12, qword ptr [rbp+0x1C0]
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
       je       G_M000_IG232
 
G_M000_IG120:
       lea      r13, [r15+0x05]
       cmp      r13, 261
       jbe      G_M000_IG132
 
G_M000_IG121:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r15
 
G_M000_IG122:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG125
 
G_M000_IG123:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rcx, rax
       dec      edi
       mov      edx, edi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG245
       cmp      dword ptr [rcx+4*rdx+0x10], 18
       je       SHORT G_M000_IG125
 
G_M000_IG124:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG245
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 18
 
G_M000_IG125:
       mov      rdi, gword ptr [rbp+0x240]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x218], rax
       mov      eax, dword ptr [(reloc)]
       mov      dword ptr [rbp-0x108], eax
       test     eax, eax
       jle      G_M000_IG133
 
G_M000_IG126:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rcx, rdi
       lea      edx, [rax-0x01]
       mov      esi, edx
       cmp      esi, dword ptr [rcx+0x08]
       jae      G_M000_IG245
       cmp      dword ptr [rcx+4*rsi+0x10], 19
       je       SHORT G_M000_IG128
 
G_M000_IG127:
       cmp      edx, dword ptr [rdi+0x08]
       jae      G_M000_IG245
       mov      ecx, edx
       mov      dword ptr [rdi+4*rcx+0x10], 19
 
G_M000_IG128:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x110], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, 0xD1FFAB1E
       mov      gword ptr [rax+0x08], rdi
       mov      rcx, gword ptr [rbp-0x110]
       lea      rdi, bword ptr [rcx+0x10]
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, gword ptr [rbp-0x110]
       mov      gword ptr [rbp+0x210], rdi
       mov      edx, dword ptr [rbp-0x108]
       test     edx, edx
       jle      SHORT G_M000_IG131
 
G_M000_IG129:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rcx, rdi
       dec      edx
       mov      esi, edx
       cmp      esi, dword ptr [rcx+0x08]
       jae      G_M000_IG245
       cmp      dword ptr [rcx+4*rsi+0x10], 20
       je       SHORT G_M000_IG131
 
G_M000_IG130:
       mov      ecx, edx
       cmp      ecx, dword ptr [rdi+0x08]
       jae      G_M000_IG245
       mov      dword ptr [rdi+4*rcx+0x10], 20
 
G_M000_IG131:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x170], rax
       mov      rdi, gword ptr [rbp+0x230]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rdi, qword ptr [rbp+0x170]
       mov      rcx, rdi
       mov      qword ptr [rbp+0x158], rax
       mov      qword ptr [rbp+0x150], 1
       mov      qword ptr [rbp+0x1C8], r15
       mov      qword ptr [rbp+0x1C0], r12
       mov      qword ptr [rbp+0x1B8], r14
       mov      r14, rcx
       mov      r13, qword ptr [rbp+0x150]
       mov      r15, gword ptr [rbp+0x210]
       jmp      G_M000_IG94
 
G_M000_IG132:
       test     r13, r13
       jl       G_M000_IG246
       mov      rsi, r13
       mov      rdi, 0xD1FFAB1E
       mov      r13, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG245
       mov      rsi, gword ptr [r13+8*rsi+0x10]
       jmp      G_M000_IG122
 
G_M000_IG133:
       jmp      G_M000_IG128
 
G_M000_IG134:
       test     rax, rax
       jl       G_M000_IG246
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG245
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       mov      qword ptr [rbp+0x160], r14
       jmp      G_M000_IG97
 
G_M000_IG135:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG137
 
G_M000_IG136:
       mov      r8d, 1
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp+0x80], xmm0
       mov      qword ptr [rbp+0x158], r12
       mov      qword ptr [rbp+0x150], r13
       mov      gword ptr [rbp+0x210], r15
       mov      r12d, r8d
       mov      r13, rax
       mov      r14, rsi
       mov      rcx, qword ptr [rbp+0xF8]
       mov      rdx, qword ptr [rbp+0xF0]
       mov      rsi, gword ptr [rbp+0x208]
       jmp      G_M000_IG07
 
G_M000_IG137:
       xor      r8d, r8d
       mov      qword ptr [rbp+0x158], r12
       mov      qword ptr [rbp+0x150], r13
       mov      gword ptr [rbp+0x210], r15
       vmovsd   qword ptr [rbp+0x80], xmm0
       mov      r12d, r8d
       mov      r13, rax
       mov      r14, rsi
       mov      rcx, qword ptr [rbp+0xF8]
       mov      rdx, qword ptr [rbp+0xF0]
       mov      rsi, gword ptr [rbp+0x208]
       jmp      G_M000_IG07
 
G_M000_IG138:
       test     r11, r11
       jl       G_M000_IG246
       mov      rdi, r11
       mov      r11, 0xD1FFAB1E
       mov      r11, gword ptr [r11]
       cmp      rdi, 262
       jae      G_M000_IG245
       mov      rdi, gword ptr [r11+8*rdi+0x10]
       mov      qword ptr [rbp+0xF8], rcx
       mov      qword ptr [rbp+0xF0], rdx
       mov      gword ptr [rbp+0x208], rsi
       mov      rax, rdi
       jmp      G_M000_IG10
 
G_M000_IG139:
       mov      edi, dword ptr [rdi+0x10]
       mov      dword ptr [rbp-0xF4], edi
       mov      edi, dword ptr [rbp-0xF4]
       jmp      G_M000_IG54
 
G_M000_IG140:
       mov      dword ptr [rbp-0xF4], edi
       movsxd   rdi, edi
       add      rdi, qword ptr [rbp-0xE0]
       mov      qword ptr [rbp-0xF0], rdi
       jmp      G_M000_IG56
 
G_M000_IG141:
       mov      r10d, dword ptr [rbp-0xE4]
       cmp      r10d, dword ptr [rdi+0x10]
       jae      G_M000_IG89
       mov      rdi, gword ptr [rdi+0x08]
       cmp      r10d, dword ptr [rdi+0x08]
       jae      G_M000_IG245
       shl      r10, 4
       mov      rdi, gword ptr [rdi+r10+0x10]
       test     rdi, rdi
       je       G_M000_IG243
 
G_M000_IG142:
       mov      edi, 1
       jmp      G_M000_IG61
 
G_M000_IG143:
       mov      gword ptr [rbp+0x208], rsi
       jmp      SHORT G_M000_IG145
 
G_M000_IG144:
       mov      r11, gword ptr [rbp+0x218]
 
G_M000_IG145:
       mov      gword ptr [rbp+0x218], r11
       mov      rdi, r11
       mov      gword ptr [rbp-0x118], rax
       mov      rsi, rax
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0xC0], rax
       mov      rdi, rbx
       mov      rsi, gword ptr [rbp-0x118]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdi, rax
       mov      rsi, gword ptr [rbp+0x208]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0xB0], rax
       test     r12d, r12d
       je       SHORT G_M000_IG147
 
G_M000_IG146:
       mov      rcx, gword ptr [rbp+0xC0]
       test     rcx, rcx
       jne      G_M000_IG84
 
G_M000_IG147:
       test     r14, r14
       sete     dil
       movzx    rdi, dil
       test     r12d, edi
       jne      G_M000_IG168
 
G_M000_IG148:
       mov      rcx, gword ptr [rbp+0xC0]
       test     rcx, rcx
       je       SHORT G_M000_IG151
 
G_M000_IG149:
       mov      rdx, qword ptr [rcx]
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       je       G_M000_IG169
 
G_M000_IG150:
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       je       G_M000_IG193
 
G_M000_IG151:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      r12, rax
 
G_M000_IG152:
       test     r14, r14
       je       G_M000_IG03
 
G_M000_IG153:
       mov      rax, qword ptr [r14]
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       je       G_M000_IG198
 
G_M000_IG154:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       je       G_M000_IG209
 
G_M000_IG155:
       mov      rsi, 0xD1FFAB1E
       cmp      rax, rsi
       jne      G_M000_IG03
 
G_M000_IG156:
       mov      rsi, r14
       mov      gword ptr [rbp-0x130], rsi
       test     r12, r12
       je       G_M000_IG03
 
G_M000_IG157:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG03
 
G_M000_IG158:
       cmp      dword ptr [rsi+0x08], 0
       je       G_M000_IG163
 
G_M000_IG159:
       cmp      dword ptr [r12+0x08], 0
       je       SHORT G_M000_IG162
 
G_M000_IG160:
       mov      r14d, dword ptr [rsi+0x08]
       mov      edi, r14d
       add      edi, dword ptr [r12+0x08]
       js       G_M000_IG240
       movsxd   rsi, edi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x138], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rsi, gword ptr [rbp-0x130]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, r14d
       mov      r14, gword ptr [rbp-0x138]
       lea      rdi, bword ptr [r14+2*rdi+0x0C]
       mov      edx, dword ptr [r12+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r12+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       nop      
 
G_M000_IG161:
       jmp      G_M000_IG04
 
G_M000_IG162:
       mov      r14, rsi
       jmp      SHORT G_M000_IG161
 
G_M000_IG163:
       cmp      dword ptr [r12+0x08], 0
       je       SHORT G_M000_IG165
 
G_M000_IG164:
       mov      r14, r12
       jmp      SHORT G_M000_IG161
 
G_M000_IG165:
       mov      r14, 0xD1FFAB1E
       jmp      SHORT G_M000_IG161
 
G_M000_IG166:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r14], rsi
       jne      G_M000_IG69
 
G_M000_IG167:
       mov      r12d, 1
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp+0x80], xmm0
       mov      rcx, qword ptr [rbp+0xF8]
       mov      rdx, qword ptr [rbp+0xF0]
       mov      rsi, gword ptr [rbp+0x208]
       jmp      G_M000_IG06
 
G_M000_IG168:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       vmovsd   xmm0, qword ptr [rbp+0x80]
       vmovsd   qword ptr [r14+0x08], xmm0
       mov      rax, gword ptr [rbp+0xB0]
       jmp      G_M000_IG148
 
G_M000_IG169:
       mov      rsi, qword ptr [rcx+0x08]
       test     rax, rax
       je       G_M000_IG151
 
G_M000_IG170:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rax], rdx
       jne      G_M000_IG191
 
G_M000_IG171:
       mov      r12, qword ptr [rax+0x08]
       lea      rdi, [rbp-0x60]
       mov      rdx, rsi
       mulx     rcx, rax, r12
       mov      qword ptr [rdi], rax
       mov      rax, qword ptr [rbp-0x60]
       mov      qword ptr [rbp-0x58], rax
       mov      rdi, rsi
       sar      rdi, 63
       and      rdi, r12
       sub      rcx, rdi
       mov      rdi, r12
       sar      rdi, 63
       and      rdi, rsi
       sub      rcx, rdi
       mov      rdi, rax
       sar      rdi, 63
       cmp      rcx, rdi
       je       G_M000_IG187
 
G_M000_IG172:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x50], xmm1
       lea      rdi, [rbp-0x50]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x148], rax
       mov      ecx, dword ptr [rbp-0x48]
       mov      dword ptr [rbp-0xFC], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       mov      rsi, r12
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0x70]
       mov      r9d, dword ptr [rbp-0x68]
       mov      rax, gword ptr [rbp-0x148]
       test     rax, rax
       jne      G_M000_IG180
 
G_M000_IG173:
       test     r12, r12
       jne      G_M000_IG214
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       movsxd   rsi, dword ptr [rbp-0xFC]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x80]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0x80]
       mov      eax, dword ptr [rbp-0x78]
 
G_M000_IG174:
       test     r12, r12
       jne      G_M000_IG215
 
G_M000_IG175:
       test     r12, r12
       jne      G_M000_IG224
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      SHORT G_M000_IG185
 
G_M000_IG176:
       mov      edi, -1
 
G_M000_IG177:
       test     edi, edi
       mov      dword ptr [rbp-0x100], eax
       jg       SHORT G_M000_IG186
 
G_M000_IG178:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x128], rax
       mov      rdi, r12
       mov      esi, dword ptr [rbp-0x100]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r12, gword ptr [rbp-0x128]
       mov      qword ptr [r12+0x08], rax
 
G_M000_IG179:
       jmp      G_M000_IG152
 
G_M000_IG180:
       lea      rdi, bword ptr [rax+0x10]
       mov      esi, dword ptr [rax+0x08]
 
G_M000_IG181:
       test     r12, r12
       je       SHORT G_M000_IG184
 
G_M000_IG182:
       lea      rcx, bword ptr [r12+0x10]
       mov      r8d, dword ptr [r12+0x08]
 
G_M000_IG183:
       mov      edx, dword ptr [rbp-0xFC]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      r12, rax
       mov      eax, edx
       jmp      G_M000_IG174
 
G_M000_IG184:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG183
 
G_M000_IG185:
       xor      edi, edi
       jmp      SHORT G_M000_IG177
 
G_M000_IG186:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x120], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r12
       call     CORINFO_HELP_ASSIGN_REF
       mov      r12, gword ptr [rbp-0x120]
       mov      edi, dword ptr [rbp-0x100]
       mov      dword ptr [r12+0x10], edi
       jmp      SHORT G_M000_IG179
 
G_M000_IG187:
       lea      r12, [rax+0x05]
       cmp      r12, 261
       jbe      SHORT G_M000_IG190
 
G_M000_IG188:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, qword ptr [rbp-0x58]
       mov      qword ptr [rax+0x08], r12
       mov      r12, rax
 
G_M000_IG189:
       jmp      G_M000_IG179
 
G_M000_IG190:
       test     r12, r12
       jl       G_M000_IG246
       mov      rdi, r12
       mov      rax, 0xD1FFAB1E
       mov      r11, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG245
       mov      r12, gword ptr [r11+8*rdi+0x10]
       jmp      SHORT G_M000_IG189
 
G_M000_IG191:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG151
 
G_M000_IG192:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rsi
       vmovsd   qword ptr [rbp-0x88], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x88]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG152
 
G_M000_IG193:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm1
       test     rax, rax
       je       G_M000_IG151
 
G_M000_IG194:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG196
 
G_M000_IG195:
       vmovsd   xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG152
 
G_M000_IG196:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG151
 
G_M000_IG197:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG152
 
G_M000_IG198:
       mov      rsi, qword ptr [r14+0x08]
       test     r12, r12
       je       G_M000_IG03
 
G_M000_IG199:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG207
 
G_M000_IG200:
       mov      r12, qword ptr [r12+0x08]
       lea      r14, [rsi+r12]
       mov      rdi, rsi
       xor      rdi, r14
       mov      rax, r12
       xor      rax, r14
       and      rdi, rax
       jge      SHORT G_M000_IG203
 
G_M000_IG201:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xB8], xmm0
       lea      rdi, [rbp-0xB8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r14, gword ptr [rbp-0xB8]
       mov      eax, dword ptr [rbp-0xB0]
       mov      dword ptr [rbp-0x104], eax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xC8], xmm0
       lea      rdi, [rbp-0xC8]
       mov      rsi, r12
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, r14
       mov      esi, dword ptr [rbp-0x104]
       mov      rdx, gword ptr [rbp-0xC8]
       mov      rcx, qword ptr [rbp-0xC0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG202:
       mov      r14, rax
       jmp      G_M000_IG04
 
G_M000_IG203:
       lea      r12, [r14+0x05]
       cmp      r12, 261
       jbe      SHORT G_M000_IG206
 
G_M000_IG204:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r14
 
G_M000_IG205:
       jmp      SHORT G_M000_IG202
 
G_M000_IG206:
       test     r12, r12
       jl       G_M000_IG246
       mov      rax, r12
       mov      rdi, 0xD1FFAB1E
       mov      r11, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG245
       mov      rax, gword ptr [r11+8*rax+0x10]
       jmp      SHORT G_M000_IG205
 
G_M000_IG207:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG03
 
G_M000_IG208:
       vmovsd   xmm0, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xD0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xD0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xA8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG04
 
G_M000_IG209:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm0
       test     r12, r12
       je       G_M000_IG03
 
G_M000_IG210:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      SHORT G_M000_IG212
 
G_M000_IG211:
       vmovsd   xmm1, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x98]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG04
 
G_M000_IG212:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG03
 
G_M000_IG213:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0xD8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x98]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xD8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG04
 
G_M000_IG214:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG181
 
G_M000_IG215:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG216
       mov      edi, dword ptr [r12+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG217
 
G_M000_IG216:
       mov      ecx, eax
       jmp      SHORT G_M000_IG223
 
G_M000_IG217:
       cmp      edi, 2
       je       SHORT G_M000_IG218
       cmp      dword ptr [r12+0x08], 0
       jbe      G_M000_IG245
       mov      edi, dword ptr [r12+0x10]
       jmp      SHORT G_M000_IG219
 
G_M000_IG218:
       mov      edi, dword ptr [r12+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r12+0x10]
       or       rdi, rcx
 
G_M000_IG219:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG220
       mov      ecx, -1
       jmp      SHORT G_M000_IG222
 
G_M000_IG220:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG221
       mov      ecx, 1
       jmp      SHORT G_M000_IG222
 
G_M000_IG221:
       xor      ecx, ecx
 
G_M000_IG222:
       imul     ecx, eax
 
G_M000_IG223:
       test     ecx, ecx
       jge      G_M000_IG175
       mov      dword ptr [rbp-0x100], eax
       jmp      G_M000_IG186
 
G_M000_IG224:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG225
       mov      edi, dword ptr [r12+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG226
 
G_M000_IG225:
       mov      dword ptr [rbp-0x100], eax
       mov      edi, eax
       mov      eax, dword ptr [rbp-0x100]
       jmp      G_M000_IG177
 
G_M000_IG226:
       cmp      edi, 2
       je       SHORT G_M000_IG227
       cmp      dword ptr [r12+0x08], 0
       jbe      G_M000_IG245
       mov      edi, dword ptr [r12+0x10]
       jmp      SHORT G_M000_IG228
 
G_M000_IG227:
       mov      edi, dword ptr [r12+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r12+0x10]
       or       rdi, rcx
 
G_M000_IG228:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG229
       mov      ecx, -1
       jmp      SHORT G_M000_IG231
 
G_M000_IG229:
       test     rdi, rdi
       jge      SHORT G_M000_IG230
       mov      ecx, 1
       jmp      SHORT G_M000_IG231
 
G_M000_IG230:
       xor      ecx, ecx
 
G_M000_IG231:
       mov      edi, eax
       imul     edi, ecx
       jmp      G_M000_IG177
 
G_M000_IG232:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG235
 
G_M000_IG233:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      ecx, dword ptr [(reloc)]
       dec      ecx
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG245
       cmp      dword ptr [rax+4*rcx+0x10], 26
       je       SHORT G_M000_IG235
 
G_M000_IG234:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      ecx, dword ptr [(reloc)]
       dec      ecx
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG245
       mov      dword ptr [rax+4*rcx+0x10], 26
 
G_M000_IG235:
       mov      rbx, qword ptr [rbp+0x1F0]
       test     rbx, rbx
       jle      SHORT G_M000_IG238
 
G_M000_IG236:
       movsxd   rax, dword ptr [(reloc)]
       cmp      rbx, rax
       jg       SHORT G_M000_IG238
 
G_M000_IG237:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG238:
       mov      rax, gword ptr [rbp+0x228]
 
G_M000_IG239:
       add      rsp, 920
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG240:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG241:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0xB63B
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyRuntimeError:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG242:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0xB63B
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyRuntimeError:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG243:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0xB63B
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyRuntimeError:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG244:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0xB63B
       mov      rsi, 0xD1FFAB1E
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyRuntimeError:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG245:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG246:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 6227

