; Assembly listing for method RyuPy.Generated.Module:m0_advance(System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 0x870
       lea      rbp, [rsp+0x870]
       xor      eax, eax
       mov      qword ptr [rbp-0x868], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqa  xmmword ptr [rbp-0x860], xmm8
       mov      rax, -0x810
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x20], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
 
G_M000_IG02:
       mov      dword ptr [rbp-0x860], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0xF8], rax
       mov      edi, 79
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x848], rax
       mov      rdi, gword ptr [rbp-0x848]
       mov      rdx, gword ptr [rbp-0x30]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x850], rax
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x868], rax
       mov      rcx, gword ptr [rbp-0x868]
       mov      rsi, gword ptr [rbp-0x848]
       mov      rdx, gword ptr [rbp-0x850]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:CallBuiltin(System.String,System.Object[],System.Object[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x100], rax
       mov      rax, gword ptr [rbp-0x100]
       mov      gword ptr [rbp-0x48], rax
       mov      edi, 80
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x108], rax
       mov      rdi, gword ptr [rbp-0x108]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x110], rax
       mov      rdi, gword ptr [rbp-0x40]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x118], rax
       mov      rax, qword ptr [rbp-0x110]
       mov      qword ptr [rbp-0x120], rax
       mov      rax, qword ptr [rbp-0x118]
       mov      qword ptr [rbp-0x128], rax
       mov      qword ptr [rbp-0x130], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x860]
       mov      esi, 118
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       cmp      qword ptr [rbp-0x130], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x134], eax
       mov      rax, qword ptr [rbp-0x120]
       cmp      rax, qword ptr [rbp-0x128]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x138], eax
       mov      rax, qword ptr [rbp-0x120]
       cmp      rax, qword ptr [rbp-0x128]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x13C], eax
       mov      eax, dword ptr [rbp-0x134]
       and      eax, dword ptr [rbp-0x138]
       movzx    rax, al
       mov      dword ptr [rbp-0x140], eax
       cmp      dword ptr [rbp-0x134], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x144], eax
       mov      eax, dword ptr [rbp-0x144]
       and      eax, dword ptr [rbp-0x13C]
       movzx    rax, al
       mov      dword ptr [rbp-0x148], eax
       mov      eax, dword ptr [rbp-0x140]
       or       eax, dword ptr [rbp-0x148]
       movzx    rax, al
       mov      dword ptr [rbp-0x14C], eax
       cmp      dword ptr [rbp-0x14C], 0
       jne      SHORT G_M000_IG06
       jmp      G_M000_IG08
 
G_M000_IG06:
       mov      edi, 81
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x160], rax
       mov      rdi, gword ptr [rbp-0x160]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x168], rax
       mov      rdi, gword ptr [rbp-0x48]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x170], rax
       mov      rax, qword ptr [rbp-0x168]
       mov      qword ptr [rbp-0x178], rax
       mov      rax, qword ptr [rbp-0x170]
       mov      qword ptr [rbp-0x180], rax
       mov      qword ptr [rbp-0x188], 1
       jmp      SHORT G_M000_IG09
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x120]
       add      rax, qword ptr [rbp-0x130]
       jo       G_M000_IG208
       mov      qword ptr [rbp-0x120], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       call     [RyuPy.Runtime.PyOps:None():System.Object]
       mov      gword ptr [rbp-0x558], rax
       jmp      G_M000_IG61
 
G_M000_IG09:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG11
 
G_M000_IG10:
       lea      rdi, [rbp-0x860]
       mov      esi, 268
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG11:
       cmp      qword ptr [rbp-0x188], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x18C], eax
       mov      rax, qword ptr [rbp-0x178]
       cmp      rax, qword ptr [rbp-0x180]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x190], eax
       mov      rax, qword ptr [rbp-0x178]
       cmp      rax, qword ptr [rbp-0x180]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x194], eax
       mov      eax, dword ptr [rbp-0x18C]
       and      eax, dword ptr [rbp-0x190]
       movzx    rax, al
       mov      dword ptr [rbp-0x198], eax
       cmp      dword ptr [rbp-0x18C], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x19C], eax
       mov      eax, dword ptr [rbp-0x19C]
       and      eax, dword ptr [rbp-0x194]
       movzx    rax, al
       mov      dword ptr [rbp-0x1A0], eax
       mov      eax, dword ptr [rbp-0x198]
       or       eax, dword ptr [rbp-0x1A0]
       movzx    rax, al
       mov      dword ptr [rbp-0x1A4], eax
       cmp      dword ptr [rbp-0x1A4], 0
       jne      SHORT G_M000_IG12
       jmp      G_M000_IG14
 
G_M000_IG12:
       mov      rdi, qword ptr [rbp-0x178]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0x1B0], rax
       mov      rax, gword ptr [rbp-0x1B0]
       mov      gword ptr [rbp-0x58], rax
       mov      edi, 82
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x1B8], rax
       mov      rax, gword ptr [rbp-0x1B8]
       mov      gword ptr [rbp-0x60], rax
       mov      edi, 83
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x1C0], rax
       mov      rdx, gword ptr [rbp-0x58]
       mov      rcx, gword ptr [rbp-0x1C0]
       mov      edi, 46
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x1C8], rax
       mov      rdi, gword ptr [rbp-0x1C8]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x1D0], rax
       mov      rdi, gword ptr [rbp-0x48]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x1D8], rax
       mov      rax, qword ptr [rbp-0x1D0]
       mov      qword ptr [rbp-0x1E0], rax
       mov      rax, qword ptr [rbp-0x1D8]
       mov      qword ptr [rbp-0x1E8], rax
       mov      qword ptr [rbp-0x1F0], 1
       jmp      SHORT G_M000_IG15
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x178]
       add      rax, qword ptr [rbp-0x188]
       jo       G_M000_IG208
       mov      qword ptr [rbp-0x178], rax
       jmp      G_M000_IG09
 
G_M000_IG14:
       mov      edi, 98
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyOps:IterStart(System.Object):System.Object]
       mov      gword ptr [rbp-0x468], rax
       jmp      G_M000_IG46
 
G_M000_IG15:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG17
 
G_M000_IG16:
       lea      rdi, [rbp-0x860]
       mov      esi, 470
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG17:
       cmp      qword ptr [rbp-0x1F0], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x1F4], eax
       mov      rax, qword ptr [rbp-0x1E0]
       cmp      rax, qword ptr [rbp-0x1E8]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x1F8], eax
       mov      rax, qword ptr [rbp-0x1E0]
       cmp      rax, qword ptr [rbp-0x1E8]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x1FC], eax
       mov      eax, dword ptr [rbp-0x1F4]
       and      eax, dword ptr [rbp-0x1F8]
       movzx    rax, al
       mov      dword ptr [rbp-0x200], eax
       cmp      dword ptr [rbp-0x1F4], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x204], eax
       mov      eax, dword ptr [rbp-0x204]
       and      eax, dword ptr [rbp-0x1FC]
       movzx    rax, al
       mov      dword ptr [rbp-0x208], eax
       mov      eax, dword ptr [rbp-0x200]
       or       eax, dword ptr [rbp-0x208]
       movzx    rax, al
       mov      dword ptr [rbp-0x20C], eax
       cmp      dword ptr [rbp-0x20C], 0
       jne      SHORT G_M000_IG20
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG19
 
G_M000_IG18:
       lea      rdi, [rbp-0x860]
       mov      esi, 536
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG19:
       jmp      G_M000_IG13
 
G_M000_IG20:
       mov      rdi, qword ptr [rbp-0x1E0]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0x218], rax
       mov      rax, gword ptr [rbp-0x218]
       mov      gword ptr [rbp-0x68], rax
       mov      edi, 84
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x30]
       mov      rsi, gword ptr [rbp-0x68]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x220], rax
       mov      rax, gword ptr [rbp-0x220]
       mov      gword ptr [rbp-0x70], rax
       mov      edi, 85
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x228], rax
       mov      rdi, gword ptr [rbp-0x60]
       mov      rsi, gword ptr [rbp-0x228]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x230], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x238], rax
       mov      rdi, gword ptr [rbp-0x70]
       mov      rsi, gword ptr [rbp-0x238]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x240], rax
       mov      rdx, gword ptr [rbp-0x230]
       mov      rcx, gword ptr [rbp-0x240]
       mov      edi, 47
       mov      esi, 2
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x248], rax
       mov      rax, gword ptr [rbp-0x248]
       mov      gword ptr [rbp-0x78], rax
       mov      edi, 86
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x250], rax
       mov      rdi, gword ptr [rbp-0x60]
       mov      rsi, gword ptr [rbp-0x250]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x258], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x260], rax
       mov      rdi, gword ptr [rbp-0x70]
       mov      rsi, gword ptr [rbp-0x260]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x268], rax
       mov      rdx, gword ptr [rbp-0x258]
       mov      rcx, gword ptr [rbp-0x268]
       mov      edi, 48
 
G_M000_IG21:
       mov      esi, 2
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x270], rax
       mov      rax, gword ptr [rbp-0x270]
       mov      gword ptr [rbp-0x80], rax
       mov      edi, 87
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x278], rax
       mov      rdi, gword ptr [rbp-0x60]
       mov      rsi, gword ptr [rbp-0x278]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x280], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x288], rax
       mov      rdi, gword ptr [rbp-0x70]
       mov      rsi, gword ptr [rbp-0x288]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x290], rax
       mov      rdx, gword ptr [rbp-0x280]
       mov      rcx, gword ptr [rbp-0x290]
       mov      edi, 49
       mov      esi, 2
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x298], rax
       mov      rax, gword ptr [rbp-0x298]
       mov      gword ptr [rbp-0x88], rax
       mov      edi, 88
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x78]
       mov      gword ptr [rbp-0x560], rax
       mov      rax, gword ptr [rbp-0x80]
       mov      gword ptr [rbp-0x570], rax
       mov      rax, gword ptr [rbp-0x88]
       mov      gword ptr [rbp-0x580], rax
       jmp      G_M000_IG77
 
G_M000_IG22:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG24
 
G_M000_IG23:
       lea      rdi, [rbp-0x860]
       mov      esi, 779
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG24:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x1E0]
       add      rax, qword ptr [rbp-0x1F0]
       jo       G_M000_IG208
       mov      qword ptr [rbp-0x1E0], rax
       jmp      G_M000_IG15
 
G_M000_IG25:
       mov      edi, 92
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x60]
       mov      gword ptr [rbp-0xB0], rax
       mov      edi, 92
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x328], rax
       mov      rdi, gword ptr [rbp-0xB0]
       mov      rsi, gword ptr [rbp-0x328]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x330], rax
       mov      rax, gword ptr [rbp-0x78]
       mov      gword ptr [rbp-0x5C8], rax
       mov      rax, gword ptr [rbp-0xA8]
       mov      gword ptr [rbp-0x5D8], rax
       mov      rax, gword ptr [rbp-0x330]
       mov      gword ptr [rbp-0x5E8], rax
       jmp      G_M000_IG93
 
G_M000_IG26:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG28
 
G_M000_IG27:
       lea      rdi, [rbp-0x860]
       mov      esi, 843
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG28:
       mov      edi, 93
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x350], rax
       mov      rdi, gword ptr [rbp-0x350]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0x354], eax
       cmp      dword ptr [rbp-0x354], 0
       jne      SHORT G_M000_IG29
       jmp      SHORT G_M000_IG30
 
G_M000_IG29:
       mov      edi, 93
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x60]
       mov      gword ptr [rbp-0xB8], rax
       mov      edi, 93
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x360], rax
       mov      rdi, gword ptr [rbp-0xB8]
       mov      rsi, gword ptr [rbp-0x360]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x368], rax
       mov      rax, gword ptr [rbp-0x80]
       mov      gword ptr [rbp-0x618], rax
       mov      rax, gword ptr [rbp-0xA8]
       mov      gword ptr [rbp-0x628], rax
       mov      rax, gword ptr [rbp-0x368]
       mov      gword ptr [rbp-0x638], rax
       jmp      G_M000_IG109
 
G_M000_IG30:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG32
 
G_M000_IG31:
       lea      rdi, [rbp-0x860]
       mov      esi, 930
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG32:
       mov      edi, 94
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x388], rax
       mov      rdi, gword ptr [rbp-0x388]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0x38C], eax
       cmp      dword ptr [rbp-0x38C], 0
       jne      SHORT G_M000_IG33
       jmp      G_M000_IG34
 
G_M000_IG33:
       mov      edi, 94
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x60]
       mov      gword ptr [rbp-0xC0], rax
       mov      edi, 94
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x398], rax
       mov      rdi, gword ptr [rbp-0xC0]
       mov      rsi, gword ptr [rbp-0x398]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x3A0], rax
       mov      rax, gword ptr [rbp-0x88]
       mov      gword ptr [rbp-0x668], rax
       mov      rax, gword ptr [rbp-0xA8]
       mov      gword ptr [rbp-0x678], rax
       mov      rax, gword ptr [rbp-0x3A0]
       mov      gword ptr [rbp-0x688], rax
       jmp      G_M000_IG125
 
G_M000_IG34:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG36
 
G_M000_IG35:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x3F9
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG36:
       mov      edi, 95
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x3C0], rax
       mov      rdi, gword ptr [rbp-0x3C0]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0x3C4], eax
       cmp      dword ptr [rbp-0x3C4], 0
       jne      SHORT G_M000_IG37
       jmp      SHORT G_M000_IG38
 
G_M000_IG37:
       mov      edi, 95
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x70]
       mov      gword ptr [rbp-0xC8], rax
       mov      edi, 95
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x3D0], rax
       mov      rdi, gword ptr [rbp-0xC8]
       mov      rsi, gword ptr [rbp-0x3D0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x3D8], rax
       mov      rax, gword ptr [rbp-0x78]
       mov      gword ptr [rbp-0x6B8], rax
       mov      rax, gword ptr [rbp-0xA0]
       mov      gword ptr [rbp-0x6C8], rax
       mov      rax, gword ptr [rbp-0x3D8]
       mov      gword ptr [rbp-0x6D8], rax
       jmp      G_M000_IG141
 
G_M000_IG38:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG40
 
G_M000_IG39:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x451
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG40:
       mov      edi, 96
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x3F8], rax
       mov      rdi, gword ptr [rbp-0x3F8]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0x3FC], eax
       cmp      dword ptr [rbp-0x3FC], 0
       jne      SHORT G_M000_IG41
       jmp      SHORT G_M000_IG42
 
G_M000_IG41:
       mov      edi, 96
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x70]
       mov      gword ptr [rbp-0xD0], rax
       mov      edi, 96
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x408], rax
       mov      rdi, gword ptr [rbp-0xD0]
       mov      rsi, gword ptr [rbp-0x408]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x410], rax
       mov      rax, gword ptr [rbp-0x80]
       mov      gword ptr [rbp-0x708], rax
       mov      rax, gword ptr [rbp-0xA0]
       mov      gword ptr [rbp-0x718], rax
       mov      rax, gword ptr [rbp-0x410]
       mov      gword ptr [rbp-0x728], rax
       jmp      G_M000_IG157
 
G_M000_IG42:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG44
 
G_M000_IG43:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x4A9
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG44:
       mov      edi, 97
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x430], rax
       mov      rdi, gword ptr [rbp-0x430]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0x434], eax
       cmp      dword ptr [rbp-0x434], 0
       jne      SHORT G_M000_IG45
       jmp      G_M000_IG22
 
G_M000_IG45:
       mov      edi, 97
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x70]
       mov      gword ptr [rbp-0xD8], rax
       mov      edi, 97
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x440], rax
       mov      rdi, gword ptr [rbp-0xD8]
       mov      rsi, gword ptr [rbp-0x440]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x448], rax
       mov      rax, gword ptr [rbp-0x88]
       mov      gword ptr [rbp-0x758], rax
       mov      rax, gword ptr [rbp-0xA0]
       mov      gword ptr [rbp-0x768], rax
       mov      rax, gword ptr [rbp-0x448]
       mov      gword ptr [rbp-0x778], rax
       jmp      G_M000_IG173
 
G_M000_IG46:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG48
 
G_M000_IG47:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x501
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG48:
       mov      rdi, gword ptr [rbp-0x468]
       call     [RyuPy.Runtime.PyOps:IterNextOk(System.Object):bool]
       mov      dword ptr [rbp-0x46C], eax
       cmp      dword ptr [rbp-0x46C], 0
       jne      SHORT G_M000_IG51
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG50
 
G_M000_IG49:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x511
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG50:
       jmp      G_M000_IG07
 
G_M000_IG51:
       mov      rdi, gword ptr [rbp-0x468]
       call     [RyuPy.Runtime.PyOps:IterValue(System.Object):System.Object]
       mov      gword ptr [rbp-0x478], rax
       mov      rax, gword ptr [rbp-0x478]
       mov      gword ptr [rbp-0x60], rax
       mov      edi, 99
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x480], rax
       mov      rdi, gword ptr [rbp-0x480]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0x484], eax
       cmp      dword ptr [rbp-0x484], 0
       jne      SHORT G_M000_IG52
       jmp      G_M000_IG53
 
G_M000_IG52:
       mov      edi, 99
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x60]
       mov      gword ptr [rbp-0xE0], rax
       mov      edi, 99
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x490], rax
       mov      rdi, gword ptr [rbp-0xE0]
       mov      rsi, gword ptr [rbp-0x490]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x498], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x4A0], rax
       mov      rdi, gword ptr [rbp-0x60]
       mov      rsi, gword ptr [rbp-0x4A0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x4A8], rax
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0x7A8], rax
       mov      rax, gword ptr [rbp-0x4A8]
       mov      gword ptr [rbp-0x7B8], rax
       mov      rax, gword ptr [rbp-0x498]
       mov      gword ptr [rbp-0x7C8], rax
       jmp      G_M000_IG189
 
G_M000_IG53:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG55
 
G_M000_IG54:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x58B
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG55:
       mov      edi, 100
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x4C8], rax
       mov      rdi, gword ptr [rbp-0x4C8]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0x4CC], eax
       cmp      dword ptr [rbp-0x4CC], 0
       jne      SHORT G_M000_IG56
       jmp      G_M000_IG57
 
G_M000_IG56:
       mov      edi, 100
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x60]
       mov      gword ptr [rbp-0xE8], rax
       mov      edi, 100
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x4D8], rax
       mov      rdi, gword ptr [rbp-0xE8]
       mov      rsi, gword ptr [rbp-0x4D8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x4E0], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x4E8], rax
       mov      rdi, gword ptr [rbp-0x60]
       mov      rsi, gword ptr [rbp-0x4E8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x4F0], rax
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0x7F8], rax
       mov      rax, gword ptr [rbp-0x4F0]
       mov      gword ptr [rbp-0x808], rax
       mov      rax, gword ptr [rbp-0x4E0]
       mov      gword ptr [rbp-0x818], rax
       jmp      G_M000_IG205
 
G_M000_IG57:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG59
 
G_M000_IG58:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x5F8
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG59:
       mov      edi, 101
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x510], rax
       mov      rdi, gword ptr [rbp-0x510]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0x514], eax
       cmp      dword ptr [rbp-0x514], 0
       jne      SHORT G_M000_IG60
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG46
 
G_M000_IG60:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 101
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x60]
       mov      gword ptr [rbp-0xF0], rax
       mov      edi, 101
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x520], rax
       mov      rdi, gword ptr [rbp-0xF0]
       mov      rsi, gword ptr [rbp-0x520]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x528], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x530], rax
       mov      rdi, gword ptr [rbp-0x60]
       mov      rsi, gword ptr [rbp-0x530]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x538], rax
       mov      rdx, gword ptr [rbp-0x38]
       mov      rcx, gword ptr [rbp-0x538]
       mov      edi, 50
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x540], rax
       mov      rdx, gword ptr [rbp-0x528]
       mov      rcx, gword ptr [rbp-0x540]
       mov      edi, 51
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x548], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x550], rax
       mov      rdi, gword ptr [rbp-0xF0]
       mov      rsi, gword ptr [rbp-0x550]
       mov      rdx, gword ptr [rbp-0x548]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
       jmp      G_M000_IG46
 
G_M000_IG61:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0xF8]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x558]
 
G_M000_IG62:
       add      rsp, 0x870
       pop      rbp
       ret      
 
G_M000_IG63:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG65
 
G_M000_IG64:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x68F
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG65:
       mov      rax, gword ptr [rbp-0x2C0]
       mov      gword ptr [rbp-0x90], rax
       mov      edi, 89
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x2C8], rax
       mov      rdx, gword ptr [rbp-0x90]
       mov      rcx, gword ptr [rbp-0x2C8]
       mov      edi, 52
       mov      esi, 7
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x2D0], rax
       mov      rdx, gword ptr [rbp-0x90]
       mov      rcx, gword ptr [rbp-0x2D0]
       mov      edi, 53
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x2D8], rax
       mov      rdx, gword ptr [rbp-0x38]
       mov      rcx, gword ptr [rbp-0x2D8]
       mov      edi, 54
       mov      esi, 4
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x2E0], rax
       mov      rax, gword ptr [rbp-0x2E0]
       mov      gword ptr [rbp-0x98], rax
       mov      edi, 90
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x2E8], rax
       mov      rdi, gword ptr [rbp-0x60]
       mov      rsi, gword ptr [rbp-0x2E8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x2F0], rax
       mov      rdx, gword ptr [rbp-0x2F0]
       mov      rcx, gword ptr [rbp-0x98]
       mov      edi, 55
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x2F8], rax
       mov      rax, gword ptr [rbp-0x2F8]
       mov      gword ptr [rbp-0xA0], rax
       mov      edi, 91
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x300], rax
       mov      rdi, gword ptr [rbp-0x70]
       mov      rsi, gword ptr [rbp-0x300]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x308], rax
       mov      rdx, gword ptr [rbp-0x308]
       mov      rcx, gword ptr [rbp-0x98]
       mov      edi, 56
       mov      esi, 3
 
G_M000_IG66:
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x310], rax
       mov      rax, gword ptr [rbp-0x310]
       mov      gword ptr [rbp-0xA8], rax
       mov      edi, 92
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x318], rax
       mov      rdi, gword ptr [rbp-0x318]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0x31C], eax
       cmp      dword ptr [rbp-0x31C], 0
       jne      G_M000_IG25
       jmp      G_M000_IG26
 
G_M000_IG67:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG69
 
G_M000_IG68:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x746
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG69:
       mov      rdx, gword ptr [rbp-0x78]
       mov      rcx, gword ptr [rbp-0x78]
       mov      edi, 57
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x2A0], rax
       mov      rdx, gword ptr [rbp-0x80]
       mov      rcx, gword ptr [rbp-0x80]
       mov      edi, 58
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x2A8], rax
       mov      rdx, gword ptr [rbp-0x2A0]
       mov      rcx, gword ptr [rbp-0x2A8]
       mov      edi, 59
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x2B0], rax
       mov      rdx, gword ptr [rbp-0x88]
       mov      rcx, gword ptr [rbp-0x88]
       mov      edi, 60
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x2B8], rax
       mov      rdx, gword ptr [rbp-0x2B0]
       mov      rcx, gword ptr [rbp-0x2B8]
       mov      edi, 61
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x2C0], rax
       jmp      G_M000_IG63
 
G_M000_IG70:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x560]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x568], xmm0
       mov      rsi, gword ptr [rbp-0x570]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x578], xmm0
       mov      rsi, gword ptr [rbp-0x580]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x588], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x568]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x568]
       vmovsd   qword ptr [rbp-0x590], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x578]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x578]
       vmovsd   qword ptr [rbp-0x598], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x590]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x598]
       vmovsd   qword ptr [rbp-0x5A0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x588]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x588]
       vmovsd   qword ptr [rbp-0x5A8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x5A0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x5A8]
       vmovsd   qword ptr [rbp-0x5B0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x858], rax
       mov      rax, gword ptr [rbp-0x858]
       vmovsd   xmm0, qword ptr [rbp-0x5B0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x858]
       mov      gword ptr [rbp-0x2C0], rax
       jmp      G_M000_IG63
 
G_M000_IG71:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG73
 
G_M000_IG72:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x7DD
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG73:
       mov      rsi, gword ptr [rbp-0x580]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x5B4], eax
       cmp      dword ptr [rbp-0x5B4], 0
       jne      G_M000_IG70
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG67
 
G_M000_IG74:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG76
 
G_M000_IG75:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x7F8
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG76:
       mov      rsi, gword ptr [rbp-0x570]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x5B8], eax
       cmp      dword ptr [rbp-0x5B8], 0
       jne      G_M000_IG71
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG67
 
G_M000_IG77:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG79
 
G_M000_IG78:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x813
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG79:
       mov      rsi, gword ptr [rbp-0x560]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x5BC], eax
       cmp      dword ptr [rbp-0x5BC], 0
       jne      G_M000_IG74
       jmp      G_M000_IG67
 
G_M000_IG80:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG82
 
G_M000_IG81:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x82E
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG82:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x348], rax
       mov      rdi, gword ptr [rbp-0xB0]
       mov      rsi, gword ptr [rbp-0x348]
       mov      rdx, gword ptr [rbp-0x340]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
       jmp      G_M000_IG26
 
G_M000_IG83:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG85
 
G_M000_IG84:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x845
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG85:
       mov      rdx, gword ptr [rbp-0x78]
       mov      rcx, gword ptr [rbp-0xA8]
       mov      edi, 62
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x338], rax
       mov      rdx, gword ptr [rbp-0x330]
       mov      rcx, gword ptr [rbp-0x338]
       mov      edi, 63
       mov      esi, 258
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x340], rax
       jmp      G_M000_IG80
 
G_M000_IG86:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x5C8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x5D0], xmm0
       mov      rsi, gword ptr [rbp-0x5D8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x5E0], xmm0
       mov      rsi, gword ptr [rbp-0x5E8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x5F0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x5D0]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x5E0]
       vmovsd   qword ptr [rbp-0x5F8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x5F0]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x5F8]
       vmovsd   qword ptr [rbp-0x600], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x858], rax
       mov      rax, gword ptr [rbp-0x858]
       vmovsd   xmm0, qword ptr [rbp-0x600]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x858]
       mov      gword ptr [rbp-0x340], rax
       jmp      G_M000_IG80
 
G_M000_IG87:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG89
 
G_M000_IG88:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x8A1
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG89:
       mov      rsi, gword ptr [rbp-0x5E8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x604], eax
       cmp      dword ptr [rbp-0x604], 0
       jne      G_M000_IG86
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG83
 
G_M000_IG90:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG92
 
G_M000_IG91:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x8BC
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG92:
       mov      rsi, gword ptr [rbp-0x5D8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x608], eax
       cmp      dword ptr [rbp-0x608], 0
       jne      G_M000_IG87
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG83
 
G_M000_IG93:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG95
 
G_M000_IG94:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x8D7
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG95:
       mov      rsi, gword ptr [rbp-0x5C8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x60C], eax
       cmp      dword ptr [rbp-0x60C], 0
       jne      G_M000_IG90
       jmp      G_M000_IG83
 
G_M000_IG96:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG98
 
G_M000_IG97:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x8F2
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG98:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x380], rax
       mov      rdi, gword ptr [rbp-0xB8]
       mov      rsi, gword ptr [rbp-0x380]
       mov      rdx, gword ptr [rbp-0x378]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
       jmp      G_M000_IG30
 
G_M000_IG99:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG101
 
G_M000_IG100:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x909
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG101:
       mov      rdx, gword ptr [rbp-0x80]
       mov      rcx, gword ptr [rbp-0xA8]
       mov      edi, 64
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x370], rax
       mov      rdx, gword ptr [rbp-0x368]
       mov      rcx, gword ptr [rbp-0x370]
       mov      edi, 65
       mov      esi, 258
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x378], rax
       jmp      G_M000_IG96
 
G_M000_IG102:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x618]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x620], xmm0
       mov      rsi, gword ptr [rbp-0x628]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x630], xmm0
       mov      rsi, gword ptr [rbp-0x638]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x640], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x620]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x630]
       vmovsd   qword ptr [rbp-0x648], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x640]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x648]
       vmovsd   qword ptr [rbp-0x650], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x858], rax
       mov      rax, gword ptr [rbp-0x858]
       vmovsd   xmm0, qword ptr [rbp-0x650]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x858]
       mov      gword ptr [rbp-0x378], rax
       jmp      G_M000_IG96
 
G_M000_IG103:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG105
 
G_M000_IG104:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x965
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG105:
       mov      rsi, gword ptr [rbp-0x638]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x654], eax
       cmp      dword ptr [rbp-0x654], 0
       jne      G_M000_IG102
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG99
 
G_M000_IG106:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG108
 
G_M000_IG107:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x980
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG108:
       mov      rsi, gword ptr [rbp-0x628]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x658], eax
       cmp      dword ptr [rbp-0x658], 0
       jne      G_M000_IG103
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG99
 
G_M000_IG109:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG111
 
G_M000_IG110:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x99B
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG111:
       mov      rsi, gword ptr [rbp-0x618]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x65C], eax
       cmp      dword ptr [rbp-0x65C], 0
       jne      G_M000_IG106
       jmp      G_M000_IG99
 
G_M000_IG112:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG114
 
G_M000_IG113:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x9B6
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG114:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x3B8], rax
       mov      rdi, gword ptr [rbp-0xC0]
       mov      rsi, gword ptr [rbp-0x3B8]
       mov      rdx, gword ptr [rbp-0x3B0]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
       jmp      G_M000_IG34
 
G_M000_IG115:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG117
 
G_M000_IG116:
       lea      rdi, [rbp-0x860]
       mov      esi, 0x9CD
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG117:
       mov      rdx, gword ptr [rbp-0x88]
       mov      rcx, gword ptr [rbp-0xA8]
       mov      edi, 66
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x3A8], rax
       mov      rdx, gword ptr [rbp-0x3A0]
       mov      rcx, gword ptr [rbp-0x3A8]
       mov      edi, 67
       mov      esi, 258
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x3B0], rax
       jmp      G_M000_IG112
 
G_M000_IG118:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x668]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x670], xmm0
       mov      rsi, gword ptr [rbp-0x678]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x680], xmm0
       mov      rsi, gword ptr [rbp-0x688]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x690], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x670]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x680]
       vmovsd   qword ptr [rbp-0x698], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x690]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x698]
       vmovsd   qword ptr [rbp-0x6A0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x858], rax
       mov      rax, gword ptr [rbp-0x858]
       vmovsd   xmm0, qword ptr [rbp-0x6A0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x858]
       mov      gword ptr [rbp-0x3B0], rax
       jmp      G_M000_IG112
 
G_M000_IG119:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG121
 
G_M000_IG120:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xA29
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG121:
       mov      rsi, gword ptr [rbp-0x688]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x6A4], eax
       cmp      dword ptr [rbp-0x6A4], 0
       jne      G_M000_IG118
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG115
 
G_M000_IG122:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG124
 
G_M000_IG123:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xA44
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG124:
       mov      rsi, gword ptr [rbp-0x678]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x6A8], eax
       cmp      dword ptr [rbp-0x6A8], 0
       jne      G_M000_IG119
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG115
 
G_M000_IG125:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG127
 
G_M000_IG126:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xA5F
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG127:
       mov      rsi, gword ptr [rbp-0x668]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x6AC], eax
       cmp      dword ptr [rbp-0x6AC], 0
       jne      G_M000_IG122
       jmp      G_M000_IG115
 
G_M000_IG128:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG130
 
G_M000_IG129:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xA7A
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG130:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x3F0], rax
       mov      rdi, gword ptr [rbp-0xC8]
       mov      rsi, gword ptr [rbp-0x3F0]
       mov      rdx, gword ptr [rbp-0x3E8]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
       jmp      G_M000_IG38
 
G_M000_IG131:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG133
 
G_M000_IG132:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xA91
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG133:
       mov      rdx, gword ptr [rbp-0x78]
       mov      rcx, gword ptr [rbp-0xA0]
       mov      edi, 68
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x3E0], rax
       mov      rdx, gword ptr [rbp-0x3D8]
       mov      rcx, gword ptr [rbp-0x3E0]
       mov      edi, 69
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x3E8], rax
       jmp      G_M000_IG128
 
G_M000_IG134:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x6B8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x6C0], xmm0
       mov      rsi, gword ptr [rbp-0x6C8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x6D0], xmm0
       mov      rsi, gword ptr [rbp-0x6D8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x6E0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x6C0]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x6D0]
       vmovsd   qword ptr [rbp-0x6E8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x6E0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x6E8]
       vmovsd   qword ptr [rbp-0x6F0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x858], rax
       mov      rax, gword ptr [rbp-0x858]
       vmovsd   xmm0, qword ptr [rbp-0x6F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x858]
       mov      gword ptr [rbp-0x3E8], rax
       jmp      G_M000_IG128
 
G_M000_IG135:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG137
 
G_M000_IG136:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xAED
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG137:
       mov      rsi, gword ptr [rbp-0x6D8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x6F4], eax
       cmp      dword ptr [rbp-0x6F4], 0
       jne      G_M000_IG134
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG131
 
G_M000_IG138:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG140
 
G_M000_IG139:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xB08
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG140:
       mov      rsi, gword ptr [rbp-0x6C8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x6F8], eax
       cmp      dword ptr [rbp-0x6F8], 0
       jne      G_M000_IG135
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG131
 
G_M000_IG141:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG143
 
G_M000_IG142:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xB23
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG143:
       mov      rsi, gword ptr [rbp-0x6B8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x6FC], eax
       cmp      dword ptr [rbp-0x6FC], 0
       jne      G_M000_IG138
       jmp      G_M000_IG131
 
G_M000_IG144:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG146
 
G_M000_IG145:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xB3E
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG146:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x428], rax
       mov      rdi, gword ptr [rbp-0xD0]
       mov      rsi, gword ptr [rbp-0x428]
       mov      rdx, gword ptr [rbp-0x420]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
       jmp      G_M000_IG42
 
G_M000_IG147:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG149
 
G_M000_IG148:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xB55
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG149:
       mov      rdx, gword ptr [rbp-0x80]
       mov      rcx, gword ptr [rbp-0xA0]
       mov      edi, 70
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x418], rax
       mov      rdx, gword ptr [rbp-0x410]
       mov      rcx, gword ptr [rbp-0x418]
       mov      edi, 71
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x420], rax
       jmp      G_M000_IG144
 
G_M000_IG150:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x708]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x710], xmm0
       mov      rsi, gword ptr [rbp-0x718]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x720], xmm0
       mov      rsi, gword ptr [rbp-0x728]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x730], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x710]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x720]
       vmovsd   qword ptr [rbp-0x738], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x730]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x738]
       vmovsd   qword ptr [rbp-0x740], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x858], rax
       mov      rax, gword ptr [rbp-0x858]
       vmovsd   xmm0, qword ptr [rbp-0x740]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x858]
       mov      gword ptr [rbp-0x420], rax
       jmp      G_M000_IG144
 
G_M000_IG151:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG153
 
G_M000_IG152:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xBB1
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG153:
       mov      rsi, gword ptr [rbp-0x728]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x744], eax
       cmp      dword ptr [rbp-0x744], 0
       jne      G_M000_IG150
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG154:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG156
 
G_M000_IG155:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xBCC
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG156:
       mov      rsi, gword ptr [rbp-0x718]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x748], eax
       cmp      dword ptr [rbp-0x748], 0
       jne      G_M000_IG151
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG157:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG159
 
G_M000_IG158:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xBE7
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG159:
       mov      rsi, gword ptr [rbp-0x708]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x74C], eax
       cmp      dword ptr [rbp-0x74C], 0
       jne      G_M000_IG154
       jmp      G_M000_IG147
 
G_M000_IG160:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG162
 
G_M000_IG161:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xC02
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG162:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x460], rax
       mov      rdi, gword ptr [rbp-0xD8]
       mov      rsi, gword ptr [rbp-0x460]
       mov      rdx, gword ptr [rbp-0x458]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
       jmp      G_M000_IG22
 
G_M000_IG163:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG165
 
G_M000_IG164:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xC19
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG165:
       mov      rdx, gword ptr [rbp-0x88]
       mov      rcx, gword ptr [rbp-0xA0]
       mov      edi, 72
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x450], rax
       mov      rdx, gword ptr [rbp-0x448]
       mov      rcx, gword ptr [rbp-0x450]
       mov      edi, 73
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x458], rax
       jmp      G_M000_IG160
 
G_M000_IG166:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x758]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x760], xmm0
       mov      rsi, gword ptr [rbp-0x768]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x770], xmm0
       mov      rsi, gword ptr [rbp-0x778]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x780], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x760]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x770]
       vmovsd   qword ptr [rbp-0x788], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x780]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x788]
       vmovsd   qword ptr [rbp-0x790], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x858], rax
       mov      rax, gword ptr [rbp-0x858]
       vmovsd   xmm0, qword ptr [rbp-0x790]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x858]
       mov      gword ptr [rbp-0x458], rax
       jmp      G_M000_IG160
 
G_M000_IG167:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG169
 
G_M000_IG168:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xC75
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG169:
       mov      rsi, gword ptr [rbp-0x778]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x794], eax
       cmp      dword ptr [rbp-0x794], 0
       jne      G_M000_IG166
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG163
 
G_M000_IG170:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG172
 
G_M000_IG171:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xC90
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG172:
       mov      rsi, gword ptr [rbp-0x768]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x798], eax
       cmp      dword ptr [rbp-0x798], 0
       jne      G_M000_IG167
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG163
 
G_M000_IG173:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG175
 
G_M000_IG174:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xCAB
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG175:
       mov      rsi, gword ptr [rbp-0x758]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x79C], eax
       cmp      dword ptr [rbp-0x79C], 0
       jne      G_M000_IG170
       jmp      G_M000_IG163
 
G_M000_IG176:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG178
 
G_M000_IG177:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xCC6
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG178:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x4C0], rax
       mov      rdi, gword ptr [rbp-0xE0]
       mov      rsi, gword ptr [rbp-0x4C0]
       mov      rdx, gword ptr [rbp-0x4B8]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
       jmp      G_M000_IG53
 
G_M000_IG179:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG181
 
G_M000_IG180:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xCDD
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG181:
       mov      rdx, gword ptr [rbp-0x38]
       mov      rcx, gword ptr [rbp-0x4A8]
       mov      edi, 74
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x4B0], rax
       mov      rdx, gword ptr [rbp-0x498]
       mov      rcx, gword ptr [rbp-0x4B0]
       mov      edi, 75
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x4B8], rax
       jmp      G_M000_IG176
 
G_M000_IG182:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x7A8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x7B0], xmm0
       mov      rsi, gword ptr [rbp-0x7B8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x7C0], xmm0
       mov      rsi, gword ptr [rbp-0x7C8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x7D0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x7B0]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x7C0]
       vmovsd   qword ptr [rbp-0x7D8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x7D0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x7D8]
       vmovsd   qword ptr [rbp-0x7E0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x858], rax
       mov      rax, gword ptr [rbp-0x858]
       vmovsd   xmm0, qword ptr [rbp-0x7E0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x858]
       mov      gword ptr [rbp-0x4B8], rax
       jmp      G_M000_IG176
 
G_M000_IG183:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG185
 
G_M000_IG184:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xD46
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG185:
       mov      rsi, gword ptr [rbp-0x7C8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x7E4], eax
       cmp      dword ptr [rbp-0x7E4], 0
       jne      G_M000_IG182
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG179
 
G_M000_IG186:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG188
 
G_M000_IG187:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xD67
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG188:
       mov      rsi, gword ptr [rbp-0x7B8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x7E8], eax
       cmp      dword ptr [rbp-0x7E8], 0
       jne      G_M000_IG183
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG179
 
G_M000_IG189:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG191
 
G_M000_IG190:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xD86
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG191:
       mov      rsi, gword ptr [rbp-0x7A8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x7EC], eax
       cmp      dword ptr [rbp-0x7EC], 0
       jne      G_M000_IG186
       jmp      G_M000_IG179
 
G_M000_IG192:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG194
 
G_M000_IG193:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xDA5
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG194:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x508], rax
       mov      rdi, gword ptr [rbp-0xE8]
       mov      rsi, gword ptr [rbp-0x508]
       mov      rdx, gword ptr [rbp-0x500]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
       jmp      G_M000_IG57
 
G_M000_IG195:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG197
 
G_M000_IG196:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xDBC
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG197:
       mov      rdx, gword ptr [rbp-0x38]
       mov      rcx, gword ptr [rbp-0x4F0]
       mov      edi, 76
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x4F8], rax
       mov      rdx, gword ptr [rbp-0x4E0]
       mov      rcx, gword ptr [rbp-0x4F8]
       mov      edi, 77
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x500], rax
       jmp      G_M000_IG192
 
G_M000_IG198:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rsi, gword ptr [rbp-0x7F8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x800], xmm0
       mov      rsi, gword ptr [rbp-0x808]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x810], xmm0
       mov      rsi, gword ptr [rbp-0x818]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x820], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x800]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x810]
       vmovsd   qword ptr [rbp-0x828], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x820]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x828]
       vmovsd   qword ptr [rbp-0x830], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x858], rax
       mov      rax, gword ptr [rbp-0x858]
       vmovsd   xmm0, qword ptr [rbp-0x830]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rax, gword ptr [rbp-0x858]
       mov      gword ptr [rbp-0x500], rax
       jmp      G_M000_IG192
 
G_M000_IG199:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG201
 
G_M000_IG200:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xE31
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG201:
       mov      rsi, gword ptr [rbp-0x818]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x834], eax
       cmp      dword ptr [rbp-0x834], 0
       jne      G_M000_IG198
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG195
 
G_M000_IG202:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG204
 
G_M000_IG203:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xE52
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG204:
       mov      rsi, gword ptr [rbp-0x808]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x838], eax
       cmp      dword ptr [rbp-0x838], 0
       jne      G_M000_IG199
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG195
 
G_M000_IG205:
       mov      eax, dword ptr [rbp-0x860]
       dec      eax
       mov      dword ptr [rbp-0x860], eax
       cmp      dword ptr [rbp-0x860], 0
       jg       SHORT G_M000_IG207
 
G_M000_IG206:
       lea      rdi, [rbp-0x860]
       mov      esi, 0xE73
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG207:
       mov      rsi, gword ptr [rbp-0x7F8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x83C], eax
       cmp      dword ptr [rbp-0x83C], 0
       jne      G_M000_IG202
       jmp      G_M000_IG195
 
G_M000_IG208:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 11994

; Assembly listing for method RyuPy.Generated.Module:m0_advance(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0xbcc
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 25 inlinees with PGO data; 50 single block inlinees; 93 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 800
       mov      qword ptr [rsp+0xB98], r15
       mov      qword ptr [rsp+0xB90], r14
       mov      qword ptr [rsp+0xB88], r13
       mov      qword ptr [rsp+0xB80], r12
       mov      qword ptr [rsp+0xB78], rbx
       lea      rbp, [rsp+0x320]
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -624
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      rbx, gword ptr [rbp+0x848]
       mov      r14, gword ptr [rbp+0x820]
       mov      r13, gword ptr [rbp+0x810]
       mov      r12, gword ptr [rbp+0x800]
 
G_M000_IG02:
       mov      r15, gword ptr [rbp+0x850]
       jmp      G_M000_IG25
 
G_M000_IG03:
       mov      gword ptr [rbp+0x800], rcx
 
G_M000_IG04:
       mov      rdx, rax
       mov      rcx, rax
       mov      edi, 57
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x318], rax
       mov      rdx, gword ptr [rbp+0x800]
       mov      gword ptr [rbp+0x800], rdx
       mov      rcx, rdx
       mov      edi, 58
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x318]
       mov      edi, 59
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x320], rax
       mov      rdx, gword ptr [rbp+0x7F8]
       mov      gword ptr [rbp+0x7F8], rdx
       mov      rcx, rdx
       mov      edi, 60
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x320]
       mov      edi, 61
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x5C0], rax
 
G_M000_IG05:
       mov      edi, 89
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, gword ptr [rbp+0x5C0]
       mov      edi, 52
       mov      esi, 7
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp+0x5C0]
       mov      edi, 53
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, rbx
       mov      edi, 54
       mov      esi, 4
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x7E8], rax
       mov      edi, 90
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, gword ptr [rbp+0x7E8]
       mov      edi, 55
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x7E0], rax
       mov      edi, 91
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x810]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG06:
       mov      rcx, gword ptr [rbp+0x7E8]
       mov      edi, 56
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x7D8], rax
       mov      edi, 92
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [r12]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG10
 
G_M000_IG07:
       mov      edi, 92
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      gword ptr [rbp+0x7D0], r14
       mov      edi, 92
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x7D0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x550], rax
       mov      rcx, gword ptr [rbp+0x808]
       mov      rdi, rcx
       mov      rdx, rax
       test     rdi, rdi
       jne      G_M000_IG295
 
G_M000_IG08:
       mov      rdx, rcx
       mov      rcx, gword ptr [rbp+0x7D8]
       mov      edi, 62
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp+0x550]
       mov      edi, 63
       mov      esi, 258
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG09:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG10:
       mov      edi, 93
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [r12]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG14
 
G_M000_IG11:
       mov      edi, 93
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      gword ptr [rbp+0x7C8], r14
       mov      edi, 93
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x7C8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x518], rax
       mov      rcx, gword ptr [rbp+0x800]
       mov      rdi, rcx
       mov      rdx, rax
       test     rdi, rdi
       jne      G_M000_IG306
 
G_M000_IG12:
       mov      gword ptr [rbp+0x800], rcx
       mov      rdx, rcx
       mov      rcx, gword ptr [rbp+0x7D8]
       mov      edi, 64
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp+0x518]
       mov      edi, 65
       mov      esi, 258
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG13:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG14:
       mov      edi, 94
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [r12]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG18
 
G_M000_IG15:
       mov      edi, 94
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      gword ptr [rbp+0x7C0], r14
       mov      edi, 94
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x7C0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x4E0], rax
       mov      rcx, gword ptr [rbp+0x7F8]
       mov      rdi, rcx
       mov      rdx, gword ptr [rbp+0x7D8]
       mov      rsi, rdx
       mov      r8, rax
       test     rdi, rdi
       jne      G_M000_IG317
 
G_M000_IG16:
       mov      gword ptr [rbp+0x7F8], rcx
       mov      rdx, rcx
       mov      rcx, gword ptr [rbp+0x7D8]
       mov      edi, 66
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp+0x4E0]
       mov      edi, 67
       mov      esi, 258
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG17:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG18:
       mov      edi, 95
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [r12]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG22
 
G_M000_IG19:
       mov      edi, 95
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp+0x810]
       mov      gword ptr [rbp+0x7B8], rax
       mov      edi, 95
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x7B8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x4A8], rax
       mov      rcx, gword ptr [rbp+0x808]
       mov      rdi, rcx
       mov      rdx, rax
       test     rdi, rdi
       jne      G_M000_IG327
 
G_M000_IG20:
       mov      rdx, rcx
       mov      rcx, gword ptr [rbp+0x7E0]
       mov      edi, 68
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp+0x4A8]
       mov      edi, 69
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG21:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x810]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG22:
       mov      edi, 96
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [r12]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       mov      qword ptr [rbp+0x6A0], r13
       mov      r13, gword ptr [rbp+0x810]
       mov      r15, gword ptr [rbp+0x850]
       je       G_M000_IG30
 
G_M000_IG23:
       mov      r12, gword ptr [rbp+0x800]
       mov      edi, 96
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      gword ptr [rbp+0x7B0], r13
       mov      edi, 96
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x7B0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rax, r12
       mov      rcx, gword ptr [rbp+0x7E0]
       mov      rdi, rcx
       mov      rsi, rdx
       test     rax, rax
       mov      gword ptr [rbp+0x7E0], rcx
       mov      gword ptr [rbp+0x470], rdx
       mov      gword ptr [rbp+0x178], rax
       mov      gword ptr [rbp+0x168], rdi
       mov      gword ptr [rbp+0x158], rsi
       je       SHORT G_M000_IG28
 
G_M000_IG24:
       mov      gword ptr [rbp+0x850], r15
       mov      r15, 0xD1FFAB1E
       mov      rsi, gword ptr [rbp+0x178]
       cmp      qword ptr [rsi], r15
       mov      gword ptr [rbp+0x178], rsi
       mov      r15, gword ptr [rbp+0x850]
       jne      SHORT G_M000_IG28
 
G_M000_IG25:
       cmp      gword ptr [rbp+0x168], 0
       je       SHORT G_M000_IG28
 
G_M000_IG26:
       mov      gword ptr [rbp+0x850], r15
       mov      r15, 0xD1FFAB1E
       mov      rsi, gword ptr [rbp+0x168]
       cmp      qword ptr [rsi], r15
       je       G_M000_IG76
 
G_M000_IG27:
       mov      r15, gword ptr [rbp+0x850]
 
G_M000_IG28:
       mov      rdx, r12
       mov      rcx, gword ptr [rbp+0x7E0]
       mov      edi, 70
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp+0x470]
       mov      edi, 71
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG29:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x7B0]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG30:
       mov      edi, 97
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      r12, 0xD1FFAB1E
       mov      rdi, gword ptr [r12]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG35
 
G_M000_IG31:
       mov      edi, 97
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 97
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r13
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x438], rax
       mov      rcx, gword ptr [rbp+0x7F8]
       mov      rdi, rcx
       mov      rdx, gword ptr [rbp+0x7E0]
       mov      rsi, rdx
       mov      r8, rax
       test     rdi, rdi
       jne      G_M000_IG83
 
G_M000_IG32:
       mov      gword ptr [rbp+0x7E0], rdx
 
G_M000_IG33:
       mov      rdx, rcx
       mov      rcx, gword ptr [rbp+0x7E0]
       mov      edi, 72
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp+0x438]
       mov      edi, 73
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG34:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r13
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG35:
       mov      r13, qword ptr [rbp+0x6A0]
       mov      rax, qword ptr [rbp+0x690]
       add      r13, rax
       jo       G_M000_IG343
 
G_M000_IG36:
       test     rax, rax
       setg     dil
       movzx    rdi, dil
       mov      rcx, qword ptr [rbp+0x698]
       cmp      r13, rcx
       setl     dl
       movzx    rdx, dl
       and      edx, edi
       cmp      r13, rcx
       setg     sil
       movzx    rsi, sil
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, esi
       or       edi, edx
       je       G_M000_IG93
 
G_M000_IG37:
       lea      rdx, [r13+0x05]
       cmp      rdx, 261
       jbe      G_M000_IG240
 
G_M000_IG38:
       mov      qword ptr [rbp+0x698], rcx
       mov      qword ptr [rbp+0x690], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r13
       mov      rsi, rax
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      dword ptr [rdx], 0
       jle      G_M000_IG242
 
G_M000_IG40:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      r8d, dword ptr [rdx]
       dec      r8d
       cmp      r8d, dword ptr [rdi+0x08]
       jae      G_M000_IG342
       cmp      dword ptr [rdi+4*r8+0x10], 84
       je       G_M000_IG241
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      qword ptr [rbp-0x2E0], rdx
       mov      r8d, dword ptr [rdx]
       dec      r8d
       cmp      r8d, dword ptr [rdi+0x08]
       jae      G_M000_IG342
       mov      dword ptr [rdi+4*r8+0x10], 84
 
G_M000_IG42:
       mov      gword ptr [rbp+0x850], r15
       mov      rdi, r15
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x810], rax
       mov      rsi, qword ptr [rbp-0x2E0]
       cmp      dword ptr [rsi], 0
       jle      SHORT G_M000_IG45
 
G_M000_IG43:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      ecx, dword ptr [rsi]
       dec      ecx
       cmp      ecx, dword ptr [rdi+0x08]
       jae      G_M000_IG342
       cmp      dword ptr [rdi+4*rcx+0x10], 85
       je       SHORT G_M000_IG45
 
G_M000_IG44:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, dword ptr [rsi]
       dec      esi
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG342
       mov      dword ptr [rdi+4*rsi+0x10], 85
 
G_M000_IG45:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x650], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x810]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, gword ptr [rbp+0x650]
       test     rcx, rcx
       je       SHORT G_M000_IG51
 
G_M000_IG46:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG243
 
G_M000_IG47:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG254
 
G_M000_IG48:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       jne      SHORT G_M000_IG51
 
G_M000_IG49:
       test     rax, rax
       je       SHORT G_M000_IG51
 
G_M000_IG50:
       mov      rdx, qword ptr [rax]
 
G_M000_IG51:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 2
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG52:
       mov      gword ptr [rbp+0x808], rax
       mov      edi, 86
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x628], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x810]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, gword ptr [rbp+0x628]
       test     rcx, rcx
       je       SHORT G_M000_IG58
 
G_M000_IG53:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG259
 
G_M000_IG54:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG272
 
G_M000_IG55:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       jne      SHORT G_M000_IG58
 
G_M000_IG56:
       test     rax, rax
       je       SHORT G_M000_IG58
 
G_M000_IG57:
       mov      rdx, qword ptr [rax]
 
G_M000_IG58:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 2
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG59:
       mov      gword ptr [rbp+0x800], rax
       mov      edi, 87
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x600], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x810]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, gword ptr [rbp+0x600]
       test     rcx, rcx
       je       SHORT G_M000_IG65
 
G_M000_IG60:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG277
 
G_M000_IG61:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG290
 
G_M000_IG62:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       jne      SHORT G_M000_IG65
 
G_M000_IG63:
       test     rax, rax
       je       SHORT G_M000_IG65
 
G_M000_IG64:
       mov      rdx, qword ptr [rax]
 
G_M000_IG65:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 2
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rcx, rax
 
G_M000_IG66:
       mov      gword ptr [rbp+0x7F8], rcx
       mov      edi, 88
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp+0x808]
       mov      rdi, rax
       test     rdi, rdi
       je       G_M000_IG04
 
G_M000_IG67:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rcx
       jne      G_M000_IG04
 
G_M000_IG68:
       mov      rcx, gword ptr [rbp+0x800]
       test     rcx, rcx
       je       G_M000_IG03
 
G_M000_IG69:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       SHORT G_M000_IG71
 
G_M000_IG70:
       mov      gword ptr [rbp+0x800], rcx
       jmp      G_M000_IG04
 
G_M000_IG71:
       mov      rdx, gword ptr [rbp+0x7F8]
       test     rdx, rdx
       je       G_M000_IG75
 
G_M000_IG72:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       SHORT G_M000_IG74
 
G_M000_IG73:
       mov      gword ptr [rbp+0x800], rcx
       mov      gword ptr [rbp+0x7F8], rdx
       jmp      G_M000_IG04
 
G_M000_IG74:
       vmovsd   xmm0, qword ptr [rax+0x08]
       mov      gword ptr [rbp+0x800], rcx
       vmovsd   xmm1, qword ptr [rcx+0x08]
       mov      gword ptr [rbp+0x7F8], rdx
       vmovsd   xmm2, qword ptr [rdx+0x08]
       vmulsd   xmm0, xmm0, xmm0
       vmulsd   xmm1, xmm1, xmm1
       vaddsd   xmm0, xmm0, xmm1
       vmulsd   xmm1, xmm2, xmm2
       vaddsd   xmm0, xmm0, xmm1
       vmovsd   qword ptr [rbp+0x2D0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x2D0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp+0x5C0], rax
       jmp      G_M000_IG05
 
G_M000_IG75:
       mov      gword ptr [rbp+0x800], rcx
       mov      gword ptr [rbp+0x7F8], rdx
       jmp      G_M000_IG04
 
G_M000_IG76:
       cmp      gword ptr [rbp+0x158], 0
       je       G_M000_IG82
 
G_M000_IG77:
       mov      r15, 0xD1FFAB1E
       mov      rsi, gword ptr [rbp+0x158]
       cmp      qword ptr [rsi], r15
       je       SHORT G_M000_IG79
 
G_M000_IG78:
       mov      r15, gword ptr [rbp+0x850]
       jmp      G_M000_IG28
 
G_M000_IG79:
       mov      r11, 0xD1FFAB1E
       mov      r15, gword ptr [rbp+0x178]
       cmp      qword ptr [r15], r11
       je       SHORT G_M000_IG81
 
G_M000_IG80:
       mov      rsi, r15
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
 
G_M000_IG81:
       vmovsd   xmm0, qword ptr [r15+0x08]
       mov      r15, gword ptr [rbp+0x168]
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmulsd   xmm0, xmm0, xmm1
       mov      rsi, gword ptr [rbp+0x158]
       vaddsd   xmm0, xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp+0x140], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0x140]
       vmovsd   qword ptr [rdx+0x08], xmm0
       mov      r15, gword ptr [rbp+0x850]
       jmp      G_M000_IG29
 
G_M000_IG82:
       mov      r15, gword ptr [rbp+0x850]
       jmp      G_M000_IG28
 
G_M000_IG83:
       mov      r9, 0xD1FFAB1E
       cmp      qword ptr [rdi], r9
       je       SHORT G_M000_IG85
 
G_M000_IG84:
       mov      gword ptr [rbp+0x7E0], rdx
       jmp      G_M000_IG33
 
G_M000_IG85:
       test     rdx, rdx
       je       SHORT G_M000_IG92
 
G_M000_IG86:
       mov      r9, 0xD1FFAB1E
       mov      gword ptr [rbp+0x7E0], rdx
       cmp      qword ptr [rdx], r9
       je       SHORT G_M000_IG88
 
G_M000_IG87:
       jmp      G_M000_IG33
 
G_M000_IG88:
       test     rax, rax
       je       SHORT G_M000_IG91
 
G_M000_IG89:
       mov      r9, 0xD1FFAB1E
       cmp      qword ptr [rax], r9
       jne      G_M000_IG33
 
G_M000_IG90:
       vmovsd   xmm0, qword ptr [rdi+0x08]
       vmovsd   xmm1, qword ptr [rsi+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp+0xF0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0xF0]
       vmovsd   qword ptr [rdx+0x08], xmm0
       jmp      G_M000_IG34
 
G_M000_IG91:
       jmp      G_M000_IG33
 
G_M000_IG92:
       mov      gword ptr [rbp+0x7E0], rdx
       jmp      G_M000_IG33
 
G_M000_IG93:
       mov      r13, qword ptr [rbp+0x708]
       mov      r14, qword ptr [rbp+0x6F8]
       add      r13, r14
       jo       G_M000_IG343
 
G_M000_IG94:
       test     r14, r14
       setg     dil
       movzx    rdi, dil
       mov      rdx, qword ptr [rbp+0x700]
       cmp      r13, rdx
       setl     al
       movzx    rax, al
       and      eax, edi
       cmp      r13, rdx
       setg     cl
       movzx    rcx, cl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, ecx
       or       edi, eax
       je       G_M000_IG111
 
G_M000_IG95:
       lea      rax, [r13+0x05]
       cmp      rax, 261
       jbe      G_M000_IG210
 
G_M000_IG96:
       mov      qword ptr [rbp+0x700], rdx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r13
 
G_M000_IG97:
       mov      gword ptr [rbp+0x828], rax
       mov      rdi, 0xD1FFAB1E
       mov      r8, rdi
       mov      qword ptr [rbp-0x2E0], r8
       mov      edi, dword ptr [r8]
       test     edi, edi
       jle      SHORT G_M000_IG100
 
G_M000_IG98:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rcx, rsi
       dec      edi
       mov      r9d, edi
       cmp      r9d, dword ptr [rcx+0x08]
       jae      G_M000_IG342
       cmp      dword ptr [rcx+4*r9+0x10], 82
       je       SHORT G_M000_IG100
 
G_M000_IG99:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG342
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 82
 
G_M000_IG100:
       mov      rdi, r15
       mov      rsi, rax
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x820], rax
       mov      rcx, qword ptr [rbp-0x2E0]
       mov      edi, dword ptr [rcx]
       test     edi, edi
       mov      rax, gword ptr [rbp+0x820]
       jle      SHORT G_M000_IG103
 
G_M000_IG101:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rcx, rsi
       dec      edi
       mov      edx, edi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG342
       cmp      dword ptr [rcx+4*rdx+0x10], 83
       je       SHORT G_M000_IG103
 
G_M000_IG102:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG342
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 83
 
G_M000_IG103:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       mov      rcx, rdx
       mov      rdx, gword ptr [rbp+0x828]
       test     rdx, rdx
       je       SHORT G_M000_IG108
 
G_M000_IG104:
       mov      r8, qword ptr [rdx]
       mov      rdi, 0xD1FFAB1E
       cmp      r8, rdi
       je       G_M000_IG211
 
G_M000_IG105:
       mov      rdi, 0xD1FFAB1E
       cmp      r8, rdi
       je       G_M000_IG223
 
G_M000_IG106:
       mov      rsi, 0xD1FFAB1E
       cmp      r8, rsi
       je       G_M000_IG229
 
G_M000_IG107:
       mov      gword ptr [rbp+0x820], rax
       jmp      SHORT G_M000_IG109
 
G_M000_IG108:
       mov      gword ptr [rbp+0x820], rax
 
G_M000_IG109:
       mov      rdx, rcx
       mov      rsi, gword ptr [rbp+0x828]
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdi, rax
 
G_M000_IG110:
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x6B0], rax
       mov      rdi, gword ptr [rbp+0x838]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, rax
       mov      rdi, qword ptr [rbp+0x6B0]
       mov      rax, rdi
       mov      qword ptr [rbp+0x698], rcx
       mov      qword ptr [rbp+0x690], 1
       mov      qword ptr [rbp+0x708], r13
       mov      qword ptr [rbp+0x6F8], r14
       mov      r13, rax
       mov      rax, qword ptr [rbp+0x690]
       mov      r14, gword ptr [rbp+0x820]
       jmp      G_M000_IG36
 
G_M000_IG111:
       mov      r14, 0xD1FFAB1E
       mov      edi, dword ptr [r14]
       test     edi, edi
       jle      SHORT G_M000_IG114
 
G_M000_IG112:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rcx, rax
       dec      edi
       mov      edx, edi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG342
       cmp      dword ptr [rcx+4*rdx+0x10], 98
       je       SHORT G_M000_IG114
 
G_M000_IG113:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG342
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 98
 
G_M000_IG114:
       mov      rdi, r15
       call     [RyuPy.Runtime.PyIter:Create(System.Object):RyuPy.Runtime.IPyIterator]
       mov      r13, rax
       jmp      G_M000_IG129
 
G_M000_IG115:
       test     rax, rax
       jl       G_M000_IG343
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rcx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG342
       mov      rax, gword ptr [rcx+8*rdi+0x10]
       jmp      G_M000_IG204
 
G_M000_IG116:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG127
 
G_M000_IG117:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x270], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x2C8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2C8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x270]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG128
 
G_M000_IG118:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG174
 
G_M000_IG119:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x258], xmm0
       test     rsi, rsi
       je       G_M000_IG126
 
G_M000_IG120:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG122
 
G_M000_IG121:
       vmovsd   xmm1, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x268], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x258]
       vaddsd   xmm1, xmm0, qword ptr [rbp-0x268]
       vmovsd   qword ptr [rax+0x08], xmm1
       mov      rdx, rax
       jmp      SHORT G_M000_IG128
 
G_M000_IG122:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG125
 
G_M000_IG123:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rsi+0x08]
       lea      rsi, [rbp-0x260]
       mov      edi, 1
       vmovsd   xmm0, qword ptr [rbp-0x258]
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x358]
       mov      rsi, gword ptr [rbp+0x340]
       je       SHORT G_M000_IG127
 
G_M000_IG124:
       mov      rdx, gword ptr [rbp-0x260]
       jmp      SHORT G_M000_IG128
 
G_M000_IG125:
       mov      rax, gword ptr [rbp+0x358]
       jmp      SHORT G_M000_IG127
 
G_M000_IG126:
       mov      rax, gword ptr [rbp+0x358]
 
G_M000_IG127:
       mov      rdx, rsi
       mov      rsi, rax
       mov      edi, 257
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG128:
       xor      rsi, rsi
       mov      gword ptr [rbp-0x260], rsi
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x790]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG129:
       mov      rdi, r13
       call     [RyuPy.Runtime.PyOps:IterNextOk(System.Object):bool]
       test     eax, eax
       je       G_M000_IG205
 
G_M000_IG130:
       mov      rdi, r13
       call     [RyuPy.Runtime.PyOps:IterValue(System.Object):System.Object]
       mov      gword ptr [rbp+0x820], rax
       mov      edi, 99
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [r12]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG144
 
G_M000_IG131:
       mov      edi, 99
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp+0x820]
       mov      gword ptr [rbp+0x7A0], rax
       mov      edi, 99
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x7A0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x3E8], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x7A0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdi, rbx
       mov      rcx, rax
       mov      rdx, gword ptr [rbp+0x3E8]
       mov      rsi, rdx
       test     rdi, rdi
       je       SHORT G_M000_IG142
 
G_M000_IG132:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rdi], r8
       je       SHORT G_M000_IG134
 
G_M000_IG133:
       jmp      SHORT G_M000_IG142
 
G_M000_IG134:
       test     rax, rax
       je       SHORT G_M000_IG141
 
G_M000_IG135:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG137
 
G_M000_IG136:
       jmp      SHORT G_M000_IG142
 
G_M000_IG137:
       test     rdx, rdx
       je       SHORT G_M000_IG140
 
G_M000_IG138:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG142
 
G_M000_IG139:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       vmovsd   qword ptr [rdx+0x08], xmm0
       jmp      SHORT G_M000_IG143
 
G_M000_IG140:
       jmp      SHORT G_M000_IG142
 
G_M000_IG141:
 
G_M000_IG142:
       mov      rdx, rbx
       mov      rcx, rax
       mov      edi, 74
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp+0x3E8]
       mov      edi, 75
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG143:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x820]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG144:
       mov      edi, 100
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [r12]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG158
 
G_M000_IG145:
       mov      edi, 100
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp+0x820]
       mov      gword ptr [rbp+0x798], rax
       mov      edi, 100
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x798]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x3A0], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x798]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdi, rbx
       mov      rcx, rax
       mov      rdx, gword ptr [rbp+0x3A0]
       mov      rsi, rdx
       test     rdi, rdi
       je       SHORT G_M000_IG156
 
G_M000_IG146:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rdi], r8
       je       SHORT G_M000_IG148
 
G_M000_IG147:
       jmp      SHORT G_M000_IG156
 
G_M000_IG148:
       test     rax, rax
       je       SHORT G_M000_IG155
 
G_M000_IG149:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG151
 
G_M000_IG150:
       jmp      SHORT G_M000_IG156
 
G_M000_IG151:
       test     rdx, rdx
       je       SHORT G_M000_IG154
 
G_M000_IG152:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG156
 
G_M000_IG153:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp+0x50], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0x50]
       vmovsd   qword ptr [rdx+0x08], xmm0
       jmp      SHORT G_M000_IG157
 
G_M000_IG154:
       jmp      SHORT G_M000_IG156
 
G_M000_IG155:
 
G_M000_IG156:
       mov      rdx, rbx
       mov      rcx, rax
       mov      edi, 76
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp+0x3A0]
       mov      edi, 77
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG157:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x820]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG158:
       mov      edi, 101
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [r12]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG129
 
G_M000_IG159:
       mov      edi, 101
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp+0x820]
       mov      gword ptr [rbp+0x790], rax
       mov      edi, 101
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x790]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x358], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x790]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       test     rbx, rbx
       je       G_M000_IG192
 
G_M000_IG160:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdx
       jne      G_M000_IG186
 
G_M000_IG161:
       mov      rsi, qword ptr [rbx+0x08]
       test     rax, rax
       je       G_M000_IG192
 
G_M000_IG162:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rax], rdx
       jne      G_M000_IG184
 
G_M000_IG163:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x1F0], rax
       lea      rdi, [rbp-0x208]
       mov      rdx, rsi
       mulx     rdx, rcx, rax
       mov      qword ptr [rdi], rcx
       mov      rcx, qword ptr [rbp-0x208]
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
       je       G_M000_IG180
 
G_M000_IG164:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x200], xmm0
       lea      rdi, [rbp-0x200]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x200]
       mov      gword ptr [rbp-0x310], rax
       mov      ecx, dword ptr [rbp-0x1F8]
       mov      dword ptr [rbp-0x2D4], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x218], xmm0
       lea      rdi, [rbp-0x218]
       mov      rsi, qword ptr [rbp-0x1F0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x218]
       mov      r9d, dword ptr [rbp-0x210]
       mov      rcx, gword ptr [rbp-0x310]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG169
 
G_M000_IG165:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x238], xmm0
       movsxd   rsi, dword ptr [rbp-0x2D4]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x238]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG166:
       vmovdqu  xmm0, xmmword ptr [rbp-0x238]
       vmovdqu  xmmword ptr [rbp-0x228], xmm0
 
G_M000_IG167:
       mov      rdi, gword ptr [rbp-0x228]
       mov      rsi, qword ptr [rbp-0x220]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG168:
       jmp      G_M000_IG193
 
G_M000_IG169:
       test     rcx, rcx
       je       SHORT G_M000_IG178
 
G_M000_IG170:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG171:
       test     rax, rax
       je       SHORT G_M000_IG179
 
G_M000_IG172:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG173:
       mov      edx, dword ptr [rbp-0x2D4]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x228], rax
       mov      qword ptr [rbp-0x220], rdx
       jmp      SHORT G_M000_IG167
 
G_M000_IG174:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG127
 
G_M000_IG175:
       mov      rdi, rax
       test     rsi, rsi
       je       G_M000_IG127
 
G_M000_IG176:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       jne      G_M000_IG127
 
G_M000_IG177:
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rdx, rax
       jmp      G_M000_IG128
 
G_M000_IG178:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG171
 
G_M000_IG179:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG173
 
G_M000_IG180:
       mov      rax, rcx
       mov      qword ptr [rbp-0x240], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG183
 
G_M000_IG181:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x240]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG182:
       jmp      G_M000_IG168
 
G_M000_IG183:
       test     rcx, rcx
       jl       G_M000_IG343
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG342
       mov      rax, gword ptr [rcx+8*rax+0x10]
       jmp      SHORT G_M000_IG182
 
G_M000_IG184:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG192
 
G_M000_IG185:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x1E8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x248], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x248]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x1E8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG193
 
G_M000_IG186:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG192
 
G_M000_IG187:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x1D8], xmm0
       test     rax, rax
       je       G_M000_IG192
 
G_M000_IG188:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG190
 
G_M000_IG189:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x1E0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1D8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x1E0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG193
 
G_M000_IG190:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG192
 
G_M000_IG191:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x250], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1D8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x250]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG193
 
G_M000_IG192:
       mov      rdx, rax
       mov      rsi, rbx
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG193:
       mov      rsi, rax
       mov      gword ptr [rbp+0x340], rsi
       xor      rdi, rdi
       mov      gword ptr [rbp-0x260], rdi
       mov      rax, gword ptr [rbp+0x358]
       test     rax, rax
       je       G_M000_IG127
 
G_M000_IG194:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG118
 
G_M000_IG195:
       mov      rcx, qword ptr [rax+0x08]
       test     rsi, rsi
       je       G_M000_IG127
 
G_M000_IG196:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG116
 
G_M000_IG197:
       mov      rax, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x278], rax
       lea      rdx, [rcx+rax]
       mov      qword ptr [rbp-0x280], rdx
       mov      rdi, rcx
       xor      rdi, rdx
       mov      rsi, rax
       xor      rsi, rdx
       and      rdi, rsi
       jge      G_M000_IG202
 
G_M000_IG198:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x290], xmm0
       lea      rdi, [rbp-0x290]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG199:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x290]
       vmovdqu32 xmmword ptr [rbp-0x2A0], xmm0
 
G_M000_IG200:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2C0], xmm0
       lea      rdi, [rbp-0x2C0]
       mov      rsi, qword ptr [rbp-0x278]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x2A0]
       mov      rsi, qword ptr [rbp-0x298]
       mov      rdx, gword ptr [rbp-0x2C0]
       mov      rcx, qword ptr [rbp-0x2B8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x2B0], rax
       mov      qword ptr [rbp-0x2A8], rdx
       mov      rdi, gword ptr [rbp-0x2B0]
       mov      rsi, qword ptr [rbp-0x2A8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG201:
       mov      rdx, rax
       jmp      G_M000_IG128
 
G_M000_IG202:
       lea      rax, [rdx+0x05]
       cmp      rax, 261
       jbe      G_M000_IG115
 
G_M000_IG203:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x280]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG204:
       jmp      SHORT G_M000_IG201
 
G_M000_IG205:
       mov      r13, qword ptr [rbp+0x760]
       mov      rax, qword ptr [rbp+0x750]
       add      r13, rax
       jo       G_M000_IG343
       mov      qword ptr [rbp+0x750], rax
       test     rax, rax
       setg     dil
       movzx    rdi, dil
       mov      rcx, qword ptr [rbp+0x758]
       cmp      r13, rcx
       setl     dl
       movzx    rdx, dl
       and      edx, edi
       mov      qword ptr [rbp+0x758], rcx
       cmp      r13, rcx
       setg     sil
       movzx    rsi, sil
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, esi
       or       edi, edx
       je       G_M000_IG338
 
G_M000_IG206:
       mov      edi, dword ptr [r14]
       test     edi, edi
       jle      SHORT G_M000_IG209
 
G_M000_IG207:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rsi, rdx
       dec      edi
       mov      r8d, edi
       cmp      r8d, dword ptr [rsi+0x08]
       jae      G_M000_IG342
       cmp      dword ptr [rsi+4*r8+0x10], 81
       je       SHORT G_M000_IG209
 
G_M000_IG208:
       cmp      edi, dword ptr [rdx+0x08]
       jae      G_M000_IG342
       mov      edi, edi
       mov      dword ptr [rdx+4*rdi+0x10], 81
 
G_M000_IG209:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r14, rax
       mov      rdi, gword ptr [rbp+0x838]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rdx, rax
       mov      qword ptr [rbp+0x700], rdx
       mov      ecx, 1
       mov      qword ptr [rbp+0x760], r13
       mov      r13, r14
       mov      r14, rcx
       jmp      G_M000_IG94
 
G_M000_IG210:
       test     rax, rax
       jl       G_M000_IG343
       mov      rdi, rax
       mov      rsi, 0xD1FFAB1E
       mov      rcx, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG342
       mov      rax, gword ptr [rcx+8*rdi+0x10]
       mov      qword ptr [rbp+0x700], rdx
       jmp      G_M000_IG97
 
G_M000_IG211:
       mov      rsi, qword ptr [rdx+0x08]
       test     rcx, rcx
       je       G_M000_IG222
 
G_M000_IG212:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG220
 
G_M000_IG213:
       mov      rcx, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x48], rcx
       lea      rdx, [rsi+rcx]
       mov      qword ptr [rbp-0x50], rdx
       mov      rdi, rsi
       xor      rdi, rdx
       mov      r8, rcx
       xor      r8, rdx
       and      rdi, r8
       jge      SHORT G_M000_IG216
 
G_M000_IG214:
       mov      gword ptr [rbp+0x820], rax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x60], xmm0
       lea      rdi, [rbp-0x60]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x60]
       mov      gword ptr [rbp-0x300], rax
       mov      ecx, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0x2CC], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       mov      rsi, qword ptr [rbp-0x48]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x300]
       mov      esi, dword ptr [rbp-0x2CC]
       mov      rdx, gword ptr [rbp-0x70]
       mov      rcx, qword ptr [rbp-0x68]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG215:
       mov      rdi, rax
       jmp      G_M000_IG110
 
G_M000_IG216:
       lea      rcx, [rdx+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG219
 
G_M000_IG217:
       mov      gword ptr [rbp+0x820], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x50]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG218:
       jmp      SHORT G_M000_IG215
 
G_M000_IG219:
       test     rcx, rcx
       jl       G_M000_IG343
       mov      rdi, rcx
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG342
       mov      rdi, gword ptr [rcx+8*rdi+0x10]
       mov      gword ptr [rbp+0x820], rax
       mov      rax, rdi
       jmp      SHORT G_M000_IG218
 
G_M000_IG220:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       mov      gword ptr [rbp+0x820], rax
       jne      G_M000_IG109
 
G_M000_IG221:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x78], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x78]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdi, rax
       jmp      G_M000_IG110
 
G_M000_IG222:
       mov      gword ptr [rbp+0x820], rax
       jmp      G_M000_IG109
 
G_M000_IG223:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     rcx, rcx
       je       G_M000_IG228
 
G_M000_IG224:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      SHORT G_M000_IG226
 
G_M000_IG225:
       mov      gword ptr [rbp+0x820], rax
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdi, rax
       jmp      G_M000_IG110
 
G_M000_IG226:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       mov      gword ptr [rbp+0x820], rax
       jne      G_M000_IG109
 
G_M000_IG227:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x80], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x80]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdi, rax
       jmp      G_M000_IG110
 
G_M000_IG228:
       mov      gword ptr [rbp+0x820], rax
       jmp      G_M000_IG109
 
G_M000_IG229:
       mov      rdi, rdx
       mov      gword ptr [rbp-0x2E8], rdi
       test     rcx, rcx
       je       G_M000_IG239
 
G_M000_IG230:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rsi
       je       SHORT G_M000_IG232
 
G_M000_IG231:
       mov      gword ptr [rbp+0x820], rax
       jmp      G_M000_IG109
 
G_M000_IG232:
       mov      gword ptr [rbp-0x2F0], rcx
       cmp      dword ptr [rdi+0x08], 0
       je       G_M000_IG237
 
G_M000_IG233:
       cmp      dword ptr [rcx+0x08], 0
       je       G_M000_IG236
 
G_M000_IG234:
       mov      edx, dword ptr [rdi+0x08]
       mov      dword ptr [rbp-0x84], edx
       mov      esi, edx
       add      esi, dword ptr [rcx+0x08]
       js       G_M000_IG341
       mov      gword ptr [rbp+0x820], rax
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      rdi, rax
       mov      gword ptr [rbp-0x2F8], rdi
       cmp      byte  ptr [rdi], dil
       add      rdi, 12
       mov      rax, gword ptr [rbp-0x2E8]
       lea      rsi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [rax+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       movsxd   rdi, dword ptr [rbp-0x84]
       mov      rax, gword ptr [rbp-0x2F8]
       lea      rdi, bword ptr [rax+2*rdi+0x0C]
       mov      rcx, gword ptr [rbp-0x2F0]
       lea      rsi, bword ptr [rcx+0x0C]
       mov      edx, dword ptr [rcx+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rdi, gword ptr [rbp-0x2F8]
       mov      rax, gword ptr [rbp+0x820]
 
G_M000_IG235:
       mov      gword ptr [rbp+0x820], rax
       jmp      G_M000_IG110
 
G_M000_IG236:
       jmp      SHORT G_M000_IG235
 
G_M000_IG237:
       cmp      dword ptr [rcx+0x08], 0
       je       G_M000_IG340
 
G_M000_IG238:
       mov      rdi, rcx
       jmp      SHORT G_M000_IG235
 
G_M000_IG239:
       mov      gword ptr [rbp+0x820], rax
       jmp      G_M000_IG109
 
G_M000_IG240:
       test     rdx, rdx
       jl       G_M000_IG343
       mov      rsi, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG342
       test     rdx, rdx
       jl       G_M000_IG343
       mov      rsi, rdx
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       mov      qword ptr [rbp+0x698], rcx
       mov      qword ptr [rbp+0x690], rax
       jmp      G_M000_IG39
 
G_M000_IG241:
       mov      qword ptr [rbp-0x2E0], rdx
       jmp      G_M000_IG42
 
G_M000_IG242:
       mov      qword ptr [rbp-0x2E0], rdx
       jmp      G_M000_IG42
 
G_M000_IG243:
       mov      rsi, qword ptr [rcx+0x08]
       test     rax, rax
       je       G_M000_IG51
 
G_M000_IG244:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG252
 
G_M000_IG245:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0xA8], rax
       mov      rcx, rsi
       sub      rcx, rax
       mov      qword ptr [rbp-0xB0], rcx
       mov      rdi, rsi
       xor      rdi, rax
       mov      rdx, rsi
       xor      rdx, rcx
       and      rdi, rdx
       jge      G_M000_IG248
 
G_M000_IG246:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xC0], xmm0
       lea      rdi, [rbp-0xC0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xC0]
       mov      gword ptr [rbp-0x308], rax
       mov      ecx, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0x2D0], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xD0], xmm0
       lea      rdi, [rbp-0xD0]
       mov      rsi, qword ptr [rbp-0xA8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x308]
       mov      esi, dword ptr [rbp-0x2D0]
       mov      rdx, gword ptr [rbp-0xD0]
       mov      rcx, qword ptr [rbp-0xC8]
       call     [System.Numerics.BigInteger:op_Subtraction(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG247:
       jmp      G_M000_IG52
 
G_M000_IG248:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG251
 
G_M000_IG249:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0xB0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG250:
       jmp      SHORT G_M000_IG247
 
G_M000_IG251:
       test     rax, rax
       jl       G_M000_IG343
       mov      rdi, rax
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG342
       test     rax, rax
       jl       G_M000_IG343
       mov      rdi, rax
       mov      rax, gword ptr [rcx+8*rdi+0x10]
       jmp      SHORT G_M000_IG250
 
G_M000_IG252:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG51
 
G_M000_IG253:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xD8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xD8]
       vsubsd   xmm0, xmm1, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG52
 
G_M000_IG254:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm0
       test     rax, rax
       je       G_M000_IG51
 
G_M000_IG255:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG257
 
G_M000_IG256:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x90]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG52
 
G_M000_IG257:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG51
 
G_M000_IG258:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xE0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x90]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0xE0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG52
 
G_M000_IG259:
       mov      rsi, qword ptr [rcx+0x08]
       test     rax, rax
       je       G_M000_IG58
 
G_M000_IG260:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG270
 
G_M000_IG261:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x100], rax
       mov      rcx, rsi
       sub      rcx, rax
       mov      qword ptr [rbp-0x108], rcx
       mov      rdi, rsi
       xor      rdi, rax
       mov      rdx, rsi
       xor      rdx, rcx
       and      rdi, rdx
       jge      G_M000_IG266
 
G_M000_IG262:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x118], xmm0
       lea      rdi, [rbp-0x118]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG263:
       vmovdqu  xmm0, xmmword ptr [rbp-0x118]
       vmovdqu  xmmword ptr [rbp-0x128], xmm0
 
G_M000_IG264:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x148], xmm0
       lea      rdi, [rbp-0x148]
       mov      rsi, qword ptr [rbp-0x100]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x128]
       mov      rsi, qword ptr [rbp-0x120]
       mov      rdx, gword ptr [rbp-0x148]
       mov      rcx, qword ptr [rbp-0x140]
       call     [System.Numerics.BigInteger:op_Subtraction(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x138], rax
       mov      qword ptr [rbp-0x130], rdx
       mov      rdi, gword ptr [rbp-0x138]
       mov      rsi, qword ptr [rbp-0x130]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG265:
       jmp      G_M000_IG59
 
G_M000_IG266:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG269
 
G_M000_IG267:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x108]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG268:
       jmp      SHORT G_M000_IG265
 
G_M000_IG269:
       test     rax, rax
       jl       G_M000_IG343
       mov      rdi, rax
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG342
       test     rax, rax
       jl       G_M000_IG343
       mov      rdi, rax
       mov      rax, gword ptr [rcx+8*rdi+0x10]
       jmp      SHORT G_M000_IG268
 
G_M000_IG270:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG58
 
G_M000_IG271:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xF8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x150], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x150]
       vsubsd   xmm0, xmm1, qword ptr [rbp-0xF8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG59
 
G_M000_IG272:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0xE8], xmm0
       test     rax, rax
       je       G_M000_IG58
 
G_M000_IG273:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG275
 
G_M000_IG274:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xF0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xE8]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG59
 
G_M000_IG275:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG58
 
G_M000_IG276:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x158], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xE8]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x158]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG59
 
G_M000_IG277:
       mov      rsi, qword ptr [rcx+0x08]
       test     rax, rax
       je       G_M000_IG65
 
G_M000_IG278:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG288
 
G_M000_IG279:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x178], rax
       mov      rcx, rsi
       sub      rcx, rax
       mov      qword ptr [rbp-0x180], rcx
       mov      rdi, rsi
       xor      rdi, rax
       mov      rdx, rsi
       xor      rdx, rcx
       and      rdi, rdx
       jge      G_M000_IG284
 
G_M000_IG280:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x190], xmm0
       lea      rdi, [rbp-0x190]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG281:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x190]
       vmovdqu32 xmmword ptr [rbp-0x1A0], xmm0
 
G_M000_IG282:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1C0], xmm0
       lea      rdi, [rbp-0x1C0]
       mov      rsi, qword ptr [rbp-0x178]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x1A0]
       mov      rsi, qword ptr [rbp-0x198]
       mov      rdx, gword ptr [rbp-0x1C0]
       mov      rcx, qword ptr [rbp-0x1B8]
       call     [System.Numerics.BigInteger:op_Subtraction(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x1B0], rax
       mov      qword ptr [rbp-0x1A8], rdx
       mov      rdi, gword ptr [rbp-0x1B0]
       mov      rsi, qword ptr [rbp-0x1A8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG283:
       mov      rcx, rax
       jmp      G_M000_IG66
 
G_M000_IG284:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG287
 
G_M000_IG285:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x180]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG286:
       jmp      SHORT G_M000_IG283
 
G_M000_IG287:
       test     rax, rax
       jl       G_M000_IG343
       mov      rdi, rax
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG342
       test     rax, rax
       jl       G_M000_IG343
       mov      rdi, rax
       mov      rax, gword ptr [rcx+8*rdi+0x10]
       jmp      SHORT G_M000_IG286
 
G_M000_IG288:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG65
 
G_M000_IG289:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x170], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x1C8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1C8]
       vsubsd   xmm0, xmm1, qword ptr [rbp-0x170]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rcx, rax
       jmp      G_M000_IG66
 
G_M000_IG290:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x160], xmm0
       test     rax, rax
       je       G_M000_IG65
 
G_M000_IG291:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG293
 
G_M000_IG292:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x168], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x160]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x168]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rcx, rax
       jmp      G_M000_IG66
 
G_M000_IG293:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG65
 
G_M000_IG294:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x1D0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x160]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x1D0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rcx, rax
       jmp      G_M000_IG66
 
G_M000_IG295:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       SHORT G_M000_IG297
 
G_M000_IG296:
       jmp      G_M000_IG08
 
G_M000_IG297:
       mov      rsi, gword ptr [rbp+0x7D8]
       test     rsi, rsi
       je       SHORT G_M000_IG305
 
G_M000_IG298:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG300
 
G_M000_IG299:
       jmp      G_M000_IG08
 
G_M000_IG300:
       test     rax, rax
       je       SHORT G_M000_IG304
 
G_M000_IG301:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG303
 
G_M000_IG302:
       jmp      G_M000_IG08
 
G_M000_IG303:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   xmm1, qword ptr [rsi+0x08]
       vmovsd   xmm2, qword ptr [rdx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vsubsd   xmm0, xmm2, xmm0
       vmovsd   qword ptr [rbp+0x280], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0x280]
       vmovsd   qword ptr [rdx+0x08], xmm0
       jmp      G_M000_IG09
 
G_M000_IG304:
       jmp      G_M000_IG08
 
G_M000_IG305:
       jmp      G_M000_IG08
 
G_M000_IG306:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       SHORT G_M000_IG308
 
G_M000_IG307:
       jmp      G_M000_IG12
 
G_M000_IG308:
       mov      rsi, gword ptr [rbp+0x7D8]
       test     rsi, rsi
       je       SHORT G_M000_IG316
 
G_M000_IG309:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG311
 
G_M000_IG310:
       jmp      G_M000_IG12
 
G_M000_IG311:
       test     rax, rax
       je       SHORT G_M000_IG315
 
G_M000_IG312:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG314
 
G_M000_IG313:
       jmp      G_M000_IG12
 
G_M000_IG314:
       mov      gword ptr [rbp+0x800], rcx
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   xmm1, qword ptr [rsi+0x08]
       vmovsd   xmm2, qword ptr [rdx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vsubsd   xmm0, xmm2, xmm0
       vmovsd   qword ptr [rbp+0x230], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0x230]
       vmovsd   qword ptr [rdx+0x08], xmm0
       jmp      G_M000_IG13
 
G_M000_IG315:
       jmp      G_M000_IG12
 
G_M000_IG316:
       jmp      G_M000_IG12
 
G_M000_IG317:
       mov      r9, 0xD1FFAB1E
       cmp      qword ptr [rdi], r9
       je       SHORT G_M000_IG319
 
G_M000_IG318:
       jmp      G_M000_IG16
 
G_M000_IG319:
       test     rdx, rdx
       je       SHORT G_M000_IG326
 
G_M000_IG320:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       SHORT G_M000_IG322
 
G_M000_IG321:
       jmp      G_M000_IG16
 
G_M000_IG322:
       test     rax, rax
       je       SHORT G_M000_IG325
 
G_M000_IG323:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG16
 
G_M000_IG324:
       mov      gword ptr [rbp+0x7F8], rcx
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   xmm1, qword ptr [rsi+0x08]
       vmovsd   xmm2, qword ptr [r8+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vsubsd   xmm0, xmm2, xmm0
       vmovsd   qword ptr [rbp+0x1E0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0x1E0]
       vmovsd   qword ptr [rdx+0x08], xmm0
       jmp      G_M000_IG17
 
G_M000_IG325:
       jmp      G_M000_IG16
 
G_M000_IG326:
       jmp      G_M000_IG16
 
G_M000_IG327:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       SHORT G_M000_IG329
 
G_M000_IG328:
       jmp      G_M000_IG20
 
G_M000_IG329:
       mov      rsi, gword ptr [rbp+0x7E0]
       test     rsi, rsi
       je       G_M000_IG337
 
G_M000_IG330:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rsi], r8
       je       SHORT G_M000_IG332
 
G_M000_IG331:
       mov      gword ptr [rbp+0x7E0], rsi
       jmp      G_M000_IG20
 
G_M000_IG332:
       test     rax, rax
       je       SHORT G_M000_IG336
 
G_M000_IG333:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rax], r8
       je       SHORT G_M000_IG335
 
G_M000_IG334:
       mov      gword ptr [rbp+0x7E0], rsi
       jmp      G_M000_IG20
 
G_M000_IG335:
       vmovsd   xmm0, qword ptr [rdi+0x08]
       mov      gword ptr [rbp+0x7E0], rsi
       vmovsd   xmm1, qword ptr [rsi+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp+0x190], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0x190]
       vmovsd   qword ptr [rdx+0x08], xmm0
       jmp      G_M000_IG21
 
G_M000_IG336:
       mov      gword ptr [rbp+0x7E0], rsi
       jmp      G_M000_IG20
 
G_M000_IG337:
       mov      gword ptr [rbp+0x7E0], rsi
       jmp      G_M000_IG20
 
G_M000_IG338:
       mov      rdi, 0xD1FFAB1E
       mov      rbx, gword ptr [rdi]
       mov      rdi, qword ptr [rbp+0x788]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, rbx
 
G_M000_IG339:
       add      rsp, 0xB78
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG340:
       mov      rdi, 0xD1FFAB1E
       jmp      G_M000_IG235
 
G_M000_IG341:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG342:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG343:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 9954

; Assembly listing for method RyuPy.Generated.Module:m0_advance(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0xa5f
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 44 inlinees with PGO data; 30 single block inlinees; 57 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 912
       mov      qword ptr [rsp+0xC08], r15
       mov      qword ptr [rsp+0xC00], r14
       mov      qword ptr [rsp+0xBF8], r13
       mov      qword ptr [rsp+0xBF0], r12
       mov      qword ptr [rsp+0xBE8], rbx
       lea      rbp, [rsp+0x390]
       xor      eax, eax
       mov      qword ptr [rbp-0x348], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x340], xmm8
       mov      rax, -720
       vmovdqa  xmmword ptr [rbp+rax-0x60], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      qword ptr [rbp-0x60], rax
       mov      rbx, gword ptr [rbp+0x848]
       mov      r14, gword ptr [rbp+0x820]
       mov      r13, gword ptr [rbp+0x810]
       mov      r12, gword ptr [rbp+0x800]
       mov      rsi, gword ptr [rbp+0x7D8]
 
G_M000_IG02:
       cmp      qword ptr [rbp+0x750], 0
       setg     r15b
       movzx    r15, r15b
       mov      dword ptr [rbp-0x358], r15d
       mov      rcx, gword ptr [rbp+0x7F8]
       jmp      G_M000_IG129
 
G_M000_IG03:
       mov      rsi, gword ptr [rbp+0x7E0]
       test     rsi, rsi
       je       G_M000_IG11
 
G_M000_IG04:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rsi], r8
       je       SHORT G_M000_IG06
 
G_M000_IG05:
       mov      gword ptr [rbp+0x7E0], rsi
       jmp      SHORT G_M000_IG12
 
G_M000_IG06:
       test     rax, rax
       je       SHORT G_M000_IG10
 
G_M000_IG07:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rax], r8
       je       SHORT G_M000_IG09
 
G_M000_IG08:
       mov      gword ptr [rbp+0x7E0], rsi
       jmp      SHORT G_M000_IG12
 
G_M000_IG09:
       vmovsd   xmm0, qword ptr [rdi+0x08]
       mov      gword ptr [rbp+0x7E0], rsi
       vmovsd   xmm1, qword ptr [rsi+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp+0x190], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0x190]
       vmovsd   qword ptr [rdx+0x08], xmm0
       jmp      SHORT G_M000_IG13
 
G_M000_IG10:
       mov      gword ptr [rbp+0x7E0], rsi
       jmp      SHORT G_M000_IG12
 
G_M000_IG11:
       mov      gword ptr [rbp+0x7E0], rsi
 
G_M000_IG12:
       mov      rdx, rcx
       mov      rcx, gword ptr [rbp+0x7E0]
       mov      edi, 68
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp+0x4A8]
       mov      edi, 69
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG13:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r13
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG14:
       mov      edi, 96
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [r15]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG29
 
G_M000_IG15:
       mov      edi, 96
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      gword ptr [rbp+0x7B0], r13
       mov      edi, 96
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x7B0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x470], rax
       mov      rdi, r12
       mov      rcx, rax
       test     rdi, rdi
       je       G_M000_IG27
 
G_M000_IG16:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rdx
       je       SHORT G_M000_IG18
 
G_M000_IG17:
       jmp      G_M000_IG27
 
G_M000_IG18:
       mov      rdx, gword ptr [rbp+0x7E0]
       test     rdx, rdx
       je       G_M000_IG26
 
G_M000_IG19:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       je       SHORT G_M000_IG21
 
G_M000_IG20:
       mov      gword ptr [rbp+0x7E0], rdx
       jmp      SHORT G_M000_IG27
 
G_M000_IG21:
       test     rax, rax
       je       SHORT G_M000_IG25
 
G_M000_IG22:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       je       SHORT G_M000_IG24
 
G_M000_IG23:
       mov      gword ptr [rbp+0x7E0], rdx
       jmp      SHORT G_M000_IG27
 
G_M000_IG24:
       vmovsd   xmm0, qword ptr [rdi+0x08]
       mov      gword ptr [rbp+0x7E0], rdx
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp+0x140], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0x140]
       vmovsd   qword ptr [rdx+0x08], xmm0
       jmp      SHORT G_M000_IG28
 
G_M000_IG25:
       mov      gword ptr [rbp+0x7E0], rdx
       jmp      SHORT G_M000_IG27
 
G_M000_IG26:
       mov      gword ptr [rbp+0x7E0], rdx
 
G_M000_IG27:
       mov      rdx, r12
       mov      rcx, gword ptr [rbp+0x7E0]
       mov      edi, 70
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp+0x470]
       mov      edi, 71
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG28:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r13
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG29:
       mov      edi, 97
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [r15]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG39
 
G_M000_IG30:
       mov      edi, 97
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 97
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r13
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      r12, rax
       mov      rax, gword ptr [rbp+0x7F8]
       mov      rdi, rax
       mov      rcx, gword ptr [rbp+0x7E0]
       mov      rdx, rcx
       mov      rsi, r12
       test     rdi, rdi
       je       SHORT G_M000_IG37
 
G_M000_IG31:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rdi], r8
       jne      SHORT G_M000_IG37
 
G_M000_IG32:
       test     rcx, rcx
       je       SHORT G_M000_IG37
 
G_M000_IG33:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rcx], r8
       jne      SHORT G_M000_IG37
 
G_M000_IG34:
       test     r12, r12
       je       SHORT G_M000_IG37
 
G_M000_IG35:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [r12], r8
       jne      SHORT G_M000_IG37
 
G_M000_IG36:
       vmovsd   xmm0, qword ptr [rdi+0x08]
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp+0xF0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0xF0]
       vmovsd   qword ptr [rdx+0x08], xmm0
       jmp      SHORT G_M000_IG38
 
G_M000_IG37:
       mov      rdx, rax
       mov      edi, 72
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, r12
       mov      edi, 73
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG38:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r13
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG39:
       mov      r13, qword ptr [rbp+0x6A0]
       mov      r12, qword ptr [rbp+0x690]
       add      r13, r12
       jo       G_M000_IG471
 
G_M000_IG40:
       test     r12, r12
       setg     dil
       movzx    rdi, dil
       mov      rax, qword ptr [rbp+0x698]
       cmp      r13, rax
       setl     cl
       movzx    rcx, cl
       and      ecx, edi
       cmp      r13, rax
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, ecx
       je       G_M000_IG142
 
G_M000_IG41:
       lea      rcx, [r13+0x05]
       cmp      rcx, 261
       jbe      G_M000_IG311
 
G_M000_IG42:
       mov      qword ptr [rbp+0x698], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r13
       mov      rsi, rax
 
G_M000_IG43:
       mov      rcx, 0xD1FFAB1E
       mov      qword ptr [rbp-0x360], rcx
       mov      edi, dword ptr [rcx]
       test     edi, edi
       jle      SHORT G_M000_IG46
 
G_M000_IG44:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       dec      edi
       mov      r8d, edi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      G_M000_IG470
       cmp      dword ptr [rdx+4*r8+0x10], 84
       je       SHORT G_M000_IG46
 
G_M000_IG45:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      edi, dword ptr [rdx+0x08]
       jae      G_M000_IG470
       mov      edi, edi
       mov      dword ptr [rdx+4*rdi+0x10], 84
 
G_M000_IG46:
       mov      rdi, gword ptr [rbp+0x850]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x810], rax
       mov      rcx, qword ptr [rbp-0x360]
       mov      esi, dword ptr [rcx]
       test     esi, esi
       jle      SHORT G_M000_IG49
 
G_M000_IG47:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       dec      esi
       mov      edx, esi
       cmp      edx, dword ptr [rdi+0x08]
       jae      G_M000_IG470
       cmp      dword ptr [rdi+4*rdx+0x10], 85
       je       SHORT G_M000_IG49
 
G_M000_IG48:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG470
       mov      esi, esi
       mov      dword ptr [rdi+4*rsi+0x10], 85
 
G_M000_IG49:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x650], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x810]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x640], rax
       mov      rcx, gword ptr [rbp+0x650]
       test     rcx, rcx
       je       SHORT G_M000_IG52
 
G_M000_IG50:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG312
 
G_M000_IG51:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG329
 
G_M000_IG52:
       mov      rdx, rcx
       test     rdx, rdx
       je       SHORT G_M000_IG55
 
G_M000_IG53:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       je       SHORT G_M000_IG55
 
G_M000_IG54:
       xor      rdx, rdx
 
G_M000_IG55:
       test     rdx, rdx
       je       SHORT G_M000_IG58
 
G_M000_IG56:
       mov      rdx, rax
       test     rdx, rdx
       je       SHORT G_M000_IG58
 
G_M000_IG57:
       mov      rdx, qword ptr [rdx]
 
G_M000_IG58:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 2
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rcx, rax
 
G_M000_IG59:
       mov      gword ptr [rbp+0x808], rcx
       mov      rsi, qword ptr [rbp-0x360]
       cmp      dword ptr [rsi], 0
       jle      SHORT G_M000_IG62
 
G_M000_IG60:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      ecx, dword ptr [rsi]
       dec      ecx
       cmp      ecx, dword ptr [rdi+0x08]
       jae      G_M000_IG470
       cmp      dword ptr [rdi+4*rcx+0x10], 86
       je       SHORT G_M000_IG62
 
G_M000_IG61:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      esi, dword ptr [rsi]
       dec      esi
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG470
       mov      dword ptr [rdi+4*rsi+0x10], 86
 
G_M000_IG62:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x628], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x810]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x618], rax
       mov      rcx, gword ptr [rbp+0x628]
       test     rcx, rcx
       je       SHORT G_M000_IG65
 
G_M000_IG63:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG340
 
G_M000_IG64:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG357
 
G_M000_IG65:
       mov      rdx, rcx
       test     rdx, rdx
       je       SHORT G_M000_IG68
 
G_M000_IG66:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       je       SHORT G_M000_IG68
 
G_M000_IG67:
       xor      rdx, rdx
 
G_M000_IG68:
       test     rdx, rdx
       je       SHORT G_M000_IG71
 
G_M000_IG69:
       mov      rdx, rax
       test     rdx, rdx
       je       SHORT G_M000_IG71
 
G_M000_IG70:
       mov      rdx, qword ptr [rdx]
 
G_M000_IG71:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 2
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG72:
       mov      gword ptr [rbp+0x800], rax
       mov      edi, 87
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x600], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x810]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x5F0], rax
       mov      rcx, gword ptr [rbp+0x600]
       test     rcx, rcx
       je       SHORT G_M000_IG75
 
G_M000_IG73:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG368
 
G_M000_IG74:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG387
 
G_M000_IG75:
       mov      rdx, rcx
       test     rdx, rdx
       je       SHORT G_M000_IG78
 
G_M000_IG76:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       je       SHORT G_M000_IG78
 
G_M000_IG77:
       xor      rdx, rdx
 
G_M000_IG78:
       test     rdx, rdx
       je       SHORT G_M000_IG81
 
G_M000_IG79:
       mov      rdx, rax
       test     rdx, rdx
       je       SHORT G_M000_IG81
 
G_M000_IG80:
       mov      rdx, qword ptr [rdx]
 
G_M000_IG81:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 2
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG82:
       mov      gword ptr [rbp+0x7F8], rax
       mov      edi, 88
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp+0x808]
       mov      rdi, rax
       test     rdi, rdi
       je       G_M000_IG93
 
G_M000_IG83:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rcx
       jne      G_M000_IG93
 
G_M000_IG84:
       mov      rcx, gword ptr [rbp+0x800]
       test     rcx, rcx
       je       G_M000_IG92
 
G_M000_IG85:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       SHORT G_M000_IG87
 
G_M000_IG86:
       mov      gword ptr [rbp+0x800], rcx
       jmp      G_M000_IG93
 
G_M000_IG87:
       mov      rdx, gword ptr [rbp+0x7F8]
       test     rdx, rdx
       je       G_M000_IG91
 
G_M000_IG88:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       SHORT G_M000_IG90
 
G_M000_IG89:
       mov      gword ptr [rbp+0x800], rcx
       mov      gword ptr [rbp+0x7F8], rdx
       jmp      SHORT G_M000_IG93
 
G_M000_IG90:
       mov      gword ptr [rbp+0x808], rax
       vmovsd   xmm0, qword ptr [rax+0x08]
       mov      gword ptr [rbp+0x800], rcx
       vmovsd   xmm1, qword ptr [rcx+0x08]
       mov      gword ptr [rbp+0x7F8], rdx
       vmovsd   xmm2, qword ptr [rdx+0x08]
       vmulsd   xmm0, xmm0, xmm0
       vmulsd   xmm1, xmm1, xmm1
       vaddsd   xmm0, xmm0, xmm1
       vmulsd   xmm1, xmm2, xmm2
       vaddsd   xmm0, xmm0, xmm1
       vmovsd   qword ptr [rbp+0x2D0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp+0x2D0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG94
 
G_M000_IG91:
       mov      gword ptr [rbp+0x800], rcx
       mov      gword ptr [rbp+0x7F8], rdx
       jmp      SHORT G_M000_IG93
 
G_M000_IG92:
       mov      gword ptr [rbp+0x800], rcx
 
G_M000_IG93:
       mov      rdx, rax
       mov      gword ptr [rbp+0x808], rax
       mov      rcx, rax
       mov      edi, 57
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x380], rax
       mov      rdx, gword ptr [rbp+0x800]
       mov      gword ptr [rbp+0x800], rdx
       mov      rcx, rdx
       mov      edi, 58
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x380]
       mov      edi, 59
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x388], rax
       mov      rdx, gword ptr [rbp+0x7F8]
       mov      gword ptr [rbp+0x7F8], rdx
       mov      rcx, rdx
       mov      edi, 60
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x388]
       mov      edi, 61
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
 
G_M000_IG94:
       mov      gword ptr [rbp+0x7F0], rax
       mov      edi, 89
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rax, gword ptr [rsi]
       mov      gword ptr [rbp+0x5B8], rax
       mov      rcx, gword ptr [rbp+0x7F0]
       test     rcx, rcx
       je       SHORT G_M000_IG98
 
G_M000_IG95:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rsi
       je       G_M000_IG398
 
G_M000_IG96:
       mov      rsi, 0xD1FFAB1E
       mov      rcx, gword ptr [rbp+0x7F0]
       cmp      qword ptr [rcx], rsi
       je       G_M000_IG409
 
G_M000_IG97:
       mov      rcx, gword ptr [rbp+0x7F0]
 
G_M000_IG98:
       mov      rdx, rcx
       test     rdx, rdx
       je       SHORT G_M000_IG101
 
G_M000_IG99:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       je       SHORT G_M000_IG101
 
G_M000_IG100:
       xor      rdx, rdx
 
G_M000_IG101:
       test     rdx, rdx
       je       SHORT G_M000_IG104
 
G_M000_IG102:
       mov      rdx, rax
       test     rdx, rdx
       je       SHORT G_M000_IG104
 
G_M000_IG103:
       mov      rdx, qword ptr [rdx]
 
G_M000_IG104:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 7
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG105:
       mov      gword ptr [rbp+0x5B0], rax
       xor      rdx, rdx
       mov      gword ptr [rbp-0x2F8], rdx
 
G_M000_IG106:
       mov      gword ptr [rbp-0x2E8], rdx
 
G_M000_IG107:
       mov      gword ptr [rbp-0x2E0], rdx
       mov      rcx, gword ptr [rbp+0x7F0]
       test     rcx, rcx
       je       SHORT G_M000_IG110
 
G_M000_IG108:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       je       G_M000_IG420
 
G_M000_IG109:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rsi
       je       G_M000_IG435
 
G_M000_IG110:
       mov      rdx, rcx
       test     rdx, rdx
       je       SHORT G_M000_IG113
 
G_M000_IG111:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       je       SHORT G_M000_IG113
 
G_M000_IG112:
       xor      rdx, rdx
 
G_M000_IG113:
       test     rdx, rdx
       je       SHORT G_M000_IG116
 
G_M000_IG114:
       mov      rdx, rax
       test     rdx, rdx
       je       SHORT G_M000_IG116
 
G_M000_IG115:
       mov      rdx, qword ptr [rdx]
 
G_M000_IG116:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rcx, rax
 
G_M000_IG117:
       xor      rdx, rdx
       mov      gword ptr [rbp-0x2F8], rdx
 
G_M000_IG118:
       mov      gword ptr [rbp-0x2E8], rdx
 
G_M000_IG119:
       mov      gword ptr [rbp-0x2E0], rdx
       mov      rdx, rbx
       mov      edi, 54
       mov      esi, 4
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x7E8], rax
       mov      edi, 90
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, gword ptr [rbp+0x7E8]
       mov      edi, 55
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x7E0], rax
       mov      edi, 91
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x810]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, gword ptr [rbp+0x7E8]
       mov      edi, 56
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x7D8], rax
       mov      edi, 92
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [r15]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG123
 
G_M000_IG120:
       mov      edi, 92
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      gword ptr [rbp+0x7D0], r14
       mov      edi, 92
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x7D0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x550], rax
       mov      rcx, gword ptr [rbp+0x808]
       mov      rdi, rcx
       mov      rdx, rax
       test     rdi, rdi
       jne      G_M000_IG446
 
G_M000_IG121:
       mov      gword ptr [rbp+0x808], rcx
       mov      rdx, rcx
       mov      rcx, gword ptr [rbp+0x7D8]
       mov      edi, 62
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp+0x550]
       mov      edi, 63
       mov      esi, 258
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG122:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG123:
       mov      edi, 93
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [r15]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG127
 
G_M000_IG124:
       mov      edi, 93
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      gword ptr [rbp+0x7C8], r14
       mov      edi, 93
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x7C8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x518], rax
       mov      rcx, gword ptr [rbp+0x800]
       mov      rdi, rcx
       mov      rdx, rax
       test     rdi, rdi
       jne      G_M000_IG457
 
G_M000_IG125:
       mov      gword ptr [rbp+0x800], rcx
       mov      rdx, rcx
       mov      rcx, gword ptr [rbp+0x7D8]
       mov      edi, 64
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp+0x518]
       mov      edi, 65
       mov      esi, 258
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG126:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG127:
       mov      edi, 94
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [r15]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       mov      qword ptr [rbp+0x6A0], r13
       mov      qword ptr [rbp+0x690], r12
       mov      r12, gword ptr [rbp+0x800]
       mov      r13, gword ptr [rbp+0x810]
       je       G_M000_IG138
 
G_M000_IG128:
       mov      r15, gword ptr [rbp+0x7D8]
       mov      edi, 94
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      gword ptr [rbp+0x7C0], r14
       mov      edi, 94
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x7C0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdi, rax
       mov      rdx, gword ptr [rbp+0x7F8]
       mov      rax, rdx
       mov      rcx, r15
       mov      rsi, rdi
       mov      gword ptr [rbp+0x4E0], rdi
       mov      gword ptr [rbp+0x218], rax
       mov      gword ptr [rbp+0x208], rcx
       mov      gword ptr [rbp+0x1F8], rsi
       mov      rcx, rdx
       mov      rsi, r15
 
G_M000_IG129:
       cmp      gword ptr [rbp+0x218], 0
       je       G_M000_IG136
 
G_M000_IG130:
       mov      r15, 0xD1FFAB1E
       mov      rdi, gword ptr [rbp+0x218]
       cmp      qword ptr [rdi], r15
       jne      G_M000_IG136
 
G_M000_IG131:
       cmp      gword ptr [rbp+0x208], 0
       je       G_M000_IG136
 
G_M000_IG132:
       mov      r15, 0xD1FFAB1E
       mov      rdi, gword ptr [rbp+0x208]
       cmp      qword ptr [rdi], r15
       jne      SHORT G_M000_IG136
 
G_M000_IG133:
       cmp      gword ptr [rbp+0x1F8], 0
       je       SHORT G_M000_IG136
 
G_M000_IG134:
       mov      r15, 0xD1FFAB1E
       mov      rdi, gword ptr [rbp+0x1F8]
       cmp      qword ptr [rdi], r15
       jne      SHORT G_M000_IG136
 
G_M000_IG135:
       mov      gword ptr [rbp+0x7F8], rcx
       mov      r15, gword ptr [rbp+0x218]
       vmovsd   xmm0, qword ptr [r15+0x08]
       mov      r15, gword ptr [rbp+0x208]
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   xmm2, qword ptr [rdi+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vsubsd   xmm0, xmm2, xmm0
       vmovsd   qword ptr [rbp+0x1E0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0x1E0]
       vmovsd   qword ptr [rdx+0x08], xmm0
       jmp      SHORT G_M000_IG137
 
G_M000_IG136:
       mov      gword ptr [rbp+0x7F8], rcx
       mov      rdx, rcx
       mov      rcx, rsi
       mov      edi, 66
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp+0x4E0]
       mov      edi, 67
       mov      esi, 258
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG137:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x7C0]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG138:
       mov      edi, 95
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      r15, 0xD1FFAB1E
       mov      rdi, gword ptr [r15]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG14
 
G_M000_IG139:
       mov      edi, 95
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      gword ptr [rbp+0x7B8], r13
       mov      edi, 95
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x7B8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x4A8], rax
       mov      rcx, gword ptr [rbp+0x808]
       mov      rdi, rcx
       mov      rdx, rax
       test     rdi, rdi
       je       G_M000_IG12
 
G_M000_IG140:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       G_M000_IG03
 
G_M000_IG141:
       jmp      G_M000_IG12
 
G_M000_IG142:
       mov      r12, qword ptr [rbp+0x708]
       mov      r13, qword ptr [rbp+0x6F8]
       add      r12, r13
       jo       G_M000_IG471
 
G_M000_IG143:
       test     r13, r13
       setg     dil
       movzx    rdi, dil
       mov      r14, qword ptr [rbp+0x700]
       cmp      r12, r14
       setl     al
       movzx    rax, al
       and      eax, edi
       cmp      r12, r14
       setg     cl
       movzx    rcx, cl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, ecx
       or       edi, eax
       je       G_M000_IG165
 
G_M000_IG144:
       lea      rax, [r12+0x05]
       cmp      rax, 261
       jbe      G_M000_IG284
 
G_M000_IG145:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
 
G_M000_IG146:
       mov      gword ptr [rbp+0x828], rax
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       mov      qword ptr [rbp-0x360], rdx
       mov      edi, dword ptr [rdx]
       test     edi, edi
       jle      SHORT G_M000_IG149
 
G_M000_IG147:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       dec      edi
       mov      ecx, edi
       cmp      ecx, dword ptr [rsi+0x08]
       jae      G_M000_IG470
       cmp      dword ptr [rsi+4*rcx+0x10], 82
       je       SHORT G_M000_IG149
 
G_M000_IG148:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG470
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 82
 
G_M000_IG149:
       mov      rdi, gword ptr [rbp+0x850]
       mov      rsi, rax
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x820], rax
       mov      rcx, qword ptr [rbp-0x360]
       mov      edi, dword ptr [rcx]
       test     edi, edi
       mov      rax, gword ptr [rbp+0x820]
       jle      SHORT G_M000_IG152
 
G_M000_IG150:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       dec      edi
       mov      ecx, edi
       cmp      ecx, dword ptr [rsi+0x08]
       jae      G_M000_IG470
       cmp      dword ptr [rsi+4*rcx+0x10], 83
       je       SHORT G_M000_IG152
 
G_M000_IG151:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG470
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 83
 
G_M000_IG152:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp+0x6C0], rcx
       mov      rdx, gword ptr [rbp+0x828]
       test     rdx, rdx
       je       SHORT G_M000_IG155
 
G_M000_IG153:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       G_M000_IG285
 
G_M000_IG154:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       G_M000_IG300
 
G_M000_IG155:
       mov      rdi, rdx
       test     rdi, rdi
       je       SHORT G_M000_IG158
 
G_M000_IG156:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       SHORT G_M000_IG158
 
G_M000_IG157:
       xor      rdi, rdi
 
G_M000_IG158:
       test     rdi, rdi
       je       G_M000_IG309
 
G_M000_IG159:
       mov      rsi, rcx
       test     rsi, rsi
       je       SHORT G_M000_IG162
 
G_M000_IG160:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rsi], r8
       je       SHORT G_M000_IG162
 
G_M000_IG161:
       xor      rsi, rsi
 
G_M000_IG162:
       test     rsi, rsi
       mov      gword ptr [rbp+0x820], rax
       je       G_M000_IG310
 
G_M000_IG163:
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rdi, rax
 
G_M000_IG164:
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x6B0], rax
       mov      rdi, gword ptr [rbp+0x838]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rdi, qword ptr [rbp+0x6B0]
       mov      rcx, rdi
       mov      qword ptr [rbp+0x698], rax
       mov      edx, 1
       mov      qword ptr [rbp+0x708], r12
       mov      qword ptr [rbp+0x700], r14
       mov      qword ptr [rbp+0x6F8], r13
       mov      r12, rdx
       mov      r13, rcx
       mov      r14, gword ptr [rbp+0x820]
       jmp      G_M000_IG40
 
G_M000_IG165:
       mov      r13, 0xD1FFAB1E
       mov      edi, dword ptr [r13]
       test     edi, edi
       jle      SHORT G_M000_IG168
 
G_M000_IG166:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       dec      edi
       mov      ecx, edi
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG470
       cmp      dword ptr [rax+4*rcx+0x10], 98
       je       SHORT G_M000_IG168
 
G_M000_IG167:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG470
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 98
 
G_M000_IG168:
       mov      rdi, gword ptr [rbp+0x850]
       call     [RyuPy.Runtime.PyIter:Create(System.Object):RyuPy.Runtime.IPyIterator]
       mov      r12, rax
       jmp      G_M000_IG265
 
G_M000_IG169:
       test     rax, rax
       jl       G_M000_IG471
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG470
       mov      rax, gword ptr [rcx+8*rsi+0x10]
       jmp      G_M000_IG215
 
G_M000_IG170:
       test     rax, rax
       je       SHORT G_M000_IG177
 
G_M000_IG171:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG173
 
G_M000_IG172:
       jmp      G_M000_IG239
 
G_M000_IG173:
       test     rdx, rdx
       je       SHORT G_M000_IG176
 
G_M000_IG174:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG239
 
G_M000_IG175:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp+0x50], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0x50]
       vmovsd   qword ptr [rdx+0x08], xmm0
       jmp      G_M000_IG240
 
G_M000_IG176:
       jmp      G_M000_IG239
 
G_M000_IG177:
       jmp      G_M000_IG239
 
G_M000_IG178:
       mov      rcx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x1E0], rcx
       test     rax, rax
       je       G_M000_IG189
 
G_M000_IG179:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rax], rdx
       jne      G_M000_IG189
 
G_M000_IG180:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x1F0], rax
       xor      edx, edx
       mov      qword ptr [rbp-0x1F8], rdx
       lea      rdx, [rbp-0x1F8]
       mov      rdi, rcx
       mov      rsi, rax
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x1F8]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG185
 
G_M000_IG181:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x208], xmm0
       lea      rdi, [rbp-0x208]
       mov      rsi, qword ptr [rbp-0x1E0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG182:
       vmovdqu  xmm0, xmmword ptr [rbp-0x208]
       vmovdqu  xmmword ptr [rbp-0x218], xmm0
 
G_M000_IG183:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x238], xmm0
       lea      rdi, [rbp-0x238]
       mov      rsi, qword ptr [rbp-0x1F0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x218]
       mov      rsi, qword ptr [rbp-0x210]
       mov      rdx, gword ptr [rbp-0x238]
       mov      rcx, qword ptr [rbp-0x230]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x228], rax
       mov      qword ptr [rbp-0x220], rdx
       mov      rdi, gword ptr [rbp-0x228]
       mov      rsi, qword ptr [rbp-0x220]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG184:
       jmp      G_M000_IG252
 
G_M000_IG185:
       mov      rax, qword ptr [rbp-0x1F8]
       mov      qword ptr [rbp-0x240], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG188
 
G_M000_IG186:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x240]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG187:
       jmp      SHORT G_M000_IG184
 
G_M000_IG188:
       test     rcx, rcx
       jl       G_M000_IG471
       mov      rax, rcx
       mov      rsi, 0xD1FFAB1E
       mov      rcx, gword ptr [rsi]
       cmp      rax, 262
       jae      G_M000_IG470
       mov      rax, gword ptr [rcx+8*rax+0x10]
       jmp      SHORT G_M000_IG187
 
G_M000_IG189:
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG192
 
G_M000_IG190:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG192
 
G_M000_IG191:
       xor      rsi, rsi
 
G_M000_IG192:
       test     rsi, rsi
       je       G_M000_IG251
 
G_M000_IG193:
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x1E8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x1E0]
       vmovsd   qword ptr [rbp-0x248], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x248]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x1E8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edi, 1
       test     edi, edi
       je       SHORT G_M000_IG195
 
G_M000_IG194:
       jmp      G_M000_IG252
 
G_M000_IG195:
       mov      rax, gword ptr [rbp+0x348]
       jmp      G_M000_IG251
 
G_M000_IG196:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x1D0], xmm0
       test     rax, rax
       je       SHORT G_M000_IG199
 
G_M000_IG197:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG199
 
G_M000_IG198:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x1D8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1D0]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x1D8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG252
 
G_M000_IG199:
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG202
 
G_M000_IG200:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG202
 
G_M000_IG201:
       xor      rsi, rsi
 
G_M000_IG202:
       test     rsi, rsi
       je       G_M000_IG251
 
G_M000_IG203:
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x250], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x250]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x1D0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       je       SHORT G_M000_IG205
 
G_M000_IG204:
       jmp      G_M000_IG252
 
G_M000_IG205:
       mov      rax, gword ptr [rbp+0x348]
       jmp      G_M000_IG251
 
G_M000_IG206:
       mov      rdx, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x268], rdx
       test     rax, rax
       je       G_M000_IG227
 
G_M000_IG207:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG227
 
G_M000_IG208:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x278], rax
       lea      rcx, [rdx+rax]
       mov      qword ptr [rbp-0x280], rcx
       mov      rdi, rdx
       xor      rdi, rcx
       mov      rsi, rax
       xor      rsi, rcx
       and      rdi, rsi
       jge      G_M000_IG213
 
G_M000_IG209:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x290], xmm0
       lea      rdi, [rbp-0x290]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG210:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x290]
       vmovdqu32 xmmword ptr [rbp-0x2A0], xmm0
 
G_M000_IG211:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2C0], xmm0
       lea      rdi, [rbp-0x2C0]
       mov      rsi, qword ptr [rbp-0x278]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x2A0]
       mov      rsi, qword ptr [rbp-0x298]
       mov      rdx, gword ptr [rbp-0x2C0]
       mov      rcx, qword ptr [rbp-0x2B8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x2B0], rax
       mov      qword ptr [rbp-0x2A8], rdx
       mov      rdi, gword ptr [rbp-0x2B0]
       mov      rsi, qword ptr [rbp-0x2A8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG212:
       mov      rdx, rax
       jmp      G_M000_IG264
 
G_M000_IG213:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      G_M000_IG169
 
G_M000_IG214:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x280]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG215:
       jmp      SHORT G_M000_IG212
 
G_M000_IG216:
       mov      rcx, gword ptr [rbp+0x358]
       jmp      G_M000_IG234
 
G_M000_IG217:
       test     rax, rax
       je       SHORT G_M000_IG224
 
G_M000_IG218:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG220
 
G_M000_IG219:
       jmp      G_M000_IG235
 
G_M000_IG220:
       test     rdx, rdx
       je       SHORT G_M000_IG223
 
G_M000_IG221:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG235
 
G_M000_IG222:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp+0xA0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0xA0]
       vmovsd   qword ptr [rdx+0x08], xmm0
       jmp      G_M000_IG236
 
G_M000_IG223:
       jmp      G_M000_IG235
 
G_M000_IG224:
       jmp      G_M000_IG235
 
G_M000_IG225:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rdi], r8
       je       G_M000_IG170
 
G_M000_IG226:
       jmp      G_M000_IG239
 
G_M000_IG227:
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG230
 
G_M000_IG228:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG230
 
G_M000_IG229:
       xor      rsi, rsi
 
G_M000_IG230:
       test     rsi, rsi
       je       SHORT G_M000_IG233
 
G_M000_IG231:
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x270], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x268]
       vmovsd   qword ptr [rbp-0x2C8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2C8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x270]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       mov      edi, 1
       test     edi, edi
       mov      rax, gword ptr [rbp+0x340]
       mov      rcx, gword ptr [rbp+0x358]
       je       SHORT G_M000_IG234
 
G_M000_IG232:
       jmp      G_M000_IG264
 
G_M000_IG233:
       mov      rcx, gword ptr [rbp+0x358]
 
G_M000_IG234:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       jmp      G_M000_IG264
 
G_M000_IG235:
       mov      rdx, rbx
       mov      rcx, rax
       mov      edi, 74
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp+0x3E8]
       mov      edi, 75
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG236:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG237:
       mov      edi, 100
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [r15]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG241
 
G_M000_IG238:
       mov      edi, 100
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      gword ptr [rbp+0x798], r14
       mov      edi, 100
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x798]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x3A0], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x798]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdi, rbx
       mov      rcx, rax
       mov      rdx, gword ptr [rbp+0x3A0]
       mov      rsi, rdx
       test     rdi, rdi
       jne      G_M000_IG225
 
G_M000_IG239:
       mov      rdx, rbx
       mov      rcx, rax
       mov      edi, 76
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp+0x3A0]
       mov      edi, 77
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG240:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG241:
       mov      edi, 101
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [r15]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG265
 
G_M000_IG242:
       mov      edi, 101
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 101
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x358], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x348], rax
       test     rbx, rbx
       je       SHORT G_M000_IG245
 
G_M000_IG243:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdx
       je       G_M000_IG178
 
G_M000_IG244:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       je       G_M000_IG196
 
G_M000_IG245:
       mov      rdx, rbx
       test     rdx, rdx
       je       SHORT G_M000_IG248
 
G_M000_IG246:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       je       SHORT G_M000_IG248
 
G_M000_IG247:
       xor      rdx, rdx
 
G_M000_IG248:
       test     rdx, rdx
       je       SHORT G_M000_IG251
 
G_M000_IG249:
       mov      rdx, rax
       test     rdx, rdx
       je       SHORT G_M000_IG251
 
G_M000_IG250:
       mov      rdx, qword ptr [rdx]
 
G_M000_IG251:
       mov      rdx, rax
       mov      rsi, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG252:
       mov      gword ptr [rbp+0x340], rax
       mov      rcx, gword ptr [rbp+0x358]
       test     rcx, rcx
       je       SHORT G_M000_IG255
 
G_M000_IG253:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG206
 
G_M000_IG254:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG270
 
G_M000_IG255:
       mov      rdi, rcx
       test     rdi, rdi
       je       SHORT G_M000_IG258
 
G_M000_IG256:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       SHORT G_M000_IG258
 
G_M000_IG257:
       xor      rdi, rdi
 
G_M000_IG258:
       test     rdi, rdi
       je       G_M000_IG234
 
G_M000_IG259:
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG262
 
G_M000_IG260:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdx
       je       SHORT G_M000_IG262
 
G_M000_IG261:
       xor      rsi, rsi
 
G_M000_IG262:
       test     rsi, rsi
       je       G_M000_IG234
 
G_M000_IG263:
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rdx, rax
 
G_M000_IG264:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG265:
       mov      rdi, r12
       call     [RyuPy.Runtime.PyOps:IterNextOk(System.Object):bool]
       test     eax, eax
       je       G_M000_IG279
 
G_M000_IG266:
       mov      rdi, r12
       call     [RyuPy.Runtime.PyOps:IterValue(System.Object):System.Object]
       mov      r14, rax
       mov      edi, 99
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [r15]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG237
 
G_M000_IG267:
       mov      edi, 99
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      gword ptr [rbp+0x7A0], r14
       mov      edi, 99
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x7A0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x3E8], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x7A0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdi, rbx
       mov      rcx, rax
       mov      rdx, gword ptr [rbp+0x3E8]
       mov      rsi, rdx
       test     rdi, rdi
       je       G_M000_IG235
 
G_M000_IG268:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rdi], r8
       je       G_M000_IG217
 
G_M000_IG269:
       jmp      G_M000_IG235
 
G_M000_IG270:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x258], xmm0
       test     rax, rax
       je       SHORT G_M000_IG273
 
G_M000_IG271:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG273
 
G_M000_IG272:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x260], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x258]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x260]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG264
 
G_M000_IG273:
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG276
 
G_M000_IG274:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG276
 
G_M000_IG275:
       xor      rsi, rsi
 
G_M000_IG276:
       test     rsi, rsi
       je       G_M000_IG216
 
G_M000_IG277:
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x2D0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x2D0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x258]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       mov      edi, 1
       test     edi, edi
       mov      rax, gword ptr [rbp+0x340]
       mov      rcx, gword ptr [rbp+0x358]
       je       G_M000_IG234
 
G_M000_IG278:
       jmp      G_M000_IG264
 
G_M000_IG279:
       mov      r14, qword ptr [rbp+0x760]
       mov      r12, qword ptr [rbp+0x750]
       add      r14, r12
       jo       G_M000_IG471
       mov      eax, dword ptr [rbp-0x358]
       mov      edi, eax
       mov      rcx, qword ptr [rbp+0x758]
       cmp      r14, rcx
       setl     dl
       movzx    rdx, dl
       and      edx, edi
       mov      qword ptr [rbp+0x758], rcx
       cmp      r14, rcx
       setg     sil
       movzx    rsi, sil
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, esi
       or       edi, edx
       je       G_M000_IG468
 
G_M000_IG280:
       mov      edi, dword ptr [r13]
       test     edi, edi
       jle      SHORT G_M000_IG283
 
G_M000_IG281:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       dec      edi
       mov      esi, edi
       cmp      esi, dword ptr [rdx+0x08]
       jae      G_M000_IG470
       cmp      dword ptr [rdx+4*rsi+0x10], 81
       je       SHORT G_M000_IG283
 
G_M000_IG282:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      edi, dword ptr [rdx+0x08]
       jae      G_M000_IG470
       mov      edi, edi
       mov      dword ptr [rdx+4*rdi+0x10], 81
 
G_M000_IG283:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r13, rax
       mov      rdi, gword ptr [rbp+0x838]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      ecx, 1
       mov      qword ptr [rbp+0x700], rax
       mov      qword ptr [rbp+0x760], r14
       mov      qword ptr [rbp+0x750], r12
       mov      r12, r13
       mov      r13, rcx
       jmp      G_M000_IG143
 
G_M000_IG284:
       test     rax, rax
       jl       G_M000_IG471
       mov      rdi, rax
       mov      rsi, 0xD1FFAB1E
       mov      rcx, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG470
       mov      rax, gword ptr [rcx+8*rdi+0x10]
       jmp      G_M000_IG146
 
G_M000_IG285:
       mov      r8, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x40], r8
       test     rcx, rcx
       je       G_M000_IG294
 
G_M000_IG286:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG294
 
G_M000_IG287:
       mov      rcx, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x50], rcx
       lea      rdx, [r8+rcx]
       mov      qword ptr [rbp-0x58], rdx
       mov      rdi, r8
       xor      rdi, rdx
       mov      rsi, rcx
       xor      rsi, rdx
       and      rdi, rsi
       jge      SHORT G_M000_IG290
 
G_M000_IG288:
       mov      gword ptr [rbp+0x820], rax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x68], xmm0
       lea      rdi, [rbp-0x68]
       mov      rsi, r8
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x68]
       mov      gword ptr [rbp-0x368], rax
       mov      ecx, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x34C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x78], xmm0
       lea      rdi, [rbp-0x78]
       mov      rsi, qword ptr [rbp-0x50]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x368]
       mov      esi, dword ptr [rbp-0x34C]
       mov      rdx, gword ptr [rbp-0x78]
       mov      rcx, qword ptr [rbp-0x70]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG289:
       mov      rdi, rax
       jmp      G_M000_IG164
 
G_M000_IG290:
       lea      rcx, [rdx+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG293
 
G_M000_IG291:
       mov      gword ptr [rbp+0x820], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x58]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG292:
       jmp      SHORT G_M000_IG289
 
G_M000_IG293:
       test     rcx, rcx
       jl       G_M000_IG471
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG470
       mov      rsi, gword ptr [rcx+8*rsi+0x10]
       mov      gword ptr [rbp+0x820], rax
       mov      rax, rsi
       jmp      SHORT G_M000_IG292
 
G_M000_IG294:
       mov      rsi, rcx
       test     rsi, rsi
       je       SHORT G_M000_IG297
 
G_M000_IG295:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG297
 
G_M000_IG296:
       xor      rsi, rsi
 
G_M000_IG297:
       test     rsi, rsi
       mov      gword ptr [rbp+0x820], rax
       je       G_M000_IG310
 
G_M000_IG298:
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x48], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rbp-0x80], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x80]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdi, rax
       mov      eax, 1
       test     eax, eax
       mov      rcx, gword ptr [rbp+0x6C0]
       je       G_M000_IG310
 
G_M000_IG299:
       jmp      G_M000_IG164
 
G_M000_IG300:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     rcx, rcx
       je       SHORT G_M000_IG303
 
G_M000_IG301:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      SHORT G_M000_IG303
 
G_M000_IG302:
       mov      gword ptr [rbp+0x820], rax
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdi, rax
       jmp      G_M000_IG164
 
G_M000_IG303:
       mov      rsi, rcx
       test     rsi, rsi
       je       SHORT G_M000_IG306
 
G_M000_IG304:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG306
 
G_M000_IG305:
       xor      rsi, rsi
 
G_M000_IG306:
       test     rsi, rsi
       mov      gword ptr [rbp+0x820], rax
       je       SHORT G_M000_IG310
 
G_M000_IG307:
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x88], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x30]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdi, rax
       mov      esi, 1
       test     esi, esi
       mov      rcx, gword ptr [rbp+0x6C0]
       je       SHORT G_M000_IG310
 
G_M000_IG308:
       jmp      G_M000_IG164
 
G_M000_IG309:
       mov      gword ptr [rbp+0x820], rax
 
G_M000_IG310:
       mov      rdx, rcx
       mov      rsi, gword ptr [rbp+0x828]
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdi, rax
       jmp      G_M000_IG164
 
G_M000_IG311:
       test     rcx, rcx
       jl       G_M000_IG471
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG470
       mov      rsi, gword ptr [rcx+8*rsi+0x10]
       mov      qword ptr [rbp+0x698], rax
       jmp      G_M000_IG43
 
G_M000_IG312:
       mov      rdx, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0xA0], rdx
       test     rax, rax
       je       G_M000_IG321
 
G_M000_IG313:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG321
 
G_M000_IG314:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0xB0], rax
       mov      rcx, rdx
       sub      rcx, rax
       mov      qword ptr [rbp-0xB8], rcx
       mov      rdi, rdx
       xor      rdi, rax
       mov      rsi, rdx
       xor      rsi, rcx
       and      rdi, rsi
       jge      G_M000_IG317
 
G_M000_IG315:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xC8], xmm0
       lea      rdi, [rbp-0xC8]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xC8]
       mov      gword ptr [rbp-0x370], rax
       mov      ecx, dword ptr [rbp-0xC0]
       mov      dword ptr [rbp-0x350], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xD8], xmm0
       lea      rdi, [rbp-0xD8]
       mov      rsi, qword ptr [rbp-0xB0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x370]
       mov      esi, dword ptr [rbp-0x350]
       mov      rdx, gword ptr [rbp-0xD8]
       mov      rcx, qword ptr [rbp-0xD0]
       call     [System.Numerics.BigInteger:op_Subtraction(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG316:
       mov      rcx, rax
       jmp      G_M000_IG59
 
G_M000_IG317:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG320
 
G_M000_IG318:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0xB8]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG319:
       jmp      SHORT G_M000_IG316
 
G_M000_IG320:
       test     rax, rax
       jl       G_M000_IG471
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG470
       test     rax, rax
       jl       G_M000_IG471
       mov      rsi, rax
       mov      rax, gword ptr [rcx+8*rsi+0x10]
       jmp      SHORT G_M000_IG319
 
G_M000_IG321:
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG324
 
G_M000_IG322:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG324
 
G_M000_IG323:
       xor      rsi, rsi
 
G_M000_IG324:
       test     rsi, rsi
       je       SHORT G_M000_IG328
 
G_M000_IG325:
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0xA8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rbp-0xE0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE0]
       vsubsd   xmm0, xmm1, qword ptr [rbp-0xA8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rcx, rax
       mov      edi, 1
       test     edi, edi
       mov      rax, gword ptr [rbp+0x640]
       je       SHORT G_M000_IG327
 
G_M000_IG326:
       jmp      G_M000_IG59
 
G_M000_IG327:
       mov      rcx, gword ptr [rbp+0x650]
       jmp      G_M000_IG58
 
G_M000_IG328:
       mov      rcx, gword ptr [rbp+0x650]
       jmp      G_M000_IG58
 
G_M000_IG329:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm0
       test     rax, rax
       je       SHORT G_M000_IG332
 
G_M000_IG330:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG332
 
G_M000_IG331:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x90]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rcx, rax
       jmp      G_M000_IG59
 
G_M000_IG332:
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG335
 
G_M000_IG333:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG335
 
G_M000_IG334:
       xor      rsi, rsi
 
G_M000_IG335:
       test     rsi, rsi
       je       SHORT G_M000_IG339
 
G_M000_IG336:
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0xE8], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x90]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0xE8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rcx, rax
       mov      edx, 1
       test     edx, edx
       mov      rax, gword ptr [rbp+0x640]
       je       SHORT G_M000_IG338
 
G_M000_IG337:
       jmp      G_M000_IG59
 
G_M000_IG338:
       mov      rcx, gword ptr [rbp+0x650]
       jmp      G_M000_IG58
 
G_M000_IG339:
       mov      rcx, gword ptr [rbp+0x650]
       jmp      G_M000_IG58
 
G_M000_IG340:
       mov      rdx, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x100], rdx
       test     rax, rax
       je       G_M000_IG349
 
G_M000_IG341:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG349
 
G_M000_IG342:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x110], rax
       mov      rcx, rdx
       sub      rcx, rax
       mov      qword ptr [rbp-0x118], rcx
       mov      rdi, rdx
       xor      rdi, rax
       mov      rsi, rdx
       xor      rsi, rcx
       and      rdi, rsi
       jge      G_M000_IG345
 
G_M000_IG343:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x128], xmm0
       lea      rdi, [rbp-0x128]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x128]
       mov      gword ptr [rbp-0x378], rax
       mov      ecx, dword ptr [rbp-0x120]
       mov      dword ptr [rbp-0x354], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x138], xmm0
       lea      rdi, [rbp-0x138]
       mov      rsi, qword ptr [rbp-0x110]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x378]
       mov      esi, dword ptr [rbp-0x354]
       mov      rdx, gword ptr [rbp-0x138]
       mov      rcx, qword ptr [rbp-0x130]
       call     [System.Numerics.BigInteger:op_Subtraction(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG344:
       jmp      G_M000_IG72
 
G_M000_IG345:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG348
 
G_M000_IG346:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x118]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG347:
       jmp      SHORT G_M000_IG344
 
G_M000_IG348:
       test     rax, rax
       jl       G_M000_IG471
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG470
       test     rax, rax
       jl       G_M000_IG471
       mov      rsi, rax
       mov      rax, gword ptr [rcx+8*rsi+0x10]
       jmp      SHORT G_M000_IG347
 
G_M000_IG349:
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG352
 
G_M000_IG350:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG352
 
G_M000_IG351:
       xor      rsi, rsi
 
G_M000_IG352:
       test     rsi, rsi
       je       SHORT G_M000_IG356
 
G_M000_IG353:
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x108], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x100]
       vmovsd   qword ptr [rbp-0x140], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x140]
       vsubsd   xmm0, xmm1, qword ptr [rbp-0x108]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edi, 1
       test     edi, edi
       mov      rcx, gword ptr [rbp+0x628]
       je       SHORT G_M000_IG355
 
G_M000_IG354:
       jmp      G_M000_IG72
 
G_M000_IG355:
       mov      rax, gword ptr [rbp+0x618]
       jmp      G_M000_IG71
 
G_M000_IG356:
       mov      rcx, gword ptr [rbp+0x628]
       jmp      G_M000_IG71
 
G_M000_IG357:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0xF0], xmm0
       test     rax, rax
       je       SHORT G_M000_IG360
 
G_M000_IG358:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG360
 
G_M000_IG359:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xF8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xF0]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0xF8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG72
 
G_M000_IG360:
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG363
 
G_M000_IG361:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG363
 
G_M000_IG362:
       xor      rsi, rsi
 
G_M000_IG363:
       test     rsi, rsi
       je       SHORT G_M000_IG367
 
G_M000_IG364:
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x148], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xF0]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x148]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       mov      rcx, gword ptr [rbp+0x628]
       je       SHORT G_M000_IG366
 
G_M000_IG365:
       jmp      G_M000_IG72
 
G_M000_IG366:
       mov      rax, gword ptr [rbp+0x618]
       jmp      G_M000_IG71
 
G_M000_IG367:
       mov      rcx, gword ptr [rbp+0x628]
       jmp      G_M000_IG71
 
G_M000_IG368:
       mov      rdx, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x160], rdx
       test     rax, rax
       je       G_M000_IG379
 
G_M000_IG369:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG379
 
G_M000_IG370:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x170], rax
       mov      rcx, rdx
       sub      rcx, rax
       mov      qword ptr [rbp-0x178], rcx
       mov      rdi, rdx
       xor      rdi, rax
       mov      rsi, rdx
       xor      rsi, rcx
       and      rdi, rsi
       jge      G_M000_IG375
 
G_M000_IG371:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x188], xmm0
       lea      rdi, [rbp-0x188]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG372:
       vmovdqu  xmm0, xmmword ptr [rbp-0x188]
       vmovdqu  xmmword ptr [rbp-0x198], xmm0
 
G_M000_IG373:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1B8], xmm0
       lea      rdi, [rbp-0x1B8]
       mov      rsi, qword ptr [rbp-0x170]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x198]
       mov      rsi, qword ptr [rbp-0x190]
       mov      rdx, gword ptr [rbp-0x1B8]
       mov      rcx, qword ptr [rbp-0x1B0]
       call     [System.Numerics.BigInteger:op_Subtraction(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x1A8], rax
       mov      qword ptr [rbp-0x1A0], rdx
       mov      rdi, gword ptr [rbp-0x1A8]
       mov      rsi, qword ptr [rbp-0x1A0]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG374:
       jmp      G_M000_IG82
 
G_M000_IG375:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG378
 
G_M000_IG376:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, qword ptr [rbp-0x178]
       mov      qword ptr [rax+0x08], rsi
 
G_M000_IG377:
       jmp      SHORT G_M000_IG374
 
G_M000_IG378:
       test     rax, rax
       jl       G_M000_IG471
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG470
       test     rax, rax
       jl       G_M000_IG471
       mov      rsi, rax
       mov      rax, gword ptr [rcx+8*rsi+0x10]
       jmp      SHORT G_M000_IG377
 
G_M000_IG379:
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG382
 
G_M000_IG380:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG382
 
G_M000_IG381:
       xor      rsi, rsi
 
G_M000_IG382:
       test     rsi, rsi
       je       SHORT G_M000_IG386
 
G_M000_IG383:
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x168], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x160]
       vmovsd   qword ptr [rbp-0x1C0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1C0]
       vsubsd   xmm0, xmm1, qword ptr [rbp-0x168]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edi, 1
       test     edi, edi
       mov      rcx, gword ptr [rbp+0x600]
       je       SHORT G_M000_IG385
 
G_M000_IG384:
       jmp      G_M000_IG82
 
G_M000_IG385:
       mov      rax, gword ptr [rbp+0x5F0]
       jmp      G_M000_IG81
 
G_M000_IG386:
       mov      rcx, gword ptr [rbp+0x600]
       jmp      G_M000_IG81
 
G_M000_IG387:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x150], xmm0
       test     rax, rax
       je       SHORT G_M000_IG390
 
G_M000_IG388:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG390
 
G_M000_IG389:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x158], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x150]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x158]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG82
 
G_M000_IG390:
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG393
 
G_M000_IG391:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG393
 
G_M000_IG392:
       xor      rsi, rsi
 
G_M000_IG393:
       test     rsi, rsi
       je       SHORT G_M000_IG397
 
G_M000_IG394:
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x1C8], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x150]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x1C8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      edx, 1
       test     edx, edx
       mov      rcx, gword ptr [rbp+0x600]
       je       SHORT G_M000_IG396
 
G_M000_IG395:
       jmp      G_M000_IG82
 
G_M000_IG396:
       mov      rax, gword ptr [rbp+0x5F0]
       jmp      G_M000_IG81
 
G_M000_IG397:
       mov      rcx, gword ptr [rbp+0x600]
       jmp      G_M000_IG81
 
G_M000_IG398:
       test     rax, rax
       je       SHORT G_M000_IG401
 
G_M000_IG399:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      SHORT G_M000_IG401
 
G_M000_IG400:
       mov      rcx, gword ptr [rbp+0x7F0]
       jmp      G_M000_IG104
 
G_M000_IG401:
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG404
 
G_M000_IG402:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG404
 
G_M000_IG403:
       xor      rsi, rsi
 
G_M000_IG404:
       test     rsi, rsi
       je       SHORT G_M000_IG408
 
G_M000_IG405:
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       cmp      dword ptr [rax], eax
       xor      rax, rax
       xor      esi, esi
       je       SHORT G_M000_IG407
 
G_M000_IG406:
       jmp      G_M000_IG105
 
G_M000_IG407:
       mov      rax, gword ptr [rbp+0x5B8]
       mov      rcx, gword ptr [rbp+0x7F0]
       jmp      G_M000_IG104
 
G_M000_IG408:
       mov      rcx, gword ptr [rbp+0x7F0]
       jmp      G_M000_IG104
 
G_M000_IG409:
       test     rax, rax
       je       SHORT G_M000_IG412
 
G_M000_IG410:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      SHORT G_M000_IG412
 
G_M000_IG411:
       mov      rcx, gword ptr [rbp+0x7F0]
       jmp      G_M000_IG104
 
G_M000_IG412:
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG415
 
G_M000_IG413:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG415
 
G_M000_IG414:
       xor      rsi, rsi
 
G_M000_IG415:
       test     rsi, rsi
       je       SHORT G_M000_IG419
 
G_M000_IG416:
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       mov      rdx, qword ptr [rax]
       xor      rax, rax
       xor      edx, edx
       je       SHORT G_M000_IG418
 
G_M000_IG417:
       jmp      G_M000_IG105
 
G_M000_IG418:
       mov      rax, gword ptr [rbp+0x5B8]
       mov      rcx, gword ptr [rbp+0x7F0]
       jmp      G_M000_IG104
 
G_M000_IG419:
       mov      rcx, gword ptr [rbp+0x7F0]
       jmp      G_M000_IG104
 
G_M000_IG420:
       mov      r8, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x2F0], r8
       test     rax, rax
       je       G_M000_IG428
 
G_M000_IG421:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rax], rdx
       jne      G_M000_IG428
 
G_M000_IG422:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x300], rax
       xor      edx, edx
       mov      qword ptr [rbp-0x308], rdx
       lea      rdx, [rbp-0x308]
       mov      rdi, r8
       mov      rsi, rax
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x308]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG427
 
G_M000_IG423:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x318], xmm0
       lea      rdi, [rbp-0x318]
       mov      rsi, qword ptr [rbp-0x2F0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG424:
       vmovdqu  xmm0, xmmword ptr [rbp-0x318]
       vmovdqu  xmmword ptr [rbp-0x328], xmm0
 
G_M000_IG425:
       mov      rdi, qword ptr [rbp-0x300]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x338], rax
       mov      qword ptr [rbp-0x330], rdx
       mov      rdi, gword ptr [rbp-0x328]
       mov      rsi, qword ptr [rbp-0x320]
       mov      rdx, gword ptr [rbp-0x338]
       mov      rcx, qword ptr [rbp-0x330]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x348], rax
       mov      qword ptr [rbp-0x340], rdx
       mov      rdi, gword ptr [rbp-0x348]
       mov      rsi, qword ptr [rbp-0x340]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG426:
       mov      rcx, rax
       jmp      G_M000_IG117
 
G_M000_IG427:
       mov      rdi, qword ptr [rbp-0x308]
       call     [RyuPy.Runtime.PyInt:Box(long):System.Object]
       jmp      SHORT G_M000_IG426
 
G_M000_IG428:
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG431
 
G_M000_IG429:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG431
 
G_M000_IG430:
       xor      rsi, rsi
 
G_M000_IG431:
       test     rsi, rsi
       je       SHORT G_M000_IG434
 
G_M000_IG432:
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm1, qword ptr [rax]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rbp-0x2F0]
       lea      rsi, [rbp-0x2F8]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x5B0]
       mov      rcx, gword ptr [rbp+0x7F0]
       je       G_M000_IG116
 
G_M000_IG433:
       mov      rcx, gword ptr [rbp-0x2F8]
       jmp      G_M000_IG117
 
G_M000_IG434:
       mov      rcx, gword ptr [rbp+0x7F0]
       jmp      G_M000_IG116
 
G_M000_IG435:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x2D8], xmm0
       test     rax, rax
       je       SHORT G_M000_IG439
 
G_M000_IG436:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      SHORT G_M000_IG439
 
G_M000_IG437:
       vmovsd   xmm1, qword ptr [rax+0x08]
       lea      rsi, [rbp-0x2E8]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x5B0]
       mov      rcx, gword ptr [rbp+0x7F0]
       je       G_M000_IG116
 
G_M000_IG438:
       mov      rcx, gword ptr [rbp-0x2E8]
       jmp      G_M000_IG117
 
G_M000_IG439:
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG442
 
G_M000_IG440:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG442
 
G_M000_IG441:
       xor      rsi, rsi
 
G_M000_IG442:
       test     rsi, rsi
       je       SHORT G_M000_IG445
 
G_M000_IG443:
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax]
       lea      rsi, [rbp-0x2E0]
       vmovsd   xmm0, qword ptr [rbp-0x2D8]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x5B0]
       mov      rcx, gword ptr [rbp+0x7F0]
       je       G_M000_IG116
 
G_M000_IG444:
       mov      rcx, gword ptr [rbp-0x2E0]
       jmp      G_M000_IG117
 
G_M000_IG445:
       mov      rcx, gword ptr [rbp+0x7F0]
       jmp      G_M000_IG116
 
G_M000_IG446:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       SHORT G_M000_IG448
 
G_M000_IG447:
       jmp      G_M000_IG121
 
G_M000_IG448:
       mov      rsi, gword ptr [rbp+0x7D8]
       test     rsi, rsi
       je       G_M000_IG456
 
G_M000_IG449:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG451
 
G_M000_IG450:
       mov      gword ptr [rbp+0x7D8], rsi
       jmp      G_M000_IG121
 
G_M000_IG451:
       test     rax, rax
       je       SHORT G_M000_IG455
 
G_M000_IG452:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG454
 
G_M000_IG453:
       mov      gword ptr [rbp+0x7D8], rsi
       jmp      G_M000_IG121
 
G_M000_IG454:
       mov      gword ptr [rbp+0x808], rcx
       vmovsd   xmm0, qword ptr [rcx+0x08]
       mov      gword ptr [rbp+0x7D8], rsi
       vmovsd   xmm1, qword ptr [rsi+0x08]
       vmovsd   xmm2, qword ptr [rdx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vsubsd   xmm0, xmm2, xmm0
       vmovsd   qword ptr [rbp+0x280], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0x280]
       vmovsd   qword ptr [rdx+0x08], xmm0
       jmp      G_M000_IG122
 
G_M000_IG455:
       mov      gword ptr [rbp+0x7D8], rsi
       jmp      G_M000_IG121
 
G_M000_IG456:
       mov      gword ptr [rbp+0x7D8], rsi
       jmp      G_M000_IG121
 
G_M000_IG457:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       SHORT G_M000_IG459
 
G_M000_IG458:
       jmp      G_M000_IG125
 
G_M000_IG459:
       mov      rsi, gword ptr [rbp+0x7D8]
       test     rsi, rsi
       je       G_M000_IG467
 
G_M000_IG460:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG462
 
G_M000_IG461:
       mov      gword ptr [rbp+0x7D8], rsi
       jmp      G_M000_IG125
 
G_M000_IG462:
       test     rax, rax
       je       SHORT G_M000_IG466
 
G_M000_IG463:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG465
 
G_M000_IG464:
       mov      gword ptr [rbp+0x7D8], rsi
       jmp      G_M000_IG125
 
G_M000_IG465:
       mov      gword ptr [rbp+0x800], rcx
       vmovsd   xmm0, qword ptr [rcx+0x08]
       mov      gword ptr [rbp+0x7D8], rsi
       vmovsd   xmm1, qword ptr [rsi+0x08]
       vmovsd   xmm2, qword ptr [rdx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vsubsd   xmm0, xmm2, xmm0
       vmovsd   qword ptr [rbp+0x230], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp+0x230]
       vmovsd   qword ptr [rdx+0x08], xmm0
       jmp      G_M000_IG126
 
G_M000_IG466:
       mov      gword ptr [rbp+0x7D8], rsi
       jmp      G_M000_IG125
 
G_M000_IG467:
       mov      gword ptr [rbp+0x7D8], rsi
       jmp      G_M000_IG125
 
G_M000_IG468:
       mov      rdi, 0xD1FFAB1E
       mov      rbx, gword ptr [rdi]
       mov      rdi, qword ptr [rbp+0x788]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, rbx
 
G_M000_IG469:
       add      rsp, 0xBE8
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG470:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG471:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 11541

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_advance(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
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
       call     [RyuPy.Generated.Module:m0_advance(System.Object,System.Object,System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 47

