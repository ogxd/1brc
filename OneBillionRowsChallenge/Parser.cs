using Microsoft.Win32.SafeHandles;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static OneBillionRowsChallenge.Constants;

namespace OneBillionRowsChallenge;

public unsafe class Parser
{
    private const int BUFFER_SIZE = 1024 * 1024;
    private const int MAP_WIDTH = 2000;
    private const int MAP_DEPTH = 3;
    private const int SUBCHUNKS_COUNT = 1;

    private ConcurrentDictionary<Hash, string> _cityHashToName = new();
    private int _parallelism = Environment.ProcessorCount;

    public void Parse(string filePath)
    {
        // Get file size
        SafeFileHandle fileHandle = File.OpenHandle(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        long fileSize = RandomAccess.GetLength(fileHandle);

        // Start processing
        var threads = new Thread[_parallelism];
        var maps = new MinMaxMeanByCityMap[_parallelism];
        var wholeChunk = new Chunk(fileHandle, 0, fileSize, fileSize);
        var chunks = wholeChunk.Split(fileHandle, _parallelism);
        for (int i = 0; i < _parallelism; i++) {
            int index = i;
            var chunk = chunks[i];
            threads[i] = new Thread(_ => maps[index] = Process(fileHandle, chunk));
            threads[i].Priority = ThreadPriority.Highest;
            threads[i].Start();
        }

        GC.TryStartNoGCRegion(1_000_000);

        // Wait for all threads to complete
        for (int i = 0; i < _parallelism; i++) {
            threads[i].Join();
        }

        // Merge results
        var map = new MinMaxMeanByCityMap(MAP_WIDTH, MAP_DEPTH);
        for (int i = 0; i < _parallelism; i++) {
            foreach (var pair in maps[i]) {
                map.Add(pair.Key, pair.Value);
            }
        }

        // Print results
        Console.WriteLine($"{{{string.Join(", ", map
            .Select(x => (_cityHashToName[x.Key], x.Value))
            .OrderBy(x => x.Item1)
            .Select(x => $"{x.Item1}={x.Value}"))}}}");
    }

    [SkipLocalsInit]
    private MinMaxMeanByCityMap Process(SafeFileHandle fileHandle, Chunk chunk)
    {
        var map = new MinMaxMeanByCityMap(MAP_WIDTH, MAP_DEPTH);

        // Computing subchunks allows breaking down the chunk reading dependency chain, which inherently improves
        // the odds for instruction level parallelization
        var subchunks = chunk.Split(fileHandle, SUBCHUNKS_COUNT);

        for (int i = 0; i < subchunks.Length; i++)
        {
            var subchunk = subchunks[i];

            int offsetInBuffer = BUFFER_SIZE;

            // Buffer for buffered read
            Span<byte> buffer = new byte[BUFFER_SIZE];
            ref byte b = ref MemoryMarshal.AsRef<byte>(buffer);

            for (long offsetInFile = 0; offsetInFile < subchunk.Length;) {

                // Re-bufferize when reaching near end of buffer
                if (offsetInBuffer > BUFFER_SIZE - MAX_ENTRY_WIDTH) {
                    _ = RandomAccess.Read(fileHandle, buffer, subchunk.Start + offsetInFile);
                    offsetInBuffer = 0;
                }

                ref byte bc = ref Unsafe.Add<byte>(ref b, offsetInBuffer);
                // Intentionally use the same starting point for IndexOf to avoid dependency chains (higher ILP)
                int separatorIndex = Utils.IndexOf(ref bc, MAX_ENTRY_WIDTH, CHAR_SEPARATOR);
                int newLineIndex = Utils.IndexOf(ref bc, MAX_ENTRY_WIDTH, CHAR_EOL);

                int temp = Utils.ParseIntP10(ref Unsafe.Add(ref bc, separatorIndex + 1), newLineIndex - separatorIndex - 1);

                Hash hash = Hash.GetHash(ref bc, separatorIndex);
                map.Add(hash, temp, out bool newEntry);
                if (newEntry) {
                    // City name is unkown, parse it and add to mapping
                    AddCity(ref bc, separatorIndex, hash);
                }

                offsetInFile += newLineIndex + 1;
                offsetInBuffer += newLineIndex + 1;
            }
        }

        return map;
    }

    // No need to inline this, it's not going to happen often
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AddCity(ref byte start, int cityNameLength, Hash cityHash) {
        string cityName;
        fixed (byte* pStart = &start) {
            cityName = Encoding.UTF8.GetString(pStart, cityNameLength);
        }
        _cityHashToName.TryAdd(cityHash, cityName);
    }
}