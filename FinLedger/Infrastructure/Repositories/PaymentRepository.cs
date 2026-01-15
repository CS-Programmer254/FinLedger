using FinLedger.Application.Interfaces;
using FinLedger.Domain.Aggregates;
using FinLedger.Domain.Enums;
using FinLedger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinLedger.Infrastructure.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly PaymentsDbContext _context;
    private readonly ILogger<PaymentRepository> _logger;

    public PaymentRepository(PaymentsDbContext context, ILogger<PaymentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddAsync(Payment payment)
    {
        _logger.LogInformation("Adding Payment aggregate - ID: {PaymentId}", payment.Id);

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment persisted - ID: {PaymentId}", payment.Id);
    }

    public async Task<Payment?> GetByReferenceAsync(string reference)
    {
        _logger.LogDebug("Loading Payment by reference: {Reference}", reference);

        // Always load tracked aggregate with child LedgerEntries
        var payment = await _context.Payments
            .Include(p => p.LedgerEntries)
            .FirstOrDefaultAsync(p => p.Reference.Value == reference);

        return payment;
    }

    public async Task<Payment?> GetByIdAsync(Guid id)
    {
        _logger.LogDebug("Loading Payment by ID: {PaymentId}", id);

        var payment = await _context.Payments
            .Include(p => p.LedgerEntries)
            .FirstOrDefaultAsync(p => p.Id == id);

        return payment;
    }

    //public async Task UpdateAsync(Payment payment)
    //{
    //    _logger.LogInformation("Updating Payment aggregate - ID: {PaymentId}", payment.Id);

    //    // Attach if not tracked
    //    var entry = _context.Entry(payment);
    //    if (entry.State == EntityState.Detached)
    //    {
    //        _context.Payments.Attach(payment);
    //        entry = _context.Entry(payment);
    //    }

    //    // Tell EF Core to update aggregate and its owned child entities
    //    entry.State = EntityState.Modified;

    //    foreach (var ledger in payment.LedgerEntries)
    //    {
    //        var ledgerEntry = _context.Entry(ledger);
    //        if (ledgerEntry.State == EntityState.Detached)
    //        {
    //            _context.LedgerEntries.Attach(ledger);
    //            ledgerEntry.State = EntityState.Modified;
    //        }
    //    }

    //    await _context.SaveChangesAsync();

    //    _logger.LogInformation("Payment updated - ID: {PaymentId}", payment.Id);
    //}

    public async Task<IEnumerable<Payment>> GetPendingPaymentsAsync()
    {
        _logger.LogDebug("Loading pending payments");

        return await _context.Payments
            .Where(p => p.Status == PaymentStatus.Pending)
            .Include(p => p.LedgerEntries)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetByStatusAsync(PaymentStatus status)
    {
        _logger.LogDebug("Loading payments with status: {Status}", status);

        return await _context.Payments
            .Where(p => p.Status == status)
            .Include(p => p.LedgerEntries)
            .ToListAsync();
    }

    public Task UpdateAsync(Payment payment)
    {
        throw new NotImplementedException();
    }
}
