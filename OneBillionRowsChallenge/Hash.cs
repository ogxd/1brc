using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace OneBillionRowsChallenge;

public readonly struct Hash : IEquatable<Hash>
{
    private readonly int _low;
    private readonly long _eq;

    private Hash(int low, long eq)
    {
        _low = low;
        _eq = eq;
    }
    
    public bool Equals(Hash other) => _eq == other._eq;

    public override bool Equals(object? obj) => obj is Hash other && Equals(other);

    public override int GetHashCode() => _low;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Hash GetHash(ref byte b, int length)
    {
        // Inspired from GxHash (by me)
        ref var vec = ref Unsafe.As<byte, Vector128<byte>>(ref b);
        var indices = Vector128.Create(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
        var mask = Vector128.GreaterThan(Vector128.Create((sbyte)length), indices).AsByte();
        Vector128<byte> hashVector = Vector128.BitwiseAnd(mask, vec);
        
        var keys1 = Vector128.Create(0xFC3BC28E, 0x89C222E5, 0xB09D3E21, 0xF2784542).AsByte();
        var keys2 = Vector128.Create(0x03FCE279, 0xCB6B2E9B, 0xB361DC58, 0x39136BD9).AsByte();

        if (Aes.IsSupported)
        {
            hashVector = AdvSimd.Xor(Aes.MixColumns(Aes.Encrypt(hashVector, Vector128<byte>.Zero)), keys1);
            hashVector = AdvSimd.Xor(Aes.Encrypt(hashVector, Vector128<byte>.Zero), keys2);
        }
        else if (System.Runtime.Intrinsics.X86.Aes.IsSupported)
        {
            hashVector = System.Runtime.Intrinsics.X86.Aes.Encrypt(hashVector, keys1);
            hashVector = System.Runtime.Intrinsics.X86.Aes.EncryptLast(hashVector, keys2);
        }

        return new Hash(hashVector.AsInt32().GetElement(0), hashVector.AsInt64().GetElement(1));
    }
}