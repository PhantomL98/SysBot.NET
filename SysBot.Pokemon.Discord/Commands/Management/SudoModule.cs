using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Discord;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon.Discord
{
    public class SudoModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
    {
        [Command("blacklist")]
        [Summary("Blacklists mentioned user.")]
        [RequireSudo]
        // ReSharper disable once UnusedParameter.Global
        public async Task BlackListUsers([Remainder] string _)
        {
            var users = Context.Message.MentionedUsers;
            var objects = users.Select(GetReference);
            SysCordSettings.Settings.UserBlacklist.AddIfNew(objects);
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("blacklistComment")]
        [Summary("Adds a comment for a blacklisted user ID.")]
        [RequireSudo]
        // ReSharper disable once UnusedParameter.Global
        public async Task BlackListUsers(ulong id, [Remainder] string comment)
        {
            var obj = SysCordSettings.Settings.UserBlacklist.List.Find(z => z.ID == id);
            if (obj is null)
            {
                await ReplyAsync($"Unable to find a user with that ID ({id}).").ConfigureAwait(false);
                return;
            }

            var oldComment = obj.Comment;
            obj.Comment = comment;
            await ReplyAsync($"Done. Changed existing comment ({oldComment}) to ({comment}).").ConfigureAwait(false);
        }

        [Command("unblacklist")]
        [Summary("Un-Blacklists mentioned user.")]
        [RequireSudo]
        // ReSharper disable once UnusedParameter.Global
        public async Task UnBlackListUsers([Remainder] string _)
        {
            var users = Context.Message.MentionedUsers;
            var objects = users.Select(GetReference);
            SysCordSettings.Settings.UserBlacklist.RemoveAll(z => objects.Any(o => o.ID == z.ID));
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("blacklistId")]
        [Summary("Blacklists IDs. (Useful if user is not in the server).")]
        [RequireSudo]
        public async Task BlackListIDs([Summary("Comma Separated Discord IDs")][Remainder] string content)
        {
            var IDs = GetIDs(content);
            var objects = IDs.Select(GetReference);
            SysCordSettings.Settings.UserBlacklist.AddIfNew(objects);
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("unBlacklistId")]
        [Summary("Un-Blacklists IDs. (Useful if user is not in the server).")]
        [RequireSudo]
        public async Task UnBlackListIDs([Summary("Comma Separated Discord IDs")][Remainder] string content)
        {
            var IDs = GetIDs(content);
            SysCordSettings.Settings.UserBlacklist.RemoveAll(z => IDs.Any(o => o == z.ID));
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        [Command("blacklistSummary")]
        [Alias("printBlacklist", "blacklistPrint")]
        [Summary("Prints the list of blacklisted users.")]
        [RequireSudo]
        public async Task PrintBlacklist()
        {
            var lines = SysCordSettings.Settings.UserBlacklist.Summarize();
            var msg = string.Join("\n", lines);
            await ReplyAsync(Format.Code(msg)).ConfigureAwait(false);
        }

        [Command("trademon")]
        [Summary("Changes LedySpecies of Pokémon for idle distribution.")]
        [RequireSudo]
        public async Task ChangeTradeMon([Remainder] string input)
        {
            var trainer = AutoLegalityWrapper.GetTrainerInfo<T>();
            var sav = SaveUtil.GetBlankSAV((GameVersion)trainer.Game, trainer.OT);
            bool isSpecies = Enum.TryParse(input, true, out Species ledyMonSpecies);

            if (!isSpecies)
                await ReplyAsync("Please verify you entered a valid Pokémon species").ConfigureAwait(false);

            else if (!sav.Personal.IsSpeciesInGame((ushort)ledyMonSpecies))
                await ReplyAsync("Please enter a valid Pokémon within the current Game Version").ConfigureAwait(false);

            else
            {
                SysCordSettings.HubConfig.Distribution.LedySpecies = ledyMonSpecies;
                EchoUtil.Echo($"Updated LedySpecies to: {ledyMonSpecies}");
                await ReplyAsync($"Updated LedySpecies to: {ledyMonSpecies}").ConfigureAwait(false);
            }

        }

        [Command("checkmon")]
        [Summary("Checks what the current LedySpecies is")]
        [RequireSudo]
        public async Task EchoLedySpecies()
        {
            EchoUtil.Echo($"LedySpecies was checked by {Context.User.Username} and is currently: {SysCordSettings.HubConfig.Distribution.LedySpecies}");
            await ReplyAsync(Format.Code($"LedySpecies is currently: {SysCordSettings.HubConfig.Distribution.LedySpecies}")).ConfigureAwait(false);
        }

        [Command("addwl")]
        [Summary("Adds NID to whitelist for cooldown skipping. Format: addwl [NID] [IGN] [Duration in hours](optional)")]
        [RequireSudo]
        // Adds a <NID> to cooldown whitelist.  Syntax: <prefix>addwl <NID>, <OT_Name>, <Reason for whitelisting>, <day/hour>, <duration>
        // Do not provide last two parameters for non-expiring whitelist.
        public async Task AddWhiteList([Summary("Whitelist user from cooldowns. Format: <NID>, <OT Name>, <Duration>:<Day/Hour>, <Reason for whitelisting>")][Remainder] string input)
        {
            string msg = "";
            var wlParams = input.Split(", ", 4);
            DateTime wlExpires = DateTime.Now;
            RemoteControlAccess wlRef = new RemoteControlAccess();

            if (wlParams.Length < 2)
            {
                await ReplyAsync(Format.Code($"Please enter the command with the correct syntax. Format: <NID>, <OT Name>, <Duration>:<Day/Hour>, <Reason for whitelisting> (Last two are optional but BOTH must be given if one is)")).ConfigureAwait(false);
                return;
            }
            else if (wlParams.Length == 4)
            {
                var durParams = wlParams[2].Split(":", 2);
                durParams[1] = durParams[1].ToLower();
                bool isValidDur = int.TryParse(durParams[0], out int duration);
                if (!isValidDur)
                {
                    msg += $"{durParams[0]} is an invalid number. Defaulting to no expiration\r\n";
                    wlExpires = DateTime.MaxValue;
                }
                else
                {
                    wlExpires = durParams[1] switch
                    {
                        "days" or "day" => wlExpires.AddDays(duration),
                        "hours" or "hour" => wlExpires.AddHours(duration),
                        _ => wlExpires.AddHours(duration),
                    };
                }
                wlRef = GetReference(wlParams[1], Convert.ToUInt64(wlParams[0]), wlExpires, wlParams[3]);
            }
            else
            {
                wlExpires = DateTime.MaxValue;
                wlRef = GetReference(wlParams[1], Convert.ToUInt64(wlParams[0]), wlExpires, wlParams[2]);
            }

            SysCordSettings.HubConfig.TradeAbuse.WhiteListedIDs.AddIfNew(new[] { wlRef });
            msg += $"{wlParams[1]}({wlParams[0]}) added to the whitelist";
            await ReplyAsync(Format.Code(msg)).ConfigureAwait(false);
        }

        private RemoteControlAccess GetReference(IUser channel) => new()
        {
            ID = channel.Id,
            Name = channel.Username,
            Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
        };

        private RemoteControlAccess GetReference(ulong id) => new()
        {
            ID = id,
            Name = "Manual",
            Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
        };

        private RemoteControlAccess GetReference(string name, ulong id, DateTime expiration, string comment) => new()
        {
            ID = id,
            Name = name,
            Expiration = expiration,
            Comment = $"{comment} - Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
        };

        protected static IEnumerable<ulong> GetIDs(string content)
        {
            return content.Split(new[] { ",", ", ", " " }, StringSplitOptions.RemoveEmptyEntries)
                .Select(z => ulong.TryParse(z, out var x) ? x : 0).Where(z => z != 0);
        }
    }
}