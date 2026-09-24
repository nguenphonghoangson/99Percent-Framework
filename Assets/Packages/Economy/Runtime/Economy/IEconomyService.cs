using NinetyNine.Core;

namespace NinetyNine.Modules.Economy
{
    /// <summary>
    ///     Currency balances. Every change carries a <c>source</c> ("shop:coin_pack", "level_win") so
    ///     analytics and support can trace where a balance came from.
    /// </summary>
    public interface IEconomyService
    {
        bool IsKnown(string currencyId);

        long GetBalance(string currencyId);

        /// <summary>Configured max balance; 0 when uncapped or unknown. Lets UI show "MAX" (lives, energy).</summary>
        long GetCap(string currencyId);

        bool CanAfford(Cost cost);

        /// <summary>All-or-nothing: an overspend is refused, never clamped.</summary>
        Result TrySpend(Cost cost, string source);

        /// <summary>Clamps at the currency's max balance when one is configured.</summary>
        Result Add(string currencyId, long amount, string source);
    }

    public static class EconomyErrors
    {
        public const string UnknownCurrency = "economy.unknown_currency";
        public const string InvalidAmount = "economy.invalid_amount";
        public const string InsufficientFunds = "economy.insufficient_funds";
    }

    public readonly struct CurrencyChangedEvent
    {
        public CurrencyChangedEvent(string currencyId, long previous, long current, string source)
        {
            CurrencyId = currencyId;
            Previous = previous;
            Current = current;
            Source = source;
        }

        public string CurrencyId { get; }
        public long Previous { get; }
        public long Current { get; }
        public long Delta => Current - Previous;
        public string Source { get; }
    }
}
