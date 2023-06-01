/*******************************
 *  Sword/Shield Methods       *
 *******************************
 *  Methods:                   *
 *  
 *******************************/

using PKHeX.Core;
using System;
using System.Threading.Tasks;
using System.Threading;
using PKHeX.Core.AutoMod;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public partial class PokeTradeBot : PokeRoutineExecutor8, ICountBot
    {
        private async Task<(PK8 toSend, PokeTradeResult check)> HandleRandomLedy(bool Fraudious, SAV8SWSH sav, PokeTradeDetail<PK8> poke, PK8 offered, PK8 toSend, PartnerDataHolder partner, CancellationToken token)
        {
            // Allow the trade partner to do a Ledy swap.
            var config = Hub.Config.Distribution;

            var tradeeevohelditem = CheckOfferedSpecies(offered);

            if (tradeeevohelditem != 0)
            {
                toSend = offered;

                DumpPokemon("C:\\Pokemon\\Bot Dats\\SwShSys_Dump", "tester", toSend);

                if (tradeeevohelditem > 0) toSend.HeldItem = (tradeeevohelditem - 1);
                else if (tradeeevohelditem == -2)
                {
                    switch (toSend.Species)
                    {
                        case (ushort)Species.Karrablast:
                            toSend.Species = (ushort)Species.Shelmet;
                            break;
                        case (ushort)Species.Shelmet:
                            toSend.Species = (ushort)Species.Karrablast;
                            break;
                    }

                    switch (toSend.AbilityNumber)
                    {
                        case 1:
                        case 2:
                            {
                                toSend.RefreshAbility(toSend.AbilityNumber - 1);
                                break;
                            }
                        case 4:
                            {
                                toSend.RefreshAbility(2);
                                break;
                            }

                    }
                    toSend.ClearRelearnMoves();
                    toSend.SetSuggestedMoves(true);
                    for (ushort i = 0; i < 4; i++) toSend.HealPPIndex(i);
                    if (!toSend.IsNicknamed) toSend.ClearNickname();
                    toSend.LegalizePokemon();
                }
                if (toSend.IsShiny)
                {
                    if (toSend.ShinyXor == 0)
                    {
                        do
                        {
                            toSend.SetShiny();
                        } while (toSend.ShinyXor != 0);
                    }
                    else
                    {
                        do
                        {
                            toSend.SetShiny();
                        } while (toSend.ShinyXor != 1);
                    }
                }
                toSend.SetRandomEC();
                toSend.RefreshChecksum();

                DumpPokemon("C:\\Pokemon\\Bot Dats\\SwShSys_Dump", "tester", toSend);

                poke.TradeData = toSend;

                poke.SendNotification(this, "Injecting the requested Pokémon.");
                await Click(A, 0_800, token).ConfigureAwait(false);
                await SetBoxPokemon(toSend, 0, 0, token, sav).ConfigureAwait(false);
                await Task.Delay(2_500, token).ConfigureAwait(false);

                for (int i = 0; i < 5; i++)
                {
                    if (await IsUserBeingShifty(poke, token).ConfigureAwait(false))
                        return (toSend, PokeTradeResult.SuspiciousActivity);
                    await Click(A, 0_500, token).ConfigureAwait(false);
                }

                return (toSend, PokeTradeResult.Success);
            }

            var trade = Hub.Ledy.GetLedyTrade(offered, partner.TrainerOnlineID, config.LedySpecies, config.LedySpecies2);

            if (trade != null)
            {
                if (trade.Type == LedyResponseType.AbuseDetected)
                {
                    var msg = $"Found {partner.TrainerName} has been detected for abusing Ledy trades.";
                    if (AbuseSettings.EchoNintendoOnlineIDLedy)
                        msg += $"\nID: {partner.TrainerOnlineID}";
                    if (!string.IsNullOrWhiteSpace(AbuseSettings.LedyAbuseEchoMention))
                        msg = $"{AbuseSettings.LedyAbuseEchoMention} {msg}";
                    EchoUtil.Echo(msg);

                    return (toSend, PokeTradeResult.SuspiciousActivity);
                }

                toSend = trade.Receive;
                bool clearName = true;

                if (!toSend.IsEgg && (Species)toSend.Species != Hub.Config.Distribution.LedySpecies2)
                {
                    var result = await SetOTDetails(toSend, partner, sav, clearName, token).ConfigureAwait(false);
                    if (result.Item1 == true)
                        toSend = result.Item2;
                }

                poke.TradeData = toSend;

                poke.SendNotification(this, "Injecting the requested Pokémon.");
                await Click(A, 0_800, token).ConfigureAwait(false);
                await SetBoxPokemon(toSend, 0, 0, token, sav).ConfigureAwait(false);
                await Task.Delay(2_500, token).ConfigureAwait(false);
            }
            else if (config.LedyQuitIfNoMatch)
            {
                return (toSend, PokeTradeResult.TrainerRequestBad);
            }

            for (int i = 0; i < 5; i++)
            {
                if (await IsUserBeingShifty(poke, token).ConfigureAwait(false))
                    return (toSend, PokeTradeResult.SuspiciousActivity);
                await Click(A, 0_500, token).ConfigureAwait(false);
            }

            return (toSend, PokeTradeResult.Success);
        }
        private async Task<PokeTradeResult> CheckPartnerReputation(bool Fraudious, PokeTradeDetail<PK8> poke, ulong TrainerNID, string TrainerName, CancellationToken token)
        {
            bool quit = false;
            var user = poke.Trainer;
            var isDistribution = poke.Type == PokeTradeType.Random;
            var useridmsg = isDistribution ? "" : $" ({user.ID})";
            var list = isDistribution ? PreviousUsersDistribution : PreviousUsers;

            int wlIndex = AbuseSettings.WhiteListedIDs.List.FindIndex(z => z.ID == TrainerNID);
            DateTime wlCheck = DateTime.Now;
            bool wlAllow = false;

            if (wlIndex > -1)
            {
                ulong wlID = AbuseSettings.WhiteListedIDs.List[wlIndex].ID;
                var wlExpires = AbuseSettings.WhiteListedIDs.List[wlIndex].Expiration;

                if (wlID != 0 && wlExpires <= wlCheck)
                {
                    AbuseSettings.WhiteListedIDs.RemoveAll(z => z.ID == TrainerNID);
                    EchoUtil.Echo($"Removed {TrainerName} from Whitelist due to an expired duration.");
                    wlAllow = false;
                }
                else if (wlID != 0)
                    wlAllow = true;
            }

            var cooldown = list.TryGetPrevious(TrainerNID);
            if (cooldown != null)
            {
                var delta = DateTime.Now - cooldown.Time;
                Log($"Last saw {TrainerName} {delta.TotalMinutes:F1} minutes ago (OT: {TrainerName}).");

                list.TryRegister(TrainerNID, TrainerName);

                var cd = AbuseSettings.TradeCooldown;
                if (cd != 0 && TimeSpan.FromMinutes(cd) > delta && !wlAllow)
                {
                    list.TryRegister(TrainerNID, TrainerName);
                    poke.Notifier.SendNotification(this, poke, "You have ignored the trade cooldown set by the bot owner. The owner has been notified.");
                    var msg = $"Found {TrainerName}{useridmsg} ignoring the {cd} minute trade cooldown. Last encountered {delta.TotalMinutes:F1} minutes ago.";
                    if (AbuseSettings.EchoNintendoOnlineIDCooldown)
                        msg += $"\nID: {TrainerNID}";
                    if (!string.IsNullOrWhiteSpace(AbuseSettings.CooldownAbuseEchoMention))
                        msg = $"{AbuseSettings.CooldownAbuseEchoMention} {msg}";
                    EchoUtil.Echo(msg);
                    quit = true;
                }
            }

            if (!isDistribution)
            {
                var previousEncounter = EncounteredUsers.TryRegister(poke.Trainer.ID, TrainerName, poke.Trainer.ID);
                if (previousEncounter != null && previousEncounter.Name != TrainerName)
                {
                    if (AbuseSettings.TradeAbuseAction != TradeAbuseAction.Ignore)
                    {
                        if (AbuseSettings.TradeAbuseAction == TradeAbuseAction.BlockAndQuit)
                        {
                            await BlockUser(token).ConfigureAwait(false);
                            if (AbuseSettings.BanIDWhenBlockingUser)
                            {
                                AbuseSettings.BannedIDs.AddIfNew(new[] { GetReference(TrainerName, TrainerNID, "in-game block for sending to multiple in-game players") });
                                Log($"Added {TrainerNID} to the BannedIDs list.");
                            }
                        }
                        quit = true;
                    }

                    var msg = $"Found {TrainerName}{useridmsg} sending to multiple in-game players. Previous OT: {previousEncounter.Name}, Current OT: {TrainerName}";
                    if (AbuseSettings.EchoNintendoOnlineIDMultiRecipients)
                        msg += $"\nID: {TrainerNID}";
                    if (!string.IsNullOrWhiteSpace(AbuseSettings.MultiRecipientEchoMention))
                        msg = $"{AbuseSettings.MultiRecipientEchoMention} {msg}";
                    EchoUtil.Echo(msg);
                }
            }

            if (quit)
                return PokeTradeResult.SuspiciousActivity;

            // Try registering the partner in our list of recently seen.
            // Get back the details of their previous interaction.
            var previous = isDistribution
                ? list.TryRegister(TrainerNID, TrainerName)
                : list.TryRegister(TrainerNID, TrainerName, poke.Trainer.ID);
            if (previous != null && previous.NetworkID == TrainerNID && previous.RemoteID != user.ID && !isDistribution)
            {
                var delta = DateTime.Now - previous.Time;
                if (delta < TimeSpan.FromMinutes(AbuseSettings.TradeAbuseExpiration) && AbuseSettings.TradeAbuseAction != TradeAbuseAction.Ignore)
                {
                    if (AbuseSettings.TradeAbuseAction == TradeAbuseAction.BlockAndQuit)
                    {
                        await BlockUser(token).ConfigureAwait(false);
                        if (AbuseSettings.BanIDWhenBlockingUser)
                        {
                            AbuseSettings.BannedIDs.AddIfNew(new[] { GetReference(TrainerName, TrainerNID, "in-game block for multiple accounts") });
                            Log($"Added {TrainerNID} to the BannedIDs list.");
                        }
                    }
                    quit = true;
                }

                var msg = $"Found {TrainerName}{useridmsg} using multiple accounts.\nPreviously encountered {previous.Name} ({previous.RemoteID}) {delta.TotalMinutes:F1} minutes ago on OT: {TrainerName}.";
                if (AbuseSettings.EchoNintendoOnlineIDMulti)
                    msg += $"\nID: {TrainerNID}";
                if (!string.IsNullOrWhiteSpace(AbuseSettings.MultiAbuseEchoMention))
                    msg = $"{AbuseSettings.MultiAbuseEchoMention} {msg}";
                EchoUtil.Echo(msg);
            }

            if (quit)
                return PokeTradeResult.SuspiciousActivity;

            var entry = AbuseSettings.BannedIDs.List.Find(z => z.ID == TrainerNID);
            if (entry != null)
            {
                if (AbuseSettings.BlockDetectedBannedUser)
                    await BlockUser(token).ConfigureAwait(false);

                var msg = $"{TrainerName}{useridmsg} is a banned user, and was encountered in-game using OT: {TrainerName}.";
                if (!string.IsNullOrWhiteSpace(entry.Comment))
                    msg += $"\nUser was banned for: {entry.Comment}";
                if (!string.IsNullOrWhiteSpace(AbuseSettings.BannedIDMatchEchoMention))
                    msg = $"{AbuseSettings.BannedIDMatchEchoMention} {msg}";
                EchoUtil.Echo(msg);
                return PokeTradeResult.SuspiciousActivity;
            }

            return PokeTradeResult.Success;
        }
    }
}