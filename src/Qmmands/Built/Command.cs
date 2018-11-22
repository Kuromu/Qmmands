using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Qmmands
{
    /// <summary>
    ///     Represents a command built using the <see cref="CommandService"/>.
    /// </summary>
    public sealed class Command
    {
        /// <summary>
        ///     Gets the name of this <see cref="Command"/>.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Gets the description of this <see cref="Command"/>.
        /// </summary>
        public string Description { get; }

        /// <summary>
        ///     Gets the remarks of this <see cref="Command"/>.
        /// </summary>
        public string Remarks { get; }

        /// <summary>
        ///     Gets the priority of this <see cref="Command"/>.
        /// </summary>
        public int Priority { get; }

        /// <summary>
        ///     Gets the <see cref="Qmmands.RunMode"/> of this <see cref="Command"/>.
        /// </summary>
        public RunMode RunMode { get; }

        /// <summary>
        ///     Gets whether this <see cref="Command"/> ignores extra arguments or not.
        /// </summary>
        public bool IgnoreExtraArguments { get; }

        /// <summary>
        ///     Gets the callback of this <see cref="Command"/>.
        /// </summary>
        public CommandCallbackDelegate Callback { get; }

        /// <summary>
        ///     Gets the <see cref="CooldownAttribute"/>s of this <see cref="Command"/>.
        /// </summary>
        public IReadOnlyList<CooldownAttribute> Cooldowns { get; }

        /// <summary>
        ///     Gets the aliases of this <see cref="Command"/>.
        /// </summary>
        public IReadOnlyList<string> Aliases { get; }

        /// <summary>
        ///     Gets the full aliases of this <see cref="Command"/>.
        /// </summary>
        /// <remarks>
        ///     Aliases of parent modules and this command concatenated using the <see cref="CommandService.Separator"/>.
        /// </remarks>
        public IReadOnlyList<string> FullAliases { get; }

        /// <summary>
        ///     Gets the checks of this <see cref="Command"/>.
        /// </summary>
        public IReadOnlyList<CheckBaseAttribute> Checks { get; }

        /// <summary>
        ///     Gets the attributes of this <see cref="Command"/>.
        /// </summary>
        public IReadOnlyList<Attribute> Attributes { get; }

        /// <summary>
        ///     Gets the parameters of this <see cref="Command"/>.
        /// </summary>
        public IReadOnlyList<Parameter> Parameters { get; }

        /// <summary>
        ///     Gets the <see cref="Qmmands.Module"/> of this <see cref="Command"/>.
        /// </summary>
        public Module Module { get; }

        internal CommandService Service => Module.Service;

        internal Command(CommandBuilder builder, Module module)
        {
            Module = module;

            Description = builder.Description;
            Remarks = builder.Remarks;
            Priority = builder.Priority;
            RunMode = builder.RunMode ?? module.RunMode;
            IgnoreExtraArguments = builder.IgnoreExtraArguments ?? module.IgnoreExtraArguments;
            Callback = builder.Callback;
            Cooldowns = builder.Cooldowns;
            Aliases = builder.Aliases.ToImmutableArray();

            var fullAliases = ImmutableArray.CreateBuilder<string>();
            if (Module.FullAliases.Count == 0)
                fullAliases.AddRange(Aliases);

            else if (Aliases.Count == 0)
                fullAliases.AddRange(Module.FullAliases);

            else
            {
                for (var i = 0; i < Module.FullAliases.Count; i++)
                    for (var j = 0; j < Aliases.Count; j++)
                        fullAliases.Add(string.Concat(Module.FullAliases[i], Service.Separator, Aliases[j]));
            }
            FullAliases = fullAliases.ToImmutable();

            Name = builder.Name ?? (FullAliases.Count > 0 ? FullAliases[0] : null);

            for (var i = 0; i < builder.Checks.Count; i++)
            {
                var check = builder.Checks[i];
                check.Module = Module;
                check.Command = this;
            }
            Checks = builder.Checks.ToImmutableArray();
            Attributes = builder.Attributes.Except(builder.Attributes.OfType<CooldownAttribute>()).ToImmutableArray();

            var parameters = ImmutableArray.CreateBuilder<Parameter>();
            for (var i = 0; i < builder.Parameters.Count; i++)
                parameters.Add(builder.Parameters[i].Build(this));
            Parameters = parameters.ToImmutable();
        }

        /// <summary>
        ///     Runs checks on parent modules and this command.
        /// </summary>
        /// <param name="context"> The <see cref="ICommandContext"/> used for execution. </param>
        /// <param name="provider"> The <see cref="IServiceProvider"/> used for execution. </param>
        /// <returns>
        ///     A <see cref="SuccessfulResult"/> if all of the checks pass, otherwise a <see cref="ChecksFailedResult"/>.
        /// </returns>
        public async Task<IResult> RunChecksAsync(ICommandContext context, IServiceProvider provider = null)
        {
            if (provider is null)
                provider = EmptyServiceProvider.Instance;

            var result = await Module.RunChecksAsync(context, provider).ConfigureAwait(false);
            if (!result.IsSuccessful)
                return result;

            if (Checks.Count > 0)
            {
                var checkResults = await Task.WhenAll(Checks.Select(x => RunCheckAsync(x, context, provider))).ConfigureAwait(false);
                var failedGroups = checkResults.GroupBy(x => x.Check.Group).Where(x => x.Key == null ? x.Any(y => y.Error != null) : x.All(y => y.Error != null)).ToImmutableArray();
                if (failedGroups.Length > 0)
                    return new ChecksFailedResult(this, failedGroups.SelectMany(x => x).Where(x => x.Error != null).ToImmutableArray());
            }

            return new SuccessfulResult();
        }

        private async Task<(CheckBaseAttribute Check, string Error)> RunCheckAsync(CheckBaseAttribute check, ICommandContext context, IServiceProvider provider)
        {
            var checkResult = await check.CheckAsync(context, provider).ConfigureAwait(false);
            return (check, checkResult.Error);
        }

        /// <summary>
        ///     Runs cooldowns on this command.
        /// </summary>
        /// <param name="context"> The <see cref="ICommandContext"/> used for execution. </param>
        /// <param name="provider"> The <see cref="IServiceProvider"/> used for execution. </param>
        /// <returns>
        ///     A <see cref="SuccessfulResult"/> if no ratelimits are in place, otherwise a <see cref="CommandOnCooldownResult"/>.
        /// </returns>
        public async Task<IResult> RunCooldownsAsync(ICommandContext context, IServiceProvider provider = null)
        {
            provider = provider ?? EmptyServiceProvider.Instance;
            var ratelimited = new List<CooldownResult>();
            foreach (var cooldown in Cooldowns)
            {
                cooldown.Command = this;
                var result = await cooldown.GetCooldownAsync(context, provider).ConfigureAwait(false);
                if (!result.IsSuccessful)
                    ratelimited.Add(result);
            }

            return ratelimited.Any()
                ? new CommandOnCooldownResult(this, ratelimited.ToImmutableArray())
                : new SuccessfulResult() as IResult;
        }


        /// <summary>
        ///     Attempts to parse the raw arguments for this <see cref="Command"/> and execute it.
        ///     Short for <see cref="CommandService.ExecuteAsync(Command, string, ICommandContext, IServiceProvider)"/>
        /// </summary>
        /// <param name="rawArguments"> The raw arguments to use for this command's parameters. </param>
        /// <param name="context"> The <see cref="ICommandContext"/> to use during execution. </param>
        /// <param name="provider"> The <see cref="IServiceProvider"/> to use during execution. </param>
        /// <returns> An <see cref="IResult"/>. </returns>
        /// <exception cref="ArgumentNullException"> The command mustn't be null. </exception>
        /// <exception cref="ArgumentNullException"> The raw arguments mustn't be null. </exception>
        /// <exception cref="ArgumentNullException"> The context mustn't be null. </exception>
        public Task<IResult> ExecuteAsync(string rawArguments, ICommandContext context, IServiceProvider provider = null)
            => Service.ExecuteAsync(this, rawArguments, context, provider);

        /// <summary>
        ///     Returns <see cref="Name"/> or calls <see cref="object.ToString"/> if it's <see langword="null"/>.
        /// </summary>
        /// <returns>
        ///     A <see cref="string"/> representing this <see cref="Command"/>.
        /// </returns>
        public override string ToString()
            => Name ?? base.ToString();
    }
}