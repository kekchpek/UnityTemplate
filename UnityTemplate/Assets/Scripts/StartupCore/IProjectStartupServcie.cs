namespace Startup.Core
{
    public interface IProjectStartupService : IStartupService
    {
        bool IsCompleted { get; }
    }
}