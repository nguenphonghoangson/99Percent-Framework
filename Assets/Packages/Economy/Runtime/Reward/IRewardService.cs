using NinetyNine.Core;

namespace NinetyNine.Modules.Reward
{
    /// <summary>
    ///     Grants a <see cref="RewardBundle" /> by routing each entry to the module that owns it (currency →
    ///     Economy, item → Inventory). Validates the whole bundle before touching anything, so a bundle with
    ///     one bad entry grants nothing rather than half.
    /// </summary>
    public interface IRewardService
    {
        Result Validate(RewardBundle bundle);

        Result Grant(RewardBundle bundle, string source);
    }

    public static class RewardErrors
    {
        public const string EmptyBundle = "reward.empty_bundle";
        public const string InvalidEntry = "reward.invalid_entry";
        public const string UnknownCurrency = "reward.unknown_currency";
    }

    public readonly struct RewardGrantedEvent
    {
        public RewardGrantedEvent(RewardBundle bundle, string source)
        {
            Bundle = bundle;
            Source = source;
        }

        public RewardBundle Bundle { get; }
        public string Source { get; }
    }
}
