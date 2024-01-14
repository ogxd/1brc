using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static OneBillionRowsChallenge.Constants;

namespace OneBillionRowsChallenge;

public unsafe readonly struct Chunk
{
    public readonly long Start;
    public readonly long Length;

    internal Chunk(SafeFileHandle fileHandle, long start, long maxLength, long parentEnd)
    {
        // Realign chunk end
        if (start + maxLength >= parentEnd)
        {
            // Last chunk, so we make sure we don't go beyond bounds
            maxLength = parentEnd - start;
        }
        else
        {
            // We want to make sure this chunk ends with a \n and next one start with a new
            // city name, so we iterate backwards until we find an \n
            Span<byte> buffer = stackalloc byte[MAX_ENTRY_WIDTH];
            int r = RandomAccess.Read(fileHandle, buffer, start + maxLength - buffer.Length);
            int i = r - 1;
            while (buffer[i] != CHAR_EOL)
            {
                i--;
                maxLength--;
            }
            maxLength++;
        }

        Start = start;
        Length = maxLength;
    }

    // Don't inline to leave room in bytecode for methods that require inlining
    [MethodImpl(MethodImplOptions.NoInlining)]
    public Chunk[] Split(SafeFileHandle fileHandle, int chunksCount)
    {
        var chunks = new Chunk[chunksCount];
        long nextStart = Start;
        long maxChunkLength = Length / chunksCount + MAX_ENTRY_WIDTH; // Conservative margin
        for (int i = 0; i < chunks.Length; i++)
        {
            var chunk = chunks[i] = new Chunk(fileHandle, nextStart, maxChunkLength, Start + Length);
            nextStart += chunk.Length;
        }
        
        Debug.Assert(chunks.Sum(x => x.Length) == Length, "Sum of chunks lengths must be equal to total space size!");
        
        return chunks;
    }
}