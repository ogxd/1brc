using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OneBillionRowsChallenge;

public unsafe class Parser
{
    public void Parse(string filePath)
    {
        // Get file size
        FileInfo fileInfo = new FileInfo(filePath);
        long fileSize = fileInfo.Length;
        
        // Ask OS to memory map the file for fast access
        using var memoryMappedFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open);
        using MemoryMappedViewAccessor viewAccessor = memoryMappedFile.CreateViewAccessor(0, fileSize, MemoryMappedFileAccess.Read);
        byte* bytePtr = null;
        viewAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref bytePtr);

        // Start processing
        int parallelism = Environment.ProcessorCount;
        var threads = new Thread[parallelism];
        var chunks = Chunk.GetChunks(parallelism, bytePtr, fileSize);
        for (int i = 0; i < parallelism; i++)
        {
            var chunk = chunks[i];
            threads[i] = new Thread(_ => Process(chunk));
            threads[i].Start();
        }
        
        // Wait for all threads to complete
        for (int i = 0; i < parallelism; i++)
        {
            threads[i].Join();
        }
    }

    private void Process(Chunk chunk)
    {
        // Computing subchunks allows breaking down the chunk reading dependency chain, which inherently improves
        // the odds for instruction level parallelization
        const int subchunksCount = 2;
        var subchunks = Chunk.GetChunks(subchunksCount, chunk.PtrStart, chunk.Length);
        
        for (int i = 0; i < subchunks.Length; i++)
        {
            ProcessSubchunk(subchunks[i]);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessSubchunk(Chunk chunk)
    {
        Dictionary<Hash, int> data = new(500);
        
        ref byte startRef = ref Unsafe.AsRef<byte>(chunk.PtrStart);
        ref byte endRef = ref Unsafe.AsRef<byte>(chunk.PtrStart + chunk.Length);

        while (Unsafe.IsAddressLessThan(ref startRef, ref endRef))
        {
            // Intentionally use the same starting point for IndexOf to avoid dependency chains (higher ILP)
            int separatorIndex = Utils.IndexOf(ref startRef, 50, (byte)';');
            int newLineIndex = Utils.IndexOf(ref startRef, 50, (byte)'\n');
            
            int temp = Utils.ParseIntP10(ref Unsafe.Add(ref startRef, separatorIndex + 1), newLineIndex - separatorIndex - 1);

            ref int d = ref CollectionsMarshal.GetValueRefOrAddDefault(data, Hash.GetHash(ref startRef, separatorIndex), out bool _);

            // Quick and dirty for test purposes
            d += temp;
            
            // Todo: compute min/max/mean
            
            startRef = ref Unsafe.Add(ref startRef, newLineIndex + 1);
        }
    }
}

public unsafe class Chunk
{
    public readonly long Length;
    public readonly byte *PtrStart;

    private Chunk(byte* byteRef, long start, long maxLength, long fileSize)
    {
        PtrStart = byteRef + start;
        
        // Realign chunk end
        if (start + maxLength > fileSize)
        {
            // Last chunk, so we make sure we don't go beyond bounds
            maxLength = fileSize - start;
        }
        else
        {
            // We want to make sure this chunk ends with a \n and next one start with a new
            // city name, so we iterate backwards until we find an \n
            byteRef += start + maxLength;
            while (byteRef[0] != (byte)'\n')
            {
                byteRef -= 1;
                maxLength--;
            }
        }
        Length = maxLength;
    }

    // Don't inline to leave room in bytecode for methods that require inlining
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Chunk[] GetChunks(int chunksCount, byte* ptr, long length)
    {
        var chunks = new Chunk[chunksCount];
        long nextStart = 0;
        long maxChunkLength = length / chunksCount + 50; // Conservative margin
        for (int i = 0; i < chunks.Length; i++)
        {
            var chunk = chunks[i] = new Chunk(ptr, nextStart, maxChunkLength, length);
            nextStart += chunk.Length;
        }
        
        Debug.Assert(chunks.Sum(x => x.Length) == length, "Sum of chunks lengths must be equal to total space size!");
        
        return chunks;
    }
}