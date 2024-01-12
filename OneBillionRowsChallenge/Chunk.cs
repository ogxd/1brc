using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace OneBillionRowsChallenge;

public unsafe class Chunk
{
    public readonly long Length;
    public readonly byte *PtrStart;
    public readonly MinMaxMeanByCityMap Data = new MinMaxMeanByCityMap(500);

    internal Chunk(byte* byteRef, long start, long maxLength, long fileSize)
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

    public void MergeChunkData(Chunk otherChunk)
    {
        foreach (var pair in otherChunk.Data)
        {
            Data.Add(pair.Key, pair.Value);
        }
    }

    // Don't inline to leave room in bytecode for methods that require inlining
    [MethodImpl(MethodImplOptions.NoInlining)]
    public Chunk[] Split(int chunksCount)
    {
        var chunks = new Chunk[chunksCount];
        long nextStart = 0;
        long maxChunkLength = Length / chunksCount + 50; // Conservative margin
        for (int i = 0; i < chunks.Length; i++)
        {
            var chunk = chunks[i] = new Chunk(PtrStart, nextStart, maxChunkLength, Length);
            nextStart += chunk.Length;
        }
        
        Debug.Assert(chunks.Sum(x => x.Length) == Length, "Sum of chunks lengths must be equal to total space size!");
        
        return chunks;
    }
}