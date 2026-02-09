using Shiron.Lib.Utils;

using var process = ProcessUtils.RunProcessBackground("ls", [], null, (msg) => {
    Console.WriteLine($"[STDOUT_1] {msg}");
});

using var process2 = ProcessUtils.RunProcessBackground("ls", [], null, (msg) => {
    Console.WriteLine($"[STDOUT_2] {msg}");
});

var res = process.WaitForExit();
var res2 = process2.WaitForExit();
Console.WriteLine($"Process 1 exit code: {res}");
Console.WriteLine($"Process 2 exit code: {res2}");
