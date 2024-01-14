using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace OneBillionRowsChallenge;

public static class Utils
{
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static int IndexOf(ref byte start, int length, byte byteToFind)
    {
        int offset = 0;

        if (Avx2.IsSupported) {
            ref Vector256<byte> ptr = ref Unsafe.As<byte, Vector256<byte>>(ref start);
        NEXT:
            var vec = Avx2.CompareEqual(Vector256.Create(byteToFind), ptr);
            int mask = Avx2.MoveMask(vec);
            int tzc = BitOperations.TrailingZeroCount(mask);
            if (tzc == 32) {
                offset += 32;
                if (offset > length) {
                    return -1;
                }
                ptr = ref Unsafe.Add(ref ptr, 1);
                goto NEXT;
            }
            int pos = tzc + offset;
            if (pos > length) {
                return -1;
            }
            return pos;
        }
        if (Sse2.IsSupported)
        {
            ref Vector128<byte> ptr = ref Unsafe.As<byte, Vector128<byte>>(ref start);
        NEXT:
            var vec = Sse2.CompareEqual(Vector128.Create(byteToFind), ptr);
            int mask = Sse2.MoveMask(vec);
            int tzc = BitOperations.TrailingZeroCount(mask);
            if (tzc == 32)
            {
                offset += 16;
                if (offset > length)
                {
                    return -1;
                }
                ptr = ref Unsafe.Add(ref ptr, 1);
                goto NEXT;
            }
            int pos = tzc + offset;         
            if (pos > length)
            {
                return -1;
            }
            return pos;
        }
        if (AdvSimd.IsSupported)
        {
            ref Vector128<byte> ptr = ref Unsafe.As<byte, Vector128<byte>>(ref start);
        NEXT:
            var vec = AdvSimd.CompareEqual(Vector128.Create(byteToFind), ptr);
            var vec64 = Unsafe.As<Vector128<byte>, Vector128<ulong>>(ref vec);
            int lzcLow = BitOperations.TrailingZeroCount(AdvSimd.Extract(vec64, 0));
            int pos;
            if (lzcLow == 64)
            {
                int lzcHigh = BitOperations.TrailingZeroCount(AdvSimd.Extract(vec64, 1));
                if (lzcHigh == 64)
                {
                    offset += 16;
                    if (offset > length)
                    {
                        return -1;
                    }
                    ptr = ref Unsafe.Add(ref ptr, 1);
                    goto NEXT;
                }
                else
                {
                    pos = lzcHigh / 8 + 8;
                }
            }
            else
            {
                pos = lzcLow / 8;
            }
            
            pos += offset;
            
            if (pos > length)
            {
                return -1;
            }
            
            return pos;
        }

        throw new NotSupportedException();
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ParseIntP10(ref byte startRef, int length)
    {
        int sign = 1;
        int value = 0;

        ref byte endRef = ref Unsafe.Add<byte>(ref startRef, length - 2);

        while (Unsafe.IsAddressLessThan(ref startRef, ref endRef))
        {
            byte c = startRef;

            if (c == '-') {
                sign = -1;
            } else {
                value = value * 10 + (c - '0');
            }

            startRef = ref Unsafe.Add(ref startRef, 1);
        }

        startRef = ref Unsafe.Add(ref startRef, 1);
        int firstDecimal = startRef - '0';
        return sign * (value * 10 + firstDecimal);
    }
}