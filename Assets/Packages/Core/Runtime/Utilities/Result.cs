using System;

namespace NinetyNine.Core
{
    /// <summary>
    ///     Outcome of an operation that can be refused for a business reason. Refusals are values, not
    ///     exceptions: "not enough coins" is an expected path the UI has to render, not a crash.
    ///     <c>default(Result)</c> is a success.
    /// </summary>
    public readonly struct Result
    {
        public static readonly Result Success = default;

        private Result(string error) => Error = error;

        /// <summary>Error code (see the <c>*Errors</c> class of each module/feature). Null on success.</summary>
        public string Error { get; }

        public bool IsSuccess => Error == null;

        public static Result Fail(string error) =>
            new(error ?? throw new ArgumentNullException(nameof(error)));

        public override string ToString() => IsSuccess ? "Success" : Error;
    }

    public static class CommonErrors
    {
        public const string InvalidArgument = "common.invalid_argument";
        public const string FeatureLocked = "common.feature_locked";
    }
}
