using System.Diagnostics;
using OneBillionRowsChallenge;

var path = args.Length > 0 ? args[0] : "C:\\Users\\ogini\\code\\1brc-java\\measurements1B.txt";
        
var sw = Stopwatch.StartNew();
Parser parser = new Parser();
parser.Parse(path);
sw.Stop();

Console.WriteLine($"Processed in {sw.Elapsed}");