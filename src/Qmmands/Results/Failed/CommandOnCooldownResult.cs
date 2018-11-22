using System;
using System.Collections.Generic;
using System.Linq;

namespace Qmmands
{
    /// <summary>
    ///     Represents the command being on a cooldown.
    /// </summary>
    public sealed class CommandOnCooldownResult : FailedResult
    {
        /// <inheritdoc />
        public override string Reason { get; }

        /// <summary>
        ///     Gets the <see cref="Qmmands.Command"/> that's on cooldown.
        /// </summary>
        public Command Command { get; }

        /// <summary>
        ///     Gets the <see cref="CooldownResult"/>s representing failed cooldown checks.
        /// </summary>
        public IReadOnlyList<CooldownResult> Cooldowns { get;}

        internal CommandOnCooldownResult(Command command, IReadOnlyList<CooldownResult> cooldowns)
        {
            Command = command;
            Cooldowns = cooldowns;
            Reason = cooldowns.Count == 1
                ? $"Command '{command.Name}' is on a '{cooldowns[0].BucketType}' cooldown. Retry after {cooldowns[0].RetryAfter}."
                : $"Command '{command.Name}' is on multiple cooldowns: {string.Join(", ", cooldowns.Select(x => $"'{x.BucketType}' - retry after {x.RetryAfter}"))}";
        }
    }
}
