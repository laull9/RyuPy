; Assembly listing for method RyuPy.Generated.Module:m0_list_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 336
       lea      rbp, [rsp+0x150]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x140], zmm8
       vmovdqu32 zmmword ptr [rbp-0x100], zmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
       mov      gword ptr [rbp-0x48], rcx
       mov      gword ptr [rbp-0x50], r8
 
G_M000_IG02:
       mov      dword ptr [rbp-0x148], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x68], rax
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      rdi, rax
       call     [RyuPy.Runtime.PyOps:MakeList(System.Object[]):System.Object]
       mov      gword ptr [rbp-0x70], rax
       mov      rax, gword ptr [rbp-0x70]
       mov      gword ptr [rbp-0x58], rax
       mov      edi, 9
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x78], rax
       mov      rdi, gword ptr [rbp-0x78]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x80], rax
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x88], rax
       mov      rax, qword ptr [rbp-0x80]
       mov      qword ptr [rbp-0x90], rax
       mov      rax, qword ptr [rbp-0x88]
       mov      qword ptr [rbp-0x98], rax
       mov      qword ptr [rbp-0xA0], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x148]
       dec      eax
       mov      dword ptr [rbp-0x148], eax
       cmp      dword ptr [rbp-0x148], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x148]
       mov      esi, 90
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
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdx, gword ptr [rbp-0x38]
       mov      rcx, gword ptr [rbp-0x40]
       xor      edi, edi
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xD0], rax
       mov      rdx, gword ptr [rbp-0xD0]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 1
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xD8], rax
       mov      rdx, gword ptr [rbp-0xD8]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 2
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE0], rax
       mov      rdx, gword ptr [rbp-0xE0]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 3
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE8], rax
       mov      rdx, gword ptr [rbp-0xE8]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 4
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xF0], rax
       mov      rdx, gword ptr [rbp-0xF0]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 5
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xF8], rax
       mov      rdx, gword ptr [rbp-0x50]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 6
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x100], rax
       mov      rdx, gword ptr [rbp-0x100]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 7
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x108], rax
       mov      rdx, gword ptr [rbp-0x108]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 8
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x110], rax
       mov      rdx, gword ptr [rbp-0x110]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 9
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x118], rax
       mov      rdx, gword ptr [rbp-0x118]
 
G_M000_IG07:
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 10
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x120], rax
       mov      rdx, gword ptr [rbp-0x120]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 11
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x128], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x140], rax
       mov      rdi, gword ptr [rbp-0x140]
       mov      rdx, gword ptr [rbp-0xF8]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x140]
       mov      rdx, gword ptr [rbp-0x128]
       mov      esi, 1
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x140]
       call     [RyuPy.Runtime.PyOps:MakeList(System.Object[]):System.Object]
       mov      gword ptr [rbp-0x130], rax
       mov      rax, gword ptr [rbp-0x130]
       mov      gword ptr [rbp-0x58], rax
       mov      rax, qword ptr [rbp-0x90]
       add      rax, qword ptr [rbp-0xA0]
       jo       SHORT G_M000_IG10
       mov      qword ptr [rbp-0x90], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x138], rax
       mov      rdi, qword ptr [rbp-0x68]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x138]
 
G_M000_IG09:
       add      rsp, 336
       pop      rbp
       ret      
 
G_M000_IG10:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1080

; Assembly listing for method RyuPy.Generated.Module:m0_list_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x5a
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 63 inlinees with PGO data; 78 single block inlinees; 110 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 0x420
       mov      qword ptr [rsp+0x578], r15
       mov      qword ptr [rsp+0x570], r14
       mov      qword ptr [rsp+0x568], r13
       mov      qword ptr [rsp+0x560], r12
       mov      qword ptr [rsp+0x558], rbx
       lea      rbp, [rsp+0x420]
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x390], xmm8
       mov      rax, -816
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      qword ptr [rbp-0x50], rax
       mov      r13, gword ptr [rbp+0x128]
       mov      r15, gword ptr [rbp+0x120]
       mov      rbx, gword ptr [rbp+0x118]
       mov      r14, gword ptr [rbp+0x110]
       mov      rcx, gword ptr [rbp+0x108]
       mov      r12, qword ptr [rbp+0xD0]
       mov      rax, qword ptr [rbp+0xC8]
       mov      r8, qword ptr [rbp+0xC0]
 
G_M000_IG02:
       mov      qword ptr [rbp+0xC0], r8
       test     r8, r8
       setg     r10b
       movzx    r10, r10b
       mov      dword ptr [rbp-0x3AC], r10d
       jmp      G_M000_IG107
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG07
 
G_M000_IG04:
       mov      rdi, rsi
       test     rbx, rbx
       je       SHORT G_M000_IG07
 
G_M000_IG05:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG07
 
G_M000_IG06:
       mov      rsi, rbx
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       jmp      SHORT G_M000_IG08
 
G_M000_IG07:
       mov      rdx, rbx
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG08:
       test     rsi, rsi
       je       G_M000_IG38
 
G_M000_IG09:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG31
 
G_M000_IG10:
       mov      rdx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG38
 
G_M000_IG11:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG29
 
G_M000_IG12:
       mov      rcx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x210], rcx
       lea      rdi, [rbp-0x228]
       mulx     r8, rsi, rcx
       mov      qword ptr [rdi], rsi
       mov      r9, qword ptr [rbp-0x228]
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, rcx
       sub      r8, rdi
       mov      rdi, rcx
       sar      rdi, 63
       and      rdi, rdx
       sub      r8, rdi
       mov      rdi, r9
       sar      rdi, 63
       cmp      r8, rdi
       je       G_M000_IG25
 
G_M000_IG13:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x220], xmm0
       lea      rdi, [rbp-0x220]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x220]
       mov      gword ptr [rbp-0x420], rax
       mov      ecx, dword ptr [rbp-0x218]
       mov      dword ptr [rbp-0x3A8], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x238], xmm0
       lea      rdi, [rbp-0x238]
       mov      rsi, qword ptr [rbp-0x210]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x238]
       mov      r9d, dword ptr [rbp-0x230]
       mov      rcx, gword ptr [rbp-0x420]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG18
 
G_M000_IG14:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x258], xmm0
       movsxd   rsi, dword ptr [rbp-0x3A8]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x258]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG15:
       vmovdqu  xmm0, xmmword ptr [rbp-0x258]
       vmovdqu  xmmword ptr [rbp-0x248], xmm0
 
G_M000_IG16:
       mov      rdi, gword ptr [rbp-0x248]
       mov      rsi, qword ptr [rbp-0x240]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG17:
       mov      rsi, rax
       jmp      G_M000_IG39
 
G_M000_IG18:
       test     rcx, rcx
       je       SHORT G_M000_IG23
 
G_M000_IG19:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG20:
       test     rax, rax
       je       SHORT G_M000_IG24
 
G_M000_IG21:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG22:
       mov      edx, dword ptr [rbp-0x3A8]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x248], rax
       mov      qword ptr [rbp-0x240], rdx
       jmp      SHORT G_M000_IG16
 
G_M000_IG23:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG20
 
G_M000_IG24:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG22
 
G_M000_IG25:
       mov      rcx, r9
       mov      qword ptr [rbp-0x260], rcx
       lea      rdx, [rcx+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG28
 
G_M000_IG26:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x260]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG27:
       jmp      SHORT G_M000_IG17
 
G_M000_IG28:
       test     rdx, rdx
       jl       G_M000_IG287
       mov      rax, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG286
       test     rdx, rdx
       jl       G_M000_IG287
       mov      rax, rdx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG27
 
G_M000_IG29:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG38
 
G_M000_IG30:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x208], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x268], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x268]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x208]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG39
 
G_M000_IG31:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG37
 
G_M000_IG32:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x1F8], xmm0
       test     r15, r15
       je       G_M000_IG38
 
G_M000_IG33:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG35
 
G_M000_IG34:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x200], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1F8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x200]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG39
 
G_M000_IG35:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG38
 
G_M000_IG36:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x270], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1F8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x270]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG39
 
G_M000_IG37:
       mov      rdx, qword ptr [rsi]
 
G_M000_IG38:
       mov      rdx, r15
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG39:
       test     rsi, rsi
       je       G_M000_IG64
 
G_M000_IG40:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG54
 
G_M000_IG41:
       mov      rcx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG64
 
G_M000_IG42:
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG52
 
G_M000_IG43:
       mov      rdx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x290], rdx
       lea      r8, [rcx+rdx]
       mov      qword ptr [rbp-0x298], r8
       mov      rdi, rcx
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG48
 
G_M000_IG44:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x2A8], xmm0
       lea      rdi, [rbp-0x2A8]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG45:
       vmovdqu  xmm0, xmmword ptr [rbp-0x2A8]
       vmovdqu  xmmword ptr [rbp-0x2B8], xmm0
 
G_M000_IG46:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x2D8], xmm0
       lea      rdi, [rbp-0x2D8]
       mov      rsi, qword ptr [rbp-0x290]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x2B8]
       mov      rsi, qword ptr [rbp-0x2B0]
       mov      rdx, gword ptr [rbp-0x2D8]
       mov      rcx, qword ptr [rbp-0x2D0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x2C8], rax
       mov      qword ptr [rbp-0x2C0], rdx
       mov      rdi, gword ptr [rbp-0x2C8]
       mov      rsi, qword ptr [rbp-0x2C0]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG47:
       jmp      G_M000_IG65
 
G_M000_IG48:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG51
 
G_M000_IG49:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x298]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG50:
       jmp      SHORT G_M000_IG47
 
G_M000_IG51:
       test     rcx, rcx
       jl       G_M000_IG287
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG286
       test     rcx, rcx
       jl       G_M000_IG287
       mov      rax, rcx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG50
 
G_M000_IG52:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG64
 
G_M000_IG53:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x288], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x2E0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2E0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x288]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG65
 
G_M000_IG54:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG60
 
G_M000_IG55:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x278], xmm0
       test     rbx, rbx
       je       G_M000_IG64
 
G_M000_IG56:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG58
 
G_M000_IG57:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x280], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x278]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x280]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG65
 
G_M000_IG58:
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG64
 
G_M000_IG59:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x2E8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x278]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2E8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG65
 
G_M000_IG60:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG64
 
G_M000_IG61:
       mov      rdi, rsi
       test     rbx, rbx
       je       SHORT G_M000_IG64
 
G_M000_IG62:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG64
 
G_M000_IG63:
       mov      rsi, rbx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG65
 
G_M000_IG64:
       mov      rdx, rbx
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG65:
       mov      gword ptr [rbp+0x68], rax
       test     r14, r14
       je       G_M000_IG87
 
G_M000_IG66:
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      qword ptr [r14], rcx
       jne      G_M000_IG80
 
G_M000_IG67:
       mov      r8, qword ptr [r14+0x08]
       mov      qword ptr [rbp-0x300], r8
       test     r15, r15
       je       G_M000_IG87
 
G_M000_IG68:
       mov      qword ptr [rbp-0x3B8], rcx
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG78
 
G_M000_IG69:
       mov      r9, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x310], r9
       xor      edx, edx
       mov      qword ptr [rbp-0x318], rdx
       lea      rdx, [rbp-0x318]
       mov      rdi, r8
       mov      rsi, r9
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x318]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG74
 
G_M000_IG70:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x328], xmm0
       lea      rdi, [rbp-0x328]
       mov      rsi, qword ptr [rbp-0x300]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG71:
       vmovdqu  xmm0, xmmword ptr [rbp-0x328]
       vmovdqu  xmmword ptr [rbp-0x338], xmm0
 
G_M000_IG72:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x358], xmm0
       lea      rdi, [rbp-0x358]
       mov      rsi, qword ptr [rbp-0x310]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x338]
       mov      rsi, qword ptr [rbp-0x330]
       mov      rdx, gword ptr [rbp-0x358]
       mov      rcx, qword ptr [rbp-0x350]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x348], rax
       mov      qword ptr [rbp-0x340], rdx
       mov      rdi, gword ptr [rbp-0x348]
       mov      rsi, qword ptr [rbp-0x340]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG73:
       jmp      G_M000_IG88
 
G_M000_IG74:
       mov      rax, qword ptr [rbp-0x318]
       mov      qword ptr [rbp-0x360], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG77
 
G_M000_IG75:
       mov      rdi, qword ptr [rbp-0x3B8]
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x360]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG76:
       jmp      SHORT G_M000_IG73
 
G_M000_IG77:
       test     rcx, rcx
       jl       G_M000_IG287
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG286
       test     rcx, rcx
       jl       G_M000_IG287
       mov      rax, rcx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG76
 
G_M000_IG78:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG87
 
G_M000_IG79:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x308], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, r8
       vmovsd   qword ptr [rbp-0x368], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x368]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x308]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG88
 
G_M000_IG80:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG86
 
G_M000_IG81:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x2F0], xmm0
       test     r15, r15
       je       G_M000_IG87
 
G_M000_IG82:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG84
 
G_M000_IG83:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x2F8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x2F0]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x2F8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG88
 
G_M000_IG84:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG87
 
G_M000_IG85:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x370], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x2F0]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x370]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG88
 
G_M000_IG86:
       mov      rdx, qword ptr [r14]
 
G_M000_IG87:
       mov      rdx, r15
       mov      rsi, r14
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG88:
       mov      gword ptr [rbp+0x60], rax
       xor      rdx, rdx
       mov      gword ptr [rbp-0x390], rdx
 
G_M000_IG89:
       mov      gword ptr [rbp-0x388], rdx
 
G_M000_IG90:
       mov      gword ptr [rbp-0x380], rdx
 
G_M000_IG91:
       mov      gword ptr [rbp-0x378], rdx
       test     rax, rax
       je       G_M000_IG102
 
G_M000_IG92:
       mov      rcx, 0xD1FFAB1E
       mov      r8, rcx
       cmp      qword ptr [rax], r8
       jne      G_M000_IG203
 
G_M000_IG93:
       mov      rsi, qword ptr [rax+0x08]
       test     rbx, rbx
       je       SHORT G_M000_IG101
 
G_M000_IG94:
       cmp      qword ptr [rbx], r8
       jne      SHORT G_M000_IG97
 
G_M000_IG95:
       mov      rdx, qword ptr [rbx+0x08]
       lea      rcx, [rbp-0x390]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:TryLongBinary(int,long,long,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x60]
       je       SHORT G_M000_IG102
 
G_M000_IG96:
       mov      rdx, gword ptr [rbp-0x390]
       jmp      SHORT G_M000_IG103
 
G_M000_IG97:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG100
 
G_M000_IG98:
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, rsi
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x388]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x60]
       je       SHORT G_M000_IG102
 
G_M000_IG99:
       mov      rdx, gword ptr [rbp-0x388]
       jmp      SHORT G_M000_IG103
 
G_M000_IG100:
       mov      rax, gword ptr [rbp+0x60]
       jmp      SHORT G_M000_IG102
 
G_M000_IG101:
       mov      rax, gword ptr [rbp+0x60]
 
G_M000_IG102:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG103:
       xor      rcx, rcx
       mov      gword ptr [rbp-0x390], rcx
 
G_M000_IG104:
       mov      gword ptr [rbp-0x388], rcx
 
G_M000_IG105:
       mov      gword ptr [rbp-0x380], rcx
 
G_M000_IG106:
       mov      gword ptr [rbp-0x378], rcx
       mov      rcx, r15
       mov      edi, 8
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, rbx
       mov      edi, 9
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, r15
       mov      edi, 10
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, rbx
       mov      edi, 11
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x38], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x3C0], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp+0x68]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x3C0]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, gword ptr [rbp+0x38]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, gword ptr [rbp-0x3C0]
       call     [RyuPy.Runtime.PyOps:MakeList(System.Object[]):System.Object]
       mov      r8, qword ptr [rbp+0xC0]
       add      r12, r8
       jo       G_M000_IG287
       mov      qword ptr [rbp+0xC0], r8
       mov      rcx, rax
       mov      rax, qword ptr [rbp+0xC8]
       mov      r10d, dword ptr [rbp-0x3AC]
 
G_M000_IG107:
       mov      edx, r10d
       cmp      r12, rax
       setl     dil
       movzx    rdi, dil
       and      edi, edx
       mov      qword ptr [rbp+0xC8], rax
       cmp      r12, rax
       setg     sil
       movzx    rsi, sil
       test     edx, edx
       sete     dl
       movzx    rdx, dl
       and      edx, esi
       or       edx, edi
       je       G_M000_IG248
 
G_M000_IG108:
       mov      edx, dword ptr [(reloc)]
       test     edx, edx
       jle      SHORT G_M000_IG111
 
G_M000_IG109:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rsi, rdi
       dec      edx
       mov      ecx, edx
       cmp      ecx, dword ptr [rsi+0x08]
       jae      G_M000_IG286
       cmp      dword ptr [rsi+4*rcx+0x10], 10
       je       SHORT G_M000_IG111
 
G_M000_IG110:
       cmp      edx, dword ptr [rdi+0x08]
       jae      G_M000_IG286
       mov      edx, edx
       mov      dword ptr [rdi+4*rdx+0x10], 10
 
G_M000_IG111:
       test     r13, r13
       je       G_M000_IG145
 
G_M000_IG112:
       mov      rcx, qword ptr [r13]
       mov      r11, 0xD1FFAB1E
       cmp      rcx, r11
       jne      G_M000_IG139
 
G_M000_IG113:
       mov      rsi, qword ptr [r13+0x08]
       test     r15, r15
       je       G_M000_IG145
 
G_M000_IG114:
       mov      qword ptr [rbp-0x3B8], r11
       cmp      qword ptr [r15], r11
       jne      G_M000_IG137
 
G_M000_IG115:
       mov      rcx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x48], rcx
       lea      rdi, [rbp-0x60]
       mov      rdx, rsi
       mulx     rdx, r9, rcx
       mov      qword ptr [rdi], r9
       mov      r9, qword ptr [rbp-0x60]
       mov      rdi, rsi
       sar      rdi, 63
       and      rdi, rcx
       sub      rdx, rdi
       mov      rdi, rcx
       sar      rdi, 63
       and      rdi, rsi
       sub      rdx, rdi
       mov      rdi, r9
       sar      rdi, 63
       cmp      rdx, rdi
       je       G_M000_IG133
 
G_M000_IG116:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x58], xmm0
       lea      rdi, [rbp-0x58]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x3F8], rax
       mov      ecx, dword ptr [rbp-0x50]
       mov      dword ptr [rbp-0x394], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       mov      rsi, qword ptr [rbp-0x48]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x70]
       mov      r9d, dword ptr [rbp-0x68]
       mov      rcx, gword ptr [rbp-0x3F8]
       mov      rsi, rcx
       or       rsi, rax
       jne      G_M000_IG124
 
G_M000_IG117:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       movsxd   rsi, dword ptr [rbp-0x394]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x80]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x80]
       mov      r10d, dword ptr [rbp-0x78]
 
G_M000_IG118:
       mov      gword ptr [rbp-0x400], rax
       mov      ecx, r10d
       mov      dword ptr [rbp-0x398], ecx
       test     rax, rax
       jne      G_M000_IG250
 
G_M000_IG119:
       mov      rdi, rax
       test     rdi, rdi
       jne      G_M000_IG259
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jge      SHORT G_M000_IG131
 
G_M000_IG120:
       mov      edi, -1
 
G_M000_IG121:
       test     edi, edi
       jg       SHORT G_M000_IG132
 
G_M000_IG122:
       mov      rdi, qword ptr [rbp-0x3B8]
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x3D0], rax
       mov      rdi, gword ptr [rbp-0x400]
       mov      esi, dword ptr [rbp-0x398]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rsi, gword ptr [rbp-0x3D0]
       mov      qword ptr [rsi+0x08], rax
 
G_M000_IG123:
       jmp      G_M000_IG146
 
G_M000_IG124:
       test     rcx, rcx
       je       SHORT G_M000_IG129
 
G_M000_IG125:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG126:
       test     rax, rax
       je       SHORT G_M000_IG130
 
G_M000_IG127:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG128:
       mov      edx, dword ptr [rbp-0x394]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      r10d, ecx
       jmp      G_M000_IG118
 
G_M000_IG129:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG126
 
G_M000_IG130:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG128
 
G_M000_IG131:
       xor      edi, edi
       jmp      SHORT G_M000_IG121
 
G_M000_IG132:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      gword ptr [rbp-0x3C8], rsi
       lea      rdi, bword ptr [rsi+0x08]
       mov      rsi, gword ptr [rbp-0x400]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rsi, gword ptr [rbp-0x3C8]
       mov      edi, dword ptr [rbp-0x398]
       mov      dword ptr [rsi+0x10], edi
       jmp      SHORT G_M000_IG123
 
G_M000_IG133:
       mov      r11, qword ptr [rbp-0x3B8]
       mov      rcx, r9
       mov      qword ptr [rbp-0x88], rcx
       lea      rdx, [rcx+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG136
 
G_M000_IG134:
       mov      rdi, r11
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x88]
       mov      qword ptr [rax+0x08], rdi
       mov      rsi, rax
 
G_M000_IG135:
       jmp      G_M000_IG123
 
G_M000_IG136:
       test     rdx, rdx
       jl       G_M000_IG287
       mov      rsi, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG286
       mov      rsi, gword ptr [rcx+8*rsi+0x10]
       jmp      SHORT G_M000_IG135
 
G_M000_IG137:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG145
 
G_M000_IG138:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x90], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x90]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG146
 
G_M000_IG139:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG145
 
G_M000_IG140:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     r15, r15
       je       SHORT G_M000_IG145
 
G_M000_IG141:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG143
 
G_M000_IG142:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG146
 
G_M000_IG143:
       cmp      qword ptr [r15], r11
       jne      SHORT G_M000_IG145
 
G_M000_IG144:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG146
 
G_M000_IG145:
       mov      rdx, r15
       mov      rsi, r13
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG146:
       test     rsi, rsi
       je       G_M000_IG181
 
G_M000_IG147:
       mov      r11, 0xD1FFAB1E
       mov      rax, r11
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG165
 
G_M000_IG148:
       mov      rcx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG181
 
G_M000_IG149:
       mov      qword ptr [rbp-0x3B8], rax
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG163
 
G_M000_IG150:
       mov      rdx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0xB8], rdx
       lea      r8, [rcx+rdx]
       mov      qword ptr [rbp-0xC0], r8
       mov      rdi, rcx
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG159
 
G_M000_IG151:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xD0], xmm0
       lea      rdi, [rbp-0xD0]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xD0]
       mov      gword ptr [rbp-0x408], rax
       mov      ecx, dword ptr [rbp-0xC8]
       mov      dword ptr [rbp-0x39C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xE0], xmm0
       lea      rdi, [rbp-0xE0]
       mov      rsi, qword ptr [rbp-0xB8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x408]
       mov      esi, dword ptr [rbp-0x39C]
       mov      rdx, gword ptr [rbp-0xE0]
       mov      rcx, qword ptr [rbp-0xD8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x410], rax
       mov      ecx, edx
       mov      dword ptr [rbp-0x3A0], edx
       mov      rdi, rax
       test     rdi, rdi
       jne      G_M000_IG267
 
G_M000_IG152:
       mov      rdi, rax
       test     rdi, rdi
       jne      G_M000_IG276
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jge      SHORT G_M000_IG157
 
G_M000_IG153:
       mov      edi, -1
 
G_M000_IG154:
       test     edi, edi
       jg       SHORT G_M000_IG158
 
G_M000_IG155:
       mov      rdi, qword ptr [rbp-0x3B8]
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x3E8], rax
       mov      rdi, gword ptr [rbp-0x410]
       mov      esi, dword ptr [rbp-0x3A0]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rsi, gword ptr [rbp-0x3E8]
       mov      qword ptr [rsi+0x08], rax
 
G_M000_IG156:
       jmp      G_M000_IG182
 
G_M000_IG157:
       xor      edi, edi
       jmp      SHORT G_M000_IG154
 
G_M000_IG158:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      gword ptr [rbp-0x3E0], rsi
       lea      rdi, bword ptr [rsi+0x08]
       mov      rsi, gword ptr [rbp-0x410]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rsi, gword ptr [rbp-0x3E0]
       mov      edi, dword ptr [rbp-0x3A0]
       mov      dword ptr [rsi+0x10], edi
       jmp      SHORT G_M000_IG156
 
G_M000_IG159:
       mov      rax, qword ptr [rbp-0x3B8]
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG162
 
G_M000_IG160:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0xC0]
       mov      qword ptr [rax+0x08], rdi
       mov      rsi, rax
 
G_M000_IG161:
       jmp      SHORT G_M000_IG156
 
G_M000_IG162:
       test     rcx, rcx
       jl       G_M000_IG287
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG286
       test     rcx, rcx
       jl       G_M000_IG287
       mov      rax, rcx
       mov      rsi, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG161
 
G_M000_IG163:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG181
 
G_M000_IG164:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG182
 
G_M000_IG165:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG171
 
G_M000_IG166:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       test     rbx, rbx
       je       G_M000_IG181
 
G_M000_IG167:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG169
 
G_M000_IG168:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xA8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG182
 
G_M000_IG169:
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG181
 
G_M000_IG170:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0xF0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG182
 
G_M000_IG171:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG181
 
G_M000_IG172:
       mov      rax, rsi
       mov      gword ptr [rbp-0x3D8], rax
       test     rbx, rbx
       je       G_M000_IG181
 
G_M000_IG173:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG181
 
G_M000_IG174:
       cmp      dword ptr [rax+0x08], 0
       je       G_M000_IG179
 
G_M000_IG175:
       cmp      dword ptr [rbx+0x08], 0
       je       SHORT G_M000_IG178
 
G_M000_IG176:
       mov      ecx, dword ptr [rax+0x08]
       mov      dword ptr [rbp-0xF4], ecx
       mov      esi, ecx
       add      esi, dword ptr [rbx+0x08]
       js       G_M000_IG285
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      rsi, rax
       mov      gword ptr [rbp-0x3F0], rsi
       cmp      byte  ptr [rsi], sil
       lea      rdi, bword ptr [rsi+0x0C]
       mov      rax, gword ptr [rbp-0x3D8]
       lea      rsi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [rax+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       movsxd   rdi, dword ptr [rbp-0xF4]
       mov      rsi, gword ptr [rbp-0x3F0]
       lea      rdi, bword ptr [rsi+2*rdi+0x0C]
       lea      rsi, bword ptr [rbx+0x0C]
       mov      edx, dword ptr [rbx+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rsi, gword ptr [rbp-0x3F0]
 
G_M000_IG177:
       jmp      SHORT G_M000_IG182
 
G_M000_IG178:
       mov      rsi, rax
       jmp      SHORT G_M000_IG177
 
G_M000_IG179:
       cmp      dword ptr [rbx+0x08], 0
       je       G_M000_IG284
 
G_M000_IG180:
       mov      rsi, rbx
       jmp      SHORT G_M000_IG177
 
G_M000_IG181:
       mov      rdx, rbx
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG182:
       test     rsi, rsi
       je       G_M000_IG217
 
G_M000_IG183:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG210
 
G_M000_IG184:
       mov      rdx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG217
 
G_M000_IG185:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG208
 
G_M000_IG186:
       mov      rcx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x118], rcx
       lea      rdi, [rbp-0x130]
       mulx     r8, rsi, rcx
       mov      qword ptr [rdi], rsi
       mov      r9, qword ptr [rbp-0x130]
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, rcx
       sub      r8, rdi
       mov      rdi, rcx
       sar      rdi, 63
       and      rdi, rdx
       sub      r8, rdi
       mov      rdi, r9
       sar      rdi, 63
       cmp      r8, rdi
       je       G_M000_IG199
 
G_M000_IG187:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x128], xmm0
       lea      rdi, [rbp-0x128]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x128]
       mov      gword ptr [rbp-0x418], rax
       mov      ecx, dword ptr [rbp-0x120]
       mov      dword ptr [rbp-0x3A4], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x140], xmm0
       lea      rdi, [rbp-0x140]
       mov      rsi, qword ptr [rbp-0x118]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x140]
       mov      r9d, dword ptr [rbp-0x138]
       mov      rcx, gword ptr [rbp-0x418]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG192
 
G_M000_IG188:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x160], xmm0
       movsxd   rsi, dword ptr [rbp-0x3A4]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x160]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG189:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x160]
       vmovdqu32 xmmword ptr [rbp-0x150], xmm0
 
G_M000_IG190:
       mov      rdi, gword ptr [rbp-0x150]
       mov      rsi, qword ptr [rbp-0x148]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG191:
       mov      rsi, rax
       jmp      G_M000_IG218
 
G_M000_IG192:
       test     rcx, rcx
       je       SHORT G_M000_IG197
 
G_M000_IG193:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG194:
       test     rax, rax
       je       SHORT G_M000_IG198
 
G_M000_IG195:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG196:
       mov      edx, dword ptr [rbp-0x3A4]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x150], rax
       mov      qword ptr [rbp-0x148], rdx
       jmp      SHORT G_M000_IG190
 
G_M000_IG197:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG194
 
G_M000_IG198:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG196
 
G_M000_IG199:
       mov      rcx, r9
       mov      qword ptr [rbp-0x168], rcx
       lea      rdx, [rcx+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG202
 
G_M000_IG200:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x168]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG201:
       jmp      SHORT G_M000_IG191
 
G_M000_IG202:
       test     rdx, rdx
       jl       G_M000_IG287
       mov      rax, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG286
       test     rdx, rdx
       jl       G_M000_IG287
       mov      rax, rdx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG201
 
G_M000_IG203:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG244
 
G_M000_IG204:
       vmovsd   xmm0, qword ptr [rax+0x08]
       test     rbx, rbx
       je       G_M000_IG243
 
G_M000_IG205:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      G_M000_IG239
 
G_M000_IG206:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x380]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x60]
       je       G_M000_IG102
 
G_M000_IG207:
       mov      rdx, gword ptr [rbp-0x380]
       jmp      G_M000_IG103
 
G_M000_IG208:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG217
 
G_M000_IG209:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x110], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x170], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x170]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x110]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG218
 
G_M000_IG210:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG216
 
G_M000_IG211:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x100], xmm0
       test     r15, r15
       je       G_M000_IG217
 
G_M000_IG212:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG214
 
G_M000_IG213:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x108], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x100]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x108]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG218
 
G_M000_IG214:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG217
 
G_M000_IG215:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x178], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x100]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG218
 
G_M000_IG216:
       mov      rdx, qword ptr [rsi]
 
G_M000_IG217:
       mov      rdx, r15
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG218:
       test     rsi, rsi
       je       G_M000_IG07
 
G_M000_IG219:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG233
 
G_M000_IG220:
       mov      rcx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG07
 
G_M000_IG221:
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG231
 
G_M000_IG222:
       mov      rdx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x198], rdx
       lea      r8, [rcx+rdx]
       mov      qword ptr [rbp-0x1A0], r8
       mov      rdi, rcx
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG227
 
G_M000_IG223:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1B0], xmm0
       lea      rdi, [rbp-0x1B0]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG224:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x1B0]
       vmovdqu32 xmmword ptr [rbp-0x1C0], xmm0
 
G_M000_IG225:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1E0], xmm0
       lea      rdi, [rbp-0x1E0]
       mov      rsi, qword ptr [rbp-0x198]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x1C0]
       mov      rsi, qword ptr [rbp-0x1B8]
       mov      rdx, gword ptr [rbp-0x1E0]
       mov      rcx, qword ptr [rbp-0x1D8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x1D0], rax
       mov      qword ptr [rbp-0x1C8], rdx
       mov      rdi, gword ptr [rbp-0x1D0]
       mov      rsi, qword ptr [rbp-0x1C8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG226:
       mov      rsi, rax
       jmp      G_M000_IG08
 
G_M000_IG227:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG230
 
G_M000_IG228:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x1A0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG229:
       jmp      SHORT G_M000_IG226
 
G_M000_IG230:
       test     rcx, rcx
       jl       G_M000_IG287
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG286
       test     rcx, rcx
       jl       G_M000_IG287
       mov      rax, rcx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG229
 
G_M000_IG231:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG07
 
G_M000_IG232:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x190], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x1E8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1E8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x190]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG08
 
G_M000_IG233:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG03
 
G_M000_IG234:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x180], xmm0
       test     rbx, rbx
       je       G_M000_IG07
 
G_M000_IG235:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG237
 
G_M000_IG236:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x188], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x180]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x188]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG08
 
G_M000_IG237:
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG07
 
G_M000_IG238:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x1F0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x180]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x1F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG08
 
G_M000_IG239:
       cmp      qword ptr [rbx], r8
       jne      SHORT G_M000_IG242
 
G_M000_IG240:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x378]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x60]
       je       G_M000_IG102
 
G_M000_IG241:
       mov      rdx, gword ptr [rbp-0x378]
       jmp      G_M000_IG103
 
G_M000_IG242:
       mov      rax, gword ptr [rbp+0x60]
       jmp      G_M000_IG102
 
G_M000_IG243:
       mov      rax, gword ptr [rbp+0x60]
       jmp      G_M000_IG102
 
G_M000_IG244:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG102
 
G_M000_IG245:
       mov      rdi, rax
       test     rbx, rbx
       je       G_M000_IG102
 
G_M000_IG246:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      G_M000_IG102
 
G_M000_IG247:
       mov      rsi, rbx
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rdx, rax
       jmp      G_M000_IG103
 
G_M000_IG248:
       mov      gword ptr [rbp+0x108], rcx
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, qword ptr [rbp+0xF8]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp+0x108]
 
G_M000_IG249:
       add      rsp, 0x558
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG250:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG251
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG252
 
G_M000_IG251:
       mov      edx, ecx
       jmp      SHORT G_M000_IG258
 
G_M000_IG252:
       cmp      edi, 2
       je       SHORT G_M000_IG253
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG286
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG254
 
G_M000_IG253:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
 
G_M000_IG254:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG255
       mov      edx, -1
       jmp      SHORT G_M000_IG257
 
G_M000_IG255:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jbe      SHORT G_M000_IG256
       mov      edx, 1
       jmp      SHORT G_M000_IG257
 
G_M000_IG256:
       xor      edx, edx
 
G_M000_IG257:
       imul     edx, ecx
 
G_M000_IG258:
       test     edx, edx
       jge      G_M000_IG119
       jmp      G_M000_IG132
 
G_M000_IG259:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG260
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG261
 
G_M000_IG260:
       mov      edi, ecx
       mov      ecx, dword ptr [rbp-0x398]
       jmp      G_M000_IG121
 
G_M000_IG261:
       cmp      edi, 2
       je       SHORT G_M000_IG262
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG286
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG263
 
G_M000_IG262:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
       mov      rax, gword ptr [rbp-0x400]
 
G_M000_IG263:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG264
       mov      edx, -1
       jmp      SHORT G_M000_IG266
 
G_M000_IG264:
       test     rdi, rdi
       jge      SHORT G_M000_IG265
       mov      edx, 1
       jmp      SHORT G_M000_IG266
 
G_M000_IG265:
       xor      edx, edx
 
G_M000_IG266:
       mov      edi, ecx
       imul     edi, edx
       jmp      G_M000_IG121
 
G_M000_IG267:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG268
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG269
 
G_M000_IG268:
       mov      edx, ecx
       jmp      SHORT G_M000_IG275
 
G_M000_IG269:
       cmp      edi, 2
       je       SHORT G_M000_IG270
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG286
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG271
 
G_M000_IG270:
       cmp      dword ptr [rax+0x08], 1
       jbe      G_M000_IG286
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG286
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
 
G_M000_IG271:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG272
       mov      edx, -1
       jmp      SHORT G_M000_IG274
 
G_M000_IG272:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jbe      SHORT G_M000_IG273
       mov      edx, 1
       jmp      SHORT G_M000_IG274
 
G_M000_IG273:
       xor      edx, edx
 
G_M000_IG274:
       imul     edx, ecx
 
G_M000_IG275:
       test     edx, edx
       jge      G_M000_IG152
       jmp      G_M000_IG158
 
G_M000_IG276:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG277
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG278
 
G_M000_IG277:
       mov      edi, ecx
       mov      ecx, dword ptr [rbp-0x3A0]
       jmp      G_M000_IG154
 
G_M000_IG278:
       cmp      edi, 2
       je       SHORT G_M000_IG279
       cmp      dword ptr [rax+0x08], 0
       jbe      SHORT G_M000_IG286
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG280
 
G_M000_IG279:
       cmp      dword ptr [rax+0x08], 1
       jbe      SHORT G_M000_IG286
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       cmp      dword ptr [rax+0x08], 0
       jbe      SHORT G_M000_IG286
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
       mov      rax, gword ptr [rbp-0x410]
 
G_M000_IG280:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG281
       mov      edx, -1
       jmp      SHORT G_M000_IG283
 
G_M000_IG281:
       test     rdi, rdi
       jge      SHORT G_M000_IG282
       mov      edx, 1
       jmp      SHORT G_M000_IG283
 
G_M000_IG282:
       xor      edx, edx
 
G_M000_IG283:
       mov      edi, ecx
       imul     edi, edx
       jmp      G_M000_IG154
 
G_M000_IG284:
       mov      rsi, 0xD1FFAB1E
       jmp      G_M000_IG177
 
G_M000_IG285:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG286:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG287:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 6992

; Assembly listing for method RyuPy.Generated.Module:m0_tuple_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 336
       lea      rbp, [rsp+0x150]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x140], zmm8
       vmovdqu32 zmmword ptr [rbp-0x100], zmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
       mov      gword ptr [rbp-0x48], rcx
       mov      gword ptr [rbp-0x50], r8
 
G_M000_IG02:
       mov      dword ptr [rbp-0x148], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x68], rax
       mov      edi, 15
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      rdi, rax
       call     [RyuPy.Runtime.PyOps:MakeTuple(System.Object[]):System.Object]
       mov      gword ptr [rbp-0x70], rax
       mov      rax, gword ptr [rbp-0x70]
       mov      gword ptr [rbp-0x58], rax
       mov      edi, 16
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x78], rax
       mov      rdi, gword ptr [rbp-0x78]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x80], rax
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x88], rax
       mov      rax, qword ptr [rbp-0x80]
       mov      qword ptr [rbp-0x90], rax
       mov      rax, qword ptr [rbp-0x88]
       mov      qword ptr [rbp-0x98], rax
       mov      qword ptr [rbp-0xA0], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x148]
       dec      eax
       mov      dword ptr [rbp-0x148], eax
       cmp      dword ptr [rbp-0x148], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x148]
       mov      esi, 91
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
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 17
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdx, gword ptr [rbp-0x38]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 12
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xD0], rax
       mov      rdx, gword ptr [rbp-0xD0]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 13
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xD8], rax
       mov      rdx, gword ptr [rbp-0xD8]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 14
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE0], rax
       mov      rdx, gword ptr [rbp-0xE0]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 15
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE8], rax
       mov      rdx, gword ptr [rbp-0xE8]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 16
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xF0], rax
       mov      rdx, gword ptr [rbp-0xF0]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 17
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xF8], rax
       mov      rdx, gword ptr [rbp-0x50]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 18
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x100], rax
       mov      rdx, gword ptr [rbp-0x100]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 19
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x108], rax
       mov      rdx, gword ptr [rbp-0x108]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 20
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x110], rax
       mov      rdx, gword ptr [rbp-0x110]
       mov      rcx, gword ptr [rbp-0x48]
 
G_M000_IG07:
       mov      edi, 21
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x118], rax
       mov      rdx, gword ptr [rbp-0x118]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 22
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x120], rax
       mov      rdx, gword ptr [rbp-0x120]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 23
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x128], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x140], rax
       mov      rdi, gword ptr [rbp-0x140]
       mov      rdx, gword ptr [rbp-0xF8]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x140]
       mov      rdx, gword ptr [rbp-0x128]
       mov      esi, 1
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x140]
       call     [RyuPy.Runtime.PyOps:MakeTuple(System.Object[]):System.Object]
       mov      gword ptr [rbp-0x130], rax
       mov      rax, gword ptr [rbp-0x130]
       mov      gword ptr [rbp-0x58], rax
       mov      rax, qword ptr [rbp-0x90]
       add      rax, qword ptr [rbp-0xA0]
       jo       SHORT G_M000_IG10
       mov      qword ptr [rbp-0x90], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 18
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x138], rax
       mov      rdi, qword ptr [rbp-0x68]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x138]
 
G_M000_IG09:
       add      rsp, 336
       pop      rbp
       ret      
 
G_M000_IG10:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1083

; Assembly listing for method RyuPy.Generated.Module:m0_tuple_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x5b
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 63 inlinees with PGO data; 78 single block inlinees; 110 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 0x420
       mov      qword ptr [rsp+0x578], r15
       mov      qword ptr [rsp+0x570], r14
       mov      qword ptr [rsp+0x568], r13
       mov      qword ptr [rsp+0x560], r12
       mov      qword ptr [rsp+0x558], rbx
       lea      rbp, [rsp+0x420]
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x390], xmm8
       mov      rax, -816
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      qword ptr [rbp-0x50], rax
       mov      r13, gword ptr [rbp+0x128]
       mov      r15, gword ptr [rbp+0x120]
       mov      rbx, gword ptr [rbp+0x118]
       mov      r14, gword ptr [rbp+0x110]
       mov      rcx, gword ptr [rbp+0x108]
       mov      r12, qword ptr [rbp+0xD0]
       mov      rax, qword ptr [rbp+0xC8]
       mov      r8, qword ptr [rbp+0xC0]
 
G_M000_IG02:
       mov      qword ptr [rbp+0xC0], r8
       test     r8, r8
       setg     r10b
       movzx    r10, r10b
       mov      dword ptr [rbp-0x3AC], r10d
       jmp      G_M000_IG107
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG07
 
G_M000_IG04:
       mov      rdi, rsi
       test     rbx, rbx
       je       SHORT G_M000_IG07
 
G_M000_IG05:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG07
 
G_M000_IG06:
       mov      rsi, rbx
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       jmp      SHORT G_M000_IG08
 
G_M000_IG07:
       mov      rdx, rbx
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG08:
       test     rsi, rsi
       je       G_M000_IG38
 
G_M000_IG09:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG31
 
G_M000_IG10:
       mov      rdx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG38
 
G_M000_IG11:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG29
 
G_M000_IG12:
       mov      rcx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x210], rcx
       lea      rdi, [rbp-0x228]
       mulx     r8, rsi, rcx
       mov      qword ptr [rdi], rsi
       mov      r9, qword ptr [rbp-0x228]
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, rcx
       sub      r8, rdi
       mov      rdi, rcx
       sar      rdi, 63
       and      rdi, rdx
       sub      r8, rdi
       mov      rdi, r9
       sar      rdi, 63
       cmp      r8, rdi
       je       G_M000_IG25
 
G_M000_IG13:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x220], xmm0
       lea      rdi, [rbp-0x220]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x220]
       mov      gword ptr [rbp-0x420], rax
       mov      ecx, dword ptr [rbp-0x218]
       mov      dword ptr [rbp-0x3A8], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x238], xmm0
       lea      rdi, [rbp-0x238]
       mov      rsi, qword ptr [rbp-0x210]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x238]
       mov      r9d, dword ptr [rbp-0x230]
       mov      rcx, gword ptr [rbp-0x420]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG18
 
G_M000_IG14:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x258], xmm0
       movsxd   rsi, dword ptr [rbp-0x3A8]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x258]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG15:
       vmovdqu  xmm0, xmmword ptr [rbp-0x258]
       vmovdqu  xmmword ptr [rbp-0x248], xmm0
 
G_M000_IG16:
       mov      rdi, gword ptr [rbp-0x248]
       mov      rsi, qword ptr [rbp-0x240]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG17:
       mov      rsi, rax
       jmp      G_M000_IG39
 
G_M000_IG18:
       test     rcx, rcx
       je       SHORT G_M000_IG23
 
G_M000_IG19:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG20:
       test     rax, rax
       je       SHORT G_M000_IG24
 
G_M000_IG21:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG22:
       mov      edx, dword ptr [rbp-0x3A8]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x248], rax
       mov      qword ptr [rbp-0x240], rdx
       jmp      SHORT G_M000_IG16
 
G_M000_IG23:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG20
 
G_M000_IG24:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG22
 
G_M000_IG25:
       mov      rcx, r9
       mov      qword ptr [rbp-0x260], rcx
       lea      rdx, [rcx+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG28
 
G_M000_IG26:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x260]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG27:
       jmp      SHORT G_M000_IG17
 
G_M000_IG28:
       test     rdx, rdx
       jl       G_M000_IG287
       mov      rax, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG286
       test     rdx, rdx
       jl       G_M000_IG287
       mov      rax, rdx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG27
 
G_M000_IG29:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG38
 
G_M000_IG30:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x208], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x268], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x268]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x208]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG39
 
G_M000_IG31:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG37
 
G_M000_IG32:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x1F8], xmm0
       test     r15, r15
       je       G_M000_IG38
 
G_M000_IG33:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG35
 
G_M000_IG34:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x200], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1F8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x200]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG39
 
G_M000_IG35:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG38
 
G_M000_IG36:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x270], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1F8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x270]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG39
 
G_M000_IG37:
       mov      rdx, qword ptr [rsi]
 
G_M000_IG38:
       mov      rdx, r15
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG39:
       test     rsi, rsi
       je       G_M000_IG64
 
G_M000_IG40:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG54
 
G_M000_IG41:
       mov      rcx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG64
 
G_M000_IG42:
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG52
 
G_M000_IG43:
       mov      rdx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x290], rdx
       lea      r8, [rcx+rdx]
       mov      qword ptr [rbp-0x298], r8
       mov      rdi, rcx
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG48
 
G_M000_IG44:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x2A8], xmm0
       lea      rdi, [rbp-0x2A8]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG45:
       vmovdqu  xmm0, xmmword ptr [rbp-0x2A8]
       vmovdqu  xmmword ptr [rbp-0x2B8], xmm0
 
G_M000_IG46:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x2D8], xmm0
       lea      rdi, [rbp-0x2D8]
       mov      rsi, qword ptr [rbp-0x290]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x2B8]
       mov      rsi, qword ptr [rbp-0x2B0]
       mov      rdx, gword ptr [rbp-0x2D8]
       mov      rcx, qword ptr [rbp-0x2D0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x2C8], rax
       mov      qword ptr [rbp-0x2C0], rdx
       mov      rdi, gword ptr [rbp-0x2C8]
       mov      rsi, qword ptr [rbp-0x2C0]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG47:
       jmp      G_M000_IG65
 
G_M000_IG48:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG51
 
G_M000_IG49:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x298]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG50:
       jmp      SHORT G_M000_IG47
 
G_M000_IG51:
       test     rcx, rcx
       jl       G_M000_IG287
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG286
       test     rcx, rcx
       jl       G_M000_IG287
       mov      rax, rcx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG50
 
G_M000_IG52:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG64
 
G_M000_IG53:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x288], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x2E0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2E0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x288]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG65
 
G_M000_IG54:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG60
 
G_M000_IG55:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x278], xmm0
       test     rbx, rbx
       je       G_M000_IG64
 
G_M000_IG56:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG58
 
G_M000_IG57:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x280], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x278]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x280]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG65
 
G_M000_IG58:
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG64
 
G_M000_IG59:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x2E8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x278]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2E8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG65
 
G_M000_IG60:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG64
 
G_M000_IG61:
       mov      rdi, rsi
       test     rbx, rbx
       je       SHORT G_M000_IG64
 
G_M000_IG62:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG64
 
G_M000_IG63:
       mov      rsi, rbx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG65
 
G_M000_IG64:
       mov      rdx, rbx
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG65:
       mov      gword ptr [rbp+0x68], rax
       test     r14, r14
       je       G_M000_IG87
 
G_M000_IG66:
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      qword ptr [r14], rcx
       jne      G_M000_IG80
 
G_M000_IG67:
       mov      r8, qword ptr [r14+0x08]
       mov      qword ptr [rbp-0x300], r8
       test     r15, r15
       je       G_M000_IG87
 
G_M000_IG68:
       mov      qword ptr [rbp-0x3B8], rcx
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG78
 
G_M000_IG69:
       mov      r9, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x310], r9
       xor      edx, edx
       mov      qword ptr [rbp-0x318], rdx
       lea      rdx, [rbp-0x318]
       mov      rdi, r8
       mov      rsi, r9
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x318]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG74
 
G_M000_IG70:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x328], xmm0
       lea      rdi, [rbp-0x328]
       mov      rsi, qword ptr [rbp-0x300]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG71:
       vmovdqu  xmm0, xmmword ptr [rbp-0x328]
       vmovdqu  xmmword ptr [rbp-0x338], xmm0
 
G_M000_IG72:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x358], xmm0
       lea      rdi, [rbp-0x358]
       mov      rsi, qword ptr [rbp-0x310]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x338]
       mov      rsi, qword ptr [rbp-0x330]
       mov      rdx, gword ptr [rbp-0x358]
       mov      rcx, qword ptr [rbp-0x350]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x348], rax
       mov      qword ptr [rbp-0x340], rdx
       mov      rdi, gword ptr [rbp-0x348]
       mov      rsi, qword ptr [rbp-0x340]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG73:
       jmp      G_M000_IG88
 
G_M000_IG74:
       mov      rax, qword ptr [rbp-0x318]
       mov      qword ptr [rbp-0x360], rax
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG77
 
G_M000_IG75:
       mov      rdi, qword ptr [rbp-0x3B8]
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x360]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG76:
       jmp      SHORT G_M000_IG73
 
G_M000_IG77:
       test     rcx, rcx
       jl       G_M000_IG287
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG286
       test     rcx, rcx
       jl       G_M000_IG287
       mov      rax, rcx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG76
 
G_M000_IG78:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG87
 
G_M000_IG79:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x308], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, r8
       vmovsd   qword ptr [rbp-0x368], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x368]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x308]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG88
 
G_M000_IG80:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG86
 
G_M000_IG81:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x2F0], xmm0
       test     r15, r15
       je       G_M000_IG87
 
G_M000_IG82:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG84
 
G_M000_IG83:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x2F8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x2F0]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x2F8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG88
 
G_M000_IG84:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG87
 
G_M000_IG85:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x370], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x2F0]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x370]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG88
 
G_M000_IG86:
       mov      rdx, qword ptr [r14]
 
G_M000_IG87:
       mov      rdx, r15
       mov      rsi, r14
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG88:
       mov      gword ptr [rbp+0x60], rax
       xor      rdx, rdx
       mov      gword ptr [rbp-0x390], rdx
 
G_M000_IG89:
       mov      gword ptr [rbp-0x388], rdx
 
G_M000_IG90:
       mov      gword ptr [rbp-0x380], rdx
 
G_M000_IG91:
       mov      gword ptr [rbp-0x378], rdx
       test     rax, rax
       je       G_M000_IG102
 
G_M000_IG92:
       mov      rcx, 0xD1FFAB1E
       mov      r8, rcx
       cmp      qword ptr [rax], r8
       jne      G_M000_IG203
 
G_M000_IG93:
       mov      rsi, qword ptr [rax+0x08]
       test     rbx, rbx
       je       SHORT G_M000_IG101
 
G_M000_IG94:
       cmp      qword ptr [rbx], r8
       jne      SHORT G_M000_IG97
 
G_M000_IG95:
       mov      rdx, qword ptr [rbx+0x08]
       lea      rcx, [rbp-0x390]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:TryLongBinary(int,long,long,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x60]
       je       SHORT G_M000_IG102
 
G_M000_IG96:
       mov      rdx, gword ptr [rbp-0x390]
       jmp      SHORT G_M000_IG103
 
G_M000_IG97:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG100
 
G_M000_IG98:
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, rsi
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x388]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x60]
       je       SHORT G_M000_IG102
 
G_M000_IG99:
       mov      rdx, gword ptr [rbp-0x388]
       jmp      SHORT G_M000_IG103
 
G_M000_IG100:
       mov      rax, gword ptr [rbp+0x60]
       jmp      SHORT G_M000_IG102
 
G_M000_IG101:
       mov      rax, gword ptr [rbp+0x60]
 
G_M000_IG102:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG103:
       xor      rcx, rcx
       mov      gword ptr [rbp-0x390], rcx
 
G_M000_IG104:
       mov      gword ptr [rbp-0x388], rcx
 
G_M000_IG105:
       mov      gword ptr [rbp-0x380], rcx
 
G_M000_IG106:
       mov      gword ptr [rbp-0x378], rcx
       mov      rcx, r15
       mov      edi, 20
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, rbx
       mov      edi, 21
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, r15
       mov      edi, 22
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, rbx
       mov      edi, 23
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x38], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x3C0], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp+0x68]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x3C0]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, gword ptr [rbp+0x38]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, gword ptr [rbp-0x3C0]
       call     [RyuPy.Runtime.PyOps:MakeTuple(System.Object[]):System.Object]
       mov      r8, qword ptr [rbp+0xC0]
       add      r12, r8
       jo       G_M000_IG287
       mov      qword ptr [rbp+0xC0], r8
       mov      rcx, rax
       mov      rax, qword ptr [rbp+0xC8]
       mov      r10d, dword ptr [rbp-0x3AC]
 
G_M000_IG107:
       mov      edx, r10d
       cmp      r12, rax
       setl     dil
       movzx    rdi, dil
       and      edi, edx
       mov      qword ptr [rbp+0xC8], rax
       cmp      r12, rax
       setg     sil
       movzx    rsi, sil
       test     edx, edx
       sete     dl
       movzx    rdx, dl
       and      edx, esi
       or       edx, edi
       je       G_M000_IG248
 
G_M000_IG108:
       mov      edx, dword ptr [(reloc)]
       test     edx, edx
       jle      SHORT G_M000_IG111
 
G_M000_IG109:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rsi, rdi
       dec      edx
       mov      ecx, edx
       cmp      ecx, dword ptr [rsi+0x08]
       jae      G_M000_IG286
       cmp      dword ptr [rsi+4*rcx+0x10], 17
       je       SHORT G_M000_IG111
 
G_M000_IG110:
       cmp      edx, dword ptr [rdi+0x08]
       jae      G_M000_IG286
       mov      edx, edx
       mov      dword ptr [rdi+4*rdx+0x10], 17
 
G_M000_IG111:
       test     r13, r13
       je       G_M000_IG145
 
G_M000_IG112:
       mov      rcx, qword ptr [r13]
       mov      r11, 0xD1FFAB1E
       cmp      rcx, r11
       jne      G_M000_IG139
 
G_M000_IG113:
       mov      rsi, qword ptr [r13+0x08]
       test     r15, r15
       je       G_M000_IG145
 
G_M000_IG114:
       mov      qword ptr [rbp-0x3B8], r11
       cmp      qword ptr [r15], r11
       jne      G_M000_IG137
 
G_M000_IG115:
       mov      rcx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x48], rcx
       lea      rdi, [rbp-0x60]
       mov      rdx, rsi
       mulx     rdx, r9, rcx
       mov      qword ptr [rdi], r9
       mov      r9, qword ptr [rbp-0x60]
       mov      rdi, rsi
       sar      rdi, 63
       and      rdi, rcx
       sub      rdx, rdi
       mov      rdi, rcx
       sar      rdi, 63
       and      rdi, rsi
       sub      rdx, rdi
       mov      rdi, r9
       sar      rdi, 63
       cmp      rdx, rdi
       je       G_M000_IG133
 
G_M000_IG116:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x58], xmm0
       lea      rdi, [rbp-0x58]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x3F8], rax
       mov      ecx, dword ptr [rbp-0x50]
       mov      dword ptr [rbp-0x394], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       mov      rsi, qword ptr [rbp-0x48]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x70]
       mov      r9d, dword ptr [rbp-0x68]
       mov      rcx, gword ptr [rbp-0x3F8]
       mov      rsi, rcx
       or       rsi, rax
       jne      G_M000_IG124
 
G_M000_IG117:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       movsxd   rsi, dword ptr [rbp-0x394]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x80]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x80]
       mov      r10d, dword ptr [rbp-0x78]
 
G_M000_IG118:
       mov      gword ptr [rbp-0x400], rax
       mov      ecx, r10d
       mov      dword ptr [rbp-0x398], ecx
       test     rax, rax
       jne      G_M000_IG250
 
G_M000_IG119:
       mov      rdi, rax
       test     rdi, rdi
       jne      G_M000_IG259
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jge      SHORT G_M000_IG131
 
G_M000_IG120:
       mov      edi, -1
 
G_M000_IG121:
       test     edi, edi
       jg       SHORT G_M000_IG132
 
G_M000_IG122:
       mov      rdi, qword ptr [rbp-0x3B8]
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x3D0], rax
       mov      rdi, gword ptr [rbp-0x400]
       mov      esi, dword ptr [rbp-0x398]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rsi, gword ptr [rbp-0x3D0]
       mov      qword ptr [rsi+0x08], rax
 
G_M000_IG123:
       jmp      G_M000_IG146
 
G_M000_IG124:
       test     rcx, rcx
       je       SHORT G_M000_IG129
 
G_M000_IG125:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG126:
       test     rax, rax
       je       SHORT G_M000_IG130
 
G_M000_IG127:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG128:
       mov      edx, dword ptr [rbp-0x394]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      r10d, ecx
       jmp      G_M000_IG118
 
G_M000_IG129:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG126
 
G_M000_IG130:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG128
 
G_M000_IG131:
       xor      edi, edi
       jmp      SHORT G_M000_IG121
 
G_M000_IG132:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      gword ptr [rbp-0x3C8], rsi
       lea      rdi, bword ptr [rsi+0x08]
       mov      rsi, gword ptr [rbp-0x400]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rsi, gword ptr [rbp-0x3C8]
       mov      edi, dword ptr [rbp-0x398]
       mov      dword ptr [rsi+0x10], edi
       jmp      SHORT G_M000_IG123
 
G_M000_IG133:
       mov      r11, qword ptr [rbp-0x3B8]
       mov      rcx, r9
       mov      qword ptr [rbp-0x88], rcx
       lea      rdx, [rcx+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG136
 
G_M000_IG134:
       mov      rdi, r11
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x88]
       mov      qword ptr [rax+0x08], rdi
       mov      rsi, rax
 
G_M000_IG135:
       jmp      G_M000_IG123
 
G_M000_IG136:
       test     rdx, rdx
       jl       G_M000_IG287
       mov      rsi, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG286
       mov      rsi, gword ptr [rcx+8*rsi+0x10]
       jmp      SHORT G_M000_IG135
 
G_M000_IG137:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG145
 
G_M000_IG138:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x90], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x90]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG146
 
G_M000_IG139:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG145
 
G_M000_IG140:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     r15, r15
       je       SHORT G_M000_IG145
 
G_M000_IG141:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG143
 
G_M000_IG142:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG146
 
G_M000_IG143:
       cmp      qword ptr [r15], r11
       jne      SHORT G_M000_IG145
 
G_M000_IG144:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG146
 
G_M000_IG145:
       mov      rdx, r15
       mov      rsi, r13
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG146:
       test     rsi, rsi
       je       G_M000_IG181
 
G_M000_IG147:
       mov      r11, 0xD1FFAB1E
       mov      rax, r11
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG165
 
G_M000_IG148:
       mov      rcx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG181
 
G_M000_IG149:
       mov      qword ptr [rbp-0x3B8], rax
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG163
 
G_M000_IG150:
       mov      rdx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0xB8], rdx
       lea      r8, [rcx+rdx]
       mov      qword ptr [rbp-0xC0], r8
       mov      rdi, rcx
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG159
 
G_M000_IG151:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xD0], xmm0
       lea      rdi, [rbp-0xD0]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xD0]
       mov      gword ptr [rbp-0x408], rax
       mov      ecx, dword ptr [rbp-0xC8]
       mov      dword ptr [rbp-0x39C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xE0], xmm0
       lea      rdi, [rbp-0xE0]
       mov      rsi, qword ptr [rbp-0xB8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x408]
       mov      esi, dword ptr [rbp-0x39C]
       mov      rdx, gword ptr [rbp-0xE0]
       mov      rcx, qword ptr [rbp-0xD8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x410], rax
       mov      ecx, edx
       mov      dword ptr [rbp-0x3A0], edx
       mov      rdi, rax
       test     rdi, rdi
       jne      G_M000_IG267
 
G_M000_IG152:
       mov      rdi, rax
       test     rdi, rdi
       jne      G_M000_IG276
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jge      SHORT G_M000_IG157
 
G_M000_IG153:
       mov      edi, -1
 
G_M000_IG154:
       test     edi, edi
       jg       SHORT G_M000_IG158
 
G_M000_IG155:
       mov      rdi, qword ptr [rbp-0x3B8]
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x3E8], rax
       mov      rdi, gword ptr [rbp-0x410]
       mov      esi, dword ptr [rbp-0x3A0]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rsi, gword ptr [rbp-0x3E8]
       mov      qword ptr [rsi+0x08], rax
 
G_M000_IG156:
       jmp      G_M000_IG182
 
G_M000_IG157:
       xor      edi, edi
       jmp      SHORT G_M000_IG154
 
G_M000_IG158:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      gword ptr [rbp-0x3E0], rsi
       lea      rdi, bword ptr [rsi+0x08]
       mov      rsi, gword ptr [rbp-0x410]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rsi, gword ptr [rbp-0x3E0]
       mov      edi, dword ptr [rbp-0x3A0]
       mov      dword ptr [rsi+0x10], edi
       jmp      SHORT G_M000_IG156
 
G_M000_IG159:
       mov      rax, qword ptr [rbp-0x3B8]
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG162
 
G_M000_IG160:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0xC0]
       mov      qword ptr [rax+0x08], rdi
       mov      rsi, rax
 
G_M000_IG161:
       jmp      SHORT G_M000_IG156
 
G_M000_IG162:
       test     rcx, rcx
       jl       G_M000_IG287
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG286
       test     rcx, rcx
       jl       G_M000_IG287
       mov      rax, rcx
       mov      rsi, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG161
 
G_M000_IG163:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG181
 
G_M000_IG164:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG182
 
G_M000_IG165:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG171
 
G_M000_IG166:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       test     rbx, rbx
       je       G_M000_IG181
 
G_M000_IG167:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG169
 
G_M000_IG168:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xA8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG182
 
G_M000_IG169:
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG181
 
G_M000_IG170:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0xF0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG182
 
G_M000_IG171:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG181
 
G_M000_IG172:
       mov      rax, rsi
       mov      gword ptr [rbp-0x3D8], rax
       test     rbx, rbx
       je       G_M000_IG181
 
G_M000_IG173:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG181
 
G_M000_IG174:
       cmp      dword ptr [rax+0x08], 0
       je       G_M000_IG179
 
G_M000_IG175:
       cmp      dword ptr [rbx+0x08], 0
       je       SHORT G_M000_IG178
 
G_M000_IG176:
       mov      ecx, dword ptr [rax+0x08]
       mov      dword ptr [rbp-0xF4], ecx
       mov      esi, ecx
       add      esi, dword ptr [rbx+0x08]
       js       G_M000_IG285
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      rsi, rax
       mov      gword ptr [rbp-0x3F0], rsi
       cmp      byte  ptr [rsi], sil
       lea      rdi, bword ptr [rsi+0x0C]
       mov      rax, gword ptr [rbp-0x3D8]
       lea      rsi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [rax+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       movsxd   rdi, dword ptr [rbp-0xF4]
       mov      rsi, gword ptr [rbp-0x3F0]
       lea      rdi, bword ptr [rsi+2*rdi+0x0C]
       lea      rsi, bword ptr [rbx+0x0C]
       mov      edx, dword ptr [rbx+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rsi, gword ptr [rbp-0x3F0]
 
G_M000_IG177:
       jmp      SHORT G_M000_IG182
 
G_M000_IG178:
       mov      rsi, rax
       jmp      SHORT G_M000_IG177
 
G_M000_IG179:
       cmp      dword ptr [rbx+0x08], 0
       je       G_M000_IG284
 
G_M000_IG180:
       mov      rsi, rbx
       jmp      SHORT G_M000_IG177
 
G_M000_IG181:
       mov      rdx, rbx
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG182:
       test     rsi, rsi
       je       G_M000_IG217
 
G_M000_IG183:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG210
 
G_M000_IG184:
       mov      rdx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG217
 
G_M000_IG185:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG208
 
G_M000_IG186:
       mov      rcx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x118], rcx
       lea      rdi, [rbp-0x130]
       mulx     r8, rsi, rcx
       mov      qword ptr [rdi], rsi
       mov      r9, qword ptr [rbp-0x130]
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, rcx
       sub      r8, rdi
       mov      rdi, rcx
       sar      rdi, 63
       and      rdi, rdx
       sub      r8, rdi
       mov      rdi, r9
       sar      rdi, 63
       cmp      r8, rdi
       je       G_M000_IG199
 
G_M000_IG187:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x128], xmm0
       lea      rdi, [rbp-0x128]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x128]
       mov      gword ptr [rbp-0x418], rax
       mov      ecx, dword ptr [rbp-0x120]
       mov      dword ptr [rbp-0x3A4], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x140], xmm0
       lea      rdi, [rbp-0x140]
       mov      rsi, qword ptr [rbp-0x118]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x140]
       mov      r9d, dword ptr [rbp-0x138]
       mov      rcx, gword ptr [rbp-0x418]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG192
 
G_M000_IG188:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x160], xmm0
       movsxd   rsi, dword ptr [rbp-0x3A4]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x160]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG189:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x160]
       vmovdqu32 xmmword ptr [rbp-0x150], xmm0
 
G_M000_IG190:
       mov      rdi, gword ptr [rbp-0x150]
       mov      rsi, qword ptr [rbp-0x148]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG191:
       mov      rsi, rax
       jmp      G_M000_IG218
 
G_M000_IG192:
       test     rcx, rcx
       je       SHORT G_M000_IG197
 
G_M000_IG193:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG194:
       test     rax, rax
       je       SHORT G_M000_IG198
 
G_M000_IG195:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG196:
       mov      edx, dword ptr [rbp-0x3A4]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x150], rax
       mov      qword ptr [rbp-0x148], rdx
       jmp      SHORT G_M000_IG190
 
G_M000_IG197:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG194
 
G_M000_IG198:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG196
 
G_M000_IG199:
       mov      rcx, r9
       mov      qword ptr [rbp-0x168], rcx
       lea      rdx, [rcx+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG202
 
G_M000_IG200:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x168]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG201:
       jmp      SHORT G_M000_IG191
 
G_M000_IG202:
       test     rdx, rdx
       jl       G_M000_IG287
       mov      rax, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG286
       test     rdx, rdx
       jl       G_M000_IG287
       mov      rax, rdx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG201
 
G_M000_IG203:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG244
 
G_M000_IG204:
       vmovsd   xmm0, qword ptr [rax+0x08]
       test     rbx, rbx
       je       G_M000_IG243
 
G_M000_IG205:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      G_M000_IG239
 
G_M000_IG206:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x380]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x60]
       je       G_M000_IG102
 
G_M000_IG207:
       mov      rdx, gword ptr [rbp-0x380]
       jmp      G_M000_IG103
 
G_M000_IG208:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG217
 
G_M000_IG209:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x110], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x170], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x170]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x110]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG218
 
G_M000_IG210:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG216
 
G_M000_IG211:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x100], xmm0
       test     r15, r15
       je       G_M000_IG217
 
G_M000_IG212:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG214
 
G_M000_IG213:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x108], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x100]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x108]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG218
 
G_M000_IG214:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG217
 
G_M000_IG215:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x178], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x100]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG218
 
G_M000_IG216:
       mov      rdx, qword ptr [rsi]
 
G_M000_IG217:
       mov      rdx, r15
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG218:
       test     rsi, rsi
       je       G_M000_IG07
 
G_M000_IG219:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG233
 
G_M000_IG220:
       mov      rcx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG07
 
G_M000_IG221:
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG231
 
G_M000_IG222:
       mov      rdx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x198], rdx
       lea      r8, [rcx+rdx]
       mov      qword ptr [rbp-0x1A0], r8
       mov      rdi, rcx
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG227
 
G_M000_IG223:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1B0], xmm0
       lea      rdi, [rbp-0x1B0]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG224:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x1B0]
       vmovdqu32 xmmword ptr [rbp-0x1C0], xmm0
 
G_M000_IG225:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1E0], xmm0
       lea      rdi, [rbp-0x1E0]
       mov      rsi, qword ptr [rbp-0x198]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x1C0]
       mov      rsi, qword ptr [rbp-0x1B8]
       mov      rdx, gword ptr [rbp-0x1E0]
       mov      rcx, qword ptr [rbp-0x1D8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x1D0], rax
       mov      qword ptr [rbp-0x1C8], rdx
       mov      rdi, gword ptr [rbp-0x1D0]
       mov      rsi, qword ptr [rbp-0x1C8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG226:
       mov      rsi, rax
       jmp      G_M000_IG08
 
G_M000_IG227:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG230
 
G_M000_IG228:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x1A0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG229:
       jmp      SHORT G_M000_IG226
 
G_M000_IG230:
       test     rcx, rcx
       jl       G_M000_IG287
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG286
       test     rcx, rcx
       jl       G_M000_IG287
       mov      rax, rcx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG229
 
G_M000_IG231:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG07
 
G_M000_IG232:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x190], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x1E8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1E8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x190]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG08
 
G_M000_IG233:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG03
 
G_M000_IG234:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x180], xmm0
       test     rbx, rbx
       je       G_M000_IG07
 
G_M000_IG235:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG237
 
G_M000_IG236:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x188], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x180]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x188]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG08
 
G_M000_IG237:
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG07
 
G_M000_IG238:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x1F0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x180]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x1F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG08
 
G_M000_IG239:
       cmp      qword ptr [rbx], r8
       jne      SHORT G_M000_IG242
 
G_M000_IG240:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x378]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp+0x60]
       je       G_M000_IG102
 
G_M000_IG241:
       mov      rdx, gword ptr [rbp-0x378]
       jmp      G_M000_IG103
 
G_M000_IG242:
       mov      rax, gword ptr [rbp+0x60]
       jmp      G_M000_IG102
 
G_M000_IG243:
       mov      rax, gword ptr [rbp+0x60]
       jmp      G_M000_IG102
 
G_M000_IG244:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG102
 
G_M000_IG245:
       mov      rdi, rax
       test     rbx, rbx
       je       G_M000_IG102
 
G_M000_IG246:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      G_M000_IG102
 
G_M000_IG247:
       mov      rsi, rbx
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rdx, rax
       jmp      G_M000_IG103
 
G_M000_IG248:
       mov      gword ptr [rbp+0x108], rcx
       mov      edi, 18
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, qword ptr [rbp+0xF8]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp+0x108]
 
G_M000_IG249:
       add      rsp, 0x558
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG250:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG251
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG252
 
G_M000_IG251:
       mov      edx, ecx
       jmp      SHORT G_M000_IG258
 
G_M000_IG252:
       cmp      edi, 2
       je       SHORT G_M000_IG253
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG286
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG254
 
G_M000_IG253:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
 
G_M000_IG254:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG255
       mov      edx, -1
       jmp      SHORT G_M000_IG257
 
G_M000_IG255:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jbe      SHORT G_M000_IG256
       mov      edx, 1
       jmp      SHORT G_M000_IG257
 
G_M000_IG256:
       xor      edx, edx
 
G_M000_IG257:
       imul     edx, ecx
 
G_M000_IG258:
       test     edx, edx
       jge      G_M000_IG119
       jmp      G_M000_IG132
 
G_M000_IG259:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG260
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG261
 
G_M000_IG260:
       mov      edi, ecx
       mov      ecx, dword ptr [rbp-0x398]
       jmp      G_M000_IG121
 
G_M000_IG261:
       cmp      edi, 2
       je       SHORT G_M000_IG262
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG286
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG263
 
G_M000_IG262:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
       mov      rax, gword ptr [rbp-0x400]
 
G_M000_IG263:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG264
       mov      edx, -1
       jmp      SHORT G_M000_IG266
 
G_M000_IG264:
       test     rdi, rdi
       jge      SHORT G_M000_IG265
       mov      edx, 1
       jmp      SHORT G_M000_IG266
 
G_M000_IG265:
       xor      edx, edx
 
G_M000_IG266:
       mov      edi, ecx
       imul     edi, edx
       jmp      G_M000_IG121
 
G_M000_IG267:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG268
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG269
 
G_M000_IG268:
       mov      edx, ecx
       jmp      SHORT G_M000_IG275
 
G_M000_IG269:
       cmp      edi, 2
       je       SHORT G_M000_IG270
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG286
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG271
 
G_M000_IG270:
       cmp      dword ptr [rax+0x08], 1
       jbe      G_M000_IG286
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG286
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
 
G_M000_IG271:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG272
       mov      edx, -1
       jmp      SHORT G_M000_IG274
 
G_M000_IG272:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jbe      SHORT G_M000_IG273
       mov      edx, 1
       jmp      SHORT G_M000_IG274
 
G_M000_IG273:
       xor      edx, edx
 
G_M000_IG274:
       imul     edx, ecx
 
G_M000_IG275:
       test     edx, edx
       jge      G_M000_IG152
       jmp      G_M000_IG158
 
G_M000_IG276:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG277
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG278
 
G_M000_IG277:
       mov      edi, ecx
       mov      ecx, dword ptr [rbp-0x3A0]
       jmp      G_M000_IG154
 
G_M000_IG278:
       cmp      edi, 2
       je       SHORT G_M000_IG279
       cmp      dword ptr [rax+0x08], 0
       jbe      SHORT G_M000_IG286
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG280
 
G_M000_IG279:
       cmp      dword ptr [rax+0x08], 1
       jbe      SHORT G_M000_IG286
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       cmp      dword ptr [rax+0x08], 0
       jbe      SHORT G_M000_IG286
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
       mov      rax, gword ptr [rbp-0x410]
 
G_M000_IG280:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG281
       mov      edx, -1
       jmp      SHORT G_M000_IG283
 
G_M000_IG281:
       test     rdi, rdi
       jge      SHORT G_M000_IG282
       mov      edx, 1
       jmp      SHORT G_M000_IG283
 
G_M000_IG282:
       xor      edx, edx
 
G_M000_IG283:
       mov      edi, ecx
       imul     edi, edx
       jmp      G_M000_IG154
 
G_M000_IG284:
       mov      rsi, 0xD1FFAB1E
       jmp      G_M000_IG177
 
G_M000_IG285:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG286:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG287:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 6992

; Assembly listing for method RyuPy.Generated.Module:m0_tail_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 336
       lea      rbp, [rsp+0x150]
       xor      eax, eax
       mov      qword ptr [rbp-0x148], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x140], zmm8
       vmovdqu32 zmmword ptr [rbp-0x100], zmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
       mov      gword ptr [rbp-0x40], rdx
       mov      gword ptr [rbp-0x48], rcx
       mov      gword ptr [rbp-0x50], r8
 
G_M000_IG02:
       mov      dword ptr [rbp-0x150], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x68], rax
       mov      edi, 22
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      rdi, rax
       call     [RyuPy.Runtime.PyOps:MakeList(System.Object[]):System.Object]
       mov      gword ptr [rbp-0x70], rax
       mov      rax, gword ptr [rbp-0x70]
       mov      gword ptr [rbp-0x58], rax
       mov      edi, 23
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x78], rax
       mov      rdi, gword ptr [rbp-0x78]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x80], rax
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x88], rax
       mov      rax, qword ptr [rbp-0x80]
       mov      qword ptr [rbp-0x90], rax
       mov      rax, qword ptr [rbp-0x88]
       mov      qword ptr [rbp-0x98], rax
       mov      qword ptr [rbp-0xA0], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x150]
       dec      eax
       mov      dword ptr [rbp-0x150], eax
       cmp      dword ptr [rbp-0x150], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x150]
       mov      esi, 91
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
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdx, gword ptr [rbp-0x38]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 24
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xD0], rax
       mov      rdx, gword ptr [rbp-0xD0]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 25
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xD8], rax
       mov      rdx, gword ptr [rbp-0xD8]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 26
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE0], rax
       mov      rdx, gword ptr [rbp-0xE0]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 27
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE8], rax
       mov      rdx, gword ptr [rbp-0xE8]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 28
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xF0], rax
       mov      rdx, gword ptr [rbp-0xF0]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 29
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xF8], rax
       mov      rdx, gword ptr [rbp-0x50]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 30
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x100], rax
       mov      rdx, gword ptr [rbp-0x100]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 31
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x108], rax
       mov      rdx, gword ptr [rbp-0x108]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 32
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x110], rax
 
G_M000_IG07:
       mov      rdx, gword ptr [rbp-0x110]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 33
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x118], rax
       mov      rdx, gword ptr [rbp-0x118]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 34
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x120], rax
       mov      rdx, gword ptr [rbp-0x120]
       mov      rcx, gword ptr [rbp-0x48]
       mov      edi, 35
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x128], rax
       mov      rdx, gword ptr [rbp-0x38]
       mov      rcx, gword ptr [rbp-0x50]
       mov      edi, 36
       mov      esi, 2
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x130], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 3
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x148], rax
       mov      rdi, gword ptr [rbp-0x148]
       mov      rdx, gword ptr [rbp-0xF8]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x148]
       mov      rdx, gword ptr [rbp-0x128]
       mov      esi, 1
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x148]
       mov      rdx, gword ptr [rbp-0x130]
       mov      esi, 2
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rdi, gword ptr [rbp-0x148]
       call     [RyuPy.Runtime.PyOps:MakeList(System.Object[]):System.Object]
       mov      gword ptr [rbp-0x138], rax
       mov      rax, gword ptr [rbp-0x138]
       mov      gword ptr [rbp-0x58], rax
       mov      rax, qword ptr [rbp-0x90]
       add      rax, qword ptr [rbp-0xA0]
       jo       SHORT G_M000_IG10
       mov      qword ptr [rbp-0x90], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 25
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x140], rax
       mov      rdi, qword ptr [rbp-0x68]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x140]
 
G_M000_IG09:
       add      rsp, 336
       pop      rbp
       ret      
 
G_M000_IG10:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1148

; Assembly listing for method RyuPy.Generated.Module:m0_tail_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x5b
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 69 inlinees with PGO data; 81 single block inlinees; 106 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 0x3F0
       mov      qword ptr [rsp+0x548], r15
       mov      qword ptr [rsp+0x540], r14
       mov      qword ptr [rsp+0x538], r13
       mov      qword ptr [rsp+0x530], r12
       mov      qword ptr [rsp+0x528], rbx
       lea      rbp, [rsp+0x3F0]
       xor      eax, eax
       mov      qword ptr [rbp-0x358], rax
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -768
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      qword ptr [rbp-0x50], rax
       mov      r13, gword ptr [rbp+0x128]
       mov      rbx, gword ptr [rbp+0x120]
       mov      r15, gword ptr [rbp+0x118]
       mov      r14, gword ptr [rbp+0x110]
       mov      rcx, gword ptr [rbp+0x108]
       mov      r12, qword ptr [rbp+0xD0]
       mov      rax, qword ptr [rbp+0xC8]
       mov      r8, qword ptr [rbp+0xC0]
 
G_M000_IG02:
       mov      qword ptr [rbp+0xC0], r8
       test     r8, r8
       setg     r10b
       movzx    r10, r10b
       mov      dword ptr [rbp-0x374], r10d
       jmp      G_M000_IG102
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG15
 
G_M000_IG04:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x190], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x1E8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1E8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x190]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG16
 
G_M000_IG05:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG11
 
G_M000_IG06:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x180], xmm0
       test     r15, r15
       je       G_M000_IG15
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG09
 
G_M000_IG08:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x188], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x180]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x188]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG16
 
G_M000_IG09:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG15
 
G_M000_IG10:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x1F0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x180]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x1F0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG16
 
G_M000_IG11:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG15
 
G_M000_IG12:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG15
 
G_M000_IG13:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG15
 
G_M000_IG14:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       jmp      SHORT G_M000_IG16
 
G_M000_IG15:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG16:
       test     rsi, rsi
       je       G_M000_IG46
 
G_M000_IG17:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG39
 
G_M000_IG18:
       mov      rdx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG46
 
G_M000_IG19:
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG37
 
G_M000_IG20:
       mov      rcx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x210], rcx
       lea      rdi, [rbp-0x228]
       mulx     r8, rsi, rcx
       mov      qword ptr [rdi], rsi
       mov      r9, qword ptr [rbp-0x228]
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, rcx
       sub      r8, rdi
       mov      rdi, rcx
       sar      rdi, 63
       and      rdi, rdx
       sub      r8, rdi
       mov      rdi, r9
       sar      rdi, 63
       cmp      r8, rdi
       je       G_M000_IG33
 
G_M000_IG21:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x220], xmm0
       lea      rdi, [rbp-0x220]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x220]
       mov      gword ptr [rbp-0x3E8], rax
       mov      ecx, dword ptr [rbp-0x218]
       mov      dword ptr [rbp-0x370], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x238], xmm0
       lea      rdi, [rbp-0x238]
       mov      rsi, qword ptr [rbp-0x210]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x238]
       mov      r9d, dword ptr [rbp-0x230]
       mov      rcx, gword ptr [rbp-0x3E8]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG26
 
G_M000_IG22:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x258], xmm0
       movsxd   rsi, dword ptr [rbp-0x370]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x258]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG23:
       vmovdqu  xmm0, xmmword ptr [rbp-0x258]
       vmovdqu  xmmword ptr [rbp-0x248], xmm0
 
G_M000_IG24:
       mov      rdi, gword ptr [rbp-0x248]
       mov      rsi, qword ptr [rbp-0x240]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG25:
       mov      rsi, rax
       jmp      G_M000_IG47
 
G_M000_IG26:
       test     rcx, rcx
       je       SHORT G_M000_IG31
 
G_M000_IG27:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG28:
       test     rax, rax
       je       SHORT G_M000_IG32
 
G_M000_IG29:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG30:
       mov      edx, dword ptr [rbp-0x370]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x248], rax
       mov      qword ptr [rbp-0x240], rdx
       jmp      SHORT G_M000_IG24
 
G_M000_IG31:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG28
 
G_M000_IG32:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG30
 
G_M000_IG33:
       mov      rcx, r9
       mov      qword ptr [rbp-0x260], rcx
       lea      rdx, [rcx+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG36
 
G_M000_IG34:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x260]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG35:
       jmp      SHORT G_M000_IG25
 
G_M000_IG36:
       test     rdx, rdx
       jl       G_M000_IG260
       mov      rax, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG259
       test     rdx, rdx
       jl       G_M000_IG260
       mov      rax, rdx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG35
 
G_M000_IG37:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG46
 
G_M000_IG38:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x208], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x268], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x268]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x208]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG47
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG45
 
G_M000_IG40:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x1F8], xmm0
       test     rbx, rbx
       je       G_M000_IG46
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG43
 
G_M000_IG42:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x200], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1F8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x200]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG47
 
G_M000_IG43:
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG46
 
G_M000_IG44:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x270], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1F8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x270]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG47
 
G_M000_IG45:
       mov      rdx, qword ptr [rsi]
 
G_M000_IG46:
       mov      rdx, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG47:
       test     rsi, rsi
       je       G_M000_IG72
 
G_M000_IG48:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG62
 
G_M000_IG49:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG72
 
G_M000_IG50:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG60
 
G_M000_IG51:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x290], rdx
       lea      r8, [rcx+rdx]
       mov      qword ptr [rbp-0x298], r8
       mov      rdi, rcx
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG56
 
G_M000_IG52:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x2A8], xmm0
       lea      rdi, [rbp-0x2A8]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG53:
       vmovdqu  xmm0, xmmword ptr [rbp-0x2A8]
       vmovdqu  xmmword ptr [rbp-0x2B8], xmm0
 
G_M000_IG54:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x2D8], xmm0
       lea      rdi, [rbp-0x2D8]
       mov      rsi, qword ptr [rbp-0x290]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x2B8]
       mov      rsi, qword ptr [rbp-0x2B0]
       mov      rdx, gword ptr [rbp-0x2D8]
       mov      rcx, qword ptr [rbp-0x2D0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x2C8], rax
       mov      qword ptr [rbp-0x2C0], rdx
       mov      rdi, gword ptr [rbp-0x2C8]
       mov      rsi, qword ptr [rbp-0x2C0]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG55:
       jmp      G_M000_IG73
 
G_M000_IG56:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG59
 
G_M000_IG57:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x298]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG58:
       jmp      SHORT G_M000_IG55
 
G_M000_IG59:
       test     rcx, rcx
       jl       G_M000_IG260
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG259
       test     rcx, rcx
       jl       G_M000_IG260
       mov      rax, rcx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG58
 
G_M000_IG60:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG72
 
G_M000_IG61:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x288], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x2E0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2E0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x288]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG73
 
G_M000_IG62:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG68
 
G_M000_IG63:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x278], xmm0
       test     r15, r15
       je       G_M000_IG72
 
G_M000_IG64:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG66
 
G_M000_IG65:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x280], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x278]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x280]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG73
 
G_M000_IG66:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG72
 
G_M000_IG67:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x2E8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x278]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2E8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG73
 
G_M000_IG68:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG72
 
G_M000_IG69:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG72
 
G_M000_IG70:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG72
 
G_M000_IG71:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG73
 
G_M000_IG72:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG73:
       mov      gword ptr [rbp+0x68], rax
       xor      rdx, rdx
       mov      gword ptr [rbp-0x308], rdx
 
G_M000_IG74:
       mov      gword ptr [rbp-0x2F8], rdx
 
G_M000_IG75:
       mov      gword ptr [rbp-0x2F0], rdx
       test     r14, r14
       je       G_M000_IG97
 
G_M000_IG76:
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      qword ptr [r14], rcx
       jne      G_M000_IG88
 
G_M000_IG77:
       mov      r8, qword ptr [r14+0x08]
       mov      qword ptr [rbp-0x300], r8
       test     rbx, rbx
       je       G_M000_IG97
 
G_M000_IG78:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG85
 
G_M000_IG79:
       mov      rcx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x310], rcx
       xor      edx, edx
       mov      qword ptr [rbp-0x318], rdx
       lea      rdx, [rbp-0x318]
       mov      rdi, r8
       mov      rsi, rcx
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x318]
       sar      rdi, 63
       cmp      rax, rdi
       je       G_M000_IG84
 
G_M000_IG80:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x328], xmm0
       lea      rdi, [rbp-0x328]
       mov      rsi, qword ptr [rbp-0x300]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG81:
       vmovdqu  xmm0, xmmword ptr [rbp-0x328]
       vmovdqu  xmmword ptr [rbp-0x338], xmm0
 
G_M000_IG82:
       mov      rdi, qword ptr [rbp-0x310]
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x348], rax
       mov      qword ptr [rbp-0x340], rdx
       mov      rdi, gword ptr [rbp-0x338]
       mov      rsi, qword ptr [rbp-0x330]
       mov      rdx, gword ptr [rbp-0x348]
       mov      rcx, qword ptr [rbp-0x340]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x358], rax
       mov      qword ptr [rbp-0x350], rdx
       mov      rdi, gword ptr [rbp-0x358]
       mov      rsi, qword ptr [rbp-0x350]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG83:
       mov      rdx, rax
       jmp      G_M000_IG98
 
G_M000_IG84:
       mov      rdi, qword ptr [rbp-0x318]
       call     [RyuPy.Runtime.PyInt:Box(long):System.Object]
       jmp      SHORT G_M000_IG83
 
G_M000_IG85:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      G_M000_IG97
 
G_M000_IG86:
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, r8
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x308]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       je       G_M000_IG97
 
G_M000_IG87:
       mov      rdx, gword ptr [rbp-0x308]
       jmp      G_M000_IG98
 
G_M000_IG88:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r14], rsi
       jne      SHORT G_M000_IG96
 
G_M000_IG89:
       vmovsd   xmm0, qword ptr [r14+0x08]
       test     rbx, rbx
       je       SHORT G_M000_IG97
 
G_M000_IG90:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      SHORT G_M000_IG93
 
G_M000_IG91:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x2F8]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG97
 
G_M000_IG92:
       mov      rdx, gword ptr [rbp-0x2F8]
       jmp      SHORT G_M000_IG98
 
G_M000_IG93:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG97
 
G_M000_IG94:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x2F0]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG97
 
G_M000_IG95:
       mov      rdx, gword ptr [rbp-0x2F0]
       jmp      SHORT G_M000_IG98
 
G_M000_IG96:
       mov      rdx, qword ptr [r14]
 
G_M000_IG97:
       mov      rdx, rbx
       mov      rsi, r14
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG98:
       xor      rcx, rcx
       mov      gword ptr [rbp-0x308], rcx
 
G_M000_IG99:
       mov      gword ptr [rbp-0x2F8], rcx
 
G_M000_IG100:
       mov      gword ptr [rbp-0x2F0], rcx
       mov      rcx, r15
       mov      edi, 31
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, rbx
       mov      edi, 32
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, r15
       mov      edi, 33
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, rbx
       mov      edi, 34
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, r15
       mov      edi, 35
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x38], rax
       mov      rdx, r13
       mov      rcx, r14
       mov      edi, 36
       mov      esi, 2
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x30], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 3
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x388], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp+0x68]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x388]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, gword ptr [rbp+0x38]
 
G_M000_IG101:
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x388]
       lea      rdi, bword ptr [rax+0x20]
       mov      rsi, gword ptr [rbp+0x30]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, gword ptr [rbp-0x388]
       call     [RyuPy.Runtime.PyOps:MakeList(System.Object[]):System.Object]
       mov      r8, qword ptr [rbp+0xC0]
       add      r12, r8
       jo       G_M000_IG260
       mov      qword ptr [rbp+0xC0], r8
       mov      rcx, rax
       mov      rax, qword ptr [rbp+0xC8]
       mov      r10d, dword ptr [rbp-0x374]
 
G_M000_IG102:
       mov      edx, r10d
       cmp      r12, rax
       setl     dil
       movzx    rdi, dil
       and      edi, edx
       mov      qword ptr [rbp+0xC8], rax
       cmp      r12, rax
       setg     sil
       movzx    rsi, sil
       test     edx, edx
       sete     dl
       movzx    rdx, dl
       and      edx, esi
       or       edx, edi
       je       G_M000_IG221
 
G_M000_IG103:
       mov      edx, dword ptr [(reloc)]
       test     edx, edx
       jle      SHORT G_M000_IG106
 
G_M000_IG104:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rsi, rdi
       dec      edx
       mov      ecx, edx
       cmp      ecx, dword ptr [rsi+0x08]
       jae      G_M000_IG259
       cmp      dword ptr [rsi+4*rcx+0x10], 24
       je       SHORT G_M000_IG106
 
G_M000_IG105:
       cmp      edx, dword ptr [rdi+0x08]
       jae      G_M000_IG259
       mov      edx, edx
       mov      dword ptr [rdi+4*rdx+0x10], 24
 
G_M000_IG106:
       test     r13, r13
       je       G_M000_IG140
 
G_M000_IG107:
       mov      rcx, qword ptr [r13]
       mov      r11, 0xD1FFAB1E
       cmp      rcx, r11
       jne      G_M000_IG134
 
G_M000_IG108:
       mov      rsi, qword ptr [r13+0x08]
       test     rbx, rbx
       je       G_M000_IG140
 
G_M000_IG109:
       mov      qword ptr [rbp-0x380], r11
       cmp      qword ptr [rbx], r11
       jne      G_M000_IG132
 
G_M000_IG110:
       mov      rcx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x48], rcx
       lea      rdi, [rbp-0x60]
       mov      rdx, rsi
       mulx     rdx, r9, rcx
       mov      qword ptr [rdi], r9
       mov      r9, qword ptr [rbp-0x60]
       mov      rdi, rsi
       sar      rdi, 63
       and      rdi, rcx
       sub      rdx, rdi
       mov      rdi, rcx
       sar      rdi, 63
       and      rdi, rsi
       sub      rdx, rdi
       mov      rdi, r9
       sar      rdi, 63
       cmp      rdx, rdi
       je       G_M000_IG128
 
G_M000_IG111:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x58], xmm0
       lea      rdi, [rbp-0x58]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x3C0], rax
       mov      ecx, dword ptr [rbp-0x50]
       mov      dword ptr [rbp-0x35C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       mov      rsi, qword ptr [rbp-0x48]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x70]
       mov      r9d, dword ptr [rbp-0x68]
       mov      rcx, gword ptr [rbp-0x3C0]
       mov      rsi, rcx
       or       rsi, rax
       jne      G_M000_IG119
 
G_M000_IG112:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       movsxd   rsi, dword ptr [rbp-0x35C]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x80]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x80]
       mov      r10d, dword ptr [rbp-0x78]
 
G_M000_IG113:
       mov      gword ptr [rbp-0x3C8], rax
       mov      ecx, r10d
       mov      dword ptr [rbp-0x360], ecx
       test     rax, rax
       jne      G_M000_IG223
 
G_M000_IG114:
       mov      rdi, rax
       test     rdi, rdi
       jne      G_M000_IG232
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jge      SHORT G_M000_IG126
 
G_M000_IG115:
       mov      edi, -1
 
G_M000_IG116:
       test     edi, edi
       jg       SHORT G_M000_IG127
 
G_M000_IG117:
       mov      rdi, qword ptr [rbp-0x380]
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x398], rax
       mov      rdi, gword ptr [rbp-0x3C8]
       mov      esi, dword ptr [rbp-0x360]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rsi, gword ptr [rbp-0x398]
       mov      qword ptr [rsi+0x08], rax
 
G_M000_IG118:
       jmp      G_M000_IG141
 
G_M000_IG119:
       test     rcx, rcx
       je       SHORT G_M000_IG124
 
G_M000_IG120:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG121:
       test     rax, rax
       je       SHORT G_M000_IG125
 
G_M000_IG122:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG123:
       mov      edx, dword ptr [rbp-0x35C]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      r10d, ecx
       jmp      G_M000_IG113
 
G_M000_IG124:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG121
 
G_M000_IG125:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG123
 
G_M000_IG126:
       xor      edi, edi
       jmp      SHORT G_M000_IG116
 
G_M000_IG127:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      gword ptr [rbp-0x390], rsi
       lea      rdi, bword ptr [rsi+0x08]
       mov      rsi, gword ptr [rbp-0x3C8]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rsi, gword ptr [rbp-0x390]
       mov      edi, dword ptr [rbp-0x360]
       mov      dword ptr [rsi+0x10], edi
       jmp      SHORT G_M000_IG118
 
G_M000_IG128:
       mov      r11, qword ptr [rbp-0x380]
       mov      rcx, r9
       mov      qword ptr [rbp-0x88], rcx
       lea      rdx, [rcx+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG131
 
G_M000_IG129:
       mov      rdi, r11
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x88]
       mov      qword ptr [rax+0x08], rdi
       mov      rsi, rax
 
G_M000_IG130:
       jmp      G_M000_IG118
 
G_M000_IG131:
       test     rdx, rdx
       jl       G_M000_IG260
       mov      rsi, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG259
       mov      rsi, gword ptr [rcx+8*rsi+0x10]
       jmp      SHORT G_M000_IG130
 
G_M000_IG132:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG140
 
G_M000_IG133:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x90], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x90]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG141
 
G_M000_IG134:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG140
 
G_M000_IG135:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG140
 
G_M000_IG136:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG138
 
G_M000_IG137:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG141
 
G_M000_IG138:
       cmp      qword ptr [rbx], r11
       jne      SHORT G_M000_IG140
 
G_M000_IG139:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG141
 
G_M000_IG140:
       mov      rdx, rbx
       mov      rsi, r13
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG141:
       test     rsi, rsi
       je       G_M000_IG176
 
G_M000_IG142:
       mov      r11, 0xD1FFAB1E
       mov      rax, r11
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG160
 
G_M000_IG143:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG176
 
G_M000_IG144:
       mov      qword ptr [rbp-0x380], rax
       cmp      qword ptr [r15], rax
       jne      G_M000_IG158
 
G_M000_IG145:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0xB8], rdx
       lea      r8, [rcx+rdx]
       mov      qword ptr [rbp-0xC0], r8
       mov      rdi, rcx
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG154
 
G_M000_IG146:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xD0], xmm0
       lea      rdi, [rbp-0xD0]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xD0]
       mov      gword ptr [rbp-0x3D0], rax
       mov      ecx, dword ptr [rbp-0xC8]
       mov      dword ptr [rbp-0x364], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xE0], xmm0
       lea      rdi, [rbp-0xE0]
       mov      rsi, qword ptr [rbp-0xB8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x3D0]
       mov      esi, dword ptr [rbp-0x364]
       mov      rdx, gword ptr [rbp-0xE0]
       mov      rcx, qword ptr [rbp-0xD8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x3D8], rax
       mov      ecx, edx
       mov      dword ptr [rbp-0x368], edx
       mov      rdi, rax
       test     rdi, rdi
       jne      G_M000_IG240
 
G_M000_IG147:
       mov      rdi, rax
       test     rdi, rdi
       jne      G_M000_IG249
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jge      SHORT G_M000_IG152
 
G_M000_IG148:
       mov      edi, -1
 
G_M000_IG149:
       test     edi, edi
       jg       SHORT G_M000_IG153
 
G_M000_IG150:
       mov      rdi, qword ptr [rbp-0x380]
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x3B0], rax
       mov      rdi, gword ptr [rbp-0x3D8]
       mov      esi, dword ptr [rbp-0x368]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rsi, gword ptr [rbp-0x3B0]
       mov      qword ptr [rsi+0x08], rax
 
G_M000_IG151:
       jmp      G_M000_IG177
 
G_M000_IG152:
       xor      edi, edi
       jmp      SHORT G_M000_IG149
 
G_M000_IG153:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      gword ptr [rbp-0x3A8], rsi
       lea      rdi, bword ptr [rsi+0x08]
       mov      rsi, gword ptr [rbp-0x3D8]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rsi, gword ptr [rbp-0x3A8]
       mov      edi, dword ptr [rbp-0x368]
       mov      dword ptr [rsi+0x10], edi
       jmp      SHORT G_M000_IG151
 
G_M000_IG154:
       mov      rax, qword ptr [rbp-0x380]
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG157
 
G_M000_IG155:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0xC0]
       mov      qword ptr [rax+0x08], rdi
       mov      rsi, rax
 
G_M000_IG156:
       jmp      SHORT G_M000_IG151
 
G_M000_IG157:
       test     rcx, rcx
       jl       G_M000_IG260
       mov      rsi, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG259
       test     rcx, rcx
       jl       G_M000_IG260
       mov      rax, rcx
       mov      rsi, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG156
 
G_M000_IG158:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG176
 
G_M000_IG159:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG177
 
G_M000_IG160:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG166
 
G_M000_IG161:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       test     r15, r15
       je       G_M000_IG176
 
G_M000_IG162:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG164
 
G_M000_IG163:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xA8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG177
 
G_M000_IG164:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG176
 
G_M000_IG165:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xF0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG177
 
G_M000_IG166:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG176
 
G_M000_IG167:
       mov      rax, rsi
       mov      gword ptr [rbp-0x3A0], rax
       test     r15, r15
       je       G_M000_IG176
 
G_M000_IG168:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG176
 
G_M000_IG169:
       cmp      dword ptr [rax+0x08], 0
       je       G_M000_IG174
 
G_M000_IG170:
       cmp      dword ptr [r15+0x08], 0
       je       G_M000_IG173
 
G_M000_IG171:
       mov      ecx, dword ptr [rax+0x08]
       mov      dword ptr [rbp-0xF4], ecx
       mov      esi, ecx
       add      esi, dword ptr [r15+0x08]
       js       G_M000_IG258
       movsxd   rsi, esi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      rsi, rax
       mov      gword ptr [rbp-0x3B8], rsi
       cmp      byte  ptr [rsi], sil
       lea      rdi, bword ptr [rsi+0x0C]
       mov      rax, gword ptr [rbp-0x3A0]
       lea      rsi, bword ptr [rax+0x0C]
       mov      edx, dword ptr [rax+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       movsxd   rdi, dword ptr [rbp-0xF4]
       mov      rsi, gword ptr [rbp-0x3B8]
       lea      rdi, bword ptr [rsi+2*rdi+0x0C]
       lea      rsi, bword ptr [r15+0x0C]
       mov      edx, dword ptr [r15+0x08]
       add      rdx, rdx
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rsi, gword ptr [rbp-0x3B8]
 
G_M000_IG172:
       jmp      SHORT G_M000_IG177
 
G_M000_IG173:
       mov      rsi, rax
       jmp      SHORT G_M000_IG172
 
G_M000_IG174:
       cmp      dword ptr [r15+0x08], 0
       je       G_M000_IG257
 
G_M000_IG175:
       mov      rsi, r15
       jmp      SHORT G_M000_IG172
 
G_M000_IG176:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG177:
       test     rsi, rsi
       je       G_M000_IG207
 
G_M000_IG178:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG200
 
G_M000_IG179:
       mov      rdx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG207
 
G_M000_IG180:
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG198
 
G_M000_IG181:
       mov      rcx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x118], rcx
       lea      rdi, [rbp-0x130]
       mulx     r8, rsi, rcx
       mov      qword ptr [rdi], rsi
       mov      r9, qword ptr [rbp-0x130]
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, rcx
       sub      r8, rdi
       mov      rdi, rcx
       sar      rdi, 63
       and      rdi, rdx
       sub      r8, rdi
       mov      rdi, r9
       sar      rdi, 63
       cmp      r8, rdi
       je       G_M000_IG194
 
G_M000_IG182:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x128], xmm0
       lea      rdi, [rbp-0x128]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x128]
       mov      gword ptr [rbp-0x3E0], rax
       mov      ecx, dword ptr [rbp-0x120]
       mov      dword ptr [rbp-0x36C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x140], xmm0
       lea      rdi, [rbp-0x140]
       mov      rsi, qword ptr [rbp-0x118]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x140]
       mov      r9d, dword ptr [rbp-0x138]
       mov      rcx, gword ptr [rbp-0x3E0]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG187
 
G_M000_IG183:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x160], xmm0
       movsxd   rsi, dword ptr [rbp-0x36C]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x160]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG184:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x160]
       vmovdqu32 xmmword ptr [rbp-0x150], xmm0
 
G_M000_IG185:
       mov      rdi, gword ptr [rbp-0x150]
       mov      rsi, qword ptr [rbp-0x148]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG186:
       mov      rsi, rax
       jmp      G_M000_IG208
 
G_M000_IG187:
       test     rcx, rcx
       je       SHORT G_M000_IG192
 
G_M000_IG188:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG189:
       test     rax, rax
       je       SHORT G_M000_IG193
 
G_M000_IG190:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG191:
       mov      edx, dword ptr [rbp-0x36C]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x150], rax
       mov      qword ptr [rbp-0x148], rdx
       jmp      SHORT G_M000_IG185
 
G_M000_IG192:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG189
 
G_M000_IG193:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG191
 
G_M000_IG194:
       mov      rcx, r9
       mov      qword ptr [rbp-0x168], rcx
       lea      rdx, [rcx+0x05]
       cmp      rdx, 261
       jbe      SHORT G_M000_IG197
 
G_M000_IG195:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x168]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG196:
       jmp      SHORT G_M000_IG186
 
G_M000_IG197:
       test     rdx, rdx
       jl       G_M000_IG260
       mov      rax, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG259
       test     rdx, rdx
       jl       G_M000_IG260
       mov      rax, rdx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG196
 
G_M000_IG198:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG207
 
G_M000_IG199:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x110], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x170], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x170]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x110]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG208
 
G_M000_IG200:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG206
 
G_M000_IG201:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x100], xmm0
       test     rbx, rbx
       je       G_M000_IG207
 
G_M000_IG202:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG204
 
G_M000_IG203:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x108], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x100]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x108]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG208
 
G_M000_IG204:
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG207
 
G_M000_IG205:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x178], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x100]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG208
 
G_M000_IG206:
       mov      rdx, qword ptr [rsi]
 
G_M000_IG207:
       mov      rdx, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG208:
       test     rsi, rsi
       je       G_M000_IG15
 
G_M000_IG209:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG05
 
G_M000_IG210:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG15
 
G_M000_IG211:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG03
 
G_M000_IG212:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x198], rdx
       lea      r8, [rcx+rdx]
       mov      qword ptr [rbp-0x1A0], r8
       mov      rdi, rcx
       xor      rdi, r8
       mov      rsi, rdx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG217
 
G_M000_IG213:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1B0], xmm0
       lea      rdi, [rbp-0x1B0]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG214:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x1B0]
       vmovdqu32 xmmword ptr [rbp-0x1C0], xmm0
 
G_M000_IG215:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1E0], xmm0
       lea      rdi, [rbp-0x1E0]
       mov      rsi, qword ptr [rbp-0x198]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x1C0]
       mov      rsi, qword ptr [rbp-0x1B8]
       mov      rdx, gword ptr [rbp-0x1E0]
       mov      rcx, qword ptr [rbp-0x1D8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x1D0], rax
       mov      qword ptr [rbp-0x1C8], rdx
       mov      rdi, gword ptr [rbp-0x1D0]
       mov      rsi, qword ptr [rbp-0x1C8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG216:
       mov      rsi, rax
       jmp      G_M000_IG16
 
G_M000_IG217:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG220
 
G_M000_IG218:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x1A0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG219:
       jmp      SHORT G_M000_IG216
 
G_M000_IG220:
       test     rcx, rcx
       jl       G_M000_IG260
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG259
       test     rcx, rcx
       jl       G_M000_IG260
       mov      rax, rcx
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG219
 
G_M000_IG221:
       mov      gword ptr [rbp+0x108], rcx
       mov      edi, 25
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, qword ptr [rbp+0xF8]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp+0x108]
 
G_M000_IG222:
       add      rsp, 0x528
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG223:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG224
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG225
 
G_M000_IG224:
       mov      edx, ecx
       jmp      SHORT G_M000_IG231
 
G_M000_IG225:
       cmp      edi, 2
       je       SHORT G_M000_IG226
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG259
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG227
 
G_M000_IG226:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
 
G_M000_IG227:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG228
       mov      edx, -1
       jmp      SHORT G_M000_IG230
 
G_M000_IG228:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jbe      SHORT G_M000_IG229
       mov      edx, 1
       jmp      SHORT G_M000_IG230
 
G_M000_IG229:
       xor      edx, edx
 
G_M000_IG230:
       imul     edx, ecx
 
G_M000_IG231:
       test     edx, edx
       jge      G_M000_IG114
       jmp      G_M000_IG127
 
G_M000_IG232:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG233
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG234
 
G_M000_IG233:
       mov      edi, ecx
       mov      ecx, dword ptr [rbp-0x360]
       jmp      G_M000_IG116
 
G_M000_IG234:
       cmp      edi, 2
       je       SHORT G_M000_IG235
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG259
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG236
 
G_M000_IG235:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
       mov      rax, gword ptr [rbp-0x3C8]
 
G_M000_IG236:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG237
       mov      edx, -1
       jmp      SHORT G_M000_IG239
 
G_M000_IG237:
       test     rdi, rdi
       jge      SHORT G_M000_IG238
       mov      edx, 1
       jmp      SHORT G_M000_IG239
 
G_M000_IG238:
       xor      edx, edx
 
G_M000_IG239:
       mov      edi, ecx
       imul     edi, edx
       jmp      G_M000_IG116
 
G_M000_IG240:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG241
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG242
 
G_M000_IG241:
       mov      edx, ecx
       jmp      SHORT G_M000_IG248
 
G_M000_IG242:
       cmp      edi, 2
       je       SHORT G_M000_IG243
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG259
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG244
 
G_M000_IG243:
       cmp      dword ptr [rax+0x08], 1
       jbe      G_M000_IG259
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG259
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
 
G_M000_IG244:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG245
       mov      edx, -1
       jmp      SHORT G_M000_IG247
 
G_M000_IG245:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jbe      SHORT G_M000_IG246
       mov      edx, 1
       jmp      SHORT G_M000_IG247
 
G_M000_IG246:
       xor      edx, edx
 
G_M000_IG247:
       imul     edx, ecx
 
G_M000_IG248:
       test     edx, edx
       jge      G_M000_IG147
       jmp      G_M000_IG153
 
G_M000_IG249:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG250
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG251
 
G_M000_IG250:
       mov      edi, ecx
       mov      ecx, dword ptr [rbp-0x368]
       jmp      G_M000_IG149
 
G_M000_IG251:
       cmp      edi, 2
       je       SHORT G_M000_IG252
       cmp      dword ptr [rax+0x08], 0
       jbe      SHORT G_M000_IG259
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG253
 
G_M000_IG252:
       cmp      dword ptr [rax+0x08], 1
       jbe      SHORT G_M000_IG259
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       cmp      dword ptr [rax+0x08], 0
       jbe      SHORT G_M000_IG259
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
       mov      rax, gword ptr [rbp-0x3D8]
 
G_M000_IG253:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG254
       mov      edx, -1
       jmp      SHORT G_M000_IG256
 
G_M000_IG254:
       test     rdi, rdi
       jge      SHORT G_M000_IG255
       mov      edx, 1
       jmp      SHORT G_M000_IG256
 
G_M000_IG255:
       xor      edx, edx
 
G_M000_IG256:
       mov      edi, ecx
       imul     edi, edx
       jmp      G_M000_IG149
 
G_M000_IG257:
       mov      rsi, 0xD1FFAB1E
       jmp      G_M000_IG172
 
G_M000_IG258:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG259:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG260:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 6471

