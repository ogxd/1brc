using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using ArmAes = System.Runtime.Intrinsics.Arm.Aes;
using X86Aes = System.Runtime.Intrinsics.X86.Aes;

namespace OneBillionRowsChallenge;

public readonly struct Hash
{
    public readonly int Low;
    public readonly long Eq;

    private Hash(int low, long eq)
    {
        Low = low;
        Eq = eq;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Hash GetHash(ref byte b, int length)
    {
        // Inspired from GxHash (by me)
        ref var vec = ref Unsafe.As<byte, Vector128<byte>>(ref b);
        var indices = Vector128.Create(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
        var mask = Vector128.GreaterThan(Vector128.Create((sbyte)length), indices).AsByte();
        Vector128<byte> hashVector = Vector128.BitwiseAnd(mask, vec);
        hashVector = Vector128.Add(hashVector, Vector128.Create((byte)length));

        var keys1 = Vector128.Create(0xFC3BC28E, 0x89C222E5, 0xB09D3E21, 0xF2784542).AsByte();
        var keys2 = Vector128.Create(0x3BCA2490, 0x12617C43, 0xA37CF278, 0xBB436D52).AsByte();
        var keys3 = Vector128.Create(0x03FCE279, 0xCB6B2E9B, 0xB361DC58, 0x39136BD9).AsByte();

        if (ArmAes.IsSupported)
        {
            hashVector = AdvSimd.Xor(ArmAes.MixColumns(ArmAes.Encrypt(hashVector, Vector128<byte>.Zero)), keys1);
            hashVector = AdvSimd.Xor(ArmAes.Encrypt(hashVector, Vector128<byte>.Zero), keys2);
        }
        else if (X86Aes.IsSupported)
        {
            hashVector = X86Aes.Encrypt(hashVector, keys1);
            hashVector = X86Aes.Encrypt(hashVector, keys2);
            hashVector = X86Aes.EncryptLast(hashVector, keys3);
        }

        return new Hash(hashVector.AsInt32().GetElement(0), hashVector.AsInt64().GetElement(1));
    }

    public class HashComparer : IEqualityComparer<Hash>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Hash x, Hash y) => x.Eq == y.Eq;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode([DisallowNull] Hash obj) => obj.Low;
    }
}