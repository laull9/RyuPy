; Assembly listing for method RyuPy.Generated.Module:m0_repeat(System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 288
       lea      rbp, [rsp+0x120]
       xor      eax, eax
       mov      dword ptr [rbp-0x114], eax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x110], zmm8
       vmovdqu32 zmmword ptr [rbp-0xD0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       vmovdqa  xmmword ptr [rbp-0x50], xmm8
       mov      qword ptr [rbp-0x40], rax
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
 
G_M000_IG02:
       mov      dword ptr [rbp-0x120], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x50], rax
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       call     [RyuPy.Runtime.PyOps:None():System.Object]
       mov      gword ptr [rbp-0x58], rax
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x40], rax
       mov      edi, 12
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x60], rax
       mov      rdi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x68], rax
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x70], rax
       mov      rax, qword ptr [rbp-0x68]
       mov      qword ptr [rbp-0x78], rax
       mov      rax, qword ptr [rbp-0x70]
       mov      qword ptr [rbp-0x80], rax
       mov      qword ptr [rbp-0x88], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x120]
       dec      eax
       mov      dword ptr [rbp-0x120], eax
       cmp      dword ptr [rbp-0x120], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x120]
       mov      esi, 86
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
       mov      edi, 13
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rdi, gword ptr [rax]
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyErrors:LoadName(System.Object,System.String):System.Object]
       mov      gword ptr [rbp-0xB8], rax
       mov      rax, gword ptr [rbp-0xB8]
       mov      gword ptr [rbp-0xD0], rax
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0xD8], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rax, gword ptr [rbp-0xD0]
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x114], eax
       cmp      dword ptr [rbp-0x114], 0
       jne      G_M000_IG20
       jmp      G_M000_IG13
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x78]
       add      rax, qword ptr [rbp-0x88]
       jo       G_M000_IG23
       mov      qword ptr [rbp-0x78], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 14
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0xC8], rax
       mov      rdi, qword ptr [rbp-0x50]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xC8]
 
G_M000_IG09:
       add      rsp, 288
       pop      rbp
       ret      
 
G_M000_IG10:
       mov      eax, dword ptr [rbp-0x120]
       dec      eax
       mov      dword ptr [rbp-0x120], eax
       cmp      dword ptr [rbp-0x120], 0
       jg       SHORT G_M000_IG12
 
G_M000_IG11:
       lea      rdi, [rbp-0x120]
       mov      esi, 247
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG12:
       mov      rax, gword ptr [rbp-0xC0]
       mov      gword ptr [rbp-0x40], rax
       jmp      G_M000_IG07
 
G_M000_IG13:
       mov      eax, dword ptr [rbp-0x120]
       dec      eax
       mov      dword ptr [rbp-0x120], eax
       cmp      dword ptr [rbp-0x120], 0
       jg       SHORT G_M000_IG15
 
G_M000_IG14:
       lea      rdi, [rbp-0x120]
       mov      esi, 255
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG15:
       mov      rdx, gword ptr [rbp-0xD8]
       mov      rsi, gword ptr [rbp-0xD0]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:CallValue1(int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xC0], rax
       jmp      G_M000_IG10
 
G_M000_IG16:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       call     [RyuPy.Runtime.PyTraceback:CheckDepth()]
       mov      rdi, qword ptr [rbp-0xF0]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0xC0], rax
       jmp      G_M000_IG10
 
G_M000_IG17:
       mov      eax, dword ptr [rbp-0x120]
       dec      eax
       mov      dword ptr [rbp-0x120], eax
       cmp      dword ptr [rbp-0x120], 0
       jg       SHORT G_M000_IG19
 
G_M000_IG18:
       lea      rdi, [rbp-0x120]
       mov      esi, 291
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG19:
       mov      rsi, gword ptr [rbp-0xD8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0xE0], rax
       mov      rsi, gword ptr [rbp-0xD8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0xE8], rax
       mov      rax, qword ptr [rbp-0xE0]
       add      rax, qword ptr [rbp-0xE8]
       mov      qword ptr [rbp-0xF0], rax
       mov      rax, qword ptr [rbp-0xE0]
       xor      rax, qword ptr [rbp-0xF0]
       mov      qword ptr [rbp-0xF8], rax
       mov      rax, qword ptr [rbp-0xE8]
       xor      rax, qword ptr [rbp-0xF0]
       mov      qword ptr [rbp-0x100], rax
       mov      rax, qword ptr [rbp-0xF8]
       and      rax, qword ptr [rbp-0x100]
       mov      qword ptr [rbp-0x108], rax
       cmp      qword ptr [rbp-0x108], 0
       setge    al
       movzx    rax, al
       mov      dword ptr [rbp-0x10C], eax
       cmp      dword ptr [rbp-0x10C], 0
       jne      G_M000_IG16
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG20:
       mov      eax, dword ptr [rbp-0x120]
       dec      eax
       mov      dword ptr [rbp-0x120], eax
       cmp      dword ptr [rbp-0x120], 0
       jg       SHORT G_M000_IG22
 
G_M000_IG21:
       lea      rdi, [rbp-0x120]
       mov      esi, 367
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG22:
       mov      rsi, gword ptr [rbp-0xD8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x110], eax
       cmp      dword ptr [rbp-0x110], 0
       jne      G_M000_IG17
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG23:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1189

; Assembly listing for method RyuPy.Generated.Module:m0_repeat(System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0xf7
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 1 inlinees with PGO data; 12 single block inlinees; 10 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 112
       mov      qword ptr [rsp+0x198], r15
       mov      qword ptr [rsp+0x190], r14
       mov      qword ptr [rsp+0x188], r13
       mov      qword ptr [rsp+0x180], r12
       mov      qword ptr [rsp+0x178], rbx
       lea      rbp, [rsp+0x70]
       mov      rbx, gword ptr [rbp+0xF8]
       mov      r12, qword ptr [rbp+0xE0]
       mov      r15, qword ptr [rbp+0xB8]
       mov      r14, qword ptr [rbp+0xB0]
       mov      r13, qword ptr [rbp+0xA8]
       mov      rax, gword ptr [rbp+0x70]
 
G_M000_IG02:
       test     r13, r13
       setg     cl
       movzx    rcx, cl
       mov      dword ptr [rbp-0x30], ecx
       jmp      G_M000_IG27
 
G_M000_IG03:
       test     rcx, rcx
       jl       G_M000_IG50
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG49
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      G_M000_IG11
 
G_M000_IG04:
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:ThrowUnboundGlobal(System.String)]
 
G_M000_IG05:
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp+0x60], rax
       mov      rcx, 0xD1FFAB1E
       cmp      rax, gword ptr [rcx]
       jne      SHORT G_M000_IG12
 
G_M000_IG06:
       test     rbx, rbx
       je       SHORT G_M000_IG12
 
G_M000_IG07:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG12
 
G_M000_IG08:
       mov      rcx, qword ptr [rbx+0x08]
       mov      rdx, rcx
       add      rcx, rdx
       mov      qword ptr [rbp+0x40], rcx
       xor      rdx, rcx
       jl       SHORT G_M000_IG12
 
G_M000_IG09:
       call     [RyuPy.Runtime.PyTraceback:CheckDepth()]
       mov      rax, qword ptr [rbp+0x40]
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      G_M000_IG03
 
G_M000_IG10:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp+0x40]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG11:
       mov      ecx, dword ptr [rbp-0x30]
       jmp      G_M000_IG27
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp-0x68], rcx
       mov      rdx, gword ptr [rcx+0x18]
       test     rdx, rdx
       je       SHORT G_M000_IG14
 
G_M000_IG13:
       cmp      dword ptr [rdx+0x08], 1
       mov      gword ptr [rbp-0x48], rdx
       je       SHORT G_M000_IG15
 
G_M000_IG14:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      rdi, gword ptr [rbp-0x68]
       lea      rdi, bword ptr [rdi+0x18]
       mov      gword ptr [rbp-0x48], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG15:
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x40], rax
       mov      rdi, rax
       mov      rdx, rbx
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rax, gword ptr [rbp+0x60]
       test     rax, rax
       je       SHORT G_M000_IG19
 
G_M000_IG16:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rax], r8
       je       G_M000_IG35
 
G_M000_IG17:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rax], r8
       je       G_M000_IG34
 
G_M000_IG18:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rax], r8
       je       G_M000_IG33
 
G_M000_IG19:
       xor      r9d, r9d
 
G_M000_IG20:
       mov      dword ptr [rbp-0x2C], r9d
       mov      r8, 0xD1FFAB1E
       mov      r10, gword ptr [r8]
       mov      gword ptr [rbp-0x60], r10
       lea      r8, bword ptr [r10+0x50]
       cmp      byte  ptr [r8+0x08], 0
       jne      G_M000_IG43
 
G_M000_IG21:
       lea      r8, bword ptr [r10+0x60]
       cmp      byte  ptr [r8+0x08], 0
       jne      G_M000_IG44
 
G_M000_IG22:
       lea      r8, bword ptr [r10+0x70]
       cmp      byte  ptr [r8+0x08], 0
       jne      G_M000_IG45
 
G_M000_IG23:
       lea      r8, bword ptr [r10+0x80]
       cmp      byte  ptr [r8+0x08], 0
       jne      G_M000_IG46
 
G_M000_IG24:
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      rcx, r8
       mov      edi, r9d
       mov      rsi, rax
       mov      rdx, gword ptr [rbp-0x40]
       call     [RyuPy.Runtime.PyPic:InvokeCached(byte,System.Object,System.Object[],System.Object[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x50], rax
       cmp      dword ptr [rbp-0x2C], 0
       je       SHORT G_M000_IG25
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       add      rdi, 17
       movzx    rsi, byte  ptr [rdi]
       lea      edx, [rsi+0x01]
       mov      byte  ptr [rdi], dl
       and      esi, 3
       add      esi, 4
       mov      edi, esi
       shl      rdi, 4
       mov      rsi, gword ptr [rbp-0x60]
       lea      rdx, bword ptr [rsi+rdi+0x10]
       mov      bword ptr [rbp-0x58], rdx
       mov      rsi, gword ptr [rbp+0x60]
       mov      rdi, rdx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rsi, bword ptr [rbp-0x58]
       mov      byte  ptr [rsi+0x08], 1
       mov      edi, dword ptr [rbp-0x2C]
       mov      byte  ptr [rsi+0x09], dil
 
G_M000_IG25:
       mov      rax, gword ptr [rbp-0x50]
 
G_M000_IG26:
       mov      ecx, dword ptr [rbp-0x30]
 
G_M000_IG27:
       add      r15, r13
       jo       G_M000_IG50
       mov      edi, ecx
       cmp      r15, r14
       setl     dl
       movzx    rdx, dl
       and      edx, edi
       cmp      r15, r14
       setg     sil
       movzx    rsi, sil
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, esi
       or       edi, edx
       je       G_M000_IG36
 
G_M000_IG28:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG31
 
G_M000_IG29:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rdx, rax
       dec      edi
       mov      esi, edi
       cmp      esi, dword ptr [rdx+0x08]
       jae      G_M000_IG49
       cmp      dword ptr [rdx+4*rsi+0x10], 13
       je       SHORT G_M000_IG31
 
G_M000_IG30:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG49
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 13
 
G_M000_IG31:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x38], rax
       test     rax, rax
       je       G_M000_IG04
 
G_M000_IG32:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG05
       jmp      G_M000_IG04
 
G_M000_IG33:
       mov      r9d, 3
       jmp      G_M000_IG20
 
G_M000_IG34:
       mov      r9d, 2
       jmp      G_M000_IG20
 
G_M000_IG35:
       mov      r9d, 1
       jmp      G_M000_IG20
 
G_M000_IG36:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG39
 
G_M000_IG37:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       lea      esi, [rdi-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      G_M000_IG49
       cmp      dword ptr [rdx+4*r8+0x10], 14
       je       SHORT G_M000_IG39
 
G_M000_IG38:
       mov      edx, esi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG49
       mov      dword ptr [rcx+4*rdx+0x10], 14
 
G_M000_IG39:
       test     r12, r12
       jle      SHORT G_M000_IG42
 
G_M000_IG40:
       movsxd   rcx, edi
       cmp      r12, rcx
       jg       SHORT G_M000_IG42
 
G_M000_IG41:
       dec      r12d
       mov      dword ptr [(reloc)], r12d
 
G_M000_IG42:
       add      rsp, 376
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG43:
       cmp      gword ptr [r8], rax
       jne      G_M000_IG21
       jmp      SHORT G_M000_IG47
 
G_M000_IG44:
       cmp      gword ptr [r8], rax
       jne      G_M000_IG22
       jmp      SHORT G_M000_IG47
 
G_M000_IG45:
       cmp      gword ptr [r8], rax
       jne      G_M000_IG23
       jmp      SHORT G_M000_IG47
 
G_M000_IG46:
       cmp      gword ptr [r8], rax
       jne      G_M000_IG24
       mov      r10, gword ptr [rbp-0x60]
 
G_M000_IG47:
       cmp      r9d, 1
       jne      SHORT G_M000_IG48
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_CHKCASTCLASS]
       mov      r9, gword ptr [rax+0x18]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      rsi, rax
       mov      rdx, gword ptr [rbp-0x40]
       mov      rdi, gword ptr [r9+0x08]
       call     [r9+0x18]RyuPy.Runtime.PyCall:Invoke(RyuPy.Runtime.PyFunction,System.Object[],System.String[],System.Object[]):System.Object:this
       jmp      G_M000_IG26
 
G_M000_IG48:
       cmp      r9d, 3
       jne      G_M000_IG24
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_CHKCASTCLASS]
       mov      rdi, rax
       mov      rcx, 0xD1FFAB1E
       mov      r8, gword ptr [rcx]
       mov      rcx, r8
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rsi, gword ptr [rbp-0x40]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyClass:Invoke(System.Object[],System.String[],System.Object[]):System.Object:this]
       jmp      G_M000_IG26
 
G_M000_IG49:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG50:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1193

; Assembly listing for method RyuPy.Generated.Module:m0_repeat(System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x56
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 10 inlinees with PGO data; 11 single block inlinees; 2 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 112
       mov      qword ptr [rsp+0x198], r15
       mov      qword ptr [rsp+0x190], r14
       mov      qword ptr [rsp+0x188], r13
       mov      qword ptr [rsp+0x180], r12
       mov      qword ptr [rsp+0x178], rbx
       lea      rbp, [rsp+0x70]
       mov      rbx, gword ptr [rbp+0xF8]
       mov      rax, gword ptr [rbp+0xF0]
       mov      r12, qword ptr [rbp+0xE0]
       mov      r15, qword ptr [rbp+0xB8]
       mov      r14, qword ptr [rbp+0xB0]
       mov      r13, qword ptr [rbp+0xA8]
 
G_M000_IG02:
       test     r13, r13
       setg     cl
       movzx    rcx, cl
       mov      dword ptr [rbp-0x30], ecx
       jmp      G_M000_IG28
 
G_M000_IG03:
       test     byte  ptr [(reloc)], 1
       je       G_M000_IG48
 
G_M000_IG04:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       jmp      G_M000_IG35
 
G_M000_IG05:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdx
       jne      G_M000_IG37
 
G_M000_IG06:
       mov      rdx, qword ptr [rbx+0x08]
       mov      rdi, rdx
       add      rdx, rdi
       mov      qword ptr [rbp+0x40], rdx
       xor      rdi, rdx
       jl       G_M000_IG37
 
G_M000_IG07:
       call     [RyuPy.Runtime.PyTraceback:CheckDepth()]
       mov      rax, qword ptr [rbp+0x40]
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG10
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp+0x40]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG09:
       jmp      G_M000_IG27
 
G_M000_IG10:
       test     rcx, rcx
       jl       G_M000_IG61
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG60
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG09
 
G_M000_IG11:
       mov      rcx, rax
       test     rcx, rcx
       je       SHORT G_M000_IG14
 
G_M000_IG12:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rcx], r8
       je       SHORT G_M000_IG14
 
G_M000_IG13:
       xor      rcx, rcx
 
G_M000_IG14:
       test     rcx, rcx
       jne      SHORT G_M000_IG21
 
G_M000_IG15:
       mov      rcx, rax
       test     rcx, rcx
       je       SHORT G_M000_IG18
 
G_M000_IG16:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rcx], r8
       je       SHORT G_M000_IG18
 
G_M000_IG17:
       xor      rcx, rcx
 
G_M000_IG18:
       test     rcx, rcx
       jne      SHORT G_M000_IG20
 
G_M000_IG19:
       xor      r9d, r9d
       jmp      SHORT G_M000_IG22
 
G_M000_IG20:
       mov      r9d, 3
       jmp      SHORT G_M000_IG22
 
G_M000_IG21:
       mov      r9d, 2
 
G_M000_IG22:
       mov      dword ptr [rbp-0x2C], r9d
       mov      rcx, 0xD1FFAB1E
       mov      r10, gword ptr [rcx]
       mov      gword ptr [rbp-0x68], r10
       lea      rcx, bword ptr [r10+0x50]
       cmp      byte  ptr [rcx+0x08], 0
       je       G_M000_IG41
 
G_M000_IG23:
       cmp      gword ptr [rcx], rax
       jne      G_M000_IG41
 
G_M000_IG24:
       cmp      r9d, 1
       jne      G_M000_IG57
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG26
 
G_M000_IG25:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG56
 
G_M000_IG26:
       mov      rax, gword ptr [rsi+0x18]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      rdx, gword ptr [rbp-0x40]
       mov      rdi, gword ptr [rax+0x08]
       call     [rax+0x18]RyuPy.Runtime.PyCall:Invoke(RyuPy.Runtime.PyFunction,System.Object[],System.String[],System.Object[]):System.Object:this
 
G_M000_IG27:
       add      r15, r13
       jo       G_M000_IG61
       mov      ecx, dword ptr [rbp-0x30]
 
G_M000_IG28:
       mov      edi, ecx
       cmp      r15, r14
       setl     sil
       movzx    rsi, sil
       and      esi, edi
       cmp      r15, r14
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, esi
       je       G_M000_IG43
 
G_M000_IG29:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG31
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       dec      edi
       mov      edx, edi
       cmp      edx, dword ptr [rax+0x08]
       jae      G_M000_IG60
       cmp      dword ptr [rax+4*rdx+0x10], 13
       je       SHORT G_M000_IG31
 
G_M000_IG30:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG60
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 13
 
G_M000_IG31:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x38], rax
       test     rax, rax
       je       G_M000_IG49
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG34
 
G_M000_IG32:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [System.String:Equals(System.String,System.String):bool]
       test     eax, eax
       jne      G_M000_IG03
 
G_M000_IG33:
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:ThrowUnboundGlobal(System.String)]
 
G_M000_IG34:
       mov      rax, gword ptr [rbp-0x38]
 
G_M000_IG35:
       mov      gword ptr [rbp+0x60], rax
       mov      rdx, 0xD1FFAB1E
       cmp      rax, gword ptr [rdx]
       jne      SHORT G_M000_IG37
 
G_M000_IG36:
       test     rbx, rbx
       jne      G_M000_IG05
 
G_M000_IG37:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       mov      gword ptr [rbp-0x60], rdx
       mov      r8, gword ptr [rdx+0x18]
       test     r8, r8
       je       G_M000_IG50
       cmp      dword ptr [r8+0x08], 1
       jne      G_M000_IG50
 
G_M000_IG38:
       mov      gword ptr [rbp-0x40], r8
       mov      rdi, r8
       mov      rdx, rbx
       xor      esi, esi
       call     CORINFO_HELP_ARRADDR_ST
       mov      rax, gword ptr [rbp+0x60]
       test     rax, rax
       je       G_M000_IG11
 
G_M000_IG39:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG11
 
G_M000_IG40:
       mov      r9d, 1
       jmp      G_M000_IG22
 
G_M000_IG41:
       lea      rcx, bword ptr [r10+0x60]
       cmp      byte  ptr [rcx+0x08], 0
       je       G_M000_IG51
 
G_M000_IG42:
       cmp      gword ptr [rcx], rax
       jne      G_M000_IG51
       jmp      G_M000_IG24
 
G_M000_IG43:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG45
       mov      rcx, 0xD1FFAB1E
       mov      rsi, gword ptr [rcx]
       mov      rcx, rsi
       lea      edx, [rdi-0x01]
       mov      r8d, edx
       cmp      r8d, dword ptr [rcx+0x08]
       jae      G_M000_IG60
       cmp      dword ptr [rcx+4*r8+0x10], 14
       je       SHORT G_M000_IG45
 
G_M000_IG44:
       mov      rcx, rsi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG60
       mov      edx, edx
       mov      dword ptr [rcx+4*rdx+0x10], 14
 
G_M000_IG45:
       test     r12, r12
       jle      SHORT G_M000_IG47
 
G_M000_IG46:
       movsxd   rcx, edi
       cmp      r12, rcx
       jg       SHORT G_M000_IG47
       dec      r12d
       mov      dword ptr [(reloc)], r12d
 
G_M000_IG47:
       add      rsp, 376
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG48:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_GET_GCSTATIC_BASE
       jmp      G_M000_IG04
 
G_M000_IG49:
       jmp      G_M000_IG32
 
G_M000_IG50:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      rdi, gword ptr [rbp-0x60]
       lea      rdi, bword ptr [rdi+0x18]
       mov      gword ptr [rbp-0x48], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
       mov      r8, gword ptr [rbp-0x48]
       jmp      G_M000_IG38
 
G_M000_IG51:
       lea      rcx, bword ptr [r10+0x70]
       cmp      byte  ptr [rcx+0x08], 0
       je       SHORT G_M000_IG53
 
G_M000_IG52:
       cmp      gword ptr [rcx], rax
       je       G_M000_IG24
 
G_M000_IG53:
       lea      rcx, bword ptr [r10+0x80]
       cmp      byte  ptr [rcx+0x08], 0
       je       SHORT G_M000_IG55
 
G_M000_IG54:
       cmp      gword ptr [rcx], rax
       jne      SHORT G_M000_IG55
       mov      r10, gword ptr [rbp-0x68]
       jmp      G_M000_IG24
 
G_M000_IG55:
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      edi, r9d
       mov      rsi, rax
       mov      rdx, gword ptr [rbp-0x40]
       call     [RyuPy.Runtime.PyPic:InvokeCached(byte,System.Object,System.Object[],System.Object[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x50], rax
       cmp      dword ptr [rbp-0x2C], 0
       je       G_M000_IG59
       jmp      SHORT G_M000_IG58
 
G_M000_IG56:
       mov      rsi, rax
       call     CORINFO_HELP_CHKCASTCLASS
       int3     
 
G_M000_IG57:
       cmp      r9d, 3
       jne      SHORT G_M000_IG55
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_CHKCASTCLASS
       mov      rdi, rax
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rsi, gword ptr [rbp-0x40]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyClass:Invoke(System.Object[],System.String[],System.Object[]):System.Object:this]
       jmp      G_M000_IG27
 
G_M000_IG58:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       add      rdi, 17
       movzx    rsi, byte  ptr [rdi]
       lea      edx, [rsi+0x01]
       mov      byte  ptr [rdi], dl
       and      esi, 3
       add      esi, 4
       mov      edi, esi
       shl      rdi, 4
       mov      rsi, gword ptr [rbp-0x68]
       lea      rdx, bword ptr [rsi+rdi+0x10]
       mov      bword ptr [rbp-0x58], rdx
       mov      rsi, gword ptr [rbp+0x60]
       mov      rdi, rdx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rsi, bword ptr [rbp-0x58]
       mov      byte  ptr [rsi+0x08], 1
       mov      edi, dword ptr [rbp-0x2C]
       mov      byte  ptr [rsi+0x09], dil
 
G_M000_IG59:
       mov      rax, gword ptr [rbp-0x50]
       jmp      G_M000_IG27
 
G_M000_IG60:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG61:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1324

