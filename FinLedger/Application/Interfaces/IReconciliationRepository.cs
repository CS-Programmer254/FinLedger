using FinLedger.Domain.Entities;

namespace FinLedger.Application.Interfaces;
/// <summary>
/// Reconciliation Repository - Loads/saves reconciliation snapshots
/// </summary>
public interface IReconciliationRepository
{
    Task AddSnapshotAsync(ReconciliationSnapshot snapshot);
    Task<ReconciliationSnapshot?> GetLatestAsync();
    Task<IEnumerable<ReconciliationSnapshot>> GetHistoryAsync(int days = 30);
}