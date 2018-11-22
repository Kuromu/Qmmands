using System;

namespace Qmmands
{
    /// <summary>
    ///     Represents an individual command cooldown.
    /// </summary>
    public class CooldownResult : IResult
    {
        /// <summary>
        ///     Gets the <see cref="Qmmands.Command"/> that's on cooldown.
        /// </summary>
        public Command Command { get; }

        /// <summary>
        ///     Gets the amount of time until the command is able to be retried.
        /// </summary>
        public TimeSpan RetryAfter { get; }

        /// <inheritdoc />
        public bool IsSuccessful => RetryAfter > TimeSpan.Zero;

        /// <summary>
        ///     Gets the <see langword="enum"/> bucket type for this cooldown.
        /// </summary>
        public object BucketType { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bucketType">The <see langword="enum"/> bucket type for this cooldown.</param>
        /// <param name="command">The <see cref="Qmmands.Command"/> on cooldown.</param>
        /// <param name="retryAfter">The amount of time until the command is able to be retried.</param>
        public CooldownResult(object bucketType, Command command, TimeSpan retryAfter)
        {
            if (!bucketType.GetType().IsEnum)
                throw new ArgumentException("Bucket type must be an enum.", nameof(bucketType));
            BucketType = bucketType;
            Command = command;
            if (retryAfter <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(retryAfter), "Cooldown mustn't be zero or negative.");

            RetryAfter = retryAfter;
        }

        private CooldownResult()
        { }

        /// <summary>
        ///     Represents a command that is not on cooldown.
        /// </summary>
        public static CooldownResult None => new CooldownResult();
    }
}