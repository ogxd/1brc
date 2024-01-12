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
}