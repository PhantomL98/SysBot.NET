﻿using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using SysBot.Fraudious;
using NLog.Targets.Wrappers;
using PKHeX.Core;
using System;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public static class QueueHelper<T> where T : PKM, new()
    {
        static readonly HttpClient client = new();

        private const uint MaxTradeCode = 9999_9999;

        public static async Task AddToQueueAsync(SocketCommandContext context, int code, string trainer, RequestSignificance sig, T trade, PokeRoutineType routine, PokeTradeType type, SocketUser trader)
        {
            if ((uint)code > MaxTradeCode)
            {
                await context.Channel.SendMessageAsync("Trade code should be 00000000-99999999!").ConfigureAwait(false);
                return;
            }

            try
            {
                const string helper = "I've added you to the queue! I'll message you here when your trade is starting.";
                IUserMessage test = await trader.SendMessageAsync(helper).ConfigureAwait(false);

                // Try adding
                var result = AddToTradeQueue(context, trade, code, trainer, sig, routine, type, trader, out var msg);

                // Notify in channel
                // await context.Channel.SendMessageAsync(msg).ConfigureAwait(false); 
                
                var user = trader;
                SocketGuildUser guser = (SocketGuildUser)user;
                var UNick = guser.Nickname;

                if (UNick is null) UNick = user.Username;
                
                var userID = user.Id;
                var hub = SysCord<T>.Runner.Hub;
                var Info = hub.Queues.Info;
                var position = Info.CheckPosition(userID, routine);
                string embedMsg, embedTitle = "", embedAuthor, embedThumbUrl = "";
                bool CanGMax = false;
                uint FormArgument = 0;

                Color embedMsgColor = new();
                EmbedAuthorBuilder embedAuthorBuild = new();
                EmbedFooterBuilder embedFtr = new();

                string HeldItem = Fraudiouscl.FixHeldItemName(((HeldItem)trade.HeldItem).ToString());

                switch (trade.Version)
                {
                    case (int)GameVersion.X or (int)GameVersion.Y:
                        PK6 mon6 = (PK6)trade.Clone();
                        FormArgument = mon6.FormArgument;
                        break;
                    case (int)GameVersion.SN or (int)GameVersion.MN or (int)GameVersion.US or (int)GameVersion.UM:
                        PK7 mon7 = (PK7)trade.Clone();
                        FormArgument = mon7.FormArgument;
                        break;
                    case (int)GameVersion.GP or (int)GameVersion.GE:
                        PB7 monLGPE = (PB7)trade.Clone();
                        FormArgument = monLGPE.FormArgument;
                        break;
                    case (int)GameVersion.SW or (int)GameVersion.SH:
                        PK8 mon8 = (PK8)trade.Clone();
                        CanGMax = mon8.CanGigantamax;
                        FormArgument = mon8.FormArgument;
                        break;
                    case (int)GameVersion.BD or (int)GameVersion.SP:
                        PB8 monBDSP = (PB8)trade.Clone();
                        CanGMax = monBDSP.CanGigantamax;
                        FormArgument = monBDSP.FormArgument;
                        break;
                    case (int)GameVersion.PLA:
                        PA8 monLA = (PA8)trade.Clone();
                        CanGMax = monLA.CanGigantamax;
                        FormArgument = monLA.FormArgument;
                        break;
                    case (int)GameVersion.SL or (int)GameVersion.VL:
                        PK9 mon9 = (PK9)trade.Clone();
                        FormArgument = mon9.FormArgument;
                        break;
                }
                if (routine == PokeRoutineType.Clone || routine == PokeRoutineType.Dump || routine == PokeRoutineType.DirectTrade)
                {
                    var me = SysCord<T>.Runner;
                    var cd = SysCordSettings.HubConfig.TradeAbuse.TradeCooldown;
                    string botversion = "";
                    if (me is not null)
                        botversion = me.ToString()!.Substring(46, 3);
                    var gamever = botversion switch
                    {
                        "PK9" => "SV",
                        "PK8" => "SWSH",
                        "PA8" => "PLA",
                        "PB8" => "BDSP",
                        _ => "LGPE",
                    };

                    embedTitle = $"__Search once bot DMs you Initializing trade__\n";
                    embedAuthor = $"{UNick}'s ";
                    embedMsg = $"";

                    if (routine == PokeRoutineType.Clone)
                    {
                        embedMsgColor = 0xF9F815;
                        embedAuthor += "Clone Request";
                        embedMsg += $"**STEPS:**\n";
                        embedMsg += $"**1.** Show a Pokémon to be cloned\n";
                        embedMsg += $"**2.** Hit B once to back out of your offer\n";
                        embedMsg += $"**3.** Offer a trash Pokémon to receive your clone\n";
                        embedMsg += $"**4.** Remember to proceed past the warning message\n\n";
                        embedMsg += $"Your cooldown of **{SysCordSettings.HubConfig.TradeAbuse.TradeCooldown}** mins will start once the trade completes\n\n";
                        embedMsg += $"Thank you come again!";
                    }
                    else if (routine == PokeRoutineType.Dump)
                    {
                        embedMsgColor = 0x6015F9;
                        embedAuthor += "Dump Request";
                        embedMsg += $"Show your Pokémon to be dumped\n";
                        embedMsg += $"You have **{SysCordSettings.HubConfig.Trade.MaxDumpTradeTime}** seconds to show your Pokémon\n";
                        embedMsg += $"You can show up to **{SysCordSettings.HubConfig.Trade.MaxDumpsPerTrade}** Pokémon\n\n";
                        embedMsg += $"Your cooldown of **{SysCordSettings.HubConfig.TradeAbuse.TradeCooldown}** mins will start once the trade completes\n\n";
                        embedMsg += $"Thank you come again!";
                    }
                    else if (routine == PokeRoutineType.DirectTrade)
                    {
                        embedMsgColor = 0x6FFEEC;
                        embedAuthor += "Direct Trade Request";
                        embedMsg += $"Trade using the nicknames on sheet or use **Special Features**\n";
                        embedMsg += $"The Current Game running is **{gamever}**\n\n";
                        embedMsg += $"Commands:\n**!help**, **!rsv**, **!t**, **!dump**, **!clone**, **!checkcd**\n";
                        embedMsg += $"Your cooldown of **{cd}** mins will start once the trade completes\n";
                        embedMsg += $"Enjoy & Please come again !";
                    }

                    embedAuthorBuild.IconUrl = "https://archives.bulbagarden.net/media/upload/e/e1/PCP363.png";
                    embedAuthorBuild.Name = embedAuthor;

                    embedFtr.Text = $"Current Position: " + SysCord<T>.Runner.Hub.Queues.Info.Count.ToString();

                    var botct = Info.Hub.Bots.Count;
                    if (position.Position > botct)
                    {
                        var eta = Info.Hub.Config.Queues.EstimateDelay(position.Position, botct);
                        embedFtr.Text += $"\nEstimated wait: {eta:F1} minutes.";
                    }

                    embedFtr.IconUrl = "https://raw.githubusercontent.com/PhantomL98/HomeImages/main/approvalspheal.png";
                    
                    embedThumbUrl = "https://raw.githubusercontent.com/PhantomL98/HomeImages/main/approvalspheal.png";
                    
                }
                else
                {
                    var la = new LegalityAnalysis(trade);
                    if (trade.IsShiny)
                    {
                        if (trade.ShinyXor == 0 && !(trade.Version == (int)GameVersion.SL || trade.Version == (int)GameVersion.VL))
                            embedTitle = "■ shiny ";
                        else
                            embedTitle = "★ shiny ";
                    }

                    embedTitle += $" {(Species)trade.Species} ";
                    if (trade.Gender == 0)
                        embedTitle += "(M)";
                    else if (trade.Gender == 1)
                        embedTitle += "(F)";
                    if (trade.HeldItem > 0)
                        embedTitle += $" ➜ {HeldItem}";

                    embedAuthor = $"{UNick}'s ";
                    embedAuthor += trade.IsShiny ? "**shiny** " : "";
                    embedAuthor += "Pokémon:";

                    embedMsg = $"**Ability:** {(Ability)trade.Ability}";
                    embedMsg += $"\n**Level:** {trade.CurrentLevel}";
                    embedMsg += $"\n**Nature:** {(Nature)trade.Nature}";
                    embedMsg += $"\n**IVs:** {trade.IV_HP}/{trade.IV_ATK}/{trade.IV_DEF}/{trade.IV_SPA}/{trade.IV_SPD}/{trade.IV_SPE}";
                    embedMsg += $"\n**EVs:** {trade.EV_HP}/{trade.EV_ATK}/{trade.EV_DEF}/{trade.EV_SPA}/{trade.EV_SPD}/{trade.EV_SPE}";

                    embedMsg += $"\n**Moves:**";
                    if (trade.Move1 != 0)
                        embedMsg += $"\n- {(Move)trade.Move1}";
                    if (trade.Move2 != 0)
                        embedMsg += $"\n- {(Move)trade.Move2}";
                    if (trade.Move3 != 0)
                        embedMsg += $"\n- {(Move)trade.Move3}";
                    if (trade.Move4 != 0)
                        embedMsg += $"\n- {(Move)trade.Move4}";

                    embedMsg += $"\n\nPIDType: {la.Info.PIDIV.Type}";

                    embedMsg += $"\n\n{trader.Mention} - Added to the LinkTrade queue.\n\n";
                    embedMsg += $"Your cooldown of **{SysCordSettings.HubConfig.TradeAbuse.TradeCooldown}** mins will start once the trade completes\n\n";
                    embedMsg += $"Thank you come again!";

                    embedAuthorBuild.IconUrl = "https://raw.githubusercontent.com/PhantomL98/HomeImages/main/Ballimg/50x50/" + ((Ball)trade.Ball).ToString().ToLower() + "ball.png";
                    embedAuthorBuild.Name = embedAuthor;
                    
                    embedMsgColor = new Color((uint)Enum.Parse(typeof(embedColor), Enum.GetName(typeof(Ball), trade.Ball)));

                    embedFtr.Text = $"Current Position: " + SysCord<T>.Runner.Hub.Queues.Info.Count.ToString();

                    var botct = Info.Hub.Bots.Count;
                    if (position.Position > botct)
                    {
                        var eta = Info.Hub.Config.Queues.EstimateDelay(position.Position, botct);
                        embedFtr.Text += $"\nEstimated wait: {eta:F1} minutes.";
                    }
                    embedFtr.IconUrl = "https://raw.githubusercontent.com/PhantomL98/HomeImages/main/approvalspheal.png";

                    string URLStart = "https://raw.githubusercontent.com/PhantomL98/HomeImages/main/Sprites/200x200/poke_capture";
                    string URLGender;
                    string URLGMax = CanGMax ? "g" : "n";
                    string URLShiny = trade.IsShiny ? "r.png" : "n.png";

                    if (trade.Gender < 2)
                        URLGender = "mf";
                    else
                        URLGender = "uk";

                    embedThumbUrl = URLStart + "_" + trade.Species.ToString("0000") + "_" + trade.Form.ToString("000") + "_" + URLGender + "_" + URLGMax + "_" + FormArgument.ToString("00000000") + "_f_" + URLShiny;

                    try
                    {
                        using HttpResponseMessage response = await client.GetAsync(embedThumbUrl);
                        response.EnsureSuccessStatusCode();
                    }
                    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        if (trade.Gender == 0)
                            URLGender = "md";
                        else
                            URLGender = "fd";

                        embedThumbUrl = URLStart + "_" + trade.Species.ToString("0000") + "_" + trade.Form.ToString("000") + "_" + URLGender + "_" + URLGMax + "_" + FormArgument.ToString("00000000") + "_f_" + URLShiny;

                        try
                        {
                            using HttpResponseMessage response = await client.GetAsync(embedThumbUrl);
                            response.EnsureSuccessStatusCode();
                        }
                        catch (HttpRequestException ex1) when (ex1.StatusCode == HttpStatusCode.NotFound)
                        {
                            if (trade.Gender == 0)
                                URLGender = "mo";
                            else
                                URLGender = "fo";

                            embedThumbUrl = URLStart + "_" + trade.Species.ToString("0000") + "_" + trade.Form.ToString("000") + "_" + URLGender + "_" + URLGMax + "_" + FormArgument.ToString("00000000") + "_f_" + URLShiny;
                        }
                    }
                }

                EmbedBuilder builder = new()
                {
                    //Optional color
                    Color = embedMsgColor,
                    Author = embedAuthorBuild,
                    Title = embedTitle,
                    Description = embedMsg,
                    ThumbnailUrl = embedThumbUrl,
                    Footer = embedFtr
                };

                await context.Channel.SendMessageAsync("", false, builder.Build()).ConfigureAwait(false);

                // Notify in PM to mirror what is said in the channel.
                await trader.SendMessageAsync($"{msg}\nYour trade code will be **{code:0000 0000}**.").ConfigureAwait(false);

                // Clean Up
                if (result)
                {
                    // Delete the user's join message for privacy
                    if (!context.IsPrivate)
                        await context.Message.DeleteAsync(RequestOptions.Default).ConfigureAwait(false);
                }
                else
                {
                    // Delete our "I'm adding you!", and send the same message that we sent to the general channel.
                    await test.DeleteAsync().ConfigureAwait(false);
                }
            }
            catch (HttpException ex)
            {
                await HandleDiscordExceptionAsync(context, trader, ex).ConfigureAwait(false);
            }
        }

        public static async Task AddToQueueAsync(SocketCommandContext context, int code, string trainer, RequestSignificance sig, T trade, PokeRoutineType routine, PokeTradeType type)
        {
            await AddToQueueAsync(context, code, trainer, sig, trade, routine, type, context.User).ConfigureAwait(false);
        }

        private static bool AddToTradeQueue(SocketCommandContext context, T pk, int code, string trainerName, RequestSignificance sig, PokeRoutineType type, PokeTradeType t, SocketUser trader, out string msg)
        {
            var user = trader;
            var userID = user.Id;
            var name = user.Username;

            var trainer = new PokeTradeTrainerInfo(trainerName, userID);
            var notifier = new DiscordTradeNotifier<T>(pk, trainer, code, user);
            var detail = new PokeTradeDetail<T>(pk, trainer, notifier, t, code, sig == RequestSignificance.Favored);
            var trade = new TradeEntry<T>(detail, userID, type, name);

            var hub = SysCord<T>.Runner.Hub;
            var Info = hub.Queues.Info;
            var added = Info.AddToTradeQueue(trade, userID, sig == RequestSignificance.Owner);

            if (added == QueueResultAdd.AlreadyInQueue)
            {
                msg = "Sorry, you are already in the queue.";
                return false;
            }

            var position = Info.CheckPosition(userID, type);

            var ticketID = "";
            if (TradeStartModule<T>.IsStartChannel(context.Channel.Id))
                ticketID = $", unique ID: {detail.ID}";

            var pokeName = "";
            if (t == PokeTradeType.Specific && pk.Species != 0)
                pokeName = $" Receiving: {GameInfo.GetStrings(1).Species[pk.Species]}.";
            msg = $"{user.Mention} - Added to the {type} queue{ticketID}. Current Position: {position.Position}.{pokeName}";

            var botct = Info.Hub.Bots.Count;
            if (position.Position > botct)
            {
                var eta = Info.Hub.Config.Queues.EstimateDelay(position.Position, botct);
                msg += $" Estimated: {eta:F1} minutes.";
            }
            return true;
        }

        private static async Task HandleDiscordExceptionAsync(SocketCommandContext context, SocketUser trader, HttpException ex)
        {
            string message = string.Empty;
            switch (ex.DiscordCode)
            {
                case DiscordErrorCode.InsufficientPermissions or DiscordErrorCode.MissingPermissions:
                    {
                        // Check if the exception was raised due to missing "Send Messages" or "Manage Messages" permissions. Nag the bot owner if so.
                        var permissions = context.Guild.CurrentUser.GetPermissions(context.Channel as IGuildChannel);
                        if (!permissions.SendMessages)
                        {
                            // Nag the owner in logs.
                            message = "You must grant me \"Send Messages\" permissions!";
                            Base.LogUtil.LogError(message, "QueueHelper");
                            return;
                        }
                        if (!permissions.ManageMessages)
                        {
                            var app = await context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
                            var owner = app.Owner.Id;
                            message = $"<@{owner}> You must grant me \"Manage Messages\" permissions!";
                        }
                    }
                    break;
                case DiscordErrorCode.CannotSendMessageToUser:
                    {
                        // The user either has DMs turned off, or Discord thinks they do.
                        message = context.User == trader ? "You must enable private messages in order to be queued!" : "The mentioned user must enable private messages in order for them to be queued!";
                    }
                    break;
                default:
                    {
                        // Send a generic error message.
                        message = ex.DiscordCode != null ? $"Discord error {(int)ex.DiscordCode}: {ex.Reason}" : $"Http error {(int)ex.HttpCode}: {ex.Message}";
                    }
                    break;
            }
            await context.Channel.SendMessageAsync(message).ConfigureAwait(false);
        }
    }
}
