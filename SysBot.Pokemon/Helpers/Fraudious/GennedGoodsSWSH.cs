/*******************************
 *  Sword/Shield Methods       *
 *******************************
 *  Methods:                   *
 *  
 *******************************/

using PKHeX.Core;
using SysBot.Fraudious;
using System.Threading.Tasks;
using System.Threading;
using PKHeX.Core.AutoMod;
using SysBot.Base;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsetsSWSH;
using Discord;

namespace SysBot.Pokemon
{
    public partial class PokeTradeBotSWSH : PokeRoutineExecutor8SWSH, ICountBot
    {
        private async Task<(PK8 toSend, PokeTradeResult check)> HandleRandomLedy(int bot, SaveFile sav, PokeTradeDetail<PK8> poke, PK8 offered, PK8 toSend, PartnerDataHolder partner, CancellationToken token)
        {
            Fraudiouscl fraudious = new();

            // Allow the trade partner to do a Ledy swap.
            var config = Hub.Config.Distribution;

            short tradeeevohelditem = 0;

            if (offered.HeldItem == 229)
            {
                offered.HeldItem = 0;
                tradeeevohelditem = Fraudiouscl.CheckOfferedSpecies(offered);
            }

            if (tradeeevohelditem != 0)
            {
                toSend = offered;
                var PIDla = new LegalityAnalysis(offered);

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
                            if (PIDla.Info.PIDIV.Type == PIDType.Overworld8)
                            {
                                toSend.PID = (((uint)(toSend.TID16 ^ toSend.SID16) ^ (toSend.PID & 0xFFFF) ^ 0) << 16) | (toSend.PID & 0xFFFF);
                                Log("Detected Overworld8");
                            }
                            else
                                toSend.SetShiny();
                        } while (toSend.ShinyXor != 0);
                    }
                    else
                    {
                        do
                        {
                            if (PIDla.Info.PIDIV.Type == PIDType.Overworld8)
                            {
                                toSend.PID = (((uint)(toSend.TID16 ^ toSend.SID16) ^ (toSend.PID & 0xFFFF) ^ 0) << 16) | (toSend.PID & 0xFFFF);
                                Log("Detected Overworld8");
                            }
                            else
                                toSend.SetShiny();
                        } while (toSend.ShinyXor != 1);
                    }
                }
                if (!(PIDla.Info.PIDIV.Type == PIDType.Overworld8) && !(toSend.Met_Location==122))
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

                if ((Species)toSend.Species != Hub.Config.Distribution.LedySpecies2)
                {

                    var data = await Connection.ReadBytesAsync(LinkTradePartnerNameOffset - 0x8, 8, token).ConfigureAwait(false);
                    // var result = await SetOTDetails(toSend, partner, sav, clearName, token).ConfigureAwait(false);

                    var result = await fraudious.SetPartnerAsOT(toSend, data, partner, clearName);
                    if (result.result == true)
                    {
                        toSend = (PK8)result.toSend;
                    }
                }

                var la = new LegalityAnalysis(toSend);
                if (la.Valid)
                {
                    Log($"Pokemon is valid, used trade partnerInfo");
                    poke.TradeData = toSend;
                }
                else
                {
                    Log($"Pokemon not valid, do nothing to trade Pokemon");
                    poke.TradeData = trade.Receive;
                }

                bool canGMax = Fraudiouscl.GetCanGigantamax(poke.TradeData);
                uint FormArgument = Fraudiouscl.GetFormArgument(poke.TradeData);

                var msgdone = "**Pokémon:** ";
                if (poke.TradeData.IsShiny)
                    if (poke.TradeData.ShinyXor == 0)
                        msgdone += "■ shiny ";
                    else msgdone += "★ shiny ";
                else msgdone += "";
                msgdone += $"{(Species)poke.TradeData.Species}\n";
                msgdone += $"**OT_Name:** {poke.TradeData.OT_Name}   **OT_Gender:** {(Gender)poke.TradeData.OT_Gender}\n";
                msgdone += $"**TID:** {poke.TradeData.TrainerTID7:D6}   **SID:** {poke.TradeData.TrainerSID7:D4}\n";
                msgdone += $"**Lang:** {(LanguageID)(poke.TradeData.Language)}   **Game:** {(GameVersion)(poke.TradeData.Version)}\n";
                msgdone += $"**PID:** {poke.TradeData.PID:X}   **EC:** {poke.TradeData.EncryptionConstant:X}";

                await fraudious.EmbedPokemonMessage(poke.TradeData, canGMax, FormArgument, msgdone, $"{partner.TrainerName}, hope you enjoy this Pokémon:").ConfigureAwait(false);

                poke.SendNotification(this, "Injecting the requested Pokémon.");
                await Click(A, 0_800, token).ConfigureAwait(false);
                await SetBoxPokemon(poke.TradeData, 0, 0, token, sav).ConfigureAwait(false);
                await Task.Delay(2_500, token).ConfigureAwait(false);
            }
            else if (config.LedyQuitIfNoMatch)
            {
                var msg = $"Pokémon: {(Species)offered.Species}";
                msg += $"\nNickname: {offered.Nickname}";
                msg += $"\nTrader: {partner.TrainerName}";

                await fraudious.EmbedAlertMessage(offered, offered.CanGigantamax, offered.FormArgument, msg, "Bad Request Attempted:").ConfigureAwait(false);

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
    }
}