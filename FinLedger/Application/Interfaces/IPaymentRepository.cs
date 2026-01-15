using FinLedger.Domain.Aggregates;
using FinLedger.Domain.Enums;

namespace FinLedger.Application.Interfaces;
/// <summary>
/// Payment Repository - Only loads/saves Payment aggregate roots
/// No queries against child entities
/// </summary>
public interface IPaymentRepository
{
    Task AddAsync(Payment payment);
    Task<Payment?> GetByReferenceAsync(string reference);
    Task<Payment?> GetByIdAsync(Guid id);
    Task UpdateAsync(Payment payment);
    Task<IEnumerable<Payment>> GetPendingPaymentsAsync();
    Task<IEnumerable<Payment>> GetByStatusAsync(PaymentStatus status);
}
