using System.Diagnostics;
using Shiron.Lib.Flow;

var throttler = new Throttler(500);
var leading = new LeadingDebouncer(500); // Your implementation
var trailing = new TrailingDebouncer(500);

Console.CursorVisible = false;
Console.WriteLine("=== FLOW CONTROL VISUALIZER ===");
Console.WriteLine("Press [SPACE] to simulate input. Watch the patterns.");
Console.WriteLine("-----------------------------------------------------");
Console.WriteLine("Legend:");
Console.WriteLine("I : Input Signal");
Console.WriteLine("T : Throttler");
Console.WriteLine("L : Leading Debouncer");
Console.WriteLine("R : Trailing Debouncer");
Console.WriteLine("-----------------------------------------------------");

// Buffer for the scrolling graph
var historyLength = Console.WindowWidth - 50;
var rowInput = new char[historyLength];
var rowThrottle = new char[historyLength];
var rowLeading = new char[historyLength];
var rowTrailing = new char[historyLength];

Array.Fill(rowInput, ' ');
Array.Fill(rowThrottle, ' ');
Array.Fill(rowLeading, ' ');
Array.Fill(rowTrailing, ' ');

while (true) {
    Array.Copy(rowInput, 1, rowInput, 0, historyLength - 1);
    Array.Copy(rowThrottle, 1, rowThrottle, 0, historyLength - 1);
    Array.Copy(rowLeading, 1, rowLeading, 0, historyLength - 1);
    Array.Copy(rowTrailing, 1, rowTrailing, 0, historyLength - 1);

    rowInput[^1] = ' ';
    rowThrottle[^1] = ' ';
    rowLeading[^1] = ' ';
    rowTrailing[^1] = ' ';

    // 3. Handle Input
    var isInput = false;
    if (Console.KeyAvailable) {
        var k = Console.ReadKey(true);
        if (k.Key == ConsoleKey.Spacebar) {
            isInput = true;
        }
        if (k.Key == ConsoleKey.Escape) {
            break;
        }

        // Clear buffer so we don't process 50 keys at once
        while (Console.KeyAvailable) Console.ReadKey(true);
    }

    // 4. Logic Execution
    if (isInput) {
        rowInput[^1] = 'I'; // Mark Input

        if (throttler.TryExecute()) {
            rowThrottle[^1] = 'X';
        } else {
            rowThrottle[^1] = '-'; // Cooldown indicator
        }

        if (leading.TryExecute()) {
            rowLeading[^1] = 'X';
        } else {
            rowLeading[^1] = 'r'; // 'r' means "reset timer"
        }

        trailing.Signal();
        rowTrailing[^1] = 's'; // 's' means "signal received"
    } else {
        if (trailing.TryResolve()) {
            rowTrailing[^1] = 'X';
        }
    }

    Console.SetCursorPosition(0, 7);
    Console.Write($"INPUT : [{new string(rowInput)}]\n");
    Console.Write($"THROT : [{new string(rowThrottle)}]\n");
    Console.Write($"LEAD  : [{new string(rowLeading)}]\n");
    Console.Write($"TRAIL : [{new string(rowTrailing)}]\n");

    Thread.Sleep(50); // 20 ticks per second
}
