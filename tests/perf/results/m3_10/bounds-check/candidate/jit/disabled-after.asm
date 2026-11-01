; Assembly listing for method RyuPy.Generated.Module:m0_matmul(System.Object,System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 12 inlinees with PGO data; 37 single block inlinees; 56 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 504
       lea      rbp, [rsp+0x220]
       xor      eax, eax
       mov      qword ptr [rbp-0x168], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x160], zmm8
       vmovdqu32 zmmword ptr [rbp-0x120], zmm8
       vmovdqu32 zmmword ptr [rbp-0xF0], zmm8
       mov      qword ptr [rbp-0xB0], rax
       mov      r14, rdi
       mov      rbx, rsi
       mov      r15, rdx
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r13, rax
       mov      edi, 16
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x1B8], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, rax
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       lea      rdi, bword ptr [r12+0x08]
       mov      rsi, gword ptr [rbp-0x1B8]
       call     CORINFO_HELP_ASSIGN_REF
       mov      edi, 17
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x30], rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp-0x30]
       mov      qword ptr [rbp-0x40], rax
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:
       mov      edi, 25
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, gword ptr [rbp-0x190]
       mov      rsi, r12
       mov      edi, 7
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       mov      rax, qword ptr [rbp-0x38]
       add      rax, 1
       jo       G_M000_IG180
       mov      rcx, rax
       mov      rax, qword ptr [rbp-0x40]
 
G_M000_IG04:
       cmp      rcx, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG168
 
G_M000_IG05:
       mov      qword ptr [rbp-0x38], rcx
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG07
 
G_M000_IG06:
       test     rdi, rdi
       jl       G_M000_IG180
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG179
       mov      rdx, gword ptr [rdx+8*rdi+0x10]
       mov      gword ptr [rbp-0x1C0], rdx
       jmp      SHORT G_M000_IG08
 
G_M000_IG07:
       mov      rsi, 0xD1FFAB1E
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rax, qword ptr [rbp-0x38]
       mov      qword ptr [rdx+0x08], rax
       mov      gword ptr [rbp-0x1C0], rdx
 
G_M000_IG08:
       mov      edi, 18
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r14
       mov      rsi, gword ptr [rbp-0x1C0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x188], rax
       mov      edi, 19
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x1C8], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x1D0], rax
       test     byte  ptr [(reloc)], 1
       je       G_M000_IG171
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, gword ptr [rdi]
       mov      rax, gword ptr [rbp-0x1D0]
       lea      rdi, bword ptr [rax+0x08]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x1C8]
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp-0x1D0]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x1C8]
       mov      gword ptr [rbp-0x190], rax
       mov      edi, 20
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x48], rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp-0x48]
       mov      qword ptr [rbp-0x58], rax
       jmp      G_M000_IG164
 
G_M000_IG10:
       mov      rsi, 0xD1FFAB1E
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rax, qword ptr [rbp-0x50]
       mov      qword ptr [rsi+0x08], rax
       mov      rcx, rsi
 
G_M000_IG11:
       mov      gword ptr [rbp-0x198], rcx
       mov      edi, 21
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x1A0], rax
       mov      edi, 22
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x60], rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp-0x60]
       mov      qword ptr [rbp-0x70], rax
       vxorps   xmm0, xmm0, xmm0
       mov      rdx, gword ptr [rbp-0x1A0]
       test     rdx, rdx
       je       G_M000_IG20
 
G_M000_IG12:
       mov      rdi, qword ptr [rdx]
       mov      rsi, 0xD1FFAB1E
       cmp      rdi, rsi
       jne      G_M000_IG19
 
G_M000_IG13:
       mov      r8d, 1
       mov      gword ptr [rbp-0x1A0], rdx
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x78], xmm0
       jmp      SHORT G_M000_IG16
 
G_M000_IG14:
       mov      ecx, 1
       mov      gword ptr [rbp-0x1A0], rax
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x78], xmm0
       mov      dword ptr [rbp-0x7C], ecx
       mov      rsi, gword ptr [rbp-0x1A0]
       mov      r9, gword ptr [rbp-0x188]
 
G_M000_IG15:
       mov      rax, qword ptr [rbp-0x68]
       add      rax, 1
       jo       G_M000_IG180
       mov      gword ptr [rbp-0x1A0], rsi
       mov      rcx, rax
       mov      rax, qword ptr [rbp-0x70]
       mov      r8d, dword ptr [rbp-0x7C]
 
G_M000_IG16:
       cmp      rcx, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       jne      SHORT G_M000_IG21
 
G_M000_IG17:
       mov      dword ptr [rbp-0x7C], r8d
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x1A0]
       test     rax, rax
       sete     dil
       movzx    rdi, dil
       test     dword ptr [rbp-0x7C], edi
       jne      G_M000_IG158
 
G_M000_IG18:
       mov      rcx, rax
       mov      rsi, gword ptr [rbp-0x190]
       mov      edi, 10
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       jmp      G_M000_IG163
 
G_M000_IG19:
       xor      r8d, r8d
       mov      gword ptr [rbp-0x1A0], rdx
       vmovsd   qword ptr [rbp-0x78], xmm0
       jmp      SHORT G_M000_IG16
 
G_M000_IG20:
       xor      r8d, r8d
       mov      dword ptr [rbp-0x7C], r8d
       mov      gword ptr [rbp-0x1A0], rdx
       vmovsd   qword ptr [rbp-0x78], xmm0
       mov      r8d, dword ptr [rbp-0x7C]
       jmp      G_M000_IG16
 
G_M000_IG21:
       mov      qword ptr [rbp-0x68], rcx
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG23
 
G_M000_IG22:
       test     rdi, rdi
       jl       G_M000_IG180
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG179
       mov      rsi, gword ptr [rsi+8*rdi+0x10]
       mov      gword ptr [rbp-0x1D8], rsi
       mov      dword ptr [rbp-0x7C], r8d
       jmp      SHORT G_M000_IG24
 
G_M000_IG23:
       mov      dword ptr [rbp-0x7C], r8d
       mov      rsi, 0xD1FFAB1E
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rax, qword ptr [rbp-0x68]
       mov      qword ptr [rsi+0x08], rax
       mov      gword ptr [rbp-0x1D8], rsi
 
G_M000_IG24:
       mov      edi, 23
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       cmp      dword ptr [rbp-0x7C], 0
       je       G_M000_IG94
 
G_M000_IG25:
       mov      rcx, gword ptr [rbp-0x188]
       test     rcx, rcx
       je       G_M000_IG93
 
G_M000_IG26:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       je       SHORT G_M000_IG28
 
G_M000_IG27:
       mov      r9, rcx
       jmp      G_M000_IG95
 
G_M000_IG28:
       mov      rdx, gword ptr [rbp-0x1D8]
       test     rdx, rdx
       je       G_M000_IG92
 
G_M000_IG29:
       mov      rsi, 0xD1FFAB1E
       mov      qword ptr [rbp-0x180], rsi
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG91
 
G_M000_IG30:
       mov      gword ptr [rbp-0x1D8], rdx
       mov      r8, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x158], r8
       mov      rdi, rcx
       call     [RyuPy.Runtime.PyList:get_Count():int:this]
       mov      rdi, qword ptr [rbp-0x158]
       test     rdi, rdi
       jge      SHORT G_M000_IG31
       movsxd   rcx, eax
       add      rcx, rdi
       jmp      SHORT G_M000_IG32
 
G_M000_IG31:
       mov      rcx, rdi
 
G_M000_IG32:
       movsxd   rdi, eax
       cmp      rcx, rdi
       jb       SHORT G_M000_IG34
 
G_M000_IG33:
       mov      r9, gword ptr [rbp-0x188]
       jmp      G_M000_IG95
 
G_M000_IG34:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x168], xmm0
       mov      rax, gword ptr [rbp-0x188]
       mov      rdi, gword ptr [rax+0x10]
       test     rdi, rdi
       jne      SHORT G_M000_IG35
       mov      rdi, gword ptr [rax+0x08]
       cmp      ecx, dword ptr [rdi+0x10]
       jb       G_M000_IG172
       jmp      G_M000_IG170
 
G_M000_IG35:
       cmp      ecx, dword ptr [rdi+0x10]
       jae      G_M000_IG170
       mov      rdi, gword ptr [rdi+0x08]
       cmp      ecx, dword ptr [rdi+0x08]
       jae      G_M000_IG179
       mov      ecx, ecx
       shl      rcx, 4
 
G_M000_IG36:
       vmovdqu  xmm0, xmmword ptr [rdi+rcx+0x10]
       vmovdqu  xmmword ptr [rbp-0x168], xmm0
 
G_M000_IG37:
       lea      rdi, [rbp-0x168]
       call     [RyuPy.Runtime.PyNumericSlot:get_Number():double:this]
 
G_M000_IG38:
       mov      ecx, 1
 
G_M000_IG39:
       test     ecx, ecx
       je       G_M000_IG90
       test     rbx, rbx
       je       SHORT G_M000_IG47
 
G_M000_IG40:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rsi
       jne      SHORT G_M000_IG47
 
G_M000_IG41:
       mov      rax, gword ptr [rbp-0x1D8]
       mov      rsi, qword ptr [rax+0x08]
       mov      rdi, gword ptr [rbx+0x08]
       mov      rcx, rdi
       test     rcx, rcx
       je       SHORT G_M000_IG42
       mov      ecx, dword ptr [rcx+0x10]
       jmp      SHORT G_M000_IG43
 
G_M000_IG42:
       mov      rcx, gword ptr [rbx+0x10]
       mov      ecx, dword ptr [rcx+0x10]
 
G_M000_IG43:
       test     rsi, rsi
       jge      SHORT G_M000_IG44
       movsxd   rdx, ecx
       add      rdx, rsi
       jmp      SHORT G_M000_IG45
 
G_M000_IG44:
       mov      rdx, rsi
 
G_M000_IG45:
       movsxd   rsi, ecx
       cmp      rdx, rsi
       jb       SHORT G_M000_IG48
 
G_M000_IG46:
       mov      gword ptr [rbp-0x1D8], rax
 
G_M000_IG47:
       xor      rcx, rcx
       jmp      SHORT G_M000_IG53
 
G_M000_IG48:
       test     rdi, rdi
       je       SHORT G_M000_IG51
       cmp      edx, dword ptr [rdi+0x10]
       jae      G_M000_IG170
       mov      rsi, gword ptr [rdi+0x08]
       cmp      edx, dword ptr [rsi+0x08]
       jae      G_M000_IG179
       mov      edi, edx
       mov      rcx, gword ptr [rsi+8*rdi+0x10]
       test     rcx, rcx
       je       SHORT G_M000_IG51
 
G_M000_IG49:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rsi
       jne      SHORT G_M000_IG51
 
G_M000_IG50:
       jmp      SHORT G_M000_IG52
 
G_M000_IG51:
       xor      rcx, rcx
 
G_M000_IG52:
       mov      gword ptr [rbp-0x1D8], rax
 
G_M000_IG53:
       test     rcx, rcx
       je       G_M000_IG89
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rsi
       je       SHORT G_M000_IG55
 
G_M000_IG54:
       mov      r9, gword ptr [rbp-0x188]
       jmp      G_M000_IG95
 
G_M000_IG55:
       mov      rdx, gword ptr [rbp-0x198]
       test     rdx, rdx
       je       G_M000_IG88
 
G_M000_IG56:
       mov      r8, qword ptr [rbp-0x180]
       cmp      qword ptr [rdx], r8
       jne      G_M000_IG87
 
G_M000_IG57:
       mov      rsi, qword ptr [rdx+0x08]
       mov      r8, gword ptr [rcx+0x08]
       mov      gword ptr [rbp-0x218], r8
       mov      rdi, r8
       test     rdi, rdi
       je       SHORT G_M000_IG58
       mov      edi, dword ptr [rdi+0x10]
       jmp      SHORT G_M000_IG59
 
G_M000_IG58:
       mov      rdi, gword ptr [rcx+0x10]
       mov      edi, dword ptr [rdi+0x10]
 
G_M000_IG59:
       test     rsi, rsi
       jge      SHORT G_M000_IG60
       movsxd   r9, edi
       add      r9, rsi
       jmp      SHORT G_M000_IG61
 
G_M000_IG60:
       mov      r9, rsi
 
G_M000_IG61:
       movsxd   rsi, edi
       cmp      r9, rsi
       jb       SHORT G_M000_IG63
 
G_M000_IG62:
       mov      r9, gword ptr [rbp-0x188]
       jmp      G_M000_IG95
 
G_M000_IG63:
       mov      gword ptr [rbp-0x1F0], rcx
       mov      rsi, gword ptr [rcx+0x10]
       test     rsi, rsi
       jne      SHORT G_M000_IG67
       mov      rsi, r8
       cmp      r9d, dword ptr [rsi+0x10]
       jae      G_M000_IG170
       mov      rsi, gword ptr [rsi+0x08]
       cmp      r9d, dword ptr [rsi+0x08]
       jae      G_M000_IG179
       mov      r9d, r9d
       mov      rsi, gword ptr [rsi+8*r9+0x10]
       test     rsi, rsi
       je       SHORT G_M000_IG66
 
G_M000_IG64:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG66
 
G_M000_IG65:
       mov      r8, gword ptr [rbp-0x218]
       jmp      SHORT G_M000_IG68
 
G_M000_IG66:
       xor      edi, edi
       mov      r8, gword ptr [rbp-0x218]
       jmp      SHORT G_M000_IG69
 
G_M000_IG67:
       cmp      r9d, dword ptr [rsi+0x10]
       jae      G_M000_IG170
       mov      rdi, gword ptr [rsi+0x08]
       cmp      r9d, dword ptr [rdi+0x08]
       jae      G_M000_IG179
       mov      esi, r9d
       shl      rsi, 4
       mov      rsi, gword ptr [rdi+rsi+0x10]
       test     rsi, rsi
       je       G_M000_IG178
 
G_M000_IG68:
       mov      edi, 1
 
G_M000_IG69:
       test     edi, edi
       je       G_M000_IG86
       mov      rax, gword ptr [rbp-0x1D8]
       mov      rsi, qword ptr [rax+0x08]
       test     rsi, rsi
       jge      SHORT G_M000_IG72
       mov      r9, gword ptr [rbp-0x188]
       mov      rdi, gword ptr [r9+0x08]
       test     rdi, rdi
       je       SHORT G_M000_IG70
       mov      edi, dword ptr [rdi+0x10]
       jmp      SHORT G_M000_IG71
 
G_M000_IG70:
       mov      rdi, gword ptr [r9+0x10]
       mov      edi, dword ptr [rdi+0x10]
 
G_M000_IG71:
       movsxd   rdi, edi
       add      rdi, rsi
       jmp      SHORT G_M000_IG73
 
G_M000_IG72:
       mov      rdi, rsi
 
G_M000_IG73:
       mov      r9, gword ptr [rbp-0x188]
       mov      rax, gword ptr [r9+0x10]
       test     rax, rax
       jne      SHORT G_M000_IG76
       mov      rsi, gword ptr [r9+0x08]
       cmp      edi, dword ptr [rsi+0x10]
       jae      G_M000_IG170
       mov      r9, gword ptr [rbp-0x188]
       mov      rsi, gword ptr [rsi+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG179
       mov      edi, edi
       mov      rax, gword ptr [rsi+8*rdi+0x10]
       mov      gword ptr [rbp-0x1E0], rax
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG75
 
G_M000_IG74:
       mov      rsi, rax
       call     [CORINFO_HELP_UNBOX]
       mov      rax, gword ptr [rbp-0x1E0]
       mov      r8, gword ptr [rbp-0x218]
       mov      r9, gword ptr [rbp-0x188]
 
G_M000_IG75:
       vmovsd   xmm0, qword ptr [rax+0x08]
       jmp      SHORT G_M000_IG77
 
G_M000_IG76:
       mov      esi, edi
       cmp      esi, dword ptr [rax+0x10]
       jae      G_M000_IG170
       mov      rdi, gword ptr [rax+0x08]
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG179
       shl      rsi, 4
       lea      rsi, bword ptr [rdi+rsi+0x10]
       mov      rdi, gword ptr [rsi]
       mov      rsi, qword ptr [rsi+0x08]
       test     rdi, rdi
       je       G_M000_IG176
       vmovq    xmm0, rsi
 
G_M000_IG77:
       vmovsd   qword ptr [rbp-0x88], xmm0
       mov      rcx, gword ptr [rbp-0x1F0]
       mov      rdx, gword ptr [rbp-0x198]
       mov      rsi, qword ptr [rdx+0x08]
       test     rsi, rsi
       jge      SHORT G_M000_IG80
       mov      rdi, r8
       test     rdi, rdi
       je       SHORT G_M000_IG78
       mov      edi, dword ptr [rdi+0x10]
       jmp      SHORT G_M000_IG79
 
G_M000_IG78:
       mov      rdi, gword ptr [rcx+0x10]
       mov      edi, dword ptr [rdi+0x10]
 
G_M000_IG79:
       movsxd   rdi, edi
       add      rdi, rsi
       jmp      SHORT G_M000_IG81
 
G_M000_IG80:
       mov      rdi, rsi
 
G_M000_IG81:
       mov      rax, gword ptr [rcx+0x10]
       test     rax, rax
       jne      SHORT G_M000_IG84
       cmp      edi, dword ptr [r8+0x10]
       jae      G_M000_IG170
       mov      rsi, gword ptr [r8+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG179
       mov      edi, edi
       mov      rax, gword ptr [rsi+8*rdi+0x10]
       mov      gword ptr [rbp-0x1E8], rax
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       je       SHORT G_M000_IG83
 
G_M000_IG82:
       mov      rsi, rax
       call     [CORINFO_HELP_UNBOX]
       mov      rax, gword ptr [rbp-0x1E8]
       mov      r9, gword ptr [rbp-0x188]
 
G_M000_IG83:
       vmovsd   xmm1, qword ptr [rax+0x08]
       jmp      SHORT G_M000_IG85
 
G_M000_IG84:
       cmp      edi, dword ptr [rax+0x10]
       jae      G_M000_IG170
       mov      rsi, gword ptr [rax+0x08]
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG179
       mov      edi, edi
       shl      rdi, 4
       lea      rdi, bword ptr [rsi+rdi+0x10]
       mov      rsi, gword ptr [rdi]
       mov      rdi, qword ptr [rdi+0x08]
       test     rsi, rsi
       je       G_M000_IG177
       vmovq    xmm1, rdi
 
G_M000_IG85:
       jmp      G_M000_IG100
 
G_M000_IG86:
       mov      r9, gword ptr [rbp-0x188]
       jmp      SHORT G_M000_IG95
 
G_M000_IG87:
       mov      r9, gword ptr [rbp-0x188]
       jmp      SHORT G_M000_IG95
 
G_M000_IG88:
       mov      r9, gword ptr [rbp-0x188]
       jmp      SHORT G_M000_IG95
 
G_M000_IG89:
       mov      r9, gword ptr [rbp-0x188]
       jmp      SHORT G_M000_IG95
 
G_M000_IG90:
       mov      r9, gword ptr [rbp-0x188]
       jmp      SHORT G_M000_IG95
 
G_M000_IG91:
       mov      gword ptr [rbp-0x1D8], rdx
       mov      r9, rcx
       jmp      SHORT G_M000_IG95
 
G_M000_IG92:
       mov      gword ptr [rbp-0x1D8], rdx
       mov      r9, rcx
       jmp      SHORT G_M000_IG95
 
G_M000_IG93:
       mov      r9, rcx
       jmp      SHORT G_M000_IG95
 
G_M000_IG94:
       mov      r9, gword ptr [rbp-0x188]
 
G_M000_IG95:
       mov      rdi, r9
       mov      rsi, gword ptr [rbp-0x1D8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x1A8], rax
       mov      rdi, rbx
       mov      rsi, gword ptr [rbp-0x1D8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdi, rax
       mov      rsi, gword ptr [rbp-0x198]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x1B0], rax
       mov      ecx, dword ptr [rbp-0x7C]
       test     ecx, ecx
       je       SHORT G_M000_IG101
       mov      rdx, gword ptr [rbp-0x1A8]
       test     rdx, rdx
       je       SHORT G_M000_IG102
 
G_M000_IG96:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG102
 
G_M000_IG97:
       test     rax, rax
       je       SHORT G_M000_IG102
 
G_M000_IG98:
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG102
 
G_M000_IG99:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   xmm1, qword ptr [rax+0x08]
       mov      dword ptr [rbp-0x7C], ecx
       vmovsd   qword ptr [rbp-0x88], xmm0
       mov      r9, gword ptr [rbp-0x188]
 
G_M000_IG100:
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x78]
       vmovsd   qword ptr [rbp-0x78], xmm0
       xor      rdi, rdi
       xor      rsi, rsi
       jmp      G_M000_IG15
 
G_M000_IG101:
       mov      rdx, gword ptr [rbp-0x1A8]
 
G_M000_IG102:
       mov      rsi, gword ptr [rbp-0x1A0]
       test     rsi, rsi
       sete     dil
       movzx    rdi, dil
       test     ecx, edi
       mov      gword ptr [rbp-0x1A0], rsi
       je       SHORT G_M000_IG103
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       vmovsd   xmm0, qword ptr [rbp-0x78]
       vmovsd   qword ptr [rsi+0x08], xmm0
       mov      gword ptr [rbp-0x1A0], rsi
 
G_M000_IG103:
       mov      rcx, gword ptr [rbp-0x1A8]
       test     rcx, rcx
       je       G_M000_IG131
 
G_M000_IG104:
       mov      r8, 0xD1FFAB1E
       mov      rsi, r8
       cmp      qword ptr [rcx], rsi
       jne      G_M000_IG124
 
G_M000_IG105:
       mov      rdx, qword ptr [rcx+0x08]
       mov      r8, gword ptr [rbp-0x1B0]
       test     r8, r8
       jne      SHORT G_M000_IG107
 
G_M000_IG106:
       jmp      G_M000_IG131
 
G_M000_IG107:
       cmp      qword ptr [r8], rsi
       jne      G_M000_IG121
 
G_M000_IG108:
       mov      rcx, qword ptr [r8+0x08]
       mov      qword ptr [rbp-0xA8], rcx
       lea      rdi, [rbp-0xC8]
       mulx     r9, r8, rcx
       mov      qword ptr [rdi], r8
       mov      r8, qword ptr [rbp-0xC8]
       mov      qword ptr [rbp-0xC0], r8
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, rcx
       sub      r9, rdi
       mov      rdi, rcx
       sar      rdi, 63
       and      rdi, rdx
       sub      r9, rdi
       mov      rdi, r8
       sar      rdi, 63
       cmp      r9, rdi
       jne      SHORT G_M000_IG111
       lea      rdi, [r8+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG109
       test     rdi, rdi
       jl       G_M000_IG180
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG179
       mov      rdx, gword ptr [rsi+8*rdi+0x10]
       jmp      SHORT G_M000_IG110
 
G_M000_IG109:
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0xC0]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG110:
       jmp      G_M000_IG114
 
G_M000_IG111:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0xB8], xmm1
       lea      rdi, [rbp-0xB8]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xB8]
       mov      gword ptr [rbp-0x200], rax
       mov      ecx, dword ptr [rbp-0xB0]
       mov      dword ptr [rbp-0x16C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xD8], xmm0
       lea      rdi, [rbp-0xD8]
       mov      rsi, qword ptr [rbp-0xA8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xD8]
       mov      r9d, dword ptr [rbp-0xD0]
       mov      rcx, gword ptr [rbp-0x200]
       test     rcx, rcx
       jne      SHORT G_M000_IG116
 
G_M000_IG112:
       test     rax, rax
       jne      SHORT G_M000_IG115
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xE8], xmm0
       movsxd   rsi, dword ptr [rbp-0x16C]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xE8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0xE8]
       mov      esi, dword ptr [rbp-0xE0]
 
G_M000_IG113:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG114:
       jmp      G_M000_IG132
 
G_M000_IG115:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG117
 
G_M000_IG116:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG117:
       test     rax, rax
       je       SHORT G_M000_IG120
 
G_M000_IG118:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG119:
       mov      edx, dword ptr [rbp-0x16C]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG113
 
G_M000_IG120:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG119
 
G_M000_IG121:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r8], rdi
       jne      SHORT G_M000_IG123
 
G_M000_IG122:
       vmovsd   xmm1, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rdx
       vmovsd   qword ptr [rbp-0xF0], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0xF0]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG132
 
G_M000_IG123:
       jmp      G_M000_IG131
 
G_M000_IG124:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      SHORT G_M000_IG131
 
G_M000_IG125:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm1
       mov      r8, gword ptr [rbp-0x1B0]
       test     r8, r8
       je       SHORT G_M000_IG131
 
G_M000_IG126:
       cmp      qword ptr [r8], rdi
       jne      SHORT G_M000_IG128
 
G_M000_IG127:
       vmovsd   xmm2, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x90]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG132
 
G_M000_IG128:
       cmp      qword ptr [r8], rsi
       jne      SHORT G_M000_IG130
 
G_M000_IG129:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp-0xF8], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0xF8]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG132
 
G_M000_IG130:
 
G_M000_IG131:
       mov      rdx, gword ptr [rbp-0x1B0]
       mov      rsi, rcx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG132:
       mov      rax, gword ptr [rbp-0x1A0]
       test     rax, rax
       je       G_M000_IG153
 
G_M000_IG133:
       mov      rcx, qword ptr [rax]
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      G_M000_IG143
 
G_M000_IG134:
       mov      rcx, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG153
 
G_M000_IG135:
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG141
 
G_M000_IG136:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x118], rax
       lea      rdx, [rcx+rax]
       mov      qword ptr [rbp-0x120], rdx
       mov      rdi, rcx
       xor      rdi, rdx
       mov      r8, rax
       xor      r8, rdx
       and      rdi, r8
       jl       SHORT G_M000_IG139
       lea      rdi, [rdx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG137
       test     rdi, rdi
       jl       G_M000_IG180
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG179
       mov      rsi, gword ptr [rsi+8*rdi+0x10]
       jmp      SHORT G_M000_IG138
 
G_M000_IG137:
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x120]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG138:
       jmp      G_M000_IG140
 
G_M000_IG139:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x130], xmm0
       lea      rdi, [rbp-0x130]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x130]
       mov      gword ptr [rbp-0x208], rax
       mov      ecx, dword ptr [rbp-0x128]
       mov      dword ptr [rbp-0x170], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x140], xmm0
       lea      rdi, [rbp-0x140]
       mov      rsi, qword ptr [rbp-0x118]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x208]
       mov      esi, dword ptr [rbp-0x170]
       mov      rdx, gword ptr [rbp-0x140]
       mov      rcx, qword ptr [rbp-0x138]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG140:
       mov      rax, rsi
       jmp      G_M000_IG154
 
G_M000_IG141:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG153
 
G_M000_IG142:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x110], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x148], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x148]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x110]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG154
 
G_M000_IG143:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      SHORT G_M000_IG149
 
G_M000_IG144:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x100], xmm0
       test     rdx, rdx
       je       G_M000_IG153
 
G_M000_IG145:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG147
 
G_M000_IG146:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x108], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x100]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x108]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG154
 
G_M000_IG147:
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG153
 
G_M000_IG148:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x150], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x150]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x100]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG154
 
G_M000_IG149:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      SHORT G_M000_IG153
 
G_M000_IG150:
       mov      rdi, rax
       test     rdx, rdx
       je       SHORT G_M000_IG153
 
G_M000_IG151:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG153
 
G_M000_IG152:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG154
 
G_M000_IG153:
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
 
G_M000_IG154:
       test     rax, rax
       je       SHORT G_M000_IG157
 
G_M000_IG155:
       mov      rdi, qword ptr [rax]
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       je       G_M000_IG14
 
G_M000_IG156:
       xor      ecx, ecx
       mov      dword ptr [rbp-0x7C], ecx
       mov      rsi, rax
       mov      r9, gword ptr [rbp-0x188]
       jmp      G_M000_IG15
 
G_M000_IG157:
       xor      ecx, ecx
       mov      dword ptr [rbp-0x7C], ecx
       mov      rsi, rax
       mov      r9, gword ptr [rbp-0x188]
       jmp      G_M000_IG15
 
G_M000_IG158:
       mov      rcx, gword ptr [rbp-0x190]
       mov      rax, gword ptr [rcx+0x10]
       mov      gword ptr [rbp-0x1F8], rax
       test     rax, rax
       jne      SHORT G_M000_IG161
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rax, gword ptr [rbp-0x190]
       mov      rdi, gword ptr [rax+0x08]
       vmovsd   xmm0, qword ptr [rbp-0x78]
       vmovsd   qword ptr [rdx+0x08], xmm0
       inc      dword ptr [rdi+0x14]
       mov      rcx, gword ptr [rdi+0x08]
       mov      esi, dword ptr [rdi+0x10]
       cmp      dword ptr [rcx+0x08], esi
       jbe      SHORT G_M000_IG160
 
G_M000_IG159:
       lea      r8d, [rsi+0x01]
       mov      dword ptr [rdi+0x10], r8d
       mov      esi, esi
       mov      rdi, rcx
       call     CORINFO_HELP_ARRADDR_ST
       jmp      SHORT G_M000_IG163
 
G_M000_IG160:
       mov      rsi, rdx
       call     [System.Collections.Generic.List`1[System.__Canon]:AddWithResize(System.__Canon):this]
       jmp      SHORT G_M000_IG163
 
G_M000_IG161:
       test     byte  ptr [(reloc)], 1
       je       G_M000_IG175
 
G_M000_IG162:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r8, qword ptr [rbp-0x78]
       mov      qword ptr [rbp-0x178], r8
       mov      rax, gword ptr [rbp-0x1F8]
       inc      dword ptr [rax+0x14]
       mov      r9, gword ptr [rax+0x08]
       mov      r10d, dword ptr [rax+0x10]
       cmp      dword ptr [r9+0x08], r10d
       ja       SHORT G_M000_IG167
       mov      rdx, r8
       mov      rdi, rax
       call     [System.Collections.Generic.List`1[RyuPy.Runtime.PyNumericSlot]:AddWithResize(RyuPy.Runtime.PyNumericSlot):this]
 
G_M000_IG163:
       mov      rax, qword ptr [rbp-0x50]
       add      rax, 1
       jo       G_M000_IG180
       mov      rcx, rax
       mov      rax, qword ptr [rbp-0x58]
 
G_M000_IG164:
       cmp      rcx, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG03
 
G_M000_IG165:
       mov      qword ptr [rbp-0x50], rcx
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       G_M000_IG10
 
G_M000_IG166:
       test     rdi, rdi
       jl       G_M000_IG180
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG179
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       mov      rcx, rsi
       jmp      G_M000_IG11
 
G_M000_IG167:
       lea      edi, [r10+0x01]
       mov      dword ptr [rax+0x10], edi
       mov      edi, r10d
       shl      rdi, 4
       lea      rax, bword ptr [r9+rdi+0x10]
       mov      bword ptr [rbp-0x210], rax
       mov      rdi, rax
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, bword ptr [rbp-0x210]
       mov      rsi, qword ptr [rbp-0x178]
       mov      qword ptr [rcx+0x08], rsi
       jmp      G_M000_IG163
 
G_M000_IG168:
       mov      edi, 26
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r13
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r12
 
G_M000_IG169:
       add      rsp, 504
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG170:
       call     [System.ThrowHelper:ThrowArgumentOutOfRange_IndexMustBeLessException()]
       int3     
 
G_M000_IG171:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_GET_GCSTATIC_BASE
       jmp      G_M000_IG09
 
G_M000_IG172:
       mov      rdi, gword ptr [rdi+0x08]
       cmp      ecx, dword ptr [rdi+0x08]
       jae      G_M000_IG179
       mov      ecx, ecx
       mov      rdi, gword ptr [rdi+8*rcx+0x10]
       test     rdi, rdi
       jne      SHORT G_M000_IG174
 
G_M000_IG173:
       xor      ecx, ecx
       jmp      G_M000_IG39
 
G_M000_IG174:
       mov      rsi, 0xD1FFAB1E
       mov      rcx, rsi
       cmp      qword ptr [rdi], rcx
       jne      SHORT G_M000_IG173
       jmp      G_M000_IG38
 
G_M000_IG175:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_GET_GCSTATIC_BASE
       jmp      G_M000_IG162
 
G_M000_IG176:
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
 
G_M000_IG177:
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
 
G_M000_IG178:
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
 
G_M000_IG179:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG180:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 4693

