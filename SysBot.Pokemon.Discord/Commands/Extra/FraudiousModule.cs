using PKHeX.Core;
using Discord.Commands;
using System.Threading.Tasks;
using Discord;
using System;
using System.IO;
using System.Threading;
using SysBot.Pokemon;
using SysBot.Base;

namespace SysBot.Pokemon.Discord
{
    [Summary("Generates and queues custom modules")]
    public class FraudiousModules<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
    {
        private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;
        [Command("checkcd")]
        [Summary("Allows users to check their current cooldown using NID")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task CooldownLeft([Remainder] string input)
        {
            bool isDistribution = true;
            var list = isDistribution ? PokeRoutineExecutorBase.PreviousUsersDistribution : PokeRoutineExecutorBase.PreviousUsers;
            var cooldown = list.TryGetPreviousNID(ulong.Parse(input));
            if (cooldown != null)
            {
                string trainerName = cooldown.ToString().Substring(21, cooldown.ToString().IndexOf('=', cooldown.ToString().IndexOf('=') + 1) - 31);
                var delta = DateTime.Now - cooldown.Time;
                double ddelta = delta.TotalMinutes;
                if (ddelta.CompareTo((double)SysCordSettings.HubConfig.TradeAbuse.TradeCooldown) < 1)
                    await ReplyAsync($"{trainerName} your cooldown is currently on {delta.TotalMinutes:F1} out of {SysCordSettings.HubConfig.TradeAbuse.TradeCooldown} minutes.").ConfigureAwait(false);
                else
                    await ReplyAsync($"{trainerName} your cooldown of {SysCordSettings.HubConfig.TradeAbuse.TradeCooldown} minutes has expired. Thank you come again!").ConfigureAwait(false);
            }
            else
                await ReplyAsync($"User has not traded with the bot recently.").ConfigureAwait(false);
            if (!Context.IsPrivate)
                await Context.Message.DeleteAsync(RequestOptions.Default).ConfigureAwait(false);
        }

        [Command("peek")]
        [Summary("Take and send a screenshot from the specified Switch.")]
        [RequireSudo]
        public async Task Peek(string address)
        {
            var source = new CancellationTokenSource();
            var token = source.Token;

            var bot = SysCord<T>.Runner.GetBot(address);
            if (bot == null)
            {
                await ReplyAsync($"No bot found with the specified address ({address}).").ConfigureAwait(false);
                return;
            }

            var c = bot.Bot.Connection;
            var bytes = await c.PixelPeek(token).ConfigureAwait(false);
            if (bytes.Length == 1)
            {
                await ReplyAsync($"Failed to take a screenshot for bot at {address}. Is the bot connected?").ConfigureAwait(false);
                return;
            }
            MemoryStream ms = new(bytes);

            var img = "SphealCheck.jpg";
            var embed = new EmbedBuilder { ImageUrl = $"attachment://{img}", Color = Color.Blue }.WithFooter(new EmbedFooterBuilder { Text = $"Captured image from bot at address {address}." });
            await Context.Channel.SendFileAsync(ms, img, "", false, embed: embed.Build());
        }
    }
}
