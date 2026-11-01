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
       jo       G_M000_IG48
       mov      qword ptr [rbp-0xA8], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      edi, 26
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x1E8], rax
       jmp      G_M000_IG23
 
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
       jne      G_M000_IG43
       jmp      G_M000_IG17
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x110]
       add      rax, qword ptr [rbp-0x120]
       jo       G_M000_IG48
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
       mov      esi, 502
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
       mov      esi, 529
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
       mov      gword ptr [rbp-0x208], rax
       mov      rax, gword ptr [rbp-0x1C0]
       mov      gword ptr [rbp-0x218], rax
       cmp      dword ptr [rbp-0x1F4], 0
       jne      G_M000_IG40
       jmp      G_M000_IG33
 
G_M000_IG21:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x170]
       add      rax, qword ptr [rbp-0x180]
       jo       G_M000_IG48
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
       jne      G_M000_IG47
       jmp      G_M000_IG44
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x80]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x1E8]
 
G_M000_IG24:
       add      rsp, 592
       pop      rbp
       ret      
 
G_M000_IG25:
       mov      eax, dword ptr [rbp-0x248]
       dec      eax
       mov      dword ptr [rbp-0x248], eax
       cmp      dword ptr [rbp-0x248], 0
       jg       SHORT G_M000_IG27
 
G_M000_IG26:
       lea      rdi, [rbp-0x248]
       mov      esi, 719
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
       mov      eax, dword ptr [rbp-0x248]
       dec      eax
       mov      dword ptr [rbp-0x248], eax
       cmp      dword ptr [rbp-0x248], 0
       jg       SHORT G_M000_IG31
 
G_M000_IG30:
       lea      rdi, [rbp-0x248]
       mov      esi, 738
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
       mov      gword ptr [rbp-0x240], rax
       mov      rax, gword ptr [rbp-0x240]
       vmovsd   xmm0, qword ptr [rbp-0x1F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x240]
       mov      gword ptr [rbp-0x70], rax
       jmp      G_M000_IG29
 
G_M000_IG33:
       mov      eax, dword ptr [rbp-0x248]
       dec      eax
       mov      dword ptr [rbp-0x248], eax
       cmp      dword ptr [rbp-0x248], 0
       jg       SHORT G_M000_IG35
 
G_M000_IG34:
       lea      rdi, [rbp-0x248]
       mov      esi, 814
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
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
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
 
G_M000_IG37:
       mov      eax, dword ptr [rbp-0x248]
       dec      eax
       mov      dword ptr [rbp-0x248], eax
       cmp      dword ptr [rbp-0x248], 0
       jg       SHORT G_M000_IG39
 
G_M000_IG38:
       lea      rdi, [rbp-0x248]
       mov      esi, 884
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG39:
       mov      rsi, gword ptr [rbp-0x218]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x234], eax
       cmp      dword ptr [rbp-0x234], 0
       jne      G_M000_IG36
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG33
 
G_M000_IG40:
       mov      eax, dword ptr [rbp-0x248]
       dec      eax
       mov      dword ptr [rbp-0x248], eax
       cmp      dword ptr [rbp-0x248], 0
       jg       SHORT G_M000_IG42
 
G_M000_IG41:
       lea      rdi, [rbp-0x248]
       mov      esi, 911
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG42:
       mov      rsi, gword ptr [rbp-0x208]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x238], eax
       cmp      dword ptr [rbp-0x238], 0
       jne      G_M000_IG37
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG33
 
G_M000_IG43:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x70]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x1F0], xmm0
       jmp      G_M000_IG17
 
G_M000_IG44:
       mov      eax, dword ptr [rbp-0x248]
       dec      eax
       mov      dword ptr [rbp-0x248], eax
       cmp      dword ptr [rbp-0x248], 0
       jg       SHORT G_M000_IG46
 
G_M000_IG45:
       lea      rdi, [rbp-0x248]
       mov      esi, 952
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG46:
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, gword ptr [rbp-0x70]
       mov      rsi, gword ptr [rbp-0x60]
       mov      edi, 10
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       mov      gword ptr [rbp-0x1D8], rax
       jmp      G_M000_IG13
 
G_M000_IG47:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x240], rax
       mov      rax, gword ptr [rbp-0x240]
       vmovsd   xmm0, qword ptr [rbp-0x1F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x240]
       mov      gword ptr [rbp-0x70], rax
       jmp      G_M000_IG44
 
G_M000_IG48:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2905

; Assembly listing for method RyuPy.Generated.Module:m0_matmul(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x2cf
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 34 inlinees with PGO data; 53 single block inlinees; 64 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 256
       mov      qword ptr [rsp+0x358], r15
       mov      qword ptr [rsp+0x350], r14
       mov      qword ptr [rsp+0x348], r13
       mov      qword ptr [rsp+0x340], r12
       mov      qword ptr [rsp+0x338], rbx
       lea      rbp, [rsp+0x100]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       mov      rbx, gword ptr [rbp+0x228]
       mov      rsi, gword ptr [rbp+0x1F8]
       mov      r13, gword ptr [rbp+0x1F0]
       mov      r12, qword ptr [rbp+0xF0]
       mov      rcx, qword ptr [rbp+0xE8]
       mov      rdx, qword ptr [rbp+0xE0]
       mov      eax, dword ptr [rbp+0x6C]
 
G_M000_IG02:
       jmp      SHORT G_M000_IG06
 
G_M000_IG03:
       mov      rdx, r15
       mov      rsi, r13
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r13, rax
 
G_M000_IG04:
       test     r13, r13
       jne      G_M000_IG34
 
G_M000_IG05:
       xor      r15d, r15d
       mov      eax, r15d
       mov      rcx, qword ptr [rbp+0xE8]
       mov      rdx, qword ptr [rbp+0xE0]
       mov      rsi, gword ptr [rbp+0x1F8]
 
G_M000_IG06:
       add      r12, rdx
       jo       G_M000_IG162
 
G_M000_IG07:
       test     rdx, rdx
       setg     r14b
       movzx    r14, r14b
       cmp      r12, rcx
       setl     dil
       movzx    rdi, dil
       and      edi, r14d
       cmp      r12, rcx
       setg     r15b
       movzx    r15, r15b
       test     r14d, r14d
       sete     r14b
       movzx    r14, r14b
       and      r15d, r14d
       or       edi, r15d
       je       G_M000_IG38
 
G_M000_IG08:
       mov      r8, gword ptr [rbp+0x208]
       lea      r15, [r12+0x05]
       cmp      r15, 261
       jbe      G_M000_IG83
 
G_M000_IG09:
       mov      dword ptr [rbp+0x6C], eax
       mov      qword ptr [rbp+0xE8], rcx
       mov      qword ptr [rbp+0xE0], rdx
       mov      gword ptr [rbp+0x1F8], rsi
       mov      gword ptr [rbp+0x208], r8
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      qword ptr [r14+0x08], r12
       mov      r8, gword ptr [rbp+0x208]
 
G_M000_IG10:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG13
 
G_M000_IG11:
       mov      r15, 0xD1FFAB1E
       mov      r15, gword ptr [r15]
       mov      r11, r15
       dec      edi
       mov      r10d, edi
       cmp      r10d, dword ptr [r11+0x08]
       jae      G_M000_IG161
       cmp      dword ptr [r11+4*r10+0x10], 23
       je       SHORT G_M000_IG13
 
G_M000_IG12:
       cmp      edi, dword ptr [r15+0x08]
       jae      G_M000_IG161
       mov      edi, edi
       mov      dword ptr [r15+4*rdi+0x10], 23
 
G_M000_IG13:
       mov      gword ptr [rbp+0x208], r8
       mov      rdi, r8
       mov      rsi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      r15, rax
       mov      rdi, rbx
       mov      rsi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdi, rax
       mov      rsi, gword ptr [rbp+0x1F8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      r14, rax
       mov      eax, dword ptr [rbp+0x6C]
       test     eax, eax
       je       SHORT G_M000_IG15
 
G_M000_IG14:
       test     r15, r15
       jne      G_M000_IG84
 
G_M000_IG15:
       test     r13, r13
       sete     dil
       movzx    rdi, dil
       test     eax, edi
       jne      G_M000_IG88
 
G_M000_IG16:
       test     r15, r15
       je       SHORT G_M000_IG19
 
G_M000_IG17:
       mov      rcx, qword ptr [r15]
       mov      rdx, 0xD1FFAB1E
       cmp      rcx, rdx
       je       G_M000_IG89
 
G_M000_IG18:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       je       G_M000_IG113
 
G_M000_IG19:
       mov      rdx, r14
       mov      rsi, r15
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      r15, rax
 
G_M000_IG20:
       test     r13, r13
       je       G_M000_IG03
 
G_M000_IG21:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       je       G_M000_IG118
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       je       G_M000_IG129
 
G_M000_IG23:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r13], rsi
       jne      G_M000_IG03
 
G_M000_IG24:
       mov      r14, r13
       test     r15, r15
       je       G_M000_IG03
 
G_M000_IG25:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r15], rsi
       jne      G_M000_IG03
 
G_M000_IG26:
       cmp      dword ptr [r14+0x08], 0
       je       SHORT G_M000_IG32
 
G_M000_IG27:
       cmp      dword ptr [r15+0x08], 0
       je       SHORT G_M000_IG31
 
G_M000_IG28:
       mov      r13d, dword ptr [r14+0x08]
       mov      esi, r13d
       add      esi, dword ptr [r15+0x08]
       js       G_M000_IG37
 
G_M000_IG29:
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x100], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [r14+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r14+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, r13d
       mov      r13, gword ptr [rbp-0x100]
       lea      rdi, bword ptr [r13+2*rdi+0x0C]
       mov      edx, dword ptr [r15+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r15+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       nop      
 
G_M000_IG30:
       jmp      G_M000_IG04
 
G_M000_IG31:
       mov      r13, r14
       jmp      SHORT G_M000_IG30
 
G_M000_IG32:
       cmp      dword ptr [r15+0x08], 0
       je       G_M000_IG152
 
G_M000_IG33:
       mov      r13, r15
       jmp      SHORT G_M000_IG30
 
G_M000_IG34:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r13], rax
       jne      SHORT G_M000_IG36
 
G_M000_IG35:
       mov      r15d, 1
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp+0x70], xmm0
       mov      eax, r15d
       mov      rcx, qword ptr [rbp+0xE8]
       mov      rdx, qword ptr [rbp+0xE0]
       mov      rsi, gword ptr [rbp+0x1F8]
       jmp      G_M000_IG06
 
G_M000_IG36:
       xor      r15d, r15d
       mov      eax, r15d
       mov      rcx, qword ptr [rbp+0xE8]
       mov      rdx, qword ptr [rbp+0xE0]
       mov      rsi, gword ptr [rbp+0x1F8]
       jmp      G_M000_IG06
 
G_M000_IG37:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG38:
       test     r13, r13
       sete     dil
       movzx    rdi, dil
       test     eax, edi
       je       SHORT G_M000_IG40
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       vmovsd   xmm0, qword ptr [rbp+0x70]
       vmovsd   qword ptr [r13+0x08], xmm0
 
G_M000_IG40:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG43
 
G_M000_IG41:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       dec      edi
       mov      ecx, edi
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG161
       cmp      dword ptr [rax+4*rcx+0x10], 24
       je       SHORT G_M000_IG43
 
G_M000_IG42:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG161
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 24
 
G_M000_IG43:
       mov      rdi, 0xD1FFAB1E
       mov      r12, gword ptr [rdi]
       mov      r15, gword ptr [r12+0x60]
       test     r15, r15
       je       SHORT G_M000_IG45
 
G_M000_IG44:
       cmp      dword ptr [r15+0x08], 1
       je       SHORT G_M000_IG46
 
G_M000_IG45:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r15, rax
       lea      rdi, bword ptr [r12+0x60]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG46:
       mov      rdi, r15
       mov      rdx, r13
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, r15
       mov      rsi, gword ptr [rbp+0x200]
       mov      edi, 10
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
       mov      r15, qword ptr [rbp+0x150]
       mov      r14, qword ptr [rbp+0x140]
       add      r15, r14
       jo       G_M000_IG162
 
G_M000_IG47:
       test     r14, r14
       setg     dil
       movzx    rdi, dil
       mov      r13, qword ptr [rbp+0x148]
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
       je       G_M000_IG58
 
G_M000_IG48:
       lea      r12, [r15+0x05]
       cmp      r12, 261
       jbe      G_M000_IG79
 
G_M000_IG49:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rbp+0x150], r15
       mov      qword ptr [rax+0x08], r15
       mov      rsi, rax
 
G_M000_IG50:
       mov      gword ptr [rbp+0x1F8], rsi
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG53
 
G_M000_IG51:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rcx, rax
       lea      edx, [rdi-0x01]
       mov      esi, edx
       cmp      esi, dword ptr [rcx+0x08]
       jae      G_M000_IG161
       cmp      dword ptr [rcx+4*rsi+0x10], 21
       je       SHORT G_M000_IG53
 
G_M000_IG52:
       cmp      edx, dword ptr [rax+0x08]
       jae      G_M000_IG161
       mov      ecx, edx
       mov      dword ptr [rax+4*rcx+0x10], 21
 
G_M000_IG53:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp+0x1F0], rax
       test     edi, edi
       jle      SHORT G_M000_IG56
 
G_M000_IG54:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      esi, edi
       cmp      esi, dword ptr [rdx+0x08]
       jae      G_M000_IG161
       cmp      dword ptr [rdx+4*rsi+0x10], 22
       je       SHORT G_M000_IG56
 
G_M000_IG55:
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG161
       mov      edi, edi
       mov      dword ptr [rcx+4*rdi+0x10], 22
 
G_M000_IG56:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x100], rax
       mov      rdi, gword ptr [rbp+0x220]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, rax
       mov      rdi, qword ptr [rbp+0x100]
       mov      rax, rdi
       mov      qword ptr [rbp+0xE8], rcx
       mov      qword ptr [rbp+0xE0], 1
       vxorps   xmm0, xmm0, xmm0
       mov      rsi, gword ptr [rbp+0x1F0]
       test     rsi, rsi
       jne      G_M000_IG80
 
G_M000_IG57:
       xor      r8d, r8d
       mov      dword ptr [rbp+0x6C], r8d
       mov      qword ptr [rbp+0x148], r13
       mov      qword ptr [rbp+0x140], r14
       vmovsd   qword ptr [rbp+0x70], xmm0
       mov      r12, rax
       mov      r13, rsi
       mov      eax, dword ptr [rbp+0x6C]
       mov      rcx, qword ptr [rbp+0xE8]
       mov      rdx, qword ptr [rbp+0xE0]
       mov      rsi, gword ptr [rbp+0x1F8]
       jmp      G_M000_IG07
 
G_M000_IG58:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG61
 
G_M000_IG59:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       dec      edi
       mov      ecx, dword ptr [rax+0x08]
       cmp      edi, ecx
       jae      G_M000_IG161
       cmp      dword ptr [rax+4*rdi+0x10], 25
       je       SHORT G_M000_IG61
 
G_M000_IG60:
       mov      dword ptr [rsi+4*rdi+0x10], 25
 
G_M000_IG61:
       mov      r15, gword ptr [r12+0x48]
       test     r15, r15
       je       SHORT G_M000_IG63
 
G_M000_IG62:
       cmp      dword ptr [r15+0x08], 1
       je       SHORT G_M000_IG64
 
G_M000_IG63:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r15, rax
       lea      rdi, bword ptr [r12+0x48]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG64:
       mov      rdi, r15
       mov      rdx, gword ptr [rbp+0x200]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rcx, r15
       mov      rsi, gword ptr [rbp+0x218]
       mov      edi, 7
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethodFallback(int,System.Object,System.String,System.Object[]):System.Object]
       mov      r15, qword ptr [rbp+0x1B8]
       mov      r14, qword ptr [rbp+0x1A8]
       add      r15, r14
       jo       G_M000_IG162
       test     r14, r14
       setg     dil
       movzx    rdi, dil
       mov      r13, qword ptr [rbp+0x1B0]
       cmp      r15, r13
       setl     al
       movzx    rax, al
       and      eax, edi
       cmp      r15, r13
       setg     cl
       movzx    rcx, cl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, ecx
       or       edi, eax
       je       G_M000_IG153
 
G_M000_IG65:
       lea      rax, [r15+0x05]
       cmp      rax, 261
       jbe      G_M000_IG77
 
G_M000_IG66:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r15
 
G_M000_IG67:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG70
 
G_M000_IG68:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rcx, rax
       dec      edi
       mov      edx, dword ptr [rcx+0x08]
       cmp      edi, edx
       jae      G_M000_IG161
       cmp      dword ptr [rcx+4*rdi+0x10], 18
       je       SHORT G_M000_IG70
 
G_M000_IG69:
       mov      dword ptr [rax+4*rdi+0x10], 18
 
G_M000_IG70:
       mov      rdi, gword ptr [rbp+0x230]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x208], rax
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG73
 
G_M000_IG71:
       mov      rax, gword ptr [rbp+0x208]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      esi, dword ptr [rdx+0x08]
       cmp      edi, esi
       jae      G_M000_IG161
       cmp      dword ptr [rdx+4*rdi+0x10], 19
       je       G_M000_IG78
 
G_M000_IG72:
       mov      dword ptr [rcx+4*rdi+0x10], 19
       mov      gword ptr [rbp+0x208], rax
 
G_M000_IG73:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xE0], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xE8], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, rax
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       mov      rdx, gword ptr [rbp-0xE0]
       lea      rdi, bword ptr [rdx+0x08]
       mov      rsi, gword ptr [rbp-0xE8]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdx, gword ptr [rbp-0xE0]
       mov      gword ptr [rbp+0x200], rdx
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG76
 
G_M000_IG74:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      esi, dword ptr [rdx+0x08]
       cmp      edi, esi
       jae      G_M000_IG161
       cmp      dword ptr [rdx+4*rdi+0x10], 20
       je       SHORT G_M000_IG76
 
G_M000_IG75:
       mov      dword ptr [rcx+4*rdi+0x10], 20
 
G_M000_IG76:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x160], rax
       mov      rdi, gword ptr [rbp+0x220]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rdi, qword ptr [rbp+0x160]
       mov      rcx, rdi
       mov      qword ptr [rbp+0x148], rax
       mov      qword ptr [rbp+0x140], 1
       mov      qword ptr [rbp+0x1B8], r15
       mov      qword ptr [rbp+0x1B0], r13
       mov      qword ptr [rbp+0x1A8], r14
       mov      r15, rcx
       mov      r14, qword ptr [rbp+0x140]
       jmp      G_M000_IG47
 
G_M000_IG77:
       test     rax, rax
       jl       G_M000_IG162
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG161
       mov      rsi, gword ptr [rax+8*rsi+0x10]
       jmp      G_M000_IG67
 
G_M000_IG78:
       mov      gword ptr [rbp+0x208], rax
       jmp      G_M000_IG73
 
G_M000_IG79:
       test     r12, r12
       jl       G_M000_IG162
       mov      rsi, r12
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG161
       mov      rsi, gword ptr [rax+8*rsi+0x10]
       mov      qword ptr [rbp+0x150], r15
       jmp      G_M000_IG50
 
G_M000_IG80:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG82
 
G_M000_IG81:
       mov      r8d, 1
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp+0x70], xmm0
       mov      qword ptr [rbp+0x148], r13
       mov      qword ptr [rbp+0x140], r14
       mov      r12, rax
       mov      eax, r8d
       mov      r13, rsi
       mov      rcx, qword ptr [rbp+0xE8]
       mov      rdx, qword ptr [rbp+0xE0]
       mov      rsi, gword ptr [rbp+0x1F8]
       jmp      G_M000_IG07
 
G_M000_IG82:
       xor      r8d, r8d
       mov      qword ptr [rbp+0x148], r13
       mov      qword ptr [rbp+0x140], r14
       vmovsd   qword ptr [rbp+0x70], xmm0
       mov      r12, rax
       mov      eax, r8d
       mov      r13, rsi
       mov      rcx, qword ptr [rbp+0xE8]
       mov      rdx, qword ptr [rbp+0xE0]
       mov      rsi, gword ptr [rbp+0x1F8]
       jmp      G_M000_IG07
 
G_M000_IG83:
       test     r15, r15
       jl       G_M000_IG162
       mov      rdi, r15
       mov      r14, 0xD1FFAB1E
       mov      r15, gword ptr [r14]
       cmp      rdi, 262
       jae      G_M000_IG161
       mov      r14, gword ptr [r15+8*rdi+0x10]
       mov      dword ptr [rbp+0x6C], eax
       mov      qword ptr [rbp+0xE8], rcx
       mov      qword ptr [rbp+0xE0], rdx
       mov      gword ptr [rbp+0x1F8], rsi
       jmp      G_M000_IG10
 
G_M000_IG84:
       mov      rcx, qword ptr [r15]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG15
 
G_M000_IG85:
       test     r14, r14
       je       G_M000_IG15
 
G_M000_IG86:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG15
 
G_M000_IG87:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmulsd   xmm0, xmm0, qword ptr [r14+0x08]
       vaddsd   xmm0, xmm0, qword ptr [rbp+0x70]
       vmovsd   qword ptr [rbp+0x70], xmm0
       xor      r13, r13
       mov      rcx, qword ptr [rbp+0xE8]
       mov      rdx, qword ptr [rbp+0xE0]
       mov      rsi, gword ptr [rbp+0x1F8]
       jmp      G_M000_IG06
 
G_M000_IG88:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       vmovsd   xmm0, qword ptr [rbp+0x70]
       vmovsd   qword ptr [r13+0x08], xmm0
       jmp      G_M000_IG16
 
G_M000_IG89:
       mov      rsi, qword ptr [r15+0x08]
       test     r14, r14
       je       G_M000_IG19
 
G_M000_IG90:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [r14], rdx
       jne      G_M000_IG111
 
G_M000_IG91:
       mov      r14, qword ptr [r14+0x08]
       lea      rdi, [rbp-0x58]
       mov      rdx, rsi
       mulx     rcx, rax, r14
       mov      qword ptr [rdi], rax
       mov      r15, qword ptr [rbp-0x58]
       mov      rdi, rsi
       sar      rdi, 63
       and      rdi, r14
       sub      rcx, rdi
       mov      rdi, r14
       sar      rdi, 63
       and      rdi, rsi
       sub      rcx, rdi
       mov      rdi, r15
       sar      rdi, 63
       cmp      rcx, rdi
       je       G_M000_IG107
 
G_M000_IG92:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x50], xmm1
       lea      rdi, [rbp-0x50]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r15, gword ptr [rbp-0x50]
       mov      eax, dword ptr [rbp-0x48]
       mov      dword ptr [rbp-0xD4], eax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x68], xmm0
       lea      rdi, [rbp-0x68]
       mov      rsi, r14
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r14, gword ptr [rbp-0x68]
       mov      r9d, dword ptr [rbp-0x60]
       test     r15, r15
       jne      G_M000_IG100
 
G_M000_IG93:
       test     r14, r14
       jne      G_M000_IG134
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x78], xmm0
       movsxd   rsi, dword ptr [rbp-0xD4]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x78]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r14, gword ptr [rbp-0x78]
       mov      r15d, dword ptr [rbp-0x70]
 
G_M000_IG94:
       test     r14, r14
       jne      G_M000_IG135
 
G_M000_IG95:
       test     r14, r14
       jne      G_M000_IG144
       movsxd   rdi, r15d
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jge      SHORT G_M000_IG105
 
G_M000_IG96:
       mov      edi, -1
 
G_M000_IG97:
       test     edi, edi
       jg       SHORT G_M000_IG106
 
G_M000_IG98:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xF8], rax
       mov      rdi, r14
       mov      esi, r15d
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r15, gword ptr [rbp-0xF8]
       mov      qword ptr [r15+0x08], rax
 
G_M000_IG99:
       jmp      G_M000_IG20
 
G_M000_IG100:
       lea      rdi, bword ptr [r15+0x10]
       mov      esi, dword ptr [r15+0x08]
 
G_M000_IG101:
       test     r14, r14
       je       SHORT G_M000_IG104
 
G_M000_IG102:
       lea      rcx, bword ptr [r14+0x10]
       mov      r8d, dword ptr [r14+0x08]
 
G_M000_IG103:
       mov      edx, dword ptr [rbp-0xD4]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      r14, rax
       mov      r15d, edx
       jmp      G_M000_IG94
 
G_M000_IG104:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG103
 
G_M000_IG105:
       xor      edi, edi
       jmp      SHORT G_M000_IG97
 
G_M000_IG106:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xF0], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r14
       call     CORINFO_HELP_ASSIGN_REF
       mov      r14, gword ptr [rbp-0xF0]
       mov      dword ptr [r14+0x10], r15d
       mov      r15, r14
       jmp      SHORT G_M000_IG99
 
G_M000_IG107:
       lea      r14, [r15+0x05]
       cmp      r14, 261
       jbe      SHORT G_M000_IG110
 
G_M000_IG108:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r15
       mov      r15, rax
 
G_M000_IG109:
       jmp      G_M000_IG99
 
G_M000_IG110:
       test     r14, r14
       jl       G_M000_IG162
       mov      rdi, r14
       mov      rax, 0xD1FFAB1E
       mov      r15, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG161
       mov      r15, gword ptr [r15+8*rdi+0x10]
       jmp      SHORT G_M000_IG109
 
G_M000_IG111:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG19
 
G_M000_IG112:
       vmovsd   xmm1, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rsi
       vmovsd   qword ptr [rbp-0x80], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0x80]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG20
 
G_M000_IG113:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm1
       test     r14, r14
       je       G_M000_IG19
 
G_M000_IG114:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      SHORT G_M000_IG116
 
G_M000_IG115:
       vmovsd   xmm2, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG20
 
G_M000_IG116:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG19
 
G_M000_IG117:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x88], xmm2
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x88]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG20
 
G_M000_IG118:
       mov      rsi, qword ptr [r13+0x08]
       test     r15, r15
       je       G_M000_IG03
 
G_M000_IG119:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG127
 
G_M000_IG120:
       mov      r15, qword ptr [r15+0x08]
       lea      r13, [rsi+r15]
       mov      rdi, rsi
       xor      rdi, r13
       mov      rax, r15
       xor      rax, r13
       and      rdi, rax
       jge      SHORT G_M000_IG123
 
G_M000_IG121:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xB0], xmm0
       lea      rdi, [rbp-0xB0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r13, gword ptr [rbp-0xB0]
       mov      r14d, dword ptr [rbp-0xA8]
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xC0], xmm0
       lea      rdi, [rbp-0xC0]
       mov      rsi, r15
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, r13
       mov      esi, r14d
       mov      rdx, gword ptr [rbp-0xC0]
       mov      rcx, qword ptr [rbp-0xB8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG122:
       mov      r13, rax
       jmp      G_M000_IG04
 
G_M000_IG123:
       lea      r15, [r13+0x05]
       cmp      r15, 261
       jbe      SHORT G_M000_IG126
 
G_M000_IG124:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r13
 
G_M000_IG125:
       jmp      SHORT G_M000_IG122
 
G_M000_IG126:
       test     r15, r15
       jl       G_M000_IG162
       mov      rax, r15
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG161
       test     r15, r15
       jl       G_M000_IG162
       mov      rax, r15
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG125
 
G_M000_IG127:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG03
 
G_M000_IG128:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xC8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xC8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG04
 
G_M000_IG129:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm0
       test     r15, r15
       je       G_M000_IG03
 
G_M000_IG130:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG132
 
G_M000_IG131:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x90]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG04
 
G_M000_IG132:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG03
 
G_M000_IG133:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xD0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x90]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xD0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG04
 
G_M000_IG134:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG101
 
G_M000_IG135:
       movsxd   rdi, r15d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG136
       mov      edi, dword ptr [r14+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG137
 
G_M000_IG136:
       mov      eax, r15d
       jmp      SHORT G_M000_IG143
 
G_M000_IG137:
       cmp      edi, 2
       je       SHORT G_M000_IG138
       cmp      dword ptr [r14+0x08], 0
       jbe      G_M000_IG161
       mov      edi, dword ptr [r14+0x10]
       jmp      SHORT G_M000_IG139
 
G_M000_IG138:
       mov      edi, dword ptr [r14+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r14+0x10]
       or       rdi, rax
 
G_M000_IG139:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG140
       mov      eax, -1
       jmp      SHORT G_M000_IG142
 
G_M000_IG140:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jbe      SHORT G_M000_IG141
       mov      eax, 1
       jmp      SHORT G_M000_IG142
 
G_M000_IG141:
       xor      eax, eax
 
G_M000_IG142:
       imul     eax, r15d
 
G_M000_IG143:
       test     eax, eax
       jl       G_M000_IG106
       jmp      G_M000_IG95
 
G_M000_IG144:
       movsxd   rdi, r15d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG145
       mov      edi, dword ptr [r14+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG146
 
G_M000_IG145:
       mov      edi, r15d
       jmp      G_M000_IG97
 
G_M000_IG146:
       cmp      edi, 2
       je       SHORT G_M000_IG147
       cmp      dword ptr [r14+0x08], 0
       jbe      G_M000_IG161
       mov      edi, dword ptr [r14+0x10]
       jmp      SHORT G_M000_IG148
 
G_M000_IG147:
       mov      edi, dword ptr [r14+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r14+0x10]
       or       rdi, rax
 
G_M000_IG148:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG149
       mov      eax, -1
       jmp      SHORT G_M000_IG151
 
G_M000_IG149:
       test     rdi, rdi
       jge      SHORT G_M000_IG150
       mov      eax, 1
       jmp      SHORT G_M000_IG151
 
G_M000_IG150:
       xor      eax, eax
 
G_M000_IG151:
       mov      edi, r15d
       imul     edi, eax
       jmp      G_M000_IG97
 
G_M000_IG152:
       mov      r13, 0xD1FFAB1E
       jmp      G_M000_IG30
 
G_M000_IG153:
       cmp      dword ptr [(reloc)], 0
       jle      SHORT G_M000_IG156
 
G_M000_IG154:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      ecx, dword ptr [(reloc)]
       dec      ecx
       cmp      ecx, dword ptr [rax+0x08]
       jae      SHORT G_M000_IG161
       cmp      dword ptr [rax+4*rcx+0x10], 26
       je       SHORT G_M000_IG156
 
G_M000_IG155:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      ecx, dword ptr [(reloc)]
       dec      ecx
       cmp      ecx, dword ptr [rax+0x08]
       jae      SHORT G_M000_IG161
       mov      dword ptr [rax+4*rcx+0x10], 26
 
G_M000_IG156:
       mov      rbx, qword ptr [rbp+0x1E0]
       test     rbx, rbx
       jle      SHORT G_M000_IG159
 
G_M000_IG157:
       movsxd   rax, dword ptr [(reloc)]
       cmp      rbx, rax
       jg       SHORT G_M000_IG159
 
G_M000_IG158:
       dec      ebx
       mov      dword ptr [(reloc)], ebx
 
G_M000_IG159:
       mov      rax, gword ptr [rbp+0x218]
 
G_M000_IG160:
       add      rsp, 824
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG161:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG162:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 4374

