using Microsoft.Win32.SafeHandles;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;

namespace OneBillionRowsChallenge.Tests;

public class Tests
{
    [TestCase(new byte[] { 1, 2, 3, 4, 5, 6 }, 3)]
    [TestCase(new byte[] { 1, 2, 3, 4, 5, 6 }, 4)]
    [TestCase(new byte[] { 1, 2, 3, 4, 5, 6 }, 1)]
    [TestCase(new byte[] { 1, 2, 3, 4, 5, 6 }, 8)]
    [TestCase(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }, 1)]
    [TestCase(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }, 1)]
    [TestCase(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }, 2)]
    [TestCase(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }, 1)]
    [TestCase(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }, 2)]
    [TestCase(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, 1)]
    [TestCase(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, 2)]
    public void IndexOfCustom_Works(byte[] bytes, byte c)
    {
        ref byte byteRef = ref bytes[0];
        Assert.That(Utils.IndexOf(ref byteRef, bytes.Length, c), Is.EqualTo(bytes.AsSpan().IndexOf(c)));
    }
    
    [TestCase("3.0", 30)]
    [TestCase("-12.4", -124)]
    [TestCase("-0.9", -9)]
    public void ParseInt_Works(string str, int expected)
    {
        var bytes = Encoding.ASCII.GetBytes(str);
        ref byte byteRef = ref bytes[0];
        Assert.That(Utils.ParseIntP10(ref byteRef, bytes.Length), Is.EqualTo(expected));
    }

    [Test]
    public void HashLowCollisions() {
        var words = new string[] {
            "paris",
            "berlin",
            "rio de janeiro",
            "pontoise",
            "marcoussis",
            "tourcoing",
            "london"
        };
        var set = words.Select(Encoding.ASCII.GetBytes).Select(bytes => {
            ref byte byteRef = ref bytes[0];
            var hash = Hash.GetHash(ref byteRef, bytes.Length);
            return hash.Low;
        }).ToHashSet();

        Assert.That(set.Count, Is.EqualTo(words.Length));
    }

    const string filePath = "..\\..\\..\\..\\..\\measurements1B.txt";

    [Explicit]
    [Test]
    public unsafe void ScanRandomAccessCopyToHeap() {
        // Get file size
        SafeFileHandle fileHandle = File.OpenHandle(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        long fileSize = RandomAccess.GetLength(fileHandle);

        nint intptr = Marshal.AllocHGlobal(new nint(fileSize));
        byte* ptr = (byte*)intptr.ToPointer();

        int width = 1024 * 1024 * 8;
        long pos = 0;
        while (pos < fileSize) {
            var span = new Span<byte>(ptr/* + pos*/, (int)Math.Min(fileSize - pos, width));
            int readBytes = RandomAccess.Read(fileHandle, span, pos);
            pos += readBytes;
        }

        Marshal.FreeHGlobal(intptr);
        Console.WriteLine("Position: " + pos);
    }

    [Explicit]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(4)]
    [TestCase(8)]
    public unsafe void ScanRandomAccessCopyToStack(int parallelism) {
        // Get file size
        using SafeFileHandle fileHandle = File.OpenHandle(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        long fileSize = RandomAccess.GetLength(fileHandle);

        int width = 1024 * 1024 * 1;

        long portionSize = fileSize / parallelism;
        long r = Enumerable.Range(0, parallelism)
            .AsParallel()
            .Select(x => {
                byte* ptr = stackalloc byte[width];
                long pos = x * portionSize;
                long end = (x + 1) * portionSize;
                while (pos < end) {
                    var dstSpan = new Span<byte>(ptr, (int)Math.Min(end - pos, width));
                    int readBytes = RandomAccess.Read(fileHandle, dstSpan, pos);
                    pos += readBytes;
                }
                return pos - x * portionSize;
            })
            .Sum();

        Console.WriteLine("Position: " + r);
    }

    [Explicit]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(4)]
    [TestCase(8)]
    public unsafe void ScanRandomAccessCopyToStack_PerThreadHandle(int parallelism) {
        // Get file size
        using SafeFileHandle fileHandle = File.OpenHandle(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        long fileSize = RandomAccess.GetLength(fileHandle);
        fileHandle.Dispose();

        int width = 1024 * 1024 * 1;

        long portionSize = fileSize / parallelism;
        long r = Enumerable.Range(0, parallelism)
            .AsParallel()
            .Select(x => {
                using SafeFileHandle f = File.OpenHandle(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                byte* ptr = stackalloc byte[width];
                long pos = x * portionSize;
                long end = (x + 1) * portionSize;
                while (pos < end) {
                    var dstSpan = new Span<byte>(ptr, (int)Math.Min(end - pos, width));
                    int readBytes = RandomAccess.Read(f, dstSpan, pos);
                    pos += readBytes;
                }
                return pos - x * portionSize;
            })
            .Sum();

        Console.WriteLine("Position: " + r);
    }

    [Explicit]
    [Test]
    public unsafe void ScanMmapCopyToHeap() {
        // Get file size
        FileInfo fileInfo = new FileInfo(filePath);
        long fileSize = fileInfo.Length;

        using var memoryMappedFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open);
        using MemoryMappedViewAccessor viewAccessor = memoryMappedFile.CreateViewAccessor(0, fileSize, MemoryMappedFileAccess.Read);
        byte* srcPtr = null;
        viewAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref srcPtr);

        nint intptr = Marshal.AllocHGlobal(new nint(fileSize));
        byte* ptr = (byte*)intptr.ToPointer();

        int width = 1024 * 1024 * 8;
        long pos = 0;
        while (pos < fileSize) {
            int l = (int)Math.Min(fileSize - pos, width);
            var srcSpan = new Span<byte>(srcPtr, l);
            var span = new Span<byte>(ptr + pos, l);
            srcSpan.CopyTo(span);
            srcPtr += l;
            pos += l;
        }

        Marshal.FreeHGlobal(intptr);
        Console.WriteLine("Position: " + pos);
    }

    [Explicit]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(4)]
    [TestCase(8)]
    public unsafe void ScanMmapCopyToStack(int parallelism) {
        // Get file size
        FileInfo fileInfo = new FileInfo(filePath);
        long fileSize = fileInfo.Length;

        using var memoryMappedFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open);
        using MemoryMappedViewAccessor viewAccessor = memoryMappedFile.CreateViewAccessor(0, fileSize, MemoryMappedFileAccess.Read);
        byte* srcPtrO = null;
        viewAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref srcPtrO);

        int width = 1024 * 1024 * 1;

        long portionSize = fileSize / parallelism;
        long r = Enumerable.Range(0, parallelism)
            .AsParallel()
            .Select(x => {
                byte* ptr = stackalloc byte[width];
                long pos = x * portionSize;
                long end = (x + 1) * portionSize;
                byte* srcPtr = srcPtrO + pos;
                while (pos < end) {
                    int l = (int)Math.Min(end - pos, width);
                    var srcSpan = new Span<byte>(srcPtr, l);
                    var span = new Span<byte>(ptr, l);
                    srcSpan.CopyTo(span);
                    srcPtr += l;
                    pos += l;
                }
                return pos - x * portionSize;
            })
            .Sum();

        Console.WriteLine("Position: " + r);
    }

    [Explicit]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(4)]
    [TestCase(8)]
    public unsafe void ScanMmapToVec256(int parallelism) {
        // Get file size
        FileInfo fileInfo = new FileInfo(filePath);
        long fileSize = fileInfo.Length;

        using var memoryMappedFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open);
        using MemoryMappedViewAccessor viewAccessor = memoryMappedFile.CreateViewAccessor(0, fileSize, MemoryMappedFileAccess.Read);
        byte* srcPtrO = null;
        viewAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref srcPtrO);

        int width = 1024 * 1024 * 1;

        long portionSize = fileSize / parallelism;
        long r = Enumerable.Range(0, parallelism)
            .AsParallel()
            .Select(x => {
                byte* ptr = stackalloc byte[width];
                long pos = x * portionSize;
                long end = (x + 1) * portionSize;
                byte* srcPtr = srcPtrO + pos;
                while (pos < end) {
                    var vec = Vector256.Load(srcPtr);
                    srcPtr += 32;
                    pos += 32;
                }
                return pos - x * portionSize;
            })
            .Sum();

        Console.WriteLine("Position: " + r);
    }
}