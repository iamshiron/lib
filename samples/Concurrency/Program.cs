using Shiron.Lib.Concurrency;

namespace Shiron.Lib.Samples.Concurrency;

public readonly record struct PrintJob(string Message) : IJob {
    public void Execute() {
        while (true) {
            Thread.Sleep(1000);
            Console.WriteLine(Message);
        }
    }
}

public static class Program {
    public static async Task<int> Main(string[] args) {
        using var scheduler = new JobScheduler();
        scheduler.Schedule(new PrintJob("Messsage 1"));
        scheduler.Schedule(new PrintJob("Messsage 2"));
        scheduler.Schedule(new PrintJob("Messsage 3"));

        var res = await scheduler.RunAsync(() => 27 + 19);
        Console.WriteLine($"Result: {res}");

        while (true) {
            if (Console.ReadLine() == "exit") break;

            Thread.Sleep(1);
        }

        Console.WriteLine("Finished!");

        return 0;
    }
}
