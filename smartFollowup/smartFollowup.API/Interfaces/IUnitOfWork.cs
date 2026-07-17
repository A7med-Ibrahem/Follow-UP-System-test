using SmartFollowUp.API.Data;

namespace SmartFollowUp.API.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        AppDbContext Context { get; }
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}