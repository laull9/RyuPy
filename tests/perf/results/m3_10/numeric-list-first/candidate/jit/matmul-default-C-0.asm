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
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovsd   qword ptr [rbp-0x210], xmm0
       mov      rdi, gword ptr [rbp-0x248]
       mov      rsi, gword ptr [rbp-0x68]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
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

; Assembly listing for method RyuPy.Generated.Module:m0_matmul(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x3e7
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 37 inlinees with PGO data; 60 single block inlinees; 81 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 384
       mov      qword ptr [rsp+0x3E8], r15
       mov      qword ptr [rsp+0x3E0], r14
       mov      qword ptr [rsp+0x3D8], r13
       mov      qword ptr [rsp+0x3D0], r12
       mov      qword ptr [rsp+0x3C8], rbx
       lea      rbp, [rsp+0x180]
       xor      eax, eax
       mov      qword ptr [rbp-0xD8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xD0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       mov      qword ptr [rbp-0x50], rax
       mov      r14, gword ptr [rbp+0x240]
       mov      rbx, gword ptr [rbp+0x238]
       mov      r15, gword ptr [rbp+0x230]
       mov      r13, gword ptr [rbp+0x200]
       mov      rcx, gword ptr [rbp+0x1F8]
       mov      r12, qword ptr [rbp+0x100]
 
G_M000_IG02:
       jmp      G_M000_IG49
 
G_M000_IG03:
       mov      rdx, gword ptr [rbp+0x218]
 
G_M000_IG04:
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
       jne      G_M000_IG105
 
G_M000_IG05:
       test     r13, r13
       sete     dil
       movzx    rdi, dil
       test     ecx, edi
       jne      G_M000_IG115
 
G_M000_IG06:
       mov      rdx, gword ptr [rbp+0xC0]
       test     rdx, rdx
       je       SHORT G_M000_IG10
 
G_M000_IG07:
       mov      rsi, qword ptr [rdx]
       mov      rdi, 0xD1FFAB1E
       cmp      rsi, rdi
       je       G_M000_IG116
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       cmp      rsi, rdi
       je       G_M000_IG142
 
G_M000_IG09:
 
G_M000_IG10:
       mov      rdx, rax
       mov      rsi, gword ptr [rbp+0xC0]
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG11:
       test     r13, r13
       je       SHORT G_M000_IG15
 
G_M000_IG12:
       mov      rcx, qword ptr [r13]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       je       G_M000_IG147
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       je       G_M000_IG158
 
G_M000_IG14:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       je       G_M000_IG163
 
G_M000_IG15:
       mov      rdx, rax
       mov      rsi, r13
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r13, rax
 
G_M000_IG16:
       test     r13, r13
       jne      G_M000_IG172
 
G_M000_IG17:
       xor      eax, eax
 
G_M000_IG18:
       mov      rcx, qword ptr [rbp+0xF0]
       add      r12, rcx
       jo       G_M000_IG256
 
G_M000_IG19:
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
       je       G_M000_IG175
 
G_M000_IG20:
       lea      rsi, [r12+0x05]
       cmp      rsi, 261
       jbe      G_M000_IG199
 
G_M000_IG21:
       mov      dword ptr [rbp+0x7C], eax
       mov      qword ptr [rbp+0xF8], rdx
       mov      qword ptr [rbp+0xF0], rcx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
       mov      rsi, rax
 
G_M000_IG22:
       mov      r8, rsi
       mov      esi, dword ptr [(reloc)]
       test     esi, esi
       jle      SHORT G_M000_IG25
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      r9, rdi
       dec      esi
       mov      r10d, esi
       cmp      r10d, dword ptr [r9+0x08]
       jae      G_M000_IG255
       cmp      dword ptr [r9+4*r10+0x10], 23
       je       SHORT G_M000_IG25
 
G_M000_IG24:
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG255
       mov      esi, esi
       mov      dword ptr [rdi+4*rsi+0x10], 23
 
G_M000_IG25:
       cmp      dword ptr [rbp+0x7C], 0
       mov      rcx, r8
       je       G_M000_IG71
 
G_M000_IG26:
       mov      rdx, gword ptr [rbp+0x218]
       test     rdx, rdx
       je       G_M000_IG04
 
G_M000_IG27:
       mov      gword ptr [rbp+0x218], rdx
       mov      rsi, 0xD1FFAB1E
       mov      rdx, gword ptr [rbp+0x218]
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG04
 
G_M000_IG28:
       mov      gword ptr [rbp+0x218], rdx
       test     rcx, rcx
       je       G_M000_IG03
 
G_M000_IG29:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rsi
       jne      G_M000_IG70
 
G_M000_IG30:
       mov      gword ptr [rbp+0x1F8], rcx
       mov      rsi, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x118], rsi
       mov      rdx, gword ptr [rbp+0x218]
       mov      rsi, gword ptr [rdx+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       jne      SHORT G_M000_IG39
 
G_M000_IG31:
       mov      rcx, gword ptr [rbp+0x218]
       mov      rsi, gword ptr [rcx+0x08]
       mov      rax, rsi
       test     rax, rax
       je       SHORT G_M000_IG34
 
G_M000_IG32:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG34
 
G_M000_IG33:
       call     [CORINFO_HELP_CHKCASTCLASS_SPECIAL]
 
G_M000_IG34:
       mov      esi, dword ptr [rax+0x10]
 
G_M000_IG35:
       mov      rax, qword ptr [rbp-0x118]
       test     rax, rax
       jl       SHORT G_M000_IG40
 
G_M000_IG36:
       mov      rdx, rax
 
G_M000_IG37:
       movsxd   rsi, esi
       cmp      rdx, rsi
       jb       SHORT G_M000_IG41
 
G_M000_IG38:
       mov      rcx, gword ptr [rbp+0x1F8]
       mov      rdx, gword ptr [rbp+0x218]
       jmp      G_M000_IG04
 
G_M000_IG39:
       mov      esi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG35
 
G_M000_IG40:
       movsxd   rdx, esi
       add      rdx, rax
       mov      rax, rdx
       jmp      SHORT G_M000_IG37
 
G_M000_IG41:
       mov      rsi, rdx
       mov      dword ptr [rbp-0x11C], esi
       mov      rdx, gword ptr [rbp+0x218]
       mov      rsi, gword ptr [rdx+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       jne      G_M000_IG68
 
G_M000_IG42:
       mov      rcx, gword ptr [rbp+0x218]
       mov      rsi, gword ptr [rcx+0x08]
       mov      rax, rsi
       test     rax, rax
       je       SHORT G_M000_IG45
 
G_M000_IG43:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG45
 
G_M000_IG44:
       call     [CORINFO_HELP_CHKCASTCLASS_SPECIAL]
 
G_M000_IG45:
       mov      edi, dword ptr [rbp-0x11C]
       cmp      edi, dword ptr [rax+0x10]
       jae      G_M000_IG254
       mov      rax, gword ptr [rax+0x08]
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG255
       mov      rdi, gword ptr [rax+8*rdi+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG47
 
G_M000_IG46:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       je       G_M000_IG69
 
G_M000_IG47:
       xor      edi, edi
 
G_M000_IG48:
       test     edi, edi
       mov      rcx, gword ptr [rbp+0x1F8]
       je       G_M000_IG67
 
G_M000_IG49:
       test     rbx, rbx
       je       G_M000_IG62
 
G_M000_IG50:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      G_M000_IG62
 
G_M000_IG51:
       test     rcx, rcx
       je       G_M000_IG62
 
G_M000_IG52:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rsi
       jne      SHORT G_M000_IG62
 
G_M000_IG53:
       mov      gword ptr [rbp+0x1F8], rcx
       mov      rsi, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x108], rsi
       mov      rsi, gword ptr [rbx+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       jne      G_M000_IG72
 
G_M000_IG54:
       mov      rsi, gword ptr [rbx+0x08]
       mov      rax, rsi
       test     rax, rax
       je       SHORT G_M000_IG57
 
G_M000_IG55:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG57
 
G_M000_IG56:
       call     [CORINFO_HELP_CHKCASTCLASS_SPECIAL]
 
G_M000_IG57:
       mov      esi, dword ptr [rax+0x10]
 
G_M000_IG58:
       mov      rax, qword ptr [rbp-0x108]
       test     rax, rax
       jl       G_M000_IG73
 
G_M000_IG59:
       mov      rcx, rax
 
G_M000_IG60:
       movsxd   rsi, esi
       cmp      rcx, rsi
       jb       G_M000_IG74
 
G_M000_IG61:
       mov      rcx, gword ptr [rbp+0x1F8]
 
G_M000_IG62:
       xor      rsi, rsi
 
G_M000_IG63:
       test     rsi, rsi
       je       G_M000_IG104
 
G_M000_IG64:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG79
 
G_M000_IG65:
       mov      r10, gword ptr [rbp+0x208]
       test     r10, r10
       jne      G_M000_IG80
 
G_M000_IG66:
       mov      gword ptr [rbp+0x208], r10
       mov      rdx, gword ptr [rbp+0x218]
       jmp      G_M000_IG04
 
G_M000_IG67:
       mov      rdx, gword ptr [rbp+0x218]
       jmp      G_M000_IG04
 
G_M000_IG68:
       mov      edi, dword ptr [rbp-0x11C]
       cmp      edi, dword ptr [rax+0x10]
       jae      G_M000_IG254
       mov      rax, gword ptr [rax+0x08]
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG255
       shl      rdi, 4
       mov      rdi, gword ptr [rax+rdi+0x10]
       test     rdi, rdi
       je       G_M000_IG253
 
G_M000_IG69:
       mov      edi, 1
       jmp      G_M000_IG48
 
G_M000_IG70:
       mov      rdx, gword ptr [rbp+0x218]
       jmp      G_M000_IG04
 
G_M000_IG71:
       mov      rdx, gword ptr [rbp+0x218]
       jmp      G_M000_IG04
 
G_M000_IG72:
       mov      esi, dword ptr [rax+0x10]
       jmp      G_M000_IG58
 
G_M000_IG73:
       movsxd   rcx, esi
       add      rcx, rax
       mov      rax, rcx
       jmp      G_M000_IG60
 
G_M000_IG74:
       mov      dword ptr [rbp-0x10C], ecx
       mov      rsi, gword ptr [rbx+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       je       SHORT G_M000_IG77
 
G_M000_IG75:
       mov      esi, dword ptr [rbp-0x10C]
       cmp      esi, dword ptr [rax+0x10]
       jae      G_M000_IG254
       mov      rdi, gword ptr [rax+0x08]
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG255
       mov      rax, gword ptr [rdi+8*rsi+0x10]
       test     rax, rax
       je       SHORT G_M000_IG77
 
G_M000_IG76:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       je       G_M000_IG242
 
G_M000_IG77:
       xor      rcx, rcx
 
G_M000_IG78:
       mov      rax, rcx
       mov      rsi, rax
       mov      rcx, gword ptr [rbp+0x1F8]
       jmp      G_M000_IG63
 
G_M000_IG79:
       mov      rdx, gword ptr [rbp+0x218]
       jmp      G_M000_IG04
 
G_M000_IG80:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r10], rdi
       jne      G_M000_IG103
 
G_M000_IG81:
       mov      gword ptr [rbp+0x1F8], rcx
       mov      gword ptr [rbp+0x208], r10
       mov      rdi, qword ptr [r10+0x08]
       mov      qword ptr [rbp-0xF8], rdi
       mov      gword ptr [rbp-0x160], rsi
       mov      rsi, gword ptr [rsi+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       jne      SHORT G_M000_IG90
 
G_M000_IG82:
       mov      rcx, gword ptr [rbp-0x160]
       mov      rsi, gword ptr [rcx+0x08]
       mov      rax, rsi
       test     rax, rax
       je       SHORT G_M000_IG85
 
G_M000_IG83:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG85
 
G_M000_IG84:
       call     [CORINFO_HELP_CHKCASTCLASS_SPECIAL]
 
G_M000_IG85:
       mov      esi, dword ptr [rax+0x10]
 
G_M000_IG86:
       mov      rax, qword ptr [rbp-0xF8]
       test     rax, rax
       jl       SHORT G_M000_IG91
 
G_M000_IG87:
       mov      rdx, rax
 
G_M000_IG88:
       movsxd   rsi, esi
       cmp      rdx, rsi
       jb       SHORT G_M000_IG92
 
G_M000_IG89:
       mov      rcx, gword ptr [rbp+0x1F8]
       mov      rdx, gword ptr [rbp+0x218]
       jmp      G_M000_IG04
 
G_M000_IG90:
       mov      esi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG86
 
G_M000_IG91:
       movsxd   rdx, esi
       add      rdx, rax
       mov      rax, rdx
       jmp      SHORT G_M000_IG88
 
G_M000_IG92:
       mov      dword ptr [rbp-0xFC], edx
       mov      rcx, gword ptr [rbp-0x160]
       mov      rsi, gword ptr [rcx+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       jne      G_M000_IG101
 
G_M000_IG93:
       mov      rcx, gword ptr [rbp-0x160]
       mov      rsi, gword ptr [rcx+0x08]
       mov      rax, rsi
       test     rax, rax
       je       SHORT G_M000_IG96
 
G_M000_IG94:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG96
 
G_M000_IG95:
       call     [CORINFO_HELP_CHKCASTCLASS_SPECIAL]
 
G_M000_IG96:
       mov      edi, dword ptr [rbp-0xFC]
       cmp      edi, dword ptr [rax+0x10]
       jae      G_M000_IG254
       mov      rsi, gword ptr [rax+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG255
       mov      rdi, gword ptr [rsi+8*rdi+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG98
 
G_M000_IG97:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       SHORT G_M000_IG102
 
G_M000_IG98:
       xor      edi, edi
 
G_M000_IG99:
       test     edi, edi
       je       G_M000_IG241
 
G_M000_IG100:
       mov      rdi, gword ptr [rbp+0x218]
       mov      rsi, gword ptr [rbp+0x1F8]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovsd   qword ptr [rbp+0x60], xmm0
       mov      rdi, gword ptr [rbp-0x160]
       mov      rsi, gword ptr [rbp+0x208]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovaps  xmm1, xmm0
       mov      ecx, dword ptr [rbp+0x7C]
       vmovsd   xmm0, qword ptr [rbp+0x60]
       jmp      G_M000_IG112
 
G_M000_IG101:
       mov      edi, dword ptr [rbp-0xFC]
       cmp      edi, dword ptr [rax+0x10]
       jae      G_M000_IG254
       mov      rsi, gword ptr [rax+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG255
       shl      rdi, 4
       mov      rdi, gword ptr [rsi+rdi+0x10]
       test     rdi, rdi
       je       G_M000_IG252
 
G_M000_IG102:
       mov      edi, 1
       jmp      SHORT G_M000_IG99
 
G_M000_IG103:
       mov      gword ptr [rbp+0x208], r10
       mov      rdx, gword ptr [rbp+0x218]
       jmp      G_M000_IG04
 
G_M000_IG104:
       mov      rdx, gword ptr [rbp+0x218]
       jmp      G_M000_IG04
 
G_M000_IG105:
       mov      rdx, gword ptr [rbp+0xC0]
       test     rdx, rdx
       je       SHORT G_M000_IG114
 
G_M000_IG106:
       mov      rsi, qword ptr [rdx]
       mov      rdi, 0xD1FFAB1E
       cmp      rsi, rdi
       je       SHORT G_M000_IG108
 
G_M000_IG107:
       jmp      G_M000_IG05
 
G_M000_IG108:
       test     rax, rax
       je       SHORT G_M000_IG113
 
G_M000_IG109:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG111
 
G_M000_IG110:
       jmp      G_M000_IG05
 
G_M000_IG111:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   xmm1, qword ptr [rax+0x08]
 
G_M000_IG112:
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rbp+0x80]
       vmovsd   qword ptr [rbp+0x80], xmm0
       xor      r13, r13
       mov      eax, ecx
       jmp      G_M000_IG18
 
G_M000_IG113:
       jmp      G_M000_IG05
 
G_M000_IG114:
       jmp      G_M000_IG05
 
G_M000_IG115:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       vmovsd   xmm0, qword ptr [rbp+0x80]
       vmovsd   qword ptr [r13+0x08], xmm0
       mov      rax, gword ptr [rbp+0xB0]
       jmp      G_M000_IG06
 
G_M000_IG116:
       mov      rsi, qword ptr [rdx+0x08]
       test     rax, rax
       jne      SHORT G_M000_IG118
 
G_M000_IG117:
       jmp      G_M000_IG10
 
G_M000_IG118:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG140
 
G_M000_IG119:
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
       je       G_M000_IG136
 
G_M000_IG120:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x58], xmm1
       lea      rdi, [rbp-0x58]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x170], rax
       mov      ecx, dword ptr [rbp-0x50]
       mov      dword ptr [rbp-0x120], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x78], xmm0
       lea      rdi, [rbp-0x78]
       mov      rsi, qword ptr [rbp-0x48]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x78]
       mov      r9d, dword ptr [rbp-0x70]
       mov      rcx, gword ptr [rbp-0x170]
       test     rcx, rcx
       jne      G_M000_IG129
 
G_M000_IG121:
       test     rax, rax
       jne      G_M000_IG222
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x88], xmm0
       movsxd   rsi, dword ptr [rbp-0x120]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x88]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x88]
       mov      r10d, dword ptr [rbp-0x80]
 
G_M000_IG122:
       test     rax, rax
       jne      G_M000_IG223
 
G_M000_IG123:
       test     rax, rax
       jne      G_M000_IG232
 
G_M000_IG124:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      G_M000_IG134
 
G_M000_IG125:
       mov      edi, -1
 
G_M000_IG126:
       test     edi, edi
       mov      gword ptr [rbp-0x178], rax
       mov      dword ptr [rbp-0x124], r10d
       jg       SHORT G_M000_IG135
 
G_M000_IG127:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x148], rax
       mov      rdi, gword ptr [rbp-0x178]
       mov      esi, dword ptr [rbp-0x124]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rdx, gword ptr [rbp-0x148]
       mov      qword ptr [rdx+0x08], rax
 
G_M000_IG128:
       mov      rcx, rdx
       mov      rax, rcx
       jmp      G_M000_IG11
 
G_M000_IG129:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG130:
       test     rax, rax
       je       SHORT G_M000_IG133
 
G_M000_IG131:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG132:
       mov      edx, dword ptr [rbp-0x120]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      r10d, ecx
       jmp      G_M000_IG122
 
G_M000_IG133:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG132
 
G_M000_IG134:
       xor      edi, edi
       jmp      G_M000_IG126
 
G_M000_IG135:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      gword ptr [rbp-0x140], rdx
       lea      rdi, bword ptr [rdx+0x08]
       mov      rsi, gword ptr [rbp-0x178]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdx, gword ptr [rbp-0x140]
       mov      edi, dword ptr [rbp-0x124]
       mov      dword ptr [rdx+0x10], edi
       jmp      SHORT G_M000_IG128
 
G_M000_IG136:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG139
 
G_M000_IG137:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x60]
       mov      qword ptr [rax+0x08], rdi
       mov      rdx, rax
 
G_M000_IG138:
       jmp      G_M000_IG128
 
G_M000_IG139:
       test     rax, rax
       jl       G_M000_IG256
       mov      rdx, rax
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       cmp      rdx, 262
       jae      G_M000_IG255
       mov      rdx, gword ptr [rax+8*rdx+0x10]
       jmp      SHORT G_M000_IG138
 
G_M000_IG140:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG10
 
G_M000_IG141:
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
       jmp      G_M000_IG11
 
G_M000_IG142:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm1
       test     rax, rax
       je       G_M000_IG10
 
G_M000_IG143:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG145
 
G_M000_IG144:
       vmovsd   xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG11
 
G_M000_IG145:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG10
 
G_M000_IG146:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG11
 
G_M000_IG147:
       mov      rsi, qword ptr [r13+0x08]
       test     rax, rax
       je       G_M000_IG15
 
G_M000_IG148:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG156
 
G_M000_IG149:
       mov      r13, qword ptr [rax+0x08]
       lea      rax, [rsi+r13]
       mov      qword ptr [rbp-0xB8], rax
       mov      rdi, rsi
       xor      rdi, rax
       mov      rcx, r13
       xor      rcx, rax
       and      rdi, rcx
       jge      G_M000_IG152
 
G_M000_IG150:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xC8], xmm0
       lea      rdi, [rbp-0xC8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xC8]
       mov      gword ptr [rbp-0x180], rax
       mov      ecx, dword ptr [rbp-0xC0]
       mov      dword ptr [rbp-0x128], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xD8], xmm0
       lea      rdi, [rbp-0xD8]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x180]
       mov      esi, dword ptr [rbp-0x128]
       mov      rdx, gword ptr [rbp-0xD8]
       mov      rcx, qword ptr [rbp-0xD0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG151:
       mov      r13, rax
       jmp      G_M000_IG16
 
G_M000_IG152:
       lea      r13, [rax+0x05]
       cmp      r13, 261
       jbe      SHORT G_M000_IG155
 
G_M000_IG153:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, qword ptr [rbp-0xB8]
       mov      qword ptr [rax+0x08], r13
 
G_M000_IG154:
       jmp      SHORT G_M000_IG151
 
G_M000_IG155:
       test     r13, r13
       jl       G_M000_IG256
       mov      rax, r13
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG255
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG154
 
G_M000_IG156:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG15
 
G_M000_IG157:
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
       jmp      G_M000_IG16
 
G_M000_IG158:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       test     rax, rax
       je       G_M000_IG15
 
G_M000_IG159:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG161
 
G_M000_IG160:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xA8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG16
 
G_M000_IG161:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG15
 
G_M000_IG162:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xE8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG16
 
G_M000_IG163:
       mov      rsi, r13
       mov      gword ptr [rbp-0x150], rsi
       test     rax, rax
       je       G_M000_IG15
 
G_M000_IG164:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG15
 
G_M000_IG165:
       mov      r13, rax
       cmp      dword ptr [rsi+0x08], 0
       je       G_M000_IG170
 
G_M000_IG166:
       cmp      dword ptr [r13+0x08], 0
       je       SHORT G_M000_IG169
 
G_M000_IG167:
       mov      eax, dword ptr [rsi+0x08]
       mov      dword ptr [rbp-0xEC], eax
       mov      edi, eax
       add      edi, dword ptr [r13+0x08]
       js       G_M000_IG251
       movsxd   rsi, edi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x158], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rsi, gword ptr [rbp-0x150]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0xEC]
       mov      rax, gword ptr [rbp-0x158]
       lea      rdi, bword ptr [rax+2*rdi+0x0C]
       mov      edx, dword ptr [r13+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r13+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      r13, gword ptr [rbp-0x158]
 
G_M000_IG168:
       jmp      G_M000_IG16
 
G_M000_IG169:
       mov      r13, rsi
       jmp      SHORT G_M000_IG168
 
G_M000_IG170:
       cmp      dword ptr [r13+0x08], 0
       je       G_M000_IG240
 
G_M000_IG171:
       jmp      SHORT G_M000_IG168
 
G_M000_IG172:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      SHORT G_M000_IG174
 
G_M000_IG173:
       mov      eax, 1
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp+0x80], xmm0
       jmp      G_M000_IG18
 
G_M000_IG174:
       xor      eax, eax
       jmp      G_M000_IG18
 
G_M000_IG175:
       test     r13, r13
       sete     dil
       movzx    rdi, dil
       test     eax, edi
       je       G_M000_IG189
 
G_M000_IG176:
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp+0x210]
       vmovsd   xmm0, qword ptr [rbp+0x80]
       call     [RyuPy.Runtime.PyNumericListOps:AppendF64(System.Object,double):System.Object]
 
G_M000_IG177:
       mov      r13, qword ptr [rbp+0x160]
       mov      r12, qword ptr [rbp+0x150]
       add      r13, r12
       jo       G_M000_IG256
 
G_M000_IG178:
       test     r12, r12
       setg     dil
       movzx    rdi, dil
       mov      rax, qword ptr [rbp+0x158]
       cmp      r13, rax
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       cmp      r13, rax
       setg     cl
       movzx    rcx, cl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, ecx
       or       edi, esi
       je       G_M000_IG214
 
G_M000_IG179:
       lea      rcx, [r13+0x05]
       cmp      rcx, 261
       jbe      G_M000_IG195
 
G_M000_IG180:
       mov      qword ptr [rbp+0x158], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rbp+0x160], r13
       mov      qword ptr [rax+0x08], r13
       mov      rsi, rax
 
G_M000_IG181:
       mov      gword ptr [rbp+0x208], rsi
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG184
 
G_M000_IG182:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rsi, rdx
       lea      r8d, [rdi-0x01]
       mov      r9d, r8d
       cmp      r9d, dword ptr [rsi+0x08]
       jae      G_M000_IG255
       cmp      dword ptr [rsi+4*r9+0x10], 21
       je       SHORT G_M000_IG184
 
G_M000_IG183:
       cmp      r8d, dword ptr [rdx+0x08]
       jae      G_M000_IG255
       mov      esi, r8d
       mov      dword ptr [rdx+4*rsi+0x10], 21
 
G_M000_IG184:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      gword ptr [rbp+0x200], rdx
       test     edi, edi
       jle      SHORT G_M000_IG187
 
G_M000_IG185:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r8, rsi
       dec      edi
       mov      r9d, edi
       cmp      r9d, dword ptr [r8+0x08]
       jae      G_M000_IG255
       cmp      dword ptr [r8+4*r9+0x10], 22
       je       SHORT G_M000_IG187
 
G_M000_IG186:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG255
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 22
 
G_M000_IG187:
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
       jne      G_M000_IG196
 
G_M000_IG188:
       xor      r8d, r8d
       mov      dword ptr [rbp+0x7C], r8d
       mov      qword ptr [rbp+0x150], r12
       vmovsd   qword ptr [rbp+0x80], xmm0
       mov      r12, rax
       mov      r13, rsi
       mov      eax, dword ptr [rbp+0x7C]
       mov      rcx, qword ptr [rbp+0xF0]
       jmp      G_M000_IG19
 
G_M000_IG189:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG192
 
G_M000_IG190:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       dec      edi
       mov      ecx, dword ptr [rax+0x08]
       cmp      edi, ecx
       jae      G_M000_IG255
       cmp      dword ptr [rax+4*rdi+0x10], 24
       je       SHORT G_M000_IG192
 
G_M000_IG191:
       mov      dword ptr [rsi+4*rdi+0x10], 24
 
G_M000_IG192:
       mov      rdi, 0xD1FFAB1E
       mov      r12, gword ptr [rdi]
       mov      rax, gword ptr [r12+0x60]
       test     rax, rax
       jne      G_M000_IG221
 
G_M000_IG193:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       lea      rdi, bword ptr [r12+0x60]
       mov      gword ptr [rbp-0x168], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG194:
       mov      rdi, gword ptr [rbp-0x168]
       mov      rdx, r13
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, gword ptr [rbp-0x168]
       mov      rsi, gword ptr [rbp+0x210]
       mov      edi, 10
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
       jmp      G_M000_IG177
 
G_M000_IG195:
       test     rcx, rcx
       jl       G_M000_IG256
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG255
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       mov      qword ptr [rbp+0x160], r13
       mov      qword ptr [rbp+0x158], rax
       jmp      G_M000_IG181
 
G_M000_IG196:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG198
 
G_M000_IG197:
       mov      r8d, 1
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp+0x80], xmm0
       mov      qword ptr [rbp+0x150], r12
       mov      r12, rax
       mov      eax, r8d
       mov      r13, rsi
       mov      rcx, qword ptr [rbp+0xF0]
       jmp      G_M000_IG19
 
G_M000_IG198:
       xor      r8d, r8d
       mov      qword ptr [rbp+0x150], r12
       vmovsd   qword ptr [rbp+0x80], xmm0
       mov      r12, rax
       mov      eax, r8d
       mov      r13, rsi
       mov      rcx, qword ptr [rbp+0xF0]
       jmp      G_M000_IG19
 
G_M000_IG199:
       test     rsi, rsi
       jl       G_M000_IG256
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG255
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       mov      dword ptr [rbp+0x7C], eax
       mov      qword ptr [rbp+0xF8], rdx
       mov      qword ptr [rbp+0xF0], rcx
       jmp      G_M000_IG22
 
G_M000_IG200:
       lea      rcx, [r13+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG202
 
G_M000_IG201:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r13
       jmp      SHORT G_M000_IG203
 
G_M000_IG202:
       test     rcx, rcx
       jl       G_M000_IG256
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG255
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
 
G_M000_IG203:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG206
 
G_M000_IG204:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      r8d, dword ptr [rdx+0x08]
       cmp      edi, r8d
       jae      G_M000_IG255
       cmp      dword ptr [rdx+4*rdi+0x10], 18
       je       SHORT G_M000_IG206
 
G_M000_IG205:
       mov      dword ptr [rcx+4*rdi+0x10], 18
 
G_M000_IG206:
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x218], rax
       mov      ecx, dword ptr [(reloc)]
       mov      dword ptr [rbp-0x12C], ecx
       test     ecx, ecx
       jle      G_M000_IG213
 
G_M000_IG207:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rdx, rdi
       lea      esi, [rcx-0x01]
       mov      r8d, dword ptr [rdx+0x08]
       cmp      esi, r8d
       jae      G_M000_IG255
       cmp      dword ptr [rdx+4*rsi+0x10], 19
       je       SHORT G_M000_IG209
 
G_M000_IG208:
       mov      dword ptr [rdi+4*rsi+0x10], 19
 
G_M000_IG209:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x138], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, 0xD1FFAB1E
       mov      gword ptr [rax+0x08], rdi
       mov      rdx, gword ptr [rbp-0x138]
       lea      rdi, bword ptr [rdx+0x08]
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdx, gword ptr [rbp-0x138]
       mov      gword ptr [rbp+0x210], rdx
       mov      esi, dword ptr [rbp-0x12C]
       test     esi, esi
       jle      SHORT G_M000_IG212
 
G_M000_IG210:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rcx, rdi
       dec      esi
       mov      edx, esi
       mov      r8d, dword ptr [rcx+0x08]
       cmp      edx, r8d
       jae      G_M000_IG255
       mov      esi, edx
       cmp      dword ptr [rcx+4*rsi+0x10], 20
       je       SHORT G_M000_IG212
 
G_M000_IG211:
       mov      dword ptr [rdi+4*rsi+0x10], 20
 
G_M000_IG212:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x170], rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rdi, qword ptr [rbp+0x170]
       mov      rcx, rdi
       mov      qword ptr [rbp+0x158], rax
       mov      edx, 1
       mov      qword ptr [rbp+0x1C8], r13
       mov      qword ptr [rbp+0x1B8], r12
       mov      r12, rdx
       mov      r13, rcx
       jmp      G_M000_IG178
 
G_M000_IG213:
       jmp      G_M000_IG209
 
G_M000_IG214:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG217
 
G_M000_IG215:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       dec      edi
       mov      ecx, dword ptr [rax+0x08]
       cmp      edi, ecx
       jae      G_M000_IG255
       cmp      dword ptr [rax+4*rdi+0x10], 25
       je       SHORT G_M000_IG217
 
G_M000_IG216:
       mov      dword ptr [rsi+4*rdi+0x10], 25
 
G_M000_IG217:
       mov      rdi, 0xD1FFAB1E
       mov      r12, gword ptr [rdi]
       mov      r13, gword ptr [r12+0x48]
       test     r13, r13
       je       SHORT G_M000_IG219
 
G_M000_IG218:
       cmp      dword ptr [r13+0x08], 1
       je       SHORT G_M000_IG220
 
G_M000_IG219:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r13, rax
       lea      rdi, bword ptr [r12+0x48]
       mov      rsi, r13
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG220:
       mov      rdi, r13
       mov      rdx, gword ptr [rbp+0x210]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, r13
       mov      rsi, gword ptr [rbp+0x228]
       mov      edi, 7
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
       mov      r13, qword ptr [rbp+0x1C8]
       mov      r12, qword ptr [rbp+0x1B8]
       add      r13, r12
       jo       G_M000_IG256
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
       je       G_M000_IG243
       jmp      G_M000_IG200
 
G_M000_IG221:
       cmp      dword ptr [rax+0x08], 1
       mov      gword ptr [rbp-0x168], rax
       je       G_M000_IG194
       jmp      G_M000_IG193
 
G_M000_IG222:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG130
 
G_M000_IG223:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG224
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG225
 
G_M000_IG224:
       mov      ecx, r10d
       jmp      SHORT G_M000_IG231
 
G_M000_IG225:
       cmp      edi, 2
       je       SHORT G_M000_IG226
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG255
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG227
 
G_M000_IG226:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [rax+0x10]
       or       rdi, rcx
 
G_M000_IG227:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG228
       mov      ecx, -1
       jmp      SHORT G_M000_IG230
 
G_M000_IG228:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG229
       mov      ecx, 1
       jmp      SHORT G_M000_IG230
 
G_M000_IG229:
       xor      ecx, ecx
 
G_M000_IG230:
       imul     ecx, r10d
 
G_M000_IG231:
       test     ecx, ecx
       jge      G_M000_IG123
       mov      gword ptr [rbp-0x178], rax
       mov      dword ptr [rbp-0x124], r10d
       jmp      G_M000_IG135
 
G_M000_IG232:
       movsxd   rdi, r10d
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG233
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG234
 
G_M000_IG233:
       mov      dword ptr [rbp-0x124], r10d
       mov      edi, r10d
       mov      r10d, dword ptr [rbp-0x124]
       jmp      G_M000_IG126
 
G_M000_IG234:
       cmp      edi, 2
       je       SHORT G_M000_IG235
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG255
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG236
 
G_M000_IG235:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      gword ptr [rbp-0x178], rax
       mov      ecx, dword ptr [rax+0x10]
       or       rdi, rcx
       mov      rax, gword ptr [rbp-0x178]
 
G_M000_IG236:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG237
       mov      ecx, -1
       jmp      SHORT G_M000_IG239
 
G_M000_IG237:
       test     rdi, rdi
       jge      SHORT G_M000_IG238
       mov      ecx, 1
       jmp      SHORT G_M000_IG239
 
G_M000_IG238:
       xor      ecx, ecx
 
G_M000_IG239:
       mov      edi, r10d
       imul     edi, ecx
       jmp      G_M000_IG126
 
G_M000_IG240:
       mov      r13, 0xD1FFAB1E
       jmp      G_M000_IG168
 
G_M000_IG241:
       mov      rcx, gword ptr [rbp+0x1F8]
       mov      rdx, gword ptr [rbp+0x218]
       jmp      G_M000_IG04
 
G_M000_IG242:
       mov      rcx, rax
       jmp      G_M000_IG78
 
G_M000_IG243:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG246
 
G_M000_IG244:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      ecx, dword ptr [(reloc)]
       dec      ecx
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG255
       cmp      dword ptr [rax+4*rcx+0x10], 26
       je       SHORT G_M000_IG246
 
G_M000_IG245:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      ecx, dword ptr [(reloc)]
       dec      ecx
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG255
       mov      dword ptr [rax+4*rcx+0x10], 26
 
G_M000_IG246:
       mov      rbx, qword ptr [rbp+0x1F0]
       test     rbx, rbx
       jle      SHORT G_M000_IG249
 
G_M000_IG247:
       movsxd   rax, dword ptr [(reloc)]
       cmp      rbx, rax
       jg       SHORT G_M000_IG249
 
G_M000_IG248:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG249:
       mov      rax, gword ptr [rbp+0x228]
 
G_M000_IG250:
       add      rsp, 968
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG251:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG252:
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
 
G_M000_IG253:
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
 
G_M000_IG254:
       call     [System.ThrowHelper:ThrowArgumentOutOfRange_IndexMustBeLessException()]
       int3     
 
G_M000_IG255:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG256:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 5970

; Assembly listing for method RyuPy.Generated.Module:m0_matmul(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x31c
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 37 inlinees with PGO data; 60 single block inlinees; 81 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 304
       mov      qword ptr [rsp+0x398], r15
       mov      qword ptr [rsp+0x390], r14
       mov      qword ptr [rsp+0x388], r13
       mov      qword ptr [rsp+0x380], r12
       mov      qword ptr [rsp+0x378], rbx
       lea      rbp, [rsp+0x130]
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
       jmp      G_M000_IG133
 
G_M000_IG03:
       call     [System.ThrowHelper:ThrowArgumentOutOfRange_IndexMustBeLessException()]
       int3     
 
G_M000_IG04:
       test     r14, r14
       sete     dil
       movzx    rdi, dil
       test     r12d, edi
       je       G_M000_IG18
 
G_M000_IG05:
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp+0x210]
       vmovsd   xmm0, qword ptr [rbp+0x80]
       call     [RyuPy.Runtime.PyNumericListOps:AppendF64(System.Object,double):System.Object]
 
G_M000_IG06:
       mov      r15, qword ptr [rbp+0x160]
       mov      r14, qword ptr [rbp+0x150]
       add      r15, r14
       jo       G_M000_IG245
 
G_M000_IG07:
       test     r14, r14
       setg     dil
       movzx    rdi, dil
       mov      r12, qword ptr [rbp+0x158]
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
       je       G_M000_IG25
 
G_M000_IG08:
       lea      rax, [r15+0x05]
       cmp      rax, 261
       jbe      G_M000_IG46
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rbp+0x160], r15
       mov      qword ptr [rax+0x08], r15
       mov      rsi, rax
 
G_M000_IG10:
       mov      gword ptr [rbp+0x208], rsi
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG13
 
G_M000_IG11:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       lea      esi, [rdi-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      G_M000_IG244
       cmp      dword ptr [rdx+4*r8+0x10], 21
       je       SHORT G_M000_IG13
 
G_M000_IG12:
       cmp      esi, dword ptr [rcx+0x08]
       jae      G_M000_IG244
       mov      edx, esi
       mov      dword ptr [rcx+4*rdx+0x10], 21
 
G_M000_IG13:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      gword ptr [rbp+0x200], rcx
       test     edi, edi
       jle      SHORT G_M000_IG16
 
G_M000_IG14:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rsi, rdx
       dec      edi
       mov      r8d, edi
       cmp      r8d, dword ptr [rsi+0x08]
       jae      G_M000_IG244
       cmp      dword ptr [rsi+4*r8+0x10], 22
       je       SHORT G_M000_IG16
 
G_M000_IG15:
       cmp      edi, dword ptr [rdx+0x08]
       jae      G_M000_IG244
       mov      edi, edi
       mov      dword ptr [rdx+4*rdi+0x10], 22
 
G_M000_IG16:
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
       jne      G_M000_IG47
 
G_M000_IG17:
       xor      r8d, r8d
       mov      qword ptr [rbp+0x158], r12
       mov      qword ptr [rbp+0x150], r14
       vmovsd   qword ptr [rbp+0x80], xmm0
       mov      r12d, r8d
       mov      r13, rax
       mov      r14, rsi
       mov      rcx, qword ptr [rbp+0xF8]
       mov      rdx, qword ptr [rbp+0xF0]
       mov      rsi, gword ptr [rbp+0x208]
       jmp      G_M000_IG138
 
G_M000_IG18:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG21
 
G_M000_IG19:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rcx, rsi
       dec      edi
       mov      edx, edi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG244
       cmp      dword ptr [rcx+4*rdx+0x10], 24
       je       SHORT G_M000_IG21
 
G_M000_IG20:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG244
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 24
 
G_M000_IG21:
       mov      rdi, 0xD1FFAB1E
       mov      r13, gword ptr [rdi]
       mov      r12, gword ptr [r13+0x60]
       test     r12, r12
       je       SHORT G_M000_IG23
 
G_M000_IG22:
       cmp      dword ptr [r12+0x08], 1
       je       SHORT G_M000_IG24
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r12, rax
       lea      rdi, bword ptr [r13+0x60]
       mov      rsi, r12
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG24:
       mov      rdi, r12
       mov      rdx, r14
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, r12
       mov      rsi, gword ptr [rbp+0x210]
       mov      edi, 10
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
       jmp      G_M000_IG06
 
G_M000_IG25:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG28
 
G_M000_IG26:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       dec      edi
       mov      ecx, edi
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG244
       cmp      dword ptr [rax+4*rcx+0x10], 25
       je       SHORT G_M000_IG28
 
G_M000_IG27:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG244
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 25
 
G_M000_IG28:
       mov      rdi, 0xD1FFAB1E
       mov      r13, gword ptr [rdi]
       mov      r15, gword ptr [r13+0x48]
       test     r15, r15
       je       SHORT G_M000_IG30
 
G_M000_IG29:
       cmp      dword ptr [r15+0x08], 1
       je       SHORT G_M000_IG31
 
G_M000_IG30:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r15, rax
       lea      rdi, bword ptr [r13+0x48]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG31:
       mov      rdi, r15
       mov      rdx, gword ptr [rbp+0x210]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, r15
       mov      rsi, gword ptr [rbp+0x228]
       mov      edi, 7
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
       mov      r15, qword ptr [rbp+0x1C8]
       mov      r13, qword ptr [rbp+0x1B8]
       add      r15, r13
       jo       G_M000_IG245
       test     r13, r13
       setg     dil
       movzx    rdi, dil
       mov      r14, qword ptr [rbp+0x1C0]
       cmp      r15, r14
       setl     al
       movzx    rax, al
       and      eax, edi
       cmp      r15, r14
       setg     cl
       movzx    rcx, cl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, ecx
       or       edi, eax
       je       G_M000_IG233
 
G_M000_IG32:
       lea      r12, [r15+0x05]
       cmp      r12, 261
       jbe      G_M000_IG44
 
G_M000_IG33:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r15
 
G_M000_IG34:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG37
 
G_M000_IG35:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rcx, rax
       dec      edi
       mov      edx, edi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG244
       cmp      dword ptr [rcx+4*rdx+0x10], 18
       je       SHORT G_M000_IG37
 
G_M000_IG36:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG244
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 18
 
G_M000_IG37:
       mov      rdi, gword ptr [rbp+0x240]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x218], rax
       mov      eax, dword ptr [(reloc)]
       mov      dword ptr [rbp-0x104], eax
       test     eax, eax
       jle      G_M000_IG45
 
G_M000_IG38:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rcx, rdi
       lea      edx, [rax-0x01]
       mov      esi, edx
       cmp      esi, dword ptr [rcx+0x08]
       jae      G_M000_IG244
       cmp      dword ptr [rcx+4*rsi+0x10], 19
       je       SHORT G_M000_IG40
 
G_M000_IG39:
       cmp      edx, dword ptr [rdi+0x08]
       jae      G_M000_IG244
       mov      ecx, edx
       mov      dword ptr [rdi+4*rcx+0x10], 19
 
G_M000_IG40:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x110], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, 0xD1FFAB1E
       mov      gword ptr [rax+0x08], rdi
       mov      rdx, gword ptr [rbp-0x110]
       lea      rdi, bword ptr [rdx+0x08]
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdx, gword ptr [rbp-0x110]
       mov      gword ptr [rbp+0x210], rdx
       mov      edx, dword ptr [rbp-0x104]
       test     edx, edx
       jle      SHORT G_M000_IG43
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rcx, rdi
       dec      edx
       mov      esi, edx
       cmp      esi, dword ptr [rcx+0x08]
       jae      G_M000_IG244
       cmp      dword ptr [rcx+4*rsi+0x10], 20
       je       SHORT G_M000_IG43
 
G_M000_IG42:
       mov      ecx, edx
       cmp      ecx, dword ptr [rdi+0x08]
       jae      G_M000_IG244
       mov      dword ptr [rdi+4*rcx+0x10], 20
 
G_M000_IG43:
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
       mov      qword ptr [rbp+0x1C0], r14
       mov      qword ptr [rbp+0x1B8], r13
       mov      r15, rcx
       mov      r14, qword ptr [rbp+0x150]
       jmp      G_M000_IG07
 
G_M000_IG44:
       test     r12, r12
       jl       G_M000_IG245
       mov      rsi, r12
       mov      rdi, 0xD1FFAB1E
       mov      r12, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG244
       mov      rsi, gword ptr [r12+8*rsi+0x10]
       jmp      G_M000_IG34
 
G_M000_IG45:
       jmp      G_M000_IG40
 
G_M000_IG46:
       test     rax, rax
       jl       G_M000_IG245
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG244
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       mov      qword ptr [rbp+0x160], r15
       jmp      G_M000_IG10
 
G_M000_IG47:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG49
 
G_M000_IG48:
       mov      r8d, 1
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp+0x80], xmm0
       mov      qword ptr [rbp+0x158], r12
       mov      qword ptr [rbp+0x150], r14
       mov      r12d, r8d
       mov      r13, rax
       mov      r14, rsi
       mov      rcx, qword ptr [rbp+0xF8]
       mov      rdx, qword ptr [rbp+0xF0]
       mov      rsi, gword ptr [rbp+0x208]
       jmp      G_M000_IG138
 
G_M000_IG49:
       xor      r8d, r8d
       mov      qword ptr [rbp+0x158], r12
       mov      qword ptr [rbp+0x150], r14
       vmovsd   qword ptr [rbp+0x80], xmm0
       mov      r12d, r8d
       mov      r13, rax
       mov      r14, rsi
       mov      rcx, qword ptr [rbp+0xF8]
       mov      rdx, qword ptr [rbp+0xF0]
       mov      rsi, gword ptr [rbp+0x208]
       jmp      G_M000_IG138
 
G_M000_IG50:
       test     r15, r15
       jl       G_M000_IG245
       mov      rdi, r15
       mov      r11, 0xD1FFAB1E
       mov      r11, gword ptr [r11]
       cmp      rdi, 262
       jae      G_M000_IG244
       mov      r15, gword ptr [r11+8*rdi+0x10]
       mov      qword ptr [rbp+0xF8], rcx
       mov      qword ptr [rbp+0xF0], rdx
       mov      gword ptr [rbp+0x208], rsi
       jmp      G_M000_IG141
 
G_M000_IG51:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG146
 
G_M000_IG52:
       test     r15, r15
       je       G_M000_IG146
 
G_M000_IG53:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG146
 
G_M000_IG54:
       mov      r11, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0xF8], r11
       mov      gword ptr [rbp+0x218], rax
       mov      rsi, gword ptr [rax+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       jne      SHORT G_M000_IG63
 
G_M000_IG55:
       mov      rcx, gword ptr [rbp+0x218]
       mov      rsi, gword ptr [rcx+0x08]
       mov      rax, rsi
       test     rax, rax
       je       SHORT G_M000_IG58
 
G_M000_IG56:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG58
 
G_M000_IG57:
       call     [CORINFO_HELP_CHKCASTCLASS_SPECIAL]
 
G_M000_IG58:
       mov      esi, dword ptr [rax+0x10]
 
G_M000_IG59:
       mov      rax, qword ptr [rbp-0xF8]
       test     rax, rax
       jl       SHORT G_M000_IG64
 
G_M000_IG60:
       mov      rdx, rax
 
G_M000_IG61:
       movsxd   rsi, esi
       cmp      rdx, rsi
       jb       SHORT G_M000_IG65
 
G_M000_IG62:
       mov      rax, gword ptr [rbp+0x218]
       jmp      G_M000_IG146
 
G_M000_IG63:
       mov      esi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG59
 
G_M000_IG64:
       movsxd   rdx, esi
       add      rdx, rax
       mov      rax, rdx
       jmp      SHORT G_M000_IG61
 
G_M000_IG65:
       mov      dword ptr [rbp-0xFC], edx
       mov      rcx, gword ptr [rbp+0x218]
       mov      rsi, gword ptr [rcx+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       jne      G_M000_IG90
 
G_M000_IG66:
       mov      rcx, gword ptr [rbp+0x218]
       mov      rsi, gword ptr [rcx+0x08]
       mov      rax, rsi
       test     rax, rax
       mov      rcx, gword ptr [rbp+0x218]
       je       SHORT G_M000_IG69
 
G_M000_IG67:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG69
 
G_M000_IG68:
       mov      gword ptr [rbp+0x218], rcx
       call     [CORINFO_HELP_CHKCASTCLASS_SPECIAL]
       mov      rcx, gword ptr [rbp+0x218]
 
G_M000_IG69:
       mov      esi, dword ptr [rbp-0xFC]
       cmp      esi, dword ptr [rax+0x10]
       jae      G_M000_IG03
 
G_M000_IG70:
       mov      rdi, gword ptr [rax+0x08]
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG244
       mov      esi, esi
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       test     rsi, rsi
       je       SHORT G_M000_IG72
 
G_M000_IG71:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       G_M000_IG91
 
G_M000_IG72:
       xor      esi, esi
 
G_M000_IG73:
       test     esi, esi
       je       G_M000_IG127
 
G_M000_IG74:
       test     rbx, rbx
       je       G_M000_IG85
 
G_M000_IG75:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      SHORT G_M000_IG85
 
G_M000_IG76:
       mov      gword ptr [rbp+0x218], rcx
       mov      rax, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0xE8], rax
       mov      rsi, gword ptr [rbx+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       jne      G_M000_IG92
 
G_M000_IG77:
       mov      rsi, gword ptr [rbx+0x08]
       mov      rax, rsi
       test     rax, rax
       je       SHORT G_M000_IG80
 
G_M000_IG78:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG80
 
G_M000_IG79:
       call     [CORINFO_HELP_CHKCASTCLASS_SPECIAL]
 
G_M000_IG80:
       mov      esi, dword ptr [rax+0x10]
 
G_M000_IG81:
       mov      rax, qword ptr [rbp-0xE8]
       test     rax, rax
       jl       G_M000_IG93
 
G_M000_IG82:
       mov      rcx, rax
 
G_M000_IG83:
       movsxd   rsi, esi
       cmp      rcx, rsi
       jb       G_M000_IG94
 
G_M000_IG84:
       mov      rcx, gword ptr [rbp+0x218]
 
G_M000_IG85:
       xor      rax, rax
 
G_M000_IG86:
       test     rax, rax
       je       G_M000_IG126
 
G_M000_IG87:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG100
 
G_M000_IG88:
       mov      rdx, gword ptr [rbp+0x208]
       test     rdx, rdx
       jne      G_M000_IG101
 
G_M000_IG89:
       mov      gword ptr [rbp+0x208], rdx
       mov      rax, rcx
       jmp      G_M000_IG146
 
G_M000_IG90:
       mov      rcx, gword ptr [rbp+0x218]
       mov      esi, dword ptr [rbp-0xFC]
       cmp      esi, dword ptr [rax+0x10]
       jae      G_M000_IG03
       mov      rdi, gword ptr [rax+0x08]
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG244
       shl      rsi, 4
       mov      rsi, gword ptr [rdi+rsi+0x10]
       test     rsi, rsi
       je       G_M000_IG243
 
G_M000_IG91:
       mov      esi, 1
       jmp      G_M000_IG73
 
G_M000_IG92:
       mov      esi, dword ptr [rax+0x10]
       jmp      G_M000_IG81
 
G_M000_IG93:
       movsxd   rcx, esi
       add      rcx, rax
       mov      rax, rcx
       jmp      G_M000_IG83
 
G_M000_IG94:
       mov      dword ptr [rbp-0xEC], ecx
       mov      rsi, gword ptr [rbx+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       jne      SHORT G_M000_IG97
 
G_M000_IG95:
       xor      rcx, rcx
 
G_M000_IG96:
       mov      rax, rcx
       mov      rcx, gword ptr [rbp+0x218]
       jmp      G_M000_IG86
 
G_M000_IG97:
       mov      esi, dword ptr [rbp-0xEC]
       cmp      esi, dword ptr [rax+0x10]
       jae      G_M000_IG03
       mov      rdi, gword ptr [rax+0x08]
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG244
       mov      rax, gword ptr [rdi+8*rsi+0x10]
       test     rax, rax
       je       SHORT G_M000_IG95
 
G_M000_IG98:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      SHORT G_M000_IG95
 
G_M000_IG99:
       mov      rcx, rax
       jmp      SHORT G_M000_IG96
 
G_M000_IG100:
       mov      rax, rcx
       jmp      G_M000_IG146
 
G_M000_IG101:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG125
 
G_M000_IG102:
       mov      gword ptr [rbp+0x218], rcx
       mov      gword ptr [rbp+0x208], rdx
       mov      r8, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0xD8], r8
       mov      gword ptr [rbp-0x130], rax
       mov      rsi, gword ptr [rax+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       jne      SHORT G_M000_IG111
 
G_M000_IG103:
       mov      rcx, gword ptr [rbp-0x130]
       mov      rsi, gword ptr [rcx+0x08]
       mov      rax, rsi
       test     rax, rax
       je       SHORT G_M000_IG106
 
G_M000_IG104:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG106
 
G_M000_IG105:
       call     [CORINFO_HELP_CHKCASTCLASS_SPECIAL]
 
G_M000_IG106:
       mov      esi, dword ptr [rax+0x10]
 
G_M000_IG107:
       mov      rax, qword ptr [rbp-0xD8]
       test     rax, rax
       jl       SHORT G_M000_IG112
 
G_M000_IG108:
       mov      rdx, rax
 
G_M000_IG109:
       movsxd   rsi, esi
       cmp      rdx, rsi
       jb       SHORT G_M000_IG113
 
G_M000_IG110:
       mov      rax, gword ptr [rbp+0x218]
       jmp      G_M000_IG146
 
G_M000_IG111:
       mov      esi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG107
 
G_M000_IG112:
       movsxd   rdx, esi
       add      rdx, rax
       mov      rax, rdx
       jmp      SHORT G_M000_IG109
 
G_M000_IG113:
       mov      dword ptr [rbp-0xDC], edx
       mov      rcx, gword ptr [rbp-0x130]
       mov      rsi, gword ptr [rcx+0x08]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       jne      G_M000_IG123
 
G_M000_IG114:
       mov      rcx, gword ptr [rbp-0x130]
       mov      rsi, gword ptr [rcx+0x08]
       mov      rax, rsi
       test     rax, rax
       je       SHORT G_M000_IG117
 
G_M000_IG115:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG117
 
G_M000_IG116:
       call     [CORINFO_HELP_CHKCASTCLASS_SPECIAL]
 
G_M000_IG117:
       mov      edi, dword ptr [rbp-0xDC]
       cmp      edi, dword ptr [rax+0x10]
       jae      G_M000_IG03
       mov      rsi, gword ptr [rax+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG244
       mov      rdi, gword ptr [rsi+8*rdi+0x10]
       test     rdi, rdi
       je       SHORT G_M000_IG119
 
G_M000_IG118:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       G_M000_IG124
 
G_M000_IG119:
       xor      edi, edi
 
G_M000_IG120:
       test     edi, edi
       mov      rax, gword ptr [rbp+0x218]
       je       G_M000_IG146
 
G_M000_IG121:
       mov      r14, gword ptr [rbp-0x130]
       mov      gword ptr [rbp+0x218], rax
       mov      rdi, rax
       mov      rsi, r15
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovsd   qword ptr [rbp+0x60], xmm0
       mov      rdi, r14
       mov      rsi, gword ptr [rbp+0x208]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovaps  xmm2, xmm0
       mov      rcx, qword ptr [rbp+0xF8]
       mov      rdx, qword ptr [rbp+0xF0]
       mov      rsi, gword ptr [rbp+0x208]
       vmovsd   xmm0, qword ptr [rbp+0x80]
       vmovsd   xmm1, qword ptr [rbp+0x60]
       jmp      G_M000_IG133
 
G_M000_IG122:
       xor      r12d, r12d
       mov      rcx, qword ptr [rbp+0xF8]
       mov      rdx, qword ptr [rbp+0xF0]
       mov      rsi, gword ptr [rbp+0x208]
       jmp      G_M000_IG137
 
G_M000_IG123:
       mov      edi, dword ptr [rbp-0xDC]
       cmp      edi, dword ptr [rax+0x10]
       jae      G_M000_IG03
       mov      rsi, gword ptr [rax+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG244
       shl      rdi, 4
       mov      rdi, gword ptr [rsi+rdi+0x10]
       test     rdi, rdi
       je       G_M000_IG242
 
G_M000_IG124:
       mov      edi, 1
       jmp      G_M000_IG120
 
G_M000_IG125:
       mov      gword ptr [rbp+0x208], rdx
       mov      rax, rcx
       jmp      G_M000_IG146
 
G_M000_IG126:
       mov      rax, rcx
       jmp      G_M000_IG146
 
G_M000_IG127:
       mov      rax, rcx
       jmp      G_M000_IG146
 
G_M000_IG128:
       mov      rax, gword ptr [rbp+0x218]
       jmp      G_M000_IG146
 
G_M000_IG129:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG148
 
G_M000_IG130:
       test     r15, r15
       je       G_M000_IG148
 
G_M000_IG131:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG148
 
G_M000_IG132:
       mov      r14, gword ptr [rbp+0xC0]
       vmovsd   xmm1, qword ptr [r14+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       mov      rcx, qword ptr [rbp+0xF8]
       mov      rdx, qword ptr [rbp+0xF0]
       mov      rsi, gword ptr [rbp+0x208]
       vmovsd   xmm0, qword ptr [rbp+0x80]
 
G_M000_IG133:
       vmulsd   xmm1, xmm1, xmm2
       vaddsd   xmm0, xmm1, xmm0
       vmovsd   qword ptr [rbp+0x80], xmm0
       xor      r14, r14
       jmp      SHORT G_M000_IG137
 
G_M000_IG134:
       mov      rdx, r12
       mov      rsi, r14
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r14, rax
 
G_M000_IG135:
       test     r14, r14
       jne      G_M000_IG167
 
G_M000_IG136:
       xor      r12d, r12d
       mov      rcx, qword ptr [rbp+0xF8]
       mov      rdx, qword ptr [rbp+0xF0]
       mov      rsi, gword ptr [rbp+0x208]
 
G_M000_IG137:
       add      r13, rdx
       jo       G_M000_IG245
 
G_M000_IG138:
       test     rdx, rdx
       setg     dil
       movzx    rdi, dil
       cmp      r13, rcx
       setl     r11b
       movzx    r11, r11b
       and      r11d, edi
       cmp      r13, rcx
       setg     r15b
       movzx    r15, r15b
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, r15d
       or       edi, r11d
       je       G_M000_IG04
 
G_M000_IG139:
       lea      r15, [r13+0x05]
       cmp      r15, 261
       jbe      G_M000_IG50
 
G_M000_IG140:
       mov      qword ptr [rbp+0xF8], rcx
       mov      qword ptr [rbp+0xF0], rdx
       mov      gword ptr [rbp+0x208], rsi
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      qword ptr [r15+0x08], r13
 
G_M000_IG141:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG144
 
G_M000_IG142:
       mov      r11, 0xD1FFAB1E
       mov      r11, gword ptr [r11]
       mov      r10, r11
       dec      edi
       mov      r9d, edi
       cmp      r9d, dword ptr [r10+0x08]
       jae      G_M000_IG244
       cmp      dword ptr [r10+4*r9+0x10], 23
       je       SHORT G_M000_IG144
 
G_M000_IG143:
       cmp      edi, dword ptr [r11+0x08]
       jae      G_M000_IG244
       mov      edi, edi
       mov      dword ptr [r11+4*rdi+0x10], 23
 
G_M000_IG144:
       test     r12d, r12d
       je       G_M000_IG128
 
G_M000_IG145:
       mov      rax, gword ptr [rbp+0x218]
       test     rax, rax
       jne      G_M000_IG51
 
G_M000_IG146:
       mov      gword ptr [rbp+0x218], rax
       mov      rdi, rax
       mov      rsi, r15
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0xC0], rax
       mov      rdi, rbx
       mov      rsi, r15
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdi, rax
       mov      rsi, gword ptr [rbp+0x208]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      r15, rax
       test     r12d, r12d
       je       SHORT G_M000_IG148
 
G_M000_IG147:
       mov      rax, gword ptr [rbp+0xC0]
       test     rax, rax
       jne      G_M000_IG129
 
G_M000_IG148:
       test     r14, r14
       sete     dil
       movzx    rdi, dil
       test     r12d, edi
       jne      G_M000_IG169
 
G_M000_IG149:
       mov      rax, gword ptr [rbp+0xC0]
       test     rax, rax
       je       SHORT G_M000_IG152
 
G_M000_IG150:
       mov      rcx, qword ptr [rax]
       mov      rdx, 0xD1FFAB1E
       cmp      rcx, rdx
       je       G_M000_IG170
 
G_M000_IG151:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       je       G_M000_IG194
 
G_M000_IG152:
       mov      rdx, r15
       mov      rsi, rax
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      r12, rax
 
G_M000_IG153:
       test     r14, r14
       je       G_M000_IG134
 
G_M000_IG154:
       mov      r15, qword ptr [r14]
       mov      rdi, 0xD1FFAB1E
       cmp      r15, rdi
       je       G_M000_IG199
 
G_M000_IG155:
       mov      rdi, 0xD1FFAB1E
       cmp      r15, rdi
       je       G_M000_IG210
 
G_M000_IG156:
       mov      rsi, 0xD1FFAB1E
       cmp      r15, rsi
       jne      G_M000_IG134
 
G_M000_IG157:
       mov      r15, r14
       test     r12, r12
       je       G_M000_IG134
 
G_M000_IG158:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r12], rsi
       jne      G_M000_IG134
 
G_M000_IG159:
       cmp      dword ptr [r15+0x08], 0
       je       SHORT G_M000_IG164
 
G_M000_IG160:
       cmp      dword ptr [r12+0x08], 0
       je       SHORT G_M000_IG163
 
G_M000_IG161:
       mov      r14d, dword ptr [r15+0x08]
       mov      esi, r14d
       add      esi, dword ptr [r12+0x08]
       js       G_M000_IG241
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x128], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [r15+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r15+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, r14d
       mov      r14, gword ptr [rbp-0x128]
       lea      rdi, bword ptr [r14+2*rdi+0x0C]
       mov      edx, dword ptr [r12+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r12+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       nop      
 
G_M000_IG162:
       jmp      G_M000_IG135
 
G_M000_IG163:
       mov      r14, r15
       jmp      SHORT G_M000_IG162
 
G_M000_IG164:
       cmp      dword ptr [r12+0x08], 0
       je       SHORT G_M000_IG166
 
G_M000_IG165:
       mov      r14, r12
       jmp      SHORT G_M000_IG162
 
G_M000_IG166:
       mov      r14, 0xD1FFAB1E
       jmp      SHORT G_M000_IG162
 
G_M000_IG167:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG122
 
G_M000_IG168:
       mov      r12d, 1
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp+0x80], xmm0
       mov      rcx, qword ptr [rbp+0xF8]
       mov      rdx, qword ptr [rbp+0xF0]
       mov      rsi, gword ptr [rbp+0x208]
       jmp      G_M000_IG137
 
G_M000_IG169:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       vmovsd   xmm0, qword ptr [rbp+0x80]
       vmovsd   qword ptr [r14+0x08], xmm0
       jmp      G_M000_IG149
 
G_M000_IG170:
       mov      rsi, qword ptr [rax+0x08]
       test     r15, r15
       je       G_M000_IG152
 
G_M000_IG171:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [r15], rdx
       jne      G_M000_IG192
 
G_M000_IG172:
       mov      r15, qword ptr [r15+0x08]
       lea      rdi, [rbp-0x58]
       mov      rdx, rsi
       mulx     rcx, rax, r15
       mov      qword ptr [rdi], rax
       mov      r12, qword ptr [rbp-0x58]
       mov      rdi, rsi
       sar      rdi, 63
       and      rdi, r15
       sub      rcx, rdi
       mov      rdi, r15
       sar      rdi, 63
       and      rdi, rsi
       sub      rcx, rdi
       mov      rdi, r12
       sar      rdi, 63
       cmp      rcx, rdi
       je       G_M000_IG188
 
G_M000_IG173:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x50], xmm1
       lea      rdi, [rbp-0x50]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0x50]
       mov      eax, dword ptr [rbp-0x48]
       mov      dword ptr [rbp-0x100], eax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x68], xmm0
       lea      rdi, [rbp-0x68]
       mov      rsi, r15
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r15, gword ptr [rbp-0x68]
       mov      r9d, dword ptr [rbp-0x60]
       test     r12, r12
       jne      G_M000_IG181
 
G_M000_IG174:
       test     r15, r15
       jne      G_M000_IG215
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x78], xmm0
       movsxd   rsi, dword ptr [rbp-0x100]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x78]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r15, gword ptr [rbp-0x78]
       mov      r12d, dword ptr [rbp-0x70]
 
G_M000_IG175:
       test     r15, r15
       jne      G_M000_IG216
 
G_M000_IG176:
       test     r15, r15
       jne      G_M000_IG225
       movsxd   rdi, r12d
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jge      SHORT G_M000_IG186
 
G_M000_IG177:
       mov      edi, -1
 
G_M000_IG178:
       test     edi, edi
       jg       SHORT G_M000_IG187
 
G_M000_IG179:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x120], rax
       mov      rdi, r15
       mov      esi, r12d
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r12, gword ptr [rbp-0x120]
       mov      qword ptr [r12+0x08], rax
 
G_M000_IG180:
       jmp      G_M000_IG153
 
G_M000_IG181:
       lea      rdi, bword ptr [r12+0x10]
       mov      esi, dword ptr [r12+0x08]
 
G_M000_IG182:
       test     r15, r15
       je       SHORT G_M000_IG185
 
G_M000_IG183:
       lea      rcx, bword ptr [r15+0x10]
       mov      r8d, dword ptr [r15+0x08]
 
G_M000_IG184:
       mov      edx, dword ptr [rbp-0x100]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      r15, rax
       mov      r12d, edx
       jmp      G_M000_IG175
 
G_M000_IG185:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG184
 
G_M000_IG186:
       xor      edi, edi
       jmp      SHORT G_M000_IG178
 
G_M000_IG187:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x118], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
       mov      r15, gword ptr [rbp-0x118]
       mov      dword ptr [r15+0x10], r12d
       mov      r12, r15
       jmp      SHORT G_M000_IG180
 
G_M000_IG188:
       lea      r15, [r12+0x05]
       cmp      r15, 261
       jbe      SHORT G_M000_IG191
 
G_M000_IG189:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
       mov      r12, rax
 
G_M000_IG190:
       jmp      G_M000_IG180
 
G_M000_IG191:
       test     r15, r15
       jl       G_M000_IG245
       mov      rdi, r15
       mov      rax, 0xD1FFAB1E
       mov      r11, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG244
       mov      r12, gword ptr [r11+8*rdi+0x10]
       jmp      SHORT G_M000_IG190
 
G_M000_IG192:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG152
 
G_M000_IG193:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rsi
       vmovsd   qword ptr [rbp-0x80], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x80]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG153
 
G_M000_IG194:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm1
       test     r15, r15
       je       G_M000_IG152
 
G_M000_IG195:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG197
 
G_M000_IG196:
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG153
 
G_M000_IG197:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG152
 
G_M000_IG198:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x88], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x88]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG153
 
G_M000_IG199:
       mov      rsi, qword ptr [r14+0x08]
       test     r12, r12
       je       G_M000_IG134
 
G_M000_IG200:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG208
 
G_M000_IG201:
       mov      r12, qword ptr [r12+0x08]
       lea      r15, [rsi+r12]
       mov      rdi, rsi
       xor      rdi, r15
       mov      rax, r12
       xor      rax, r15
       and      rdi, rax
       jge      SHORT G_M000_IG204
 
G_M000_IG202:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xB0], xmm0
       lea      rdi, [rbp-0xB0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r15, gword ptr [rbp-0xB0]
       mov      r14d, dword ptr [rbp-0xA8]
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xC0], xmm0
       lea      rdi, [rbp-0xC0]
       mov      rsi, r12
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, r15
       mov      esi, r14d
       mov      rdx, gword ptr [rbp-0xC0]
       mov      rcx, qword ptr [rbp-0xB8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG203:
       mov      r14, rax
       jmp      G_M000_IG135
 
G_M000_IG204:
       lea      r14, [r15+0x05]
       cmp      r14, 261
       jbe      SHORT G_M000_IG207
 
G_M000_IG205:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r15
 
G_M000_IG206:
       jmp      SHORT G_M000_IG203
 
G_M000_IG207:
       test     r14, r14
       jl       G_M000_IG245
       mov      rax, r14
       mov      rdi, 0xD1FFAB1E
       mov      r11, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG244
       mov      rax, gword ptr [r11+8*rax+0x10]
       jmp      SHORT G_M000_IG206
 
G_M000_IG208:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG134
 
G_M000_IG209:
       vmovsd   xmm0, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xC8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xC8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG135
 
G_M000_IG210:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm0
       test     r12, r12
       je       G_M000_IG134
 
G_M000_IG211:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      SHORT G_M000_IG213
 
G_M000_IG212:
       vmovsd   xmm1, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x90]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG135
 
G_M000_IG213:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r12], rdi
       jne      G_M000_IG134
 
G_M000_IG214:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r12+0x08]
       vmovsd   qword ptr [rbp-0xD0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x90]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xD0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG135
 
G_M000_IG215:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG182
 
G_M000_IG216:
       movsxd   rdi, r12d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG217
       mov      edi, dword ptr [r15+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG218
 
G_M000_IG217:
       mov      eax, r12d
       jmp      SHORT G_M000_IG224
 
G_M000_IG218:
       cmp      edi, 2
       je       SHORT G_M000_IG219
       cmp      dword ptr [r15+0x08], 0
       jbe      G_M000_IG244
       mov      edi, dword ptr [r15+0x10]
       jmp      SHORT G_M000_IG220
 
G_M000_IG219:
       mov      edi, dword ptr [r15+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r15+0x10]
       or       rdi, rax
 
G_M000_IG220:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG221
       mov      eax, -1
       jmp      SHORT G_M000_IG223
 
G_M000_IG221:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jbe      SHORT G_M000_IG222
       mov      eax, 1
       jmp      SHORT G_M000_IG223
 
G_M000_IG222:
       xor      eax, eax
 
G_M000_IG223:
       imul     eax, r12d
 
G_M000_IG224:
       test     eax, eax
       jl       G_M000_IG187
       jmp      G_M000_IG176
 
G_M000_IG225:
       movsxd   rdi, r12d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG226
       mov      edi, dword ptr [r15+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG227
 
G_M000_IG226:
       mov      edi, r12d
       jmp      G_M000_IG178
 
G_M000_IG227:
       cmp      edi, 2
       je       SHORT G_M000_IG228
       cmp      dword ptr [r15+0x08], 0
       jbe      G_M000_IG244
       mov      edi, dword ptr [r15+0x10]
       jmp      SHORT G_M000_IG229
 
G_M000_IG228:
       mov      edi, dword ptr [r15+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r15+0x10]
       or       rdi, rax
 
G_M000_IG229:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG230
       mov      eax, -1
       jmp      SHORT G_M000_IG232
 
G_M000_IG230:
       test     rdi, rdi
       jge      SHORT G_M000_IG231
       mov      eax, 1
       jmp      SHORT G_M000_IG232
 
G_M000_IG231:
       xor      eax, eax
 
G_M000_IG232:
       mov      edi, r12d
       imul     edi, eax
       jmp      G_M000_IG178
 
G_M000_IG233:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG236
 
G_M000_IG234:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      ecx, dword ptr [(reloc)]
       dec      ecx
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG244
       cmp      dword ptr [rax+4*rcx+0x10], 26
       je       SHORT G_M000_IG236
 
G_M000_IG235:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      ecx, dword ptr [(reloc)]
       dec      ecx
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG244
       mov      dword ptr [rax+4*rcx+0x10], 26
 
G_M000_IG236:
       mov      rbx, qword ptr [rbp+0x1F0]
       test     rbx, rbx
       jle      SHORT G_M000_IG239
 
G_M000_IG237:
       movsxd   rax, dword ptr [(reloc)]
       cmp      rbx, rax
       jg       SHORT G_M000_IG239
 
G_M000_IG238:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG239:
       mov      rax, gword ptr [rbp+0x228]
 
G_M000_IG240:
       add      rsp, 888
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG241:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
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
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG245:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 5864

