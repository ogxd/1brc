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

    private Entry[,] buckets;
    private int size;

    public MinMaxMeanByCityMap(int size) {
        buckets = new Entry[size, MAX_DEPTH];
        this.size = size;
    }

    const int MAX_DEPTH = 5;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetBucketIndex(Hash key) {
        int hashCode = key.Low;
        int index = hashCode % size;
        return Math.Abs(index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(Hash key, int temp) {
        int bucketIndex = GetBucketIndex(key);

        for (int i = 0; i < MAX_DEPTH; i++) {
            ref var entry = ref buckets[bucketIndex, i];
            if (entry.Key.Eq == 0 && entry.Key.Low == 0) {
                MinMaxMean minMaxMean = new MinMaxMean();
                minMaxMean.Add(temp);
                entry = new Entry(key, minMaxMean);
                return;
            }
            if (entry.Key.Eq == key.Eq) {
                var value = entry.Value;
                value.Add(temp); // Merge values if key exists
                entry.Value = value;
                return;
            }
        }

        throw new InvalidOperationException("Depth reached");
    }

    public void Add(Hash key, MinMaxMean minMaxMean) {
        int bucketIndex = GetBucketIndex(key);

        for (int i = 0; i < MAX_DEPTH; i++) {
            ref var entry = ref buckets[bucketIndex, i];
            if (entry.Key.Eq == 0 && entry.Key.Low == 0) {
                entry = new Entry(key, minMaxMean);
                return;
            }
            if (entry.Key.Eq == key.Eq) {
                var value = entry.Value;
                value.Add(minMaxMean); // Merge values if key exists
                entry.Value = value;
                return;
            }
        }

        throw new InvalidOperationException("Depth reached");
    }

    public IEnumerator<KeyValuePair<Hash, MinMaxMean>> GetEnumerator() {
        foreach (var entry in buckets) {
            if (entry.Key.Eq == 0 && entry.Key.Low == 0) continue;
            yield return new KeyValuePair<Hash, MinMaxMean>(entry.Key, entry.Value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}