using System;
using System.Threading.Tasks;

namespace NinetyNine.Modules.Iap
{
    public enum IapPurchaseStatus
    {
        Success,
        Cancelled,
        Failed,
        Pending,
        AlreadyInProgress
    }

    public readonly struct IapPurchaseResult
    {
        public IapPurchaseResult(string productId, IapPurchaseStatus status, string transactionId = null,
            string error = null)
        {
            ProductId = productId;
            Status = status;
            TransactionId = transactionId;
            Error = error;
        }

        public string ProductId { get; }
        public IapPurchaseStatus Status { get; }
        public string TransactionId { get; }
        public string Error { get; }
    }

    /// <summary>
    ///     Store backend (Unity IAP, a fake for the editor). Implementations hand back purchases that are
    ///     already receipt-validated; <see cref="PurchaseDelivered" /> carries purchases that complete outside a
    ///     <see cref="PurchaseAsync" /> call — deferred approvals, or unfinished transactions replayed at launch.
    /// </summary>
    public interface IIapProvider
    {
        event Action<IapPurchaseResult> PurchaseDelivered;

        Task<IapPurchaseResult> PurchaseAsync(string productId);

        string GetLocalizedPrice(string productId);
    }

    /// <summary>
    ///     Store facade. Every successful purchase — in-call or delivered later — is announced once through
    ///     <see cref="IapPurchaseCompletedEvent" />, deduplicated by transaction id. Features grant on that
    ///     event, not on the task result, so a purchase finished after a crash is still paid out.
    /// </summary>
    public interface IIapService
    {
        bool IsPurchasing { get; }

        Task<IapPurchaseResult> PurchaseAsync(string productId);

        string GetLocalizedPrice(string productId);
    }

    public readonly struct IapPurchaseCompletedEvent
    {
        public IapPurchaseCompletedEvent(string productId, string transactionId)
        {
            ProductId = productId;
            TransactionId = transactionId;
        }

        public string ProductId { get; }
        public string TransactionId { get; }
    }
}
