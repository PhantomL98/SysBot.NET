using PKHeX.Core;
using Discord.Commands;
using System.Threading.Tasks;
using Discord;
using System;
using System.IO;
using System.Threading;
using SysBot.Pokemon;
using SysBot.Base;
using SysBot.Fraudious;
using LibUsbDotNet.Main;
using System.Diagnostics;
using System.Linq;

namespace SysBot.Pokemon.Discord
{
    [Summary("Custom Fraudious commands")]
    public class FraudiousModules<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
    {

        private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

        [Command("whatbot")]
        [Alias("wb")]
        [Summary("Sends the type of bot to requesting admin")]
        [RequireSudo]

        public async Task whatbot()
        {
            var me = SysCord<T>.Runner;
            var bot = me.Bots.Select(z => z.Bot);
            //string botversion;
            if (bot is not null && me is not null)
            {       
                var botversion = me.ToString()!.Substring(46, 3);
                
                switch (botversion)
                {
                        case "PK8":
                            IUserMessage test = await Context.User.SendMessageAsync("The Bot is currently: " + me.ToString()).ConfigureAwait(false);
                            test = await Context.User.SendMessageAsync("The Bot is currently: " + botversion).ConfigureAwait(false);
                            break;
                }
            }
        }

        [Command("reservetrade")]
        [Alias("rsv")]
        [Summary("Allows users to reserve the next distribution trade in the bot")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task RSVP()
        {
            var code = Info.GetRandomTradeCode();
            var sig = Context.User.GetFavor();
            await QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, new T(), PokeRoutineType.DirectTrade, PokeTradeType.Random).ConfigureAwait(false);
        }

        [Command("reservetrade")]
        [Alias("rsv")]
        [Summary("Allows users to reserve the next distribution trade in the bot")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task RSVP([Summary("Trade Code")] int code)
        {
            var sig = Context.User.GetFavor();
            await QueueHelper<T>.AddToQueueAsync(Context, code, Context.User.Username, sig, new T(), PokeRoutineType.DirectTrade, PokeTradeType.Random).ConfigureAwait(false);
        }

        [Command("checkcooldown")]
        [Alias("checkcd", "ccd")]
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
                var cd = SysCordSettings.HubConfig.TradeAbuse.TradeCooldown;

                if (ddelta.CompareTo((double)SysCordSettings.HubConfig.TradeAbuse.TradeCooldown) < 1)
                {
                    EmbedBuilder? embed = Fraudiouscl.EmbedCDMessage2(cd, $"{trainerName} your cooldown is currently on {delta.TotalMinutes:F1} out of {cd} minutes.", "Cooldown Notification");
                    await ReplyAsync("", embed: embed.Build()).ConfigureAwait(false);
                }
            else
                {
                    EmbedBuilder? embed = Fraudiouscl.EmbedCDMessage2(cd, $"{trainerName}, your cooldown of {cd} minutes has expired.Thank you come again!", "Cooldown Notification");
                    await ReplyAsync("", embed: embed.Build()).ConfigureAwait(false);
                }
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

            var img = "FraudCheck.jpg";
            var embed = new EmbedBuilder { ImageUrl = $"attachment://{img}", Color = Color.Blue }.WithFooter(new EmbedFooterBuilder { Text = $"Captured image from bot at address {address}." });
            await Context.Channel.SendFileAsync(ms, img, "", false, embed: embed.Build());
        }
    }
}
