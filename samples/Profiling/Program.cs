
using Shiron.Lib.Profiling;

var profiler = new Profiler(null);

using (new ProfileCategory("Init")) {
    using (new ProfileScope(profiler, "Load Config")) {
        Task.Delay(150).Wait();
    }
}

using (new ProfileCategory("Processing")) {
    using (new ProfileScope(profiler, "Step 1")) {
        Task.Delay(200).Wait();
    }

    using (new ProfileScope(profiler, "Step 2")) {
        Task.Delay(300).Wait();
    }

    using (new ProfileScope(profiler, "Step 3")) {
        Task.WaitAll(
            Task.Run(() => {
                using (new ProfileScope(profiler, "Subtask A")) {
                    Task.Delay(100).Wait();
                }
            }),
            Task.Run(() => {
                using (new ProfileScope(profiler, "Subtask B")) {
                    Task.Delay(150).Wait();
                }
            }),
            Task.Run(() => {
                using (new ProfileScope(profiler, "Subtask C")) {
                    Task.Delay(200).Wait();
                }
            }
        ));
    }
}

using (new ProfileCategory("Finalizing")) {
    using (new ProfileScope(profiler, "Cleanup")) {
        Task.Delay(100).Wait();
    }
}

profiler.SaveToFile("profiles");
