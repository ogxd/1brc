using System.Collections;
using System.Runtime.CompilerServices;

namespace OneBillionRowsChallenge;

public class MinMaxMeanByCityMap : IEnumerable<KeyValuePair<Hash, MinMaxMean>>
{
    private struct Entry
    {
        public Hash Key;
        public MinMaxMean Value;

        public Entry(Hash key, MinMaxMean value) {
            Key = key;
            Value = value;
        }
    }

    private Entry[,] _buckets;
    private uint _size;
    private uint _depth;

    public MinMaxMeanByCityMap(uint size, uint depth) {
        _buckets = new Entry[size, depth];
        _size = size;
        _depth = depth;
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint GetBucketIndex(Hash key) {
        uint hashCode = key.Low;
        uint index = hashCode % _size;
        return index;
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(Hash key, int temp, out bool newEntry) {
        uint bucketIndex = GetBucketIndex(key);

        for (int i = 0; i < _depth; i++) {
            ref var entry = ref _buckets[bucketIndex, i];
            if (entry.Key.Eq == 0) {
                entry = new Entry(key, new MinMaxMean(temp));
                newEntry = true;
                return;
            }
            if (entry.Key.Eq == key.Eq) {
                entry.Value.Add(temp); // Merge values if key exists
                newEntry = false;
                return;
            }
        }

        throw new InvalidOperationException("Depth reached");
    }

    public void Add(Hash key, MinMaxMean minMaxMean) {
        uint bucketIndex = GetBucketIndex(key);

        for (int i = 0; i < _depth; i++) {
            ref var entry = ref _buckets[bucketIndex, i];
            if (entry.Key.Eq == 0) {
                entry = new Entry(key, minMaxMean);
                return;
            }
            if (entry.Key.Eq == key.Eq) {
                entry.Value.Add(minMaxMean); // Merge values if key exists
                return;
            }
        }

        throw new InvalidOperationException("Depth reached");
    }

    public IEnumerator<KeyValuePair<Hash, MinMaxMean>> GetEnumerator() {
        foreach (var entry in _buckets) {
            if (entry.Key.Eq == 0 && entry.Key.Low == 0) continue;
            yield return new KeyValuePair<Hash, MinMaxMean>(entry.Key, entry.Value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}