using System;
using System.Collections.Generic;
using NinetyNine.Core;
using NinetyNine.Persistence;

namespace NinetyNine.Modules.Economy
{
    [Serializable]
    internal sealed class EconomyState
    {
        public CounterTable balances = new();

        // Currencies whose initial balance has been handed out. Checked instead of "balance == 0" so a player
        // who spent down to zero is not refilled on the next launch.
        public List<string> seeded = new();
    }

    internal sealed class EconomyService : IEconomyService, IInitializable
    {
        private const string SaveKey = "module.economy";

        private readonly EconomyConfig _config;
        private readonly IEventBus _events;
        private readonly ISaveService _save;
        private EconomyState _state = new();

        public EconomyService(EconomyConfig config, ISaveService save, IEventBus events)
        {
            _config = config ? config : throw new ArgumentNullException(nameof(config));
            _save = save;
            _events = events;
        }

        public void Initialize()
        {
            _state = _save.Load<EconomyState>(SaveKey);

            var changed = false;
            foreach (var currency in _config.currencies)
            {
                if (_state.seeded.Contains(currency.id)) continue;

                _state.seeded.Add(currency.id);
                _state.balances.Set(currency.id, _state.balances.Get(currency.id) + currency.initialBalance);
                changed = true;
            }

            if (changed) _save.Save(SaveKey, _state);
        }

        public bool IsKnown(string currencyId) => _config.Find(currencyId) != null;

        public long GetBalance(string currencyId) => _state.balances.Get(currencyId);

        public long GetCap(string currencyId) => _config.Find(currencyId)?.maxBalance ?? 0;

        public bool CanAfford(Cost cost) => cost.IsFree || GetBalance(cost.currencyId) >= cost.amount;

        public Result TrySpend(Cost cost, string source)
        {
            if (cost.IsFree) return Result.Success;
            if (!IsKnown(cost.currencyId)) return Result.Fail(EconomyErrors.UnknownCurrency);
            if (!CanAfford(cost)) return Result.Fail(EconomyErrors.InsufficientFunds);

            Apply(cost.currencyId, GetBalance(cost.currencyId) - cost.amount, source);
            return Result.Success;
        }

        public Result Add(string currencyId, long amount, string source)
        {
            if (amount <= 0) return Result.Fail(EconomyErrors.InvalidAmount);

            var definition = _config.Find(currencyId);
            if (definition == null) return Result.Fail(EconomyErrors.UnknownCurrency);

            var next = GetBalance(currencyId) + amount;
            if (definition.maxBalance > 0) next = Math.Min(next, definition.maxBalance);

            Apply(currencyId, next, source);
            return Result.Success;
        }

        private void Apply(string currencyId, long next, string source)
        {
            var previous = GetBalance(currencyId);
            _state.balances.Set(currencyId, next);
            _save.Save(SaveKey, _state);
            _events.Publish(new CurrencyChangedEvent(currencyId, previous, next, source));
        }
    }
}
