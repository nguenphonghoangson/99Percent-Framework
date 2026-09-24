using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NinetyNine.Core;
using NinetyNine.Persistence;

namespace NinetyNine.Modules.Iap
{
    [Serializable]
    internal sealed class IapState
    {
        public List<string> processedTransactions = new();
    }

    internal sealed class IapService : IIapService, IInitializable, IDisposable
    {
        private const string SaveKey = "module.iap";

        // Replays arrive within days, not years; the cap only keeps the save from growing forever.
        private const int MaxRememberedTransactions = 200;

        private readonly IEventBus _events;
        private readonly IIapProvider _provider;
        private readonly ISaveService _save;
        private IapState _state = new();

        public IapService(IIapProvider provider, ISaveService save, IEventBus events)
        {
            _provider = provider;
            _save = save;
            _events = events;
        }

        public void Initialize()
        {
            _state = _save.Load<IapState>(SaveKey);
            _provider.PurchaseDelivered += Deliver;
        }

        public void Dispose() => _provider.PurchaseDelivered -= Deliver;

        public bool IsPurchasing { get; private set; }

        public async Task<IapPurchaseResult> PurchaseAsync(string productId)
        {
            if (IsPurchasing) return new IapPurchaseResult(productId, IapPurchaseStatus.AlreadyInProgress);

            IsPurchasing = true;
            try
            {
                var result = await _provider.PurchaseAsync(productId);
                if (result.Status == IapPurchaseStatus.Success) Deliver(result);
                return result;
            }
            finally
            {
                IsPurchasing = false;
            }
        }

        public string GetLocalizedPrice(string productId) => _provider.GetLocalizedPrice(productId);

        private void Deliver(IapPurchaseResult result)
        {
            if (result.Status != IapPurchaseStatus.Success) return;

            if (!string.IsNullOrEmpty(result.TransactionId))
            {
                if (_state.processedTransactions.Contains(result.TransactionId)) return;

                _state.processedTransactions.Add(result.TransactionId);
                if (_state.processedTransactions.Count > MaxRememberedTransactions)
                    _state.processedTransactions.RemoveAt(0);
                _save.Save(SaveKey, _state);
            }

            _events.Publish(new IapPurchaseCompletedEvent(result.ProductId, result.TransactionId));
        }
    }
}
