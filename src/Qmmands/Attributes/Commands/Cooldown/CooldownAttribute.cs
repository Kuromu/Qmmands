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
        public virtual object BucketType { get; }

        /// <summary>
        ///     Determines whether the <see cref="Command"/> is on cooldown.
        /// </summary>
        /// <param name="context"> The <see cref="ICommandContext"/> used during execution. </param>
        /// <param name="provider"> The <see cref="IServiceProvider"/> used during execution. </param>
        /// <returns></returns>
        public abstract Task<CooldownResult> GetCooldownAsync(ICommandContext context, IServiceProvider provider);

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
