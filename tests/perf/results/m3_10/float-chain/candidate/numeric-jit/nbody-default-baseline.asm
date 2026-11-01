; Assembly listing for method RyuPy.Generated.Module:m0_advance(System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 0x580
       lea      rbp, [rsp+0x580]
       xor      eax, eax
       mov      qword ptr [rbp-0x578], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x570], xmm8
       vmovdqa32 xmmword ptr [rbp-0x560], xmm8
       mov      rax, -0x510
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x20], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
 
G_M000_IG02:
       mov      dword ptr [rbp-0x570], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0xF8], rax
       mov      edi, 79
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x560], rax
       mov      rdi, gword ptr [rbp-0x560]
       mov      rdx, gword ptr [rbp-0x30]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x568], rax
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0x578], rax
       mov      rcx, gword ptr [rbp-0x578]
       mov      rsi, gword ptr [rbp-0x560]
       mov      rdx, gword ptr [rbp-0x568]
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
       mov      eax, dword ptr [rbp-0x570]
       dec      eax
       mov      dword ptr [rbp-0x570], eax
       cmp      dword ptr [rbp-0x570], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x570]
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
       jo       G_M000_IG51
       mov      qword ptr [rbp-0x120], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       call     [RyuPy.Runtime.PyOps:None():System.Object]
       mov      gword ptr [rbp-0x558], rax
       jmp      G_M000_IG49
 
G_M000_IG09:
       mov      eax, dword ptr [rbp-0x570]
       dec      eax
       mov      dword ptr [rbp-0x570], eax
       cmp      dword ptr [rbp-0x570], 0
       jg       SHORT G_M000_IG11
 
G_M000_IG10:
       lea      rdi, [rbp-0x570]
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
       jo       G_M000_IG51
       mov      qword ptr [rbp-0x178], rax
       jmp      G_M000_IG09
 
G_M000_IG14:
       mov      edi, 98
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyOps:IterStart(System.Object):System.Object]
       mov      gword ptr [rbp-0x468], rax
       jmp      G_M000_IG38
 
G_M000_IG15:
       mov      eax, dword ptr [rbp-0x570]
       dec      eax
       mov      dword ptr [rbp-0x570], eax
       cmp      dword ptr [rbp-0x570], 0
       jg       SHORT G_M000_IG17
 
G_M000_IG16:
       lea      rdi, [rbp-0x570]
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
       mov      eax, dword ptr [rbp-0x570]
       dec      eax
       mov      dword ptr [rbp-0x570], eax
       cmp      dword ptr [rbp-0x570], 0
       jg       SHORT G_M000_IG19
 
G_M000_IG18:
       lea      rdi, [rbp-0x570]
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
       mov      rdx, gword ptr [rbp-0x78]
       mov      rcx, gword ptr [rbp-0x78]
       mov      edi, 50
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x2A0], rax
       mov      rdx, gword ptr [rbp-0x80]
       mov      rcx, gword ptr [rbp-0x80]
       mov      edi, 51
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x2A8], rax
       mov      rdx, gword ptr [rbp-0x2A0]
       mov      rcx, gword ptr [rbp-0x2A8]
       mov      edi, 52
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x2B0], rax
       mov      rdx, gword ptr [rbp-0x88]
       mov      rcx, gword ptr [rbp-0x88]
       mov      edi, 53
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x2B8], rax
       mov      rdx, gword ptr [rbp-0x2B0]
       mov      rcx, gword ptr [rbp-0x2B8]
 
G_M000_IG22:
       mov      edi, 54
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x2C0], rax
       mov      rax, gword ptr [rbp-0x2C0]
       mov      gword ptr [rbp-0x90], rax
       mov      edi, 89
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x2C8], rax
       mov      rdx, gword ptr [rbp-0x90]
       mov      rcx, gword ptr [rbp-0x2C8]
       mov      edi, 55
       mov      esi, 7
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x2D0], rax
       mov      rdx, gword ptr [rbp-0x90]
       mov      rcx, gword ptr [rbp-0x2D0]
       mov      edi, 56
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x2D8], rax
       mov      rdx, gword ptr [rbp-0x38]
       mov      rcx, gword ptr [rbp-0x2D8]
       mov      edi, 57
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
       mov      edi, 58
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
 
G_M000_IG23:
       mov      rdx, gword ptr [rbp-0x308]
       mov      rcx, gword ptr [rbp-0x98]
       mov      edi, 59
       mov      esi, 3
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
       jne      SHORT G_M000_IG27
       jmp      G_M000_IG28
 
G_M000_IG24:
       mov      eax, dword ptr [rbp-0x570]
       dec      eax
       mov      dword ptr [rbp-0x570], eax
       cmp      dword ptr [rbp-0x570], 0
       jg       SHORT G_M000_IG26
 
G_M000_IG25:
       lea      rdi, [rbp-0x570]
       mov      esi, 0x3F7
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG26:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x1E0]
       add      rax, qword ptr [rbp-0x1F0]
       jo       G_M000_IG51
       mov      qword ptr [rbp-0x1E0], rax
       jmp      G_M000_IG15
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
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
       mov      rdx, gword ptr [rbp-0x78]
       mov      rcx, gword ptr [rbp-0xA8]
       mov      edi, 60
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x338], rax
       mov      rdx, gword ptr [rbp-0x330]
       mov      rcx, gword ptr [rbp-0x338]
       mov      edi, 61
       mov      esi, 258
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x340], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x348], rax
       mov      rdi, gword ptr [rbp-0xB0]
       mov      rsi, gword ptr [rbp-0x348]
       mov      rdx, gword ptr [rbp-0x340]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
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
       jmp      G_M000_IG30
 
G_M000_IG29:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
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
       mov      rdx, gword ptr [rbp-0x80]
       mov      rcx, gword ptr [rbp-0xA8]
       mov      edi, 62
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x370], rax
       mov      rdx, gword ptr [rbp-0x368]
       mov      rcx, gword ptr [rbp-0x370]
       mov      edi, 63
       mov      esi, 258
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x378], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x380], rax
       mov      rdi, gword ptr [rbp-0xB8]
       mov      rsi, gword ptr [rbp-0x380]
       mov      rdx, gword ptr [rbp-0x378]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG30:
       mov      edi, 94
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x388], rax
       mov      rdi, gword ptr [rbp-0x388]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0x38C], eax
       cmp      dword ptr [rbp-0x38C], 0
       jne      SHORT G_M000_IG31
       jmp      G_M000_IG32
 
G_M000_IG31:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
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
       mov      rdx, gword ptr [rbp-0x88]
       mov      rcx, gword ptr [rbp-0xA8]
       mov      edi, 64
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x3A8], rax
       mov      rdx, gword ptr [rbp-0x3A0]
       mov      rcx, gword ptr [rbp-0x3A8]
       mov      edi, 65
       mov      esi, 258
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x3B0], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x3B8], rax
       mov      rdi, gword ptr [rbp-0xC0]
       mov      rsi, gword ptr [rbp-0x3B8]
       mov      rdx, gword ptr [rbp-0x3B0]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG32:
       mov      edi, 95
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x3C0], rax
       mov      rdi, gword ptr [rbp-0x3C0]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0x3C4], eax
       cmp      dword ptr [rbp-0x3C4], 0
       jne      SHORT G_M000_IG33
       jmp      G_M000_IG34
 
G_M000_IG33:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
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
       mov      rdx, gword ptr [rbp-0x78]
       mov      rcx, gword ptr [rbp-0xA0]
       mov      edi, 66
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x3E0], rax
       mov      rdx, gword ptr [rbp-0x3D8]
       mov      rcx, gword ptr [rbp-0x3E0]
       mov      edi, 67
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x3E8], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x3F0], rax
       mov      rdi, gword ptr [rbp-0xC8]
       mov      rsi, gword ptr [rbp-0x3F0]
       mov      rdx, gword ptr [rbp-0x3E8]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG34:
       mov      edi, 96
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x3F8], rax
       mov      rdi, gword ptr [rbp-0x3F8]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0x3FC], eax
       cmp      dword ptr [rbp-0x3FC], 0
       jne      SHORT G_M000_IG35
       jmp      G_M000_IG36
 
G_M000_IG35:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
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
       mov      rdx, gword ptr [rbp-0x80]
       mov      rcx, gword ptr [rbp-0xA0]
       mov      edi, 68
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x418], rax
       mov      rdx, gword ptr [rbp-0x410]
       mov      rcx, gword ptr [rbp-0x418]
       mov      edi, 69
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x420], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x428], rax
       mov      rdi, gword ptr [rbp-0xD0]
       mov      rsi, gword ptr [rbp-0x428]
       mov      rdx, gword ptr [rbp-0x420]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG36:
       mov      edi, 97
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x430], rax
       mov      rdi, gword ptr [rbp-0x430]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0x434], eax
       cmp      dword ptr [rbp-0x434], 0
       jne      SHORT G_M000_IG37
       jmp      G_M000_IG24
 
G_M000_IG37:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
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
       mov      rdx, gword ptr [rbp-0x88]
       mov      rcx, gword ptr [rbp-0xA0]
       mov      edi, 70
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x450], rax
       mov      rdx, gword ptr [rbp-0x448]
       mov      rcx, gword ptr [rbp-0x450]
       mov      edi, 71
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x458], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x460], rax
       mov      rdi, gword ptr [rbp-0xD8]
       mov      rsi, gword ptr [rbp-0x460]
       mov      rdx, gword ptr [rbp-0x458]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
       jmp      G_M000_IG24
 
G_M000_IG38:
       mov      eax, dword ptr [rbp-0x570]
       dec      eax
       mov      dword ptr [rbp-0x570], eax
       cmp      dword ptr [rbp-0x570], 0
       jg       SHORT G_M000_IG40
 
G_M000_IG39:
       lea      rdi, [rbp-0x570]
       mov      esi, 0x6D1
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG40:
       mov      rdi, gword ptr [rbp-0x468]
       call     [RyuPy.Runtime.PyOps:IterNextOk(System.Object):bool]
       mov      dword ptr [rbp-0x46C], eax
       cmp      dword ptr [rbp-0x46C], 0
       jne      SHORT G_M000_IG43
       mov      eax, dword ptr [rbp-0x570]
       dec      eax
       mov      dword ptr [rbp-0x570], eax
       cmp      dword ptr [rbp-0x570], 0
       jg       SHORT G_M000_IG42
 
G_M000_IG41:
       lea      rdi, [rbp-0x570]
       mov      esi, 0x6E1
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG42:
       jmp      G_M000_IG07
 
G_M000_IG43:
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
       jne      SHORT G_M000_IG44
       jmp      G_M000_IG45
 
G_M000_IG44:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
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
       mov      rdx, gword ptr [rbp-0x38]
       mov      rcx, gword ptr [rbp-0x4A8]
       mov      edi, 72
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x4B0], rax
       mov      rdx, gword ptr [rbp-0x498]
       mov      rcx, gword ptr [rbp-0x4B0]
       mov      edi, 73
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x4B8], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x4C0], rax
       mov      rdi, gword ptr [rbp-0xE0]
       mov      rsi, gword ptr [rbp-0x4C0]
       mov      rdx, gword ptr [rbp-0x4B8]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG45:
       mov      edi, 100
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x4C8], rax
       mov      rdi, gword ptr [rbp-0x4C8]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0x4CC], eax
       cmp      dword ptr [rbp-0x4CC], 0
       jne      SHORT G_M000_IG46
       jmp      G_M000_IG47
 
G_M000_IG46:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
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
       mov      rdx, gword ptr [rbp-0x38]
       mov      rcx, gword ptr [rbp-0x4F0]
       mov      edi, 74
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x4F8], rax
       mov      rdx, gword ptr [rbp-0x4E0]
       mov      rcx, gword ptr [rbp-0x4F8]
       mov      edi, 75
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x500], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x508], rax
       mov      rdi, gword ptr [rbp-0xE8]
       mov      rsi, gword ptr [rbp-0x508]
       mov      rdx, gword ptr [rbp-0x500]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG47:
       mov      edi, 101
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x510], rax
       mov      rdi, gword ptr [rbp-0x510]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0x514], eax
       cmp      dword ptr [rbp-0x514], 0
       jne      SHORT G_M000_IG48
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG38
 
G_M000_IG48:
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
       mov      edi, 76
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x540], rax
       mov      rdx, gword ptr [rbp-0x528]
       mov      rcx, gword ptr [rbp-0x540]
       mov      edi, 77
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
       jmp      G_M000_IG38
 
G_M000_IG49:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0xF8]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x558]
 
G_M000_IG50:
       add      rsp, 0x580
       pop      rbp
       ret      
 
G_M000_IG51:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 5531

; Assembly listing for method RyuPy.Generated.Module:m0_advance(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x1d6
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 43 inlinees with PGO data; 62 single block inlinees; 101 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 960
       mov      qword ptr [rsp+0x948], r15
       mov      qword ptr [rsp+0x940], r14
       mov      qword ptr [rsp+0x938], r13
       mov      qword ptr [rsp+0x930], r12
       mov      qword ptr [rsp+0x928], rbx
       lea      rbp, [rsp+0x3C0]
       xor      eax, eax
       mov      qword ptr [rbp-0x308], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x300], xmm8
       mov      rax, -672
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      r15, gword ptr [rbp+0x560]
       mov      rbx, gword ptr [rbp+0x558]
       mov      r14, gword ptr [rbp+0x530]
       mov      r13, qword ptr [rbp+0x3B0]
       mov      r12, qword ptr [rbp+0x3A8]
       mov      rax, qword ptr [rbp+0x3A0]
 
G_M000_IG02:
       jmp      G_M000_IG21
 
G_M000_IG03:
       mov      rax, gword ptr [rbp+0x2F0]
 
G_M000_IG04:
       mov      rdx, rsi
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x358], rax
 
G_M000_IG05:
       xor      rdx, rdx
       mov      gword ptr [rbp-0x2C0], rdx
 
G_M000_IG06:
       mov      gword ptr [rbp-0x2B8], rdx
 
G_M000_IG07:
       mov      gword ptr [rbp-0x2B0], rdx
       mov      rdx, gword ptr [rbp+0x508]
       mov      rcx, rdx
       mov      edi, 53
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x358]
       mov      edi, 54
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x500], rax
       mov      edi, 89
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, gword ptr [rbp+0x500]
       mov      edi, 55
       mov      esi, 7
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp+0x500]
       mov      edi, 56
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, rbx
       mov      edi, 57
       mov      esi, 4
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x4F8], rax
       mov      edi, 90
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, gword ptr [rbp+0x4F8]
       mov      edi, 58
       mov      esi, 3
 
G_M000_IG08:
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x4F0], rax
       mov      edi, 91
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x520]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, gword ptr [rbp+0x4F8]
       mov      edi, 59
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x4E8], rax
       mov      edi, 92
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      qword ptr [rbp-0x328], rax
       mov      rdi, gword ptr [rax]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG10
 
G_M000_IG09:
       mov      edi, 92
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 92
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x398], rax
       mov      rdx, gword ptr [rbp+0x518]
       mov      rcx, gword ptr [rbp+0x4E8]
       mov      edi, 60
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x398]
       mov      edi, 61
       mov      esi, 258
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG10:
       mov      edi, 93
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x328]
       mov      rdi, gword ptr [rax]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG12
 
G_M000_IG11:
       mov      edi, 93
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 93
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x3A0], rax
       mov      rdx, gword ptr [rbp+0x510]
       mov      rcx, gword ptr [rbp+0x4E8]
       mov      edi, 62
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x3A0]
       mov      edi, 63
       mov      esi, 258
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG12:
       mov      edi, 94
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x328]
       mov      rdi, gword ptr [rax]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG14
 
G_M000_IG13:
       mov      edi, 94
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 94
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x3A8], rax
       mov      rdx, gword ptr [rbp+0x508]
       mov      rcx, gword ptr [rbp+0x4E8]
       mov      edi, 64
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x3A8]
       mov      edi, 65
       mov      esi, 258
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG14:
       mov      edi, 95
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x328]
       mov      rdi, gword ptr [rax]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG16
 
G_M000_IG15:
       mov      edi, 95
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 95
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x520]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x3B0], rax
       mov      rdx, gword ptr [rbp+0x518]
       mov      rcx, gword ptr [rbp+0x4F0]
       mov      edi, 66
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x3B0]
       mov      edi, 67
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x520]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG16:
       mov      edi, 96
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x328]
       mov      rdi, gword ptr [rax]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG18
 
G_M000_IG17:
       mov      edi, 96
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 96
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x520]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x3B8], rax
       mov      rdx, gword ptr [rbp+0x510]
       mov      rcx, gword ptr [rbp+0x4F0]
       mov      edi, 68
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x3B8]
       mov      edi, 69
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x520]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG18:
       mov      edi, 97
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x328]
       mov      rdi, gword ptr [rax]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG20
 
G_M000_IG19:
       mov      edi, 97
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 97
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x520]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x3C0], rax
       mov      rdx, gword ptr [rbp+0x508]
       mov      rcx, gword ptr [rbp+0x4F0]
       mov      edi, 70
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x3C0]
       mov      edi, 71
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x520]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG20:
       mov      rax, qword ptr [rbp+0x3A0]
       add      r13, rax
       jo       G_M000_IG268
 
G_M000_IG21:
       mov      qword ptr [rbp+0x3A0], rax
       test     rax, rax
       setg     r10b
       movzx    r10, r10b
       cmp      r13, r12
       setl     r11b
       movzx    r11, r11b
       and      r11d, r10d
       cmp      r13, r12
       setg     r9b
       movzx    r9, r9b
       test     r10d, r10d
       sete     r10b
       movzx    r10, r10b
       and      r9d, r10d
       or       r9d, r11d
       je       G_M000_IG73
 
G_M000_IG22:
       lea      r9, [r13+0x05]
       cmp      r9, 261
       jbe      G_M000_IG142
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r13
       mov      rsi, rax
 
G_M000_IG24:
       mov      r9, 0xD1FFAB1E
       cmp      dword ptr [r9], 0
       jle      G_M000_IG144
 
G_M000_IG25:
       mov      r10, 0xD1FFAB1E
       mov      r11, gword ptr [r10]
       mov      r8d, dword ptr [r9]
       dec      r8d
       cmp      r8d, dword ptr [r11+0x08]
       jae      G_M000_IG267
       cmp      dword ptr [r11+4*r8+0x10], 84
       je       G_M000_IG143
 
G_M000_IG26:
       mov      r8, gword ptr [r10]
       mov      qword ptr [rbp-0x320], r9
       mov      r10d, dword ptr [r9]
       dec      r10d
       cmp      r10d, dword ptr [r8+0x08]
       jae      G_M000_IG267
       mov      dword ptr [r8+4*r10+0x10], 84
 
G_M000_IG27:
       mov      rdi, r15
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x520], rax
       mov      rcx, qword ptr [rbp-0x320]
       cmp      dword ptr [rcx], 0
       jle      G_M000_IG146
 
G_M000_IG28:
       mov      r10, 0xD1FFAB1E
       mov      rsi, gword ptr [r10]
       mov      edi, dword ptr [rcx]
       dec      edi
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG267
       cmp      dword ptr [rsi+4*rdi+0x10], 85
       je       G_M000_IG145
 
G_M000_IG29:
       mov      rsi, gword ptr [r10]
       mov      qword ptr [rbp-0x320], rcx
       mov      edi, dword ptr [rcx]
       dec      edi
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG267
       mov      dword ptr [rsi+4*rdi+0x10], 85
 
G_M000_IG30:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x360], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x520]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, gword ptr [rbp+0x360]
       test     rcx, rcx
       je       SHORT G_M000_IG36
 
G_M000_IG31:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG147
 
G_M000_IG32:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG158
 
G_M000_IG33:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       jne      SHORT G_M000_IG36
 
G_M000_IG34:
       test     rax, rax
       je       SHORT G_M000_IG36
 
G_M000_IG35:
       mov      rdx, qword ptr [rax]
 
G_M000_IG36:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 2
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG37:
       mov      gword ptr [rbp+0x518], rax
       mov      rcx, qword ptr [rbp-0x320]
       cmp      dword ptr [rcx], 0
       jle      SHORT G_M000_IG40
 
G_M000_IG38:
       mov      r10, 0xD1FFAB1E
       mov      rsi, gword ptr [r10]
       mov      edi, dword ptr [rcx]
       dec      edi
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG267
       cmp      dword ptr [rsi+4*rdi+0x10], 86
       je       SHORT G_M000_IG40
 
G_M000_IG39:
       mov      rsi, gword ptr [r10]
       mov      edi, dword ptr [rcx]
       dec      edi
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG267
       mov      dword ptr [rsi+4*rdi+0x10], 86
 
G_M000_IG40:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x338], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x520]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, gword ptr [rbp+0x338]
       test     rcx, rcx
       je       SHORT G_M000_IG46
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG163
 
G_M000_IG42:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG176
 
G_M000_IG43:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       jne      SHORT G_M000_IG46
 
G_M000_IG44:
       test     rax, rax
       je       SHORT G_M000_IG46
 
G_M000_IG45:
       mov      rdx, qword ptr [rax]
 
G_M000_IG46:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 2
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG47:
       mov      gword ptr [rbp+0x510], rax
       mov      edi, 87
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x310], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp+0x520]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, gword ptr [rbp+0x310]
       test     rcx, rcx
       je       SHORT G_M000_IG53
 
G_M000_IG48:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG181
 
G_M000_IG49:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG194
 
G_M000_IG50:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       jne      SHORT G_M000_IG53
 
G_M000_IG51:
       test     rax, rax
       je       SHORT G_M000_IG53
 
G_M000_IG52:
       mov      rdx, qword ptr [rax]
 
G_M000_IG53:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 2
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG54:
       mov      gword ptr [rbp+0x508], rax
       mov      edi, 88
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp+0x518]
       test     rax, rax
       je       SHORT G_M000_IG58
 
G_M000_IG55:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rax], rdx
       je       G_M000_IG199
 
G_M000_IG56:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG216
 
G_M000_IG57:
       mov      rdx, qword ptr [rax]
 
G_M000_IG58:
       mov      rdx, rax
       mov      rsi, rax
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG59:
       mov      gword ptr [rbp+0x2F0], rax
       mov      rcx, gword ptr [rbp+0x510]
       test     rcx, rcx
       je       SHORT G_M000_IG63
 
G_M000_IG60:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       je       G_M000_IG217
 
G_M000_IG61:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       G_M000_IG226
 
G_M000_IG62:
       mov      rdx, qword ptr [rcx]
 
G_M000_IG63:
       mov      rdx, rcx
       mov      rsi, rcx
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG64:
       mov      rsi, rax
       mov      gword ptr [rbp+0x2E8], rsi
       xor      rdi, rdi
       mov      gword ptr [rbp-0x2C0], rdi
 
G_M000_IG65:
       mov      gword ptr [rbp-0x2B8], rdi
 
G_M000_IG66:
       mov      gword ptr [rbp-0x2B0], rdi
       mov      rax, gword ptr [rbp+0x2F0]
       test     rax, rax
       je       G_M000_IG04
 
G_M000_IG67:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG227
 
G_M000_IG68:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       G_M000_IG239
 
G_M000_IG69:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG04
 
G_M000_IG70:
       mov      rdi, rax
       test     rsi, rsi
       je       G_M000_IG04
 
G_M000_IG71:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       jne      G_M000_IG04
 
G_M000_IG72:
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      gword ptr [rbp-0x358], rax
       jmp      G_M000_IG05
 
G_M000_IG73:
       mov      rcx, qword ptr [rbp+0x418]
       mov      rsi, qword ptr [rbp+0x408]
       add      rcx, rsi
       jo       G_M000_IG268
 
G_M000_IG74:
       test     rsi, rsi
       setg     al
       movzx    rax, al
       mov      rdx, qword ptr [rbp+0x410]
       cmp      rcx, rdx
       setl     r9b
       movzx    r9, r9b
       and      r9d, eax
       cmp      rcx, rdx
       setg     r10b
       movzx    r10, r10b
       test     eax, eax
       sete     al
       movzx    rax, al
       and      eax, r10d
       or       eax, r9d
       je       G_M000_IG90
 
G_M000_IG75:
       mov      qword ptr [rbp+0x418], rcx
       lea      r13, [rcx+0x05]
       cmp      r13, 261
       jbe      G_M000_IG106
 
G_M000_IG76:
       mov      qword ptr [rbp+0x410], rdx
       mov      qword ptr [rbp+0x408], rsi
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, qword ptr [rbp+0x418]
       mov      qword ptr [rax+0x08], r13
 
G_M000_IG77:
       mov      gword ptr [rbp+0x538], rax
       mov      rcx, 0xD1FFAB1E
       mov      qword ptr [rbp-0x320], rcx
       mov      r9d, dword ptr [rcx]
       test     r9d, r9d
       jle      SHORT G_M000_IG80
 
G_M000_IG78:
       mov      r10, 0xD1FFAB1E
       mov      r10, gword ptr [r10]
       mov      r11, r10
       dec      r9d
       mov      r12d, r9d
       cmp      r12d, dword ptr [r11+0x08]
       jae      G_M000_IG267
       cmp      dword ptr [r11+4*r12+0x10], 82
       je       SHORT G_M000_IG80
 
G_M000_IG79:
       cmp      r9d, dword ptr [r10+0x08]
       jae      G_M000_IG267
       mov      r9d, r9d
       mov      dword ptr [r10+4*r9+0x10], 82
 
G_M000_IG80:
       mov      rdi, r15
       mov      rsi, rax
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x530], rax
       mov      rdi, qword ptr [rbp-0x320]
       mov      edi, dword ptr [rdi]
       test     edi, edi
       jle      SHORT G_M000_IG83
 
G_M000_IG81:
       mov      r10, 0xD1FFAB1E
       mov      rsi, gword ptr [r10]
       mov      rcx, rsi
       dec      edi
       mov      edx, edi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG267
       cmp      dword ptr [rcx+4*rdx+0x10], 83
       je       SHORT G_M000_IG83
 
G_M000_IG82:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG267
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 83
 
G_M000_IG83:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       mov      rcx, rdx
       mov      rdx, gword ptr [rbp+0x538]
       test     rdx, rdx
       je       SHORT G_M000_IG88
 
G_M000_IG84:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       G_M000_IG107
 
G_M000_IG85:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       je       G_M000_IG125
 
G_M000_IG86:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       je       G_M000_IG131
 
G_M000_IG87:
 
G_M000_IG88:
       mov      rdx, rcx
       mov      rsi, gword ptr [rbp+0x538]
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdi, rax
 
G_M000_IG89:
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp+0x3C0], rax
       mov      rdi, gword ptr [rbp+0x548]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp+0x3C0]
       mov      edx, 1
       mov      qword ptr [rbp+0x418], r13
       mov      r12, rax
       mov      rax, rdx
       mov      r13, rcx
       mov      r14, gword ptr [rbp+0x530]
       jmp      G_M000_IG21
 
G_M000_IG90:
       mov      rcx, 0xD1FFAB1E
       mov      r13, rcx
       mov      eax, dword ptr [r13]
       test     eax, eax
       jle      SHORT G_M000_IG93
 
G_M000_IG91:
       mov      r10, 0xD1FFAB1E
       mov      rcx, gword ptr [r10]
       mov      rdx, rcx
       dec      eax
       mov      esi, eax
       cmp      esi, dword ptr [rdx+0x08]
       jae      G_M000_IG267
       cmp      dword ptr [rdx+4*rsi+0x10], 98
       je       SHORT G_M000_IG93
 
G_M000_IG92:
       cmp      eax, dword ptr [rcx+0x08]
       jae      G_M000_IG267
       mov      eax, eax
       mov      dword ptr [rcx+4*rax+0x10], 98
 
G_M000_IG93:
       mov      rdi, r15
       call     [RyuPy.Runtime.PyIter:Create(System.Object):RyuPy.Runtime.IPyIterator]
       mov      r12, rax
 
G_M000_IG94:
       mov      rdi, r12
       call     [RyuPy.Runtime.PyOps:IterNextOk(System.Object):bool]
       test     eax, eax
       je       G_M000_IG101
 
G_M000_IG95:
       mov      rdi, r12
       call     [RyuPy.Runtime.PyOps:IterValue(System.Object):System.Object]
       mov      r14, rax
       mov      edi, 99
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      qword ptr [rbp-0x328], rax
       mov      rdi, gword ptr [rax]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG97
 
G_M000_IG96:
       mov      edi, 99
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 99
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x380], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, rbx
       mov      edi, 72
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x380]
       mov      edi, 73
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG97:
       mov      edi, 100
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x328]
       mov      rdi, gword ptr [rax]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG99
 
G_M000_IG98:
       mov      edi, 100
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 100
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x388], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, rbx
       mov      edi, 74
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x388]
       mov      edi, 75
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG99:
       mov      edi, 101
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, qword ptr [rbp-0x328]
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG94
 
G_M000_IG100:
       mov      edi, 101
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 101
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x390], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, rbx
       mov      edi, 76
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x390]
       mov      edi, 77
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
       jmp      G_M000_IG94
 
G_M000_IG101:
       mov      r14, qword ptr [rbp+0x470]
       mov      r12, qword ptr [rbp+0x460]
       add      r14, r12
       jo       G_M000_IG268
       mov      qword ptr [rbp+0x460], r12
       test     r12, r12
       setg     dil
       movzx    rdi, dil
       mov      rax, qword ptr [rbp+0x468]
       cmp      r14, rax
       setl     cl
       movzx    rcx, cl
       and      ecx, edi
       mov      qword ptr [rbp+0x468], rax
       cmp      r14, rax
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, ecx
       je       G_M000_IG140
 
G_M000_IG102:
       mov      edi, dword ptr [r13]
       test     edi, edi
       jle      SHORT G_M000_IG105
 
G_M000_IG103:
       mov      r10, 0xD1FFAB1E
       mov      rcx, gword ptr [r10]
       mov      rdx, rcx
       dec      edi
       mov      esi, edi
       cmp      esi, dword ptr [rdx+0x08]
       jae      G_M000_IG267
       cmp      dword ptr [rdx+4*rsi+0x10], 81
       je       SHORT G_M000_IG105
 
G_M000_IG104:
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG267
       mov      edi, edi
       mov      dword ptr [rcx+4*rdi+0x10], 81
 
G_M000_IG105:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r13, rax
       mov      rdi, gword ptr [rbp+0x548]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rdx, rax
       mov      qword ptr [rbp+0x410], rdx
       mov      qword ptr [rbp+0x408], 1
       mov      qword ptr [rbp+0x470], r14
       mov      rcx, r13
       mov      rsi, qword ptr [rbp+0x408]
       jmp      G_M000_IG74
 
G_M000_IG106:
       test     r13, r13
       jl       G_M000_IG268
       mov      rax, r13
       mov      r9, 0xD1FFAB1E
       mov      r11, gword ptr [r9]
       cmp      rax, 262
       jae      G_M000_IG267
       mov      rax, gword ptr [r11+8*rax+0x10]
       mov      r13, rax
       mov      qword ptr [rbp+0x410], rdx
       mov      qword ptr [rbp+0x408], rsi
       mov      rax, r13
       mov      r13, qword ptr [rbp+0x418]
       jmp      G_M000_IG77
 
G_M000_IG107:
       mov      rsi, qword ptr [rdx+0x08]
       test     rcx, rcx
       je       G_M000_IG124
 
G_M000_IG108:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG122
 
G_M000_IG109:
       mov      rcx, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x48], rcx
       lea      rdx, [rsi+rcx]
       mov      qword ptr [rbp-0x50], rdx
       mov      rdi, rsi
       xor      rdi, rdx
       mov      r8, rcx
       xor      r8, rdx
       and      rdi, r8
       jge      G_M000_IG118
 
G_M000_IG110:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x60], xmm0
       lea      rdi, [rbp-0x60]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x60]
       mov      gword ptr [rbp-0x360], rax
       mov      ecx, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0x30C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       mov      rsi, qword ptr [rbp-0x48]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x360]
       mov      esi, dword ptr [rbp-0x30C]
       mov      rdx, gword ptr [rbp-0x70]
       mov      rcx, qword ptr [rbp-0x68]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x368], rax
       mov      ecx, edx
       mov      dword ptr [rbp-0x310], edx
       mov      rdi, rax
       test     rdi, rdi
       jne      G_M000_IG248
 
G_M000_IG111:
       mov      rdi, rax
       test     rdi, rdi
       jne      G_M000_IG257
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jge      SHORT G_M000_IG116
 
G_M000_IG112:
       mov      edi, -1
 
G_M000_IG113:
       test     edi, edi
       jg       SHORT G_M000_IG117
 
G_M000_IG114:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x348], rax
       mov      rdi, gword ptr [rbp-0x368]
       mov      esi, dword ptr [rbp-0x310]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rdi, gword ptr [rbp-0x348]
       mov      qword ptr [rdi+0x08], rax
 
G_M000_IG115:
       jmp      G_M000_IG89
 
G_M000_IG116:
       xor      edi, edi
       jmp      SHORT G_M000_IG113
 
G_M000_IG117:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, rax
       mov      gword ptr [rbp-0x340], rdi
       lea      rdi, bword ptr [rdi+0x08]
       mov      rsi, gword ptr [rbp-0x368]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, gword ptr [rbp-0x340]
       mov      ecx, dword ptr [rbp-0x310]
       mov      dword ptr [rdi+0x10], ecx
       jmp      SHORT G_M000_IG115
 
G_M000_IG118:
       lea      rcx, [rdx+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG121
 
G_M000_IG119:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x50]
       mov      qword ptr [rax+0x08], rdi
       mov      rdi, rax
 
G_M000_IG120:
       jmp      SHORT G_M000_IG115
 
G_M000_IG121:
       test     rcx, rcx
       jl       G_M000_IG268
       mov      rdi, rcx
       mov      rcx, 0xD1FFAB1E
       mov      r11, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG267
       mov      rdi, gword ptr [r11+8*rdi+0x10]
       jmp      SHORT G_M000_IG120
 
G_M000_IG122:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG88
 
G_M000_IG123:
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
       jmp      G_M000_IG89
 
G_M000_IG124:
       jmp      G_M000_IG88
 
G_M000_IG125:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     rcx, rcx
       je       G_M000_IG130
 
G_M000_IG126:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      SHORT G_M000_IG128
 
G_M000_IG127:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdi, rax
       jmp      G_M000_IG89
 
G_M000_IG128:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG88
 
G_M000_IG129:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x80], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x80]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdi, rax
       jmp      G_M000_IG89
 
G_M000_IG130:
       jmp      G_M000_IG88
 
G_M000_IG131:
       mov      rdi, rdx
       mov      gword ptr [rbp-0x330], rdi
       test     rcx, rcx
       je       G_M000_IG88
 
G_M000_IG132:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rsi
       jne      G_M000_IG88
 
G_M000_IG133:
       mov      gword ptr [rbp-0x338], rcx
       cmp      dword ptr [rdi+0x08], 0
       je       G_M000_IG138
 
G_M000_IG134:
       cmp      dword ptr [rcx+0x08], 0
       je       G_M000_IG137
 
G_M000_IG135:
       mov      edx, dword ptr [rdi+0x08]
       mov      dword ptr [rbp-0x84], edx
       mov      esi, edx
       add      esi, dword ptr [rcx+0x08]
       js       G_M000_IG266
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      rdi, rax
       mov      gword ptr [rbp-0x350], rdi
       cmp      byte  ptr [rdi], dil
       add      rdi, 12
       mov      rax, gword ptr [rbp-0x330]
       lea      rsi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [rax+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0x84]
       mov      rax, gword ptr [rbp-0x350]
       lea      rdi, bword ptr [rax+2*rdi+0x0C]
       mov      rcx, gword ptr [rbp-0x338]
       lea      rsi, bword ptr [rcx+0x0C]
       mov      edx, dword ptr [rcx+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rdi, gword ptr [rbp-0x350]
 
G_M000_IG136:
       jmp      G_M000_IG89
 
G_M000_IG137:
       jmp      SHORT G_M000_IG136
 
G_M000_IG138:
       cmp      dword ptr [rcx+0x08], 0
       je       G_M000_IG265
 
G_M000_IG139:
       mov      rdi, rcx
       jmp      SHORT G_M000_IG136
 
G_M000_IG140:
       mov      rdi, 0xD1FFAB1E
       mov      rbx, gword ptr [rdi]
       mov      rdi, qword ptr [rbp+0x498]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, rbx
 
G_M000_IG141:
       add      rsp, 0x928
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG142:
       test     r9, r9
       jl       G_M000_IG268
       mov      r10, r9
       mov      r11, 0xD1FFAB1E
       mov      r11, gword ptr [r11]
       cmp      r10, 262
       jae      G_M000_IG267
       test     r9, r9
       jl       G_M000_IG268
       mov      r9, gword ptr [r11+8*r9+0x10]
       mov      rsi, r9
       jmp      G_M000_IG24
 
G_M000_IG143:
       mov      qword ptr [rbp-0x320], r9
       jmp      G_M000_IG27
 
G_M000_IG144:
       mov      qword ptr [rbp-0x320], r9
       jmp      G_M000_IG27
 
G_M000_IG145:
       mov      qword ptr [rbp-0x320], rcx
       jmp      G_M000_IG30
 
G_M000_IG146:
       mov      qword ptr [rbp-0x320], rcx
       jmp      G_M000_IG30
 
G_M000_IG147:
       mov      rsi, qword ptr [rcx+0x08]
       test     rax, rax
       je       G_M000_IG36
 
G_M000_IG148:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG156
 
G_M000_IG149:
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
       jge      G_M000_IG152
 
G_M000_IG150:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xC0], xmm0
       lea      rdi, [rbp-0xC0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xC0]
       mov      gword ptr [rbp-0x370], rax
       mov      ecx, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0x314], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xD0], xmm0
       lea      rdi, [rbp-0xD0]
       mov      rsi, qword ptr [rbp-0xA8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x370]
       mov      esi, dword ptr [rbp-0x314]
       mov      rdx, gword ptr [rbp-0xD0]
       mov      rcx, qword ptr [rbp-0xC8]
       call     [System.Numerics.BigInteger:op_Subtraction(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG151:
       jmp      G_M000_IG37
 
G_M000_IG152:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG155
 
G_M000_IG153:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0xB0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG154:
       jmp      SHORT G_M000_IG151
 
G_M000_IG155:
       test     rax, rax
       jl       G_M000_IG268
       mov      rdi, rax
       mov      rcx, 0xD1FFAB1E
       mov      r11, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG267
       test     rax, rax
       jl       G_M000_IG268
       mov      rdi, rax
       mov      rax, gword ptr [r11+8*rdi+0x10]
       jmp      SHORT G_M000_IG154
 
G_M000_IG156:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG36
 
G_M000_IG157:
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
       jmp      G_M000_IG37
 
G_M000_IG158:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm0
       test     rax, rax
       je       G_M000_IG36
 
G_M000_IG159:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG161
 
G_M000_IG160:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x90]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG37
 
G_M000_IG161:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG36
 
G_M000_IG162:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xE0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x90]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0xE0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG37
 
G_M000_IG163:
       mov      rsi, qword ptr [rcx+0x08]
       test     rax, rax
       je       G_M000_IG46
 
G_M000_IG164:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG174
 
G_M000_IG165:
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
       jge      G_M000_IG170
 
G_M000_IG166:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x118], xmm0
       lea      rdi, [rbp-0x118]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG167:
       vmovdqu  xmm0, xmmword ptr [rbp-0x118]
       vmovdqu  xmmword ptr [rbp-0x128], xmm0
 
G_M000_IG168:
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
 
G_M000_IG169:
       jmp      G_M000_IG47
 
G_M000_IG170:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG173
 
G_M000_IG171:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x108]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG172:
       jmp      SHORT G_M000_IG169
 
G_M000_IG173:
       test     rax, rax
       jl       G_M000_IG268
       mov      rdi, rax
       mov      rcx, 0xD1FFAB1E
       mov      r11, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG267
       test     rax, rax
       jl       G_M000_IG268
       mov      rdi, rax
       mov      rax, gword ptr [r11+8*rdi+0x10]
       jmp      SHORT G_M000_IG172
 
G_M000_IG174:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG46
 
G_M000_IG175:
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
       jmp      G_M000_IG47
 
G_M000_IG176:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0xE8], xmm0
       test     rax, rax
       je       G_M000_IG46
 
G_M000_IG177:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG179
 
G_M000_IG178:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xF0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xE8]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG47
 
G_M000_IG179:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG46
 
G_M000_IG180:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x158], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xE8]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x158]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG47
 
G_M000_IG181:
       mov      rsi, qword ptr [rcx+0x08]
       test     rax, rax
       je       G_M000_IG53
 
G_M000_IG182:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG192
 
G_M000_IG183:
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
       jge      G_M000_IG188
 
G_M000_IG184:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x190], xmm0
       lea      rdi, [rbp-0x190]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG185:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x190]
       vmovdqu32 xmmword ptr [rbp-0x1A0], xmm0
 
G_M000_IG186:
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
 
G_M000_IG187:
       jmp      G_M000_IG54
 
G_M000_IG188:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG191
 
G_M000_IG189:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x180]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG190:
       jmp      SHORT G_M000_IG187
 
G_M000_IG191:
       test     rax, rax
       jl       G_M000_IG268
       mov      rdi, rax
       mov      rcx, 0xD1FFAB1E
       mov      r11, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG267
       test     rax, rax
       jl       G_M000_IG268
       mov      rdi, rax
       mov      rax, gword ptr [r11+8*rdi+0x10]
       jmp      SHORT G_M000_IG190
 
G_M000_IG192:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG53
 
G_M000_IG193:
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
       jmp      G_M000_IG54
 
G_M000_IG194:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x160], xmm0
       test     rax, rax
       je       G_M000_IG53
 
G_M000_IG195:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG197
 
G_M000_IG196:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x168], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x160]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x168]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG54
 
G_M000_IG197:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG53
 
G_M000_IG198:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x1D0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x160]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x1D0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG54
 
G_M000_IG199:
       mov      rsi, qword ptr [rax+0x08]
       cmp      dword ptr [rax], eax
       mov      rcx, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x1E8], rcx
       lea      rdi, [rbp-0x200]
       mov      rdx, rsi
       mulx     rdx, r8, rcx
       mov      qword ptr [rdi], r8
       mov      r8, qword ptr [rbp-0x200]
       mov      rdi, rsi
       sar      rdi, 63
       and      rdi, rcx
       sub      rdx, rdi
       mov      rdi, rcx
       sar      rdi, 63
       and      rdi, rsi
       sub      rdx, rdi
       mov      rdi, r8
       sar      rdi, 63
       cmp      rdx, rdi
       je       G_M000_IG212
 
G_M000_IG200:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1F8], xmm0
       lea      rdi, [rbp-0x1F8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x1F8]
       mov      gword ptr [rbp-0x378], rax
       mov      ecx, dword ptr [rbp-0x1F0]
       mov      dword ptr [rbp-0x318], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x210], xmm0
       lea      rdi, [rbp-0x210]
       mov      rsi, qword ptr [rbp-0x1E8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x210]
       mov      r9d, dword ptr [rbp-0x208]
       mov      rcx, gword ptr [rbp-0x378]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG205
 
G_M000_IG201:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x230], xmm0
       movsxd   rsi, dword ptr [rbp-0x318]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x230]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG202:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x230]
       vmovdqu32 xmmword ptr [rbp-0x220], xmm0
 
G_M000_IG203:
       mov      rdi, gword ptr [rbp-0x220]
       mov      rsi, qword ptr [rbp-0x218]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG204:
       jmp      G_M000_IG59
 
G_M000_IG205:
       test     rcx, rcx
       je       SHORT G_M000_IG210
 
G_M000_IG206:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG207:
       test     rax, rax
       je       SHORT G_M000_IG211
 
G_M000_IG208:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG209:
       mov      edx, dword ptr [rbp-0x318]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x220], rax
       mov      qword ptr [rbp-0x218], rdx
       jmp      SHORT G_M000_IG203
 
G_M000_IG210:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG207
 
G_M000_IG211:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG209
 
G_M000_IG212:
       mov      rcx, r8
       mov      qword ptr [rbp-0x238], rcx
       lea      rdx, [rcx+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG215
 
G_M000_IG213:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x238]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG214:
       jmp      SHORT G_M000_IG204
 
G_M000_IG215:
       test     rdx, rdx
       jl       G_M000_IG268
       mov      rdi, rdx
       mov      rcx, 0xD1FFAB1E
       mov      r11, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG267
       test     rdx, rdx
       jl       G_M000_IG268
       mov      rdi, rdx
       mov      rdi, gword ptr [r11+8*rdi+0x10]
       mov      rax, rdi
       jmp      SHORT G_M000_IG214
 
G_M000_IG216:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x1D8], xmm0
       cmp      dword ptr [rax], eax
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x1E0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1D8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x1E0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG59
 
G_M000_IG217:
       mov      r8, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x250], r8
       cmp      dword ptr [rcx], ecx
       mov      r9, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x258], r9
       xor      edx, edx
       mov      qword ptr [rbp-0x260], rdx
       lea      rdx, [rbp-0x260]
       mov      rdi, r8
       mov      rsi, r9
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x260]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG222
 
G_M000_IG218:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x270], xmm0
       lea      rdi, [rbp-0x270]
       mov      rsi, qword ptr [rbp-0x250]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG219:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x270]
       vmovdqu32 xmmword ptr [rbp-0x280], xmm0
 
G_M000_IG220:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2A0], xmm0
       lea      rdi, [rbp-0x2A0]
       mov      rsi, qword ptr [rbp-0x258]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x280]
       mov      rsi, qword ptr [rbp-0x278]
       mov      rdx, gword ptr [rbp-0x2A0]
       mov      rcx, qword ptr [rbp-0x298]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x290], rax
       mov      qword ptr [rbp-0x288], rdx
       mov      rdi, gword ptr [rbp-0x290]
       mov      rsi, qword ptr [rbp-0x288]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG221:
       jmp      G_M000_IG64
 
G_M000_IG222:
       mov      rax, qword ptr [rbp-0x260]
       mov      qword ptr [rbp-0x2A8], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG225
 
G_M000_IG223:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x2A8]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG224:
       jmp      SHORT G_M000_IG221
 
G_M000_IG225:
       test     rcx, rcx
       jl       G_M000_IG268
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      r11, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG267
       test     rcx, rcx
       jl       G_M000_IG268
       mov      rdi, rcx
       mov      rax, gword ptr [r11+8*rdi+0x10]
       jmp      SHORT G_M000_IG224
 
G_M000_IG226:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x240], xmm0
       cmp      dword ptr [rcx], ecx
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x248], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x240]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x248]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG64
 
G_M000_IG227:
       mov      rcx, qword ptr [rax+0x08]
       test     rsi, rsi
       je       G_M000_IG03
 
G_M000_IG228:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG235
 
G_M000_IG229:
       mov      rax, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x2C8], rax
       lea      rdi, [rcx+rax]
       mov      rsi, rcx
       xor      rsi, rdi
       mov      rdx, rax
       xor      rdx, rdi
       and      rsi, rdx
       jge      G_M000_IG234
 
G_M000_IG230:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x2D8], xmm0
       lea      rdi, [rbp-0x2D8]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG231:
       vmovdqu  xmm0, xmmword ptr [rbp-0x2D8]
       vmovdqu  xmmword ptr [rbp-0x2E8], xmm0
 
G_M000_IG232:
       mov      rdi, qword ptr [rbp-0x2C8]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x2F8], rax
       mov      qword ptr [rbp-0x2F0], rdx
       mov      rdi, gword ptr [rbp-0x2E8]
       mov      rsi, qword ptr [rbp-0x2E0]
       mov      rdx, gword ptr [rbp-0x2F8]
       mov      rcx, qword ptr [rbp-0x2F0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x308], rax
       mov      qword ptr [rbp-0x300], rdx
       mov      rdi, gword ptr [rbp-0x308]
       mov      rsi, qword ptr [rbp-0x300]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG233:
       mov      gword ptr [rbp-0x358], rax
       jmp      G_M000_IG05
 
G_M000_IG234:
       call     [RyuPy.Runtime.PyInt:Box(long):System.Object]
       jmp      SHORT G_M000_IG233
 
G_M000_IG235:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG238
 
G_M000_IG236:
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, rcx
       vmovsd   xmm1, qword ptr [rsi+0x08]
       lea      rsi, [rbp-0x2C0]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x2F0]
       mov      rsi, gword ptr [rbp+0x2E8]
       je       G_M000_IG04
 
G_M000_IG237:
       mov      rax, gword ptr [rbp-0x2C0]
       mov      gword ptr [rbp-0x358], rax
       jmp      G_M000_IG05
 
G_M000_IG238:
       mov      rax, gword ptr [rbp+0x2F0]
       jmp      G_M000_IG04
 
G_M000_IG239:
       vmovsd   xmm0, qword ptr [rax+0x08]
       test     rsi, rsi
       je       G_M000_IG247
 
G_M000_IG240:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG243
 
G_M000_IG241:
       vmovsd   xmm1, qword ptr [rsi+0x08]
       lea      rsi, [rbp-0x2B8]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x2F0]
       mov      rsi, gword ptr [rbp+0x2E8]
       je       G_M000_IG04
 
G_M000_IG242:
       mov      rax, gword ptr [rbp-0x2B8]
       mov      gword ptr [rbp-0x358], rax
       jmp      G_M000_IG05
 
G_M000_IG243:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG246
 
G_M000_IG244:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rsi+0x08]
       lea      rsi, [rbp-0x2B0]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x2F0]
       mov      rsi, gword ptr [rbp+0x2E8]
       je       G_M000_IG04
 
G_M000_IG245:
       mov      rax, gword ptr [rbp-0x2B0]
       mov      gword ptr [rbp-0x358], rax
       jmp      G_M000_IG05
 
G_M000_IG246:
       mov      rax, gword ptr [rbp+0x2F0]
       jmp      G_M000_IG04
 
G_M000_IG247:
       mov      rax, gword ptr [rbp+0x2F0]
       jmp      G_M000_IG04
 
G_M000_IG248:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG249
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG250
 
G_M000_IG249:
       mov      edx, ecx
       jmp      SHORT G_M000_IG256
 
G_M000_IG250:
       cmp      edi, 2
       je       SHORT G_M000_IG251
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG267
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG252
 
G_M000_IG251:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
 
G_M000_IG252:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG253
       mov      edx, -1
       jmp      SHORT G_M000_IG255
 
G_M000_IG253:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jbe      SHORT G_M000_IG254
       mov      edx, 1
       jmp      SHORT G_M000_IG255
 
G_M000_IG254:
       xor      edx, edx
 
G_M000_IG255:
       imul     edx, ecx
 
G_M000_IG256:
       test     edx, edx
       jge      G_M000_IG111
       jmp      G_M000_IG117
 
G_M000_IG257:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG258
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG259
 
G_M000_IG258:
       mov      edi, ecx
       mov      ecx, dword ptr [rbp-0x310]
       jmp      G_M000_IG113
 
G_M000_IG259:
       cmp      edi, 2
       je       SHORT G_M000_IG260
       cmp      dword ptr [rax+0x08], 0
       jbe      SHORT G_M000_IG267
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG261
 
G_M000_IG260:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
       mov      rax, gword ptr [rbp-0x368]
 
G_M000_IG261:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG262
       mov      edx, -1
       jmp      SHORT G_M000_IG264
 
G_M000_IG262:
       test     rdi, rdi
       jge      SHORT G_M000_IG263
       mov      edx, 1
       jmp      SHORT G_M000_IG264
 
G_M000_IG263:
       xor      edx, edx
 
G_M000_IG264:
       mov      edi, ecx
       imul     edi, edx
       jmp      G_M000_IG113
 
G_M000_IG265:
       mov      rdi, 0xD1FFAB1E
       jmp      G_M000_IG136
 
G_M000_IG266:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG267:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG268:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 8658

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

