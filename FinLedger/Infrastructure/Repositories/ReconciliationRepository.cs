using FinLedger.Application.Interfaces;
using FinLedger.Domain.Entities;
using FinLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinLedger.Infrastructure.Repositories;

/// <summary>
/// Reconciliation Repository Implementation
/// </summary>
public sealed class ReconciliationRepository : IReconciliationRepository
{
    private readonly PaymentsDbContext _context;
    private readonly ILogger<ReconciliationRepository> _logger;

    public ReconciliationRepository(PaymentsDbContext context, ILogger<ReconciliationRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddSnapshotAsync(ReconciliationSnapshot snapshot)
    {
        _context.ReconciliationSnapshots.Add(snapshot);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Reconciliation snapshot created - Total: {Total}, Balanced: {IsBalanced}"
            , snapshot.TotalPayments, snapshot.IsBalanced);
    }

    public async Task<ReconciliationSnapshot?> GetLatestAsync() =>
        await _context.ReconciliationSnapshots
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

    public async Task<IEnumerable<ReconciliationSnapshot>> GetHistoryAsync(int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        return await _context.ReconciliationSnapshots
            .Where(r => r.CreatedAt >= since)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }
}