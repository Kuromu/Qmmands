using System;
using System.Threading.Tasks;

namespace Qmmands
{
    /// <summary>
    ///     Marks a <see cref="Qmmands.Command"/> as having a cooldown.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public abstract class CooldownAttribute : Attribute
    {
        /// <summary>
        ///     Gets the <see langword="enum"/> bucket type for this cooldown.
        /// </summary>
        public virtual object BucketType { get; }

        /// <summary>
        ///     Gets the <see cref="Qmmands.Command"/> that's being checked for cooldowns.
        /// </summary>
        public Command Command { get; internal set; }

        /// <summary>
        ///     Determines whether the <see cref="Command"/> is on cooldown.
        /// </summary>
        /// <param name="context"> The <see cref="ICommandContext"/> used during execution. </param>
        /// <param name="provider"> The <see cref="IServiceProvider"/> used during execution. </param>
        /// <returns></returns>
        public abstract Task<CooldownResult> GetCooldownsAsync(ICommandContext context, IServiceProvider provider);

        /// <summary>
        ///     Creates a <see cref="CooldownAttribute"/> with a specified cooldown bucket <see langword="enum"/>.
        /// </summary>
        /// <param name="bucketType">The <see langword="enum"/> bucket type for this cooldown.</param>
        protected CooldownAttribute(object bucketType)
        {
            if (!bucketType.GetType().IsEnum)
                throw new ArgumentException("Bucket type must be an enum.", nameof(bucketType));

            BucketType = bucketType;
        }
    }
}
