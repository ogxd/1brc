namespace OneBillionRowsChallenge.Tests;

public class Tests
{
    [TestCase(new byte[] { 1, 2, 3, 4, 5, 6 }, 3)]
    [TestCase(new byte[] { 1, 2, 3, 4, 5, 6 }, 4)]
    [TestCase(new byte[] { 1, 2, 3, 4, 5, 6 }, 1)]
    [TestCase(new byte[] { 1, 2, 3, 4, 5, 6 }, 8)]
    [TestCase(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }, 1)]
    [TestCase(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }, 1)]
    public void IndexOfCustom_Works(byte[] bytes, byte c)
    {
        ref byte byteRef = ref bytes[0];
        Assert.That(Utils.IndexOf(ref byteRef, bytes.Length, c), Is.EqualTo(bytes.AsSpan().IndexOf(c)));
    }
}