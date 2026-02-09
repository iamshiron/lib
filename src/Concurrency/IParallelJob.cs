namespace Shiron.Lib.Concurrency;

public interface IParallelJob {
    void Execute(int index);
}
