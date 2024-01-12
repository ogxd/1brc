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
        int parallelism = 4 * Environment.ProcessorCount;
        var threads = new Thread[parallelism];
        var wholeChunk = new Chunk(bytePtr, 0, fileSize, fileSize);
        var chunks = wholeChunk.Split(parallelism);
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
        
        // Merge results
        for (int i = 0; i < parallelism; i++)
        {
            wholeChunk.MergeChunkData(chunks[i]);
        }
        
        // Print results
        foreach (var pair in wholeChunk.Data!)
        {
            Console.Write(pair.Value + ", ");
        }
    }

    private void Process(Chunk chunk)
    {
        // Computing subchunks allows breaking down the chunk reading dependency chain, which inherently improves
        // the odds for instruction level parallelization
        const int subchunksCount = 8;
        var subchunks = chunk.Split(subchunksCount);
        
        for (int i = 0; i < subchunks.Length; i++)
        {
            ProcessSubchunk(subchunks[i]);
        }
        
        for (int i = 0; i < subchunks.Length; i++)
        {
            chunk.MergeChunkData(subchunks[i]);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessSubchunk(Chunk chunk)
    {
        Dictionary<Hash, MinMaxMean> data = new(500, new Hash.HashComparer());
        
        ref byte startRef = ref Unsafe.AsRef<byte>(chunk.PtrStart);
        ref byte endRef = ref Unsafe.AsRef<byte>(chunk.PtrStart + chunk.Length);

        while (Unsafe.IsAddressLessThan(ref startRef, ref endRef))
        {
            // Intentionally use the same starting point for IndexOf to avoid dependency chains (higher ILP)
            int separatorIndex = Utils.IndexOf(ref startRef, 50, (byte)';');
            int newLineIndex = Utils.IndexOf(ref startRef, 50, (byte)'\n');
            
            int temp = Utils.ParseIntP10(ref Unsafe.Add(ref startRef, separatorIndex + 1), newLineIndex - separatorIndex - 1);

            ref MinMaxMean d = ref CollectionsMarshal.GetValueRefOrAddDefault(data, Hash.GetHash(ref startRef, separatorIndex), out bool exists);
            if (!exists) {
                d = new MinMaxMean();
            }
            d.Add(temp);

            startRef = ref Unsafe.Add(ref startRef, newLineIndex + 1);
        }

        chunk.Data = data;
    }
}