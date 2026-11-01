using System;
using System.Buffers.Binary;
using System.Numerics;

namespace RyuPy.Runtime;

/// <summary>
/// MT19937的32位状态核；按公开参数与数组初始化算法独立实现，不调用System.Random。
/// 624字状态、397字偏移与temper参数固定；算法来源/行为验收记录在docs/notes中。
/// </summary>
internal sealed class PyRandomState
{
    private readonly uint[] _state = new uint[624];
    private int _position = 624;

    internal void Seed(BigInteger seed)
    {
        byte[] bytes = BigInteger.Abs(seed).ToByteArray(isUnsigned: true, isBigEndian: false);
        var key = new uint[Math.Max(1, (bytes.Length + 3) / 4)];
        for (int index = 0; index < bytes.Length; index++)
            key[index / 4] |= (uint)bytes[index] << (8 * (index % 4));
        SeedWords(key);
    }

    // 数组种子按低有效字在前输入；所有运算在模2^32环上进行。
    internal void SeedWords(uint[] key)
    {
        unchecked
        {
            _state[0] = 19650218U;
            for (int index = 1; index < _state.Length; index++)
            {
                uint previous = _state[index - 1];
                _state[index] = 1812433253U * (previous ^ (previous >> 30)) + (uint)index;
            }
            int slot = 1, word = 0;
            for (int remaining = Math.Max(_state.Length, key.Length); remaining > 0; remaining--)
            {
                uint previous = _state[slot - 1];
                _state[slot] = (_state[slot] ^ ((previous ^ (previous >> 30)) * 1664525U)) + key[word] + (uint)word;
                if (++slot == _state.Length) { _state[0] = _state[^1]; slot = 1; }
                if (++word == key.Length) word = 0;
            }
            for (int remaining = _state.Length - 1; remaining > 0; remaining--)
            {
                uint previous = _state[slot - 1];
                _state[slot] = (_state[slot] ^ ((previous ^ (previous >> 30)) * 1566083941U)) - (uint)slot;
                if (++slot == _state.Length) { _state[0] = _state[^1]; slot = 1; }
            }
            _state[0] = 0x80000000U;
            _position = _state.Length;
        }
    }

    private uint NextWord()
    {
        if (_position == _state.Length)
        {
            // 原地更新是递推定义的一部分，绕回的397偏移读本轮已更新状态，不能改成旧数组快照。
            for (int index = 0; index < _state.Length; index++)
            {
                uint joined = (_state[index] & 0x80000000U) | (_state[(index + 1) % 624] & 0x7fffffffU);
                _state[index] = _state[(index + 397) % 624] ^ (joined >> 1)
                    ^ ((joined & 1) == 0 ? 0U : 0x9908b0dfU);
            }
            _position = 0;
        }
        uint result = _state[_position++];
        result ^= result >> 11;
        result ^= (result << 7) & 0x9d2c5680U;
        result ^= (result << 15) & 0xefc60000U;
        return result ^ (result >> 18);
    }

    // 两次取样分别提供27与26位；不能用一个64位抽样或.NET随机数替代。
    internal double Random() => ((double)(NextWord() >> 5) * 67108864.0 + (NextWord() >> 6)) / 9007199254740992.0;

    internal BigInteger Bits(int count)
    {
        if (count == 0) return BigInteger.Zero;
        if (count <= 32) return new BigInteger(NextWord() >> (32 - count));
        int words = (count + 31) / 32;
        var bytes = new byte[words * 4];
        for (int index = 0; index < words; index++)
        {
            uint word = NextWord();
            int remaining = count - 32 * index;
            if (remaining < 32) word >>= 32 - remaining;
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(index * 4, 4), word);
        }
        return new BigInteger(bytes, isUnsigned: true, isBigEndian: false);
    }
}
