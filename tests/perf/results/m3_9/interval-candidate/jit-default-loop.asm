; Assembly listing for method RyuPy.Generated.Module:m0_loop_diamond(System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 272
       lea      rbp, [rsp+0x110]
       xor      eax, eax
       mov      qword ptr [rbp-0x108], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x100], zmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       mov      qword ptr [rbp-0x40], rax
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
 
G_M000_IG02:
       mov      dword ptr [rbp-0x110], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x58], rax
       mov      edi, 19
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x60], rax
       mov      rax, gword ptr [rbp-0x60]
       mov      gword ptr [rbp-0x40], rax
       mov      edi, 20
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x68], rax
       mov      rdi, gword ptr [rbp-0x68]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x70], rax
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x78], rax
       mov      rax, qword ptr [rbp-0x70]
       mov      qword ptr [rbp-0x80], rax
       mov      rax, qword ptr [rbp-0x78]
       mov      qword ptr [rbp-0x88], rax
       mov      qword ptr [rbp-0x90], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x110]
       dec      eax
       mov      dword ptr [rbp-0x110], eax
       cmp      dword ptr [rbp-0x110], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x110]
       mov      esi, 88
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       cmp      qword ptr [rbp-0x90], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x94], eax
       mov      rax, qword ptr [rbp-0x80]
       cmp      rax, qword ptr [rbp-0x88]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x98], eax
       mov      rax, qword ptr [rbp-0x80]
       cmp      rax, qword ptr [rbp-0x88]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x9C], eax
       mov      eax, dword ptr [rbp-0x94]
       and      eax, dword ptr [rbp-0x98]
       movzx    rax, al
       mov      dword ptr [rbp-0xA0], eax
       cmp      dword ptr [rbp-0x94], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0xA4], eax
       mov      eax, dword ptr [rbp-0xA4]
       and      eax, dword ptr [rbp-0x9C]
       movzx    rax, al
       mov      dword ptr [rbp-0xA8], eax
       mov      eax, dword ptr [rbp-0xA0]
       or       eax, dword ptr [rbp-0xA8]
       movzx    rax, al
       mov      dword ptr [rbp-0xAC], eax
       cmp      dword ptr [rbp-0xAC], 0
       jne      SHORT G_M000_IG06
       jmp      SHORT G_M000_IG08
 
G_M000_IG06:
       mov      edi, 21
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x38]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0xBC], eax
       cmp      dword ptr [rbp-0xBC], 0
       jne      SHORT G_M000_IG09
       jmp      G_M000_IG10
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x80]
       add      rax, qword ptr [rbp-0x90]
       jo       G_M000_IG16
       mov      qword ptr [rbp-0x80], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      edi, 26
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0xF0], rax
       jmp      G_M000_IG14
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 22
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xC8], rax
       mov      rax, gword ptr [rbp-0xC8]
       mov      gword ptr [rbp-0x50], rax
       jmp      SHORT G_M000_IG11
 
G_M000_IG10:
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xD0], rax
       mov      rax, gword ptr [rbp-0xD0]
       mov      gword ptr [rbp-0x50], rax
 
G_M000_IG11:
       mov      eax, dword ptr [rbp-0x110]
       dec      eax
       mov      dword ptr [rbp-0x110], eax
       cmp      dword ptr [rbp-0x110], 0
       jg       SHORT G_M000_IG13
 
G_M000_IG12:
       lea      rdi, [rbp-0x110]
       mov      esi, 257
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG13:
       mov      edi, 25
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xD8], rax
       mov      rsi, gword ptr [rbp-0x50]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0xF8], rax
       mov      rsi, gword ptr [rbp-0xD8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x100], rax
       mov      rax, qword ptr [rbp-0xF8]
       add      rax, qword ptr [rbp-0x100]
       jo       SHORT G_M000_IG16
       mov      qword ptr [rbp-0x108], rax
       mov      rdi, qword ptr [rbp-0x108]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0xE0], rax
       mov      rdx, gword ptr [rbp-0x40]
       mov      rcx, gword ptr [rbp-0xE0]
       xor      edi, edi
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE8], rax
       mov      rax, gword ptr [rbp-0xE8]
       mov      gword ptr [rbp-0x40], rax
       jmp      G_M000_IG07
 
G_M000_IG14:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xF0]
 
G_M000_IG15:
       add      rsp, 272
       pop      rbp
       ret      
 
G_M000_IG16:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 905

; Assembly listing for method RyuPy.Generated.Module:m0_loop_diamond(System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x101
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 2
; 26 inlinees with PGO data; 24 single block inlinees; 28 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 176
       mov      qword ptr [rsp+0x1C8], r15
       mov      qword ptr [rsp+0x1C0], r14
       mov      qword ptr [rsp+0x1B8], r13
       mov      qword ptr [rsp+0x1B0], r12
       mov      qword ptr [rsp+0x1A8], rbx
       lea      rbp, [rsp+0xB0]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x60], ymm8
       mov      rbx, gword ptr [rbp+0xE8]
       mov      r14, gword ptr [rbp+0xE0]
       mov      r13, gword ptr [rbp+0xD0]
       mov      r15, qword ptr [rbp+0xA0]
       mov      r12, qword ptr [rbp+0x98]
 
G_M000_IG02:
       jmp      G_M000_IG18
 
G_M000_IG03:
       mov      rdi, r9
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      qword ptr [rdx+0x08], r13
       mov      r13, rdx
 
G_M000_IG04:
       test     r14, r14
       je       SHORT G_M000_IG08
 
G_M000_IG05:
       mov      r10, qword ptr [r14]
       mov      r9, qword ptr [rbp-0x88]
       cmp      r10, r9
       je       G_M000_IG27
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       cmp      r10, rdi
       je       G_M000_IG44
 
G_M000_IG07:
       mov      rsi, 0xD1FFAB1E
       cmp      r10, rsi
       je       G_M000_IG49
 
G_M000_IG08:
       mov      rdx, r13
       mov      rsi, r14
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r14, rax
 
G_M000_IG09:
       mov      r13, qword ptr [rbp+0x90]
       add      r15, r13
       jo       G_M000_IG90
       test     r13, r13
       setg     dil
       movzx    rdi, dil
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
       je       G_M000_IG62
 
G_M000_IG10:
       mov      rax, qword ptr [rbp-0x80]
       mov      edi, dword ptr [rax]
       test     edi, edi
       jle      SHORT G_M000_IG13
 
G_M000_IG11:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      esi, edi
       cmp      esi, dword ptr [rdx+0x08]
       jae      G_M000_IG89
       cmp      dword ptr [rdx+4*rsi+0x10], 21
       je       SHORT G_M000_IG13
 
G_M000_IG12:
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG89
       mov      edi, edi
       mov      dword ptr [rcx+4*rdi+0x10], 21
 
G_M000_IG13:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG58
 
G_M000_IG14:
       mov      rax, qword ptr [rbp-0x80]
       mov      ecx, dword ptr [rax]
       test     ecx, ecx
       jle      SHORT G_M000_IG17
 
G_M000_IG15:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rdx, rax
       dec      ecx
       mov      edi, ecx
       cmp      edi, dword ptr [rdx+0x08]
       jae      G_M000_IG89
       cmp      dword ptr [rdx+4*rdi+0x10], 22
       je       SHORT G_M000_IG17
 
G_M000_IG16:
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG89
       mov      ecx, ecx
       mov      dword ptr [rax+4*rcx+0x10], 22
 
G_M000_IG17:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      qword ptr [rbp+0x90], r13
       mov      r13, rax
 
G_M000_IG18:
       mov      rdx, 0xD1FFAB1E
       mov      qword ptr [rbp-0x80], rdx
       mov      esi, dword ptr [rdx]
       test     esi, esi
       jle      SHORT G_M000_IG21
 
G_M000_IG19:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      r8, rdi
       dec      esi
       mov      r9d, esi
       cmp      r9d, dword ptr [r8+0x08]
       jae      G_M000_IG89
       cmp      dword ptr [r8+4*r9+0x10], 25
       je       SHORT G_M000_IG21
 
G_M000_IG20:
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG89
       mov      esi, esi
       mov      dword ptr [rdi+4*rsi+0x10], 25
 
G_M000_IG21:
       mov      rsi, 0xD1FFAB1E
       mov      r8, gword ptr [rsi]
       mov      gword ptr [rbp+0x48], r8
       mov      r9, 0xD1FFAB1E
       mov      qword ptr [rbp-0x88], r9
       cmp      qword ptr [r13], r9
       je       SHORT G_M000_IG23
 
G_M000_IG22:
       mov      rsi, r13
       mov      rdi, r9
       call     [CORINFO_HELP_UNBOX]
       mov      r9, qword ptr [rbp-0x88]
 
G_M000_IG23:
       mov      r13, qword ptr [r13+0x08]
       mov      r8, gword ptr [rbp+0x48]
       cmp      qword ptr [r8], r9
       je       SHORT G_M000_IG25
 
G_M000_IG24:
       mov      rsi, r8
       mov      rdi, r9
       call     [CORINFO_HELP_UNBOX]
       mov      r8, gword ptr [rbp+0x48]
       mov      r9, qword ptr [rbp-0x88]
 
G_M000_IG25:
       add      r13, qword ptr [r8+0x08]
       jo       G_M000_IG90
       lea      rsi, [r13+0x05]
       cmp      rsi, 261
       ja       G_M000_IG03
 
G_M000_IG26:
       test     rsi, rsi
       jl       G_M000_IG90
       mov      rdi, rsi
       mov      rsi, 0xD1FFAB1E
       mov      r8, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG89
       mov      r13, gword ptr [r8+8*rdi+0x10]
       jmp      G_M000_IG04
 
G_M000_IG27:
       mov      rsi, qword ptr [r14+0x08]
       test     r13, r13
       je       G_M000_IG08
 
G_M000_IG28:
       cmp      qword ptr [r13], r9
       jne      G_M000_IG42
 
G_M000_IG29:
       mov      r13, qword ptr [r13+0x08]
       lea      r14, [rsi+r13]
       mov      rdi, rsi
       xor      rdi, r14
       mov      r8, r13
       xor      r8, r14
       and      rdi, r8
       jge      G_M000_IG38
 
G_M000_IG30:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x50], xmm0
       lea      rdi, [rbp-0x50]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r14, gword ptr [rbp-0x50]
       mov      eax, dword ptr [rbp-0x48]
       mov      dword ptr [rbp-0x74], eax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x60], xmm0
       lea      rdi, [rbp-0x60]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, r14
       mov      esi, dword ptr [rbp-0x74]
       mov      rdx, gword ptr [rbp-0x60]
       mov      rcx, qword ptr [rbp-0x58]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      r14, rax
       mov      r13d, edx
       test     r14, r14
       jne      G_M000_IG70
 
G_M000_IG31:
       test     r14, r14
       jne      G_M000_IG79
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jge      SHORT G_M000_IG36
 
G_M000_IG32:
       mov      edi, -1
 
G_M000_IG33:
       test     edi, edi
       jg       SHORT G_M000_IG37
 
G_M000_IG34:
       mov      rdi, qword ptr [rbp-0x88]
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xA0], rax
       mov      rdi, r14
       mov      esi, r13d
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r13, gword ptr [rbp-0xA0]
       mov      qword ptr [r13+0x08], rax
 
G_M000_IG35:
       mov      r14, r13
       jmp      G_M000_IG09
 
G_M000_IG36:
       xor      edi, edi
       jmp      SHORT G_M000_IG33
 
G_M000_IG37:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x98], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r14
       call     CORINFO_HELP_ASSIGN_REF
       mov      r14, gword ptr [rbp-0x98]
       mov      dword ptr [r14+0x10], r13d
       mov      r13, r14
       jmp      SHORT G_M000_IG35
 
G_M000_IG38:
       mov      r9, qword ptr [rbp-0x88]
       lea      r13, [r14+0x05]
       cmp      r13, 261
       jbe      SHORT G_M000_IG41
 
G_M000_IG39:
       mov      rdi, r9
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r14
 
G_M000_IG40:
       mov      r13, rax
       jmp      SHORT G_M000_IG35
 
G_M000_IG41:
       test     r13, r13
       jl       G_M000_IG90
       mov      rdi, r13
       mov      rsi, 0xD1FFAB1E
       mov      r8, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG89
       mov      rdi, gword ptr [r8+8*rdi+0x10]
       mov      r13, rdi
       mov      rax, r13
       jmp      SHORT G_M000_IG40
 
G_M000_IG42:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG08
 
G_M000_IG43:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x68], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x68]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG09
 
G_M000_IG44:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     r13, r13
       je       G_M000_IG08
 
G_M000_IG45:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      SHORT G_M000_IG47
 
G_M000_IG46:
       vmovsd   xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG09
 
G_M000_IG47:
       cmp      qword ptr [r13], r9
       jne      G_M000_IG08
 
G_M000_IG48:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x70], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x70]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG09
 
G_M000_IG49:
       mov      rsi, r14
       mov      gword ptr [rbp-0x90], rsi
       test     r13, r13
       je       G_M000_IG08
 
G_M000_IG50:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG08
 
G_M000_IG51:
       cmp      dword ptr [rsi+0x08], 0
       je       SHORT G_M000_IG56
 
G_M000_IG52:
       cmp      dword ptr [r13+0x08], 0
       je       SHORT G_M000_IG55
 
G_M000_IG53:
       mov      r14d, dword ptr [rsi+0x08]
       mov      edi, r14d
       add      edi, dword ptr [r13+0x08]
       js       G_M000_IG88
       movsxd   rsi, edi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0xA8], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rsi, gword ptr [rbp-0x90]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, r14d
       mov      r14, gword ptr [rbp-0xA8]
       lea      rdi, bword ptr [r14+2*rdi+0x0C]
       mov      edx, dword ptr [r13+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r13+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       nop      
 
G_M000_IG54:
       jmp      G_M000_IG09
 
G_M000_IG55:
       mov      r14, rsi
       jmp      SHORT G_M000_IG54
 
G_M000_IG56:
       cmp      dword ptr [r13+0x08], 0
       je       G_M000_IG87
 
G_M000_IG57:
       mov      r14, r13
       jmp      SHORT G_M000_IG54
 
G_M000_IG58:
       mov      rcx, qword ptr [rbp-0x80]
       mov      ecx, dword ptr [rcx]
       test     ecx, ecx
       jle      SHORT G_M000_IG61
 
G_M000_IG59:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rdx, rax
       dec      ecx
       mov      edi, ecx
       cmp      edi, dword ptr [rdx+0x08]
       jae      G_M000_IG89
       cmp      dword ptr [rdx+4*rdi+0x10], 24
       je       SHORT G_M000_IG61
 
G_M000_IG60:
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG89
       mov      ecx, ecx
       mov      dword ptr [rax+4*rcx+0x10], 24
 
G_M000_IG61:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      qword ptr [rbp+0x90], r13
       mov      r13, rax
       jmp      G_M000_IG18
 
G_M000_IG62:
       mov      rbx, qword ptr [rbp-0x80]
       mov      edi, dword ptr [rbx]
       test     edi, edi
       jle      SHORT G_M000_IG65
 
G_M000_IG63:
       mov      rax, 0xD1FFAB1E
       mov      rcx, gword ptr [rax]
       mov      rax, rcx
       lea      edx, [rdi-0x01]
       mov      esi, edx
       cmp      esi, dword ptr [rax+0x08]
       jae      G_M000_IG89
       cmp      dword ptr [rax+4*rsi+0x10], 26
       je       SHORT G_M000_IG65
 
G_M000_IG64:
       mov      rax, rcx
       mov      ecx, edx
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG89
       mov      dword ptr [rax+4*rcx+0x10], 26
 
G_M000_IG65:
       mov      r15, qword ptr [rbp+0xC8]
       test     r15, r15
       jle      SHORT G_M000_IG68
 
G_M000_IG66:
       movsxd   rax, edi
       cmp      r15, rax
       jg       SHORT G_M000_IG68
 
G_M000_IG67:
       dec      r15d
       mov      dword ptr [rbx], r15d
 
G_M000_IG68:
       mov      rax, r14
 
G_M000_IG69:
       add      rsp, 424
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG70:
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG71
       mov      edi, dword ptr [r14+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG72
 
G_M000_IG71:
       mov      eax, r13d
       jmp      SHORT G_M000_IG78
 
G_M000_IG72:
       cmp      edi, 2
       je       SHORT G_M000_IG73
       cmp      dword ptr [r14+0x08], 0
       jbe      G_M000_IG89
       mov      edi, dword ptr [r14+0x10]
       jmp      SHORT G_M000_IG74
 
G_M000_IG73:
       mov      edi, dword ptr [r14+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r14+0x10]
       or       rdi, rax
 
G_M000_IG74:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG75
       mov      eax, -1
       jmp      SHORT G_M000_IG77
 
G_M000_IG75:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jbe      SHORT G_M000_IG76
       mov      eax, 1
       jmp      SHORT G_M000_IG77
 
G_M000_IG76:
       xor      eax, eax
 
G_M000_IG77:
       imul     eax, r13d
 
G_M000_IG78:
       test     eax, eax
       jl       G_M000_IG37
       jmp      G_M000_IG31
 
G_M000_IG79:
       movsxd   rdi, r13d
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG80
       mov      edi, dword ptr [r14+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG81
 
G_M000_IG80:
       mov      edi, r13d
       jmp      G_M000_IG33
 
G_M000_IG81:
       cmp      edi, 2
       je       SHORT G_M000_IG82
       cmp      dword ptr [r14+0x08], 0
       jbe      SHORT G_M000_IG89
       mov      edi, dword ptr [r14+0x10]
       jmp      SHORT G_M000_IG83
 
G_M000_IG82:
       mov      edi, dword ptr [r14+0x14]
       shl      rdi, 32
       mov      eax, dword ptr [r14+0x10]
       or       rdi, rax
 
G_M000_IG83:
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       jae      SHORT G_M000_IG84
       mov      eax, -1
       jmp      SHORT G_M000_IG86
 
G_M000_IG84:
       test     rdi, rdi
       jge      SHORT G_M000_IG85
       mov      eax, 1
       jmp      SHORT G_M000_IG86
 
G_M000_IG85:
       xor      eax, eax
 
G_M000_IG86:
       mov      edi, r13d
       imul     edi, eax
       jmp      G_M000_IG33
 
G_M000_IG87:
       mov      r14, 0xD1FFAB1E
       jmp      G_M000_IG54
 
G_M000_IG88:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG89:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG90:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1964

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_loop_diamond(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
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
 
G_M000_IG02:
       test     edx, edx
       je       SHORT G_M000_IG04
       mov      rdi, gword ptr [rsi]
       cmp      edx, 1
       jbe      SHORT G_M000_IG04
       mov      rsi, gword ptr [rsi+0x08]
       call     [RyuPy.Generated.Module:m0_loop_diamond(System.Object,System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 35

