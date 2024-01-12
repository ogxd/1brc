using System.Runtime.CompilerServices;

namespace OneBillionRowsChallenge;

public struct MinMaxMean
{
    private int _min;
    private int _max;
    private long _sum;
    private int _count;

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MinMaxMean()
    {
        _min = int.MaxValue;
        _max = int.MinValue;
        _sum = 0;
        _count = 0;
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MinMaxMean(int temp) {
        _min = temp;
        _max = temp;
        _sum = temp;
        _count = 1;
    }

    public int Min => _min;
    public int Max => _max;
    public long Mean => _sum / _count;

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(int value)
    {
        if (value < _min)
        {
            _min = value;
        }
        if (value > _max)
        {
            _max = value;
        }

        _sum += value;
        _count += 1;
    }

    // Not in hotpath
    public void Add(MinMaxMean minMaxMean)
    {
        if (minMaxMean._min < _min)
        {
            _min = minMaxMean._min;
        }
        if (minMaxMean._max > _max)
        {
            _max = minMaxMean._max;
        }

        _sum += minMaxMean._sum;
        _count += minMaxMean._count;
    }

    public override string ToString()
    {
        return $"{0.1d * _min:N1}/{0.1d * _sum / _count:N1}/{0.1d * _max:N1}";
    }
}