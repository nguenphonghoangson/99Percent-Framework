using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NinetyNine.Modules.Iap
{
    /// <summary>Editor / dev-build store. Completes synchronously with <see cref="NextStatus" />.</summary>
    public sealed class FakeIapProvider : IIapProvider
    {
        private readonly Dictionary<string, string> _prices = new();

        public IapPurchaseStatus NextStatus { get; set; } = IapPurchaseStatus.Success;

        public event Action<IapPurchaseResult> PurchaseDelivered;

        public Task<IapPurchaseResult> PurchaseAsync(string productId) =>
            Task.FromResult(NextStatus == IapPurchaseStatus.Success
                ? new IapPurchaseResult(productId, IapPurchaseStatus.Success, NewTransactionId())
                : new IapPurchaseResult(productId, NextStatus, error: "Fake store: " + NextStatus));

        public string GetLocalizedPrice(string productId) => _prices.TryGetValue(productId, out var price) ? price : "$0.99";

        public FakeIapProvider SetPrice(string productId, string label)
        {
            _prices[productId] = label;
            return this;
        }

        /// <summary>Simulates a purchase finishing outside a PurchaseAsync call (deferred, or replayed at launch).</summary>
        public void Deliver(string productId, string transactionId = null) =>
            PurchaseDelivered?.Invoke(new IapPurchaseResult(productId, IapPurchaseStatus.Success,
                transactionId ?? NewTransactionId()));

        private static string NewTransactionId() => "fake-" + Guid.NewGuid().ToString("N");
    }
}
