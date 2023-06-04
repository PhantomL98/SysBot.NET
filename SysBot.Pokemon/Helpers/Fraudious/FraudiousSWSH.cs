/*******************************
 *  Sword/Shield Methods       *
 *******************************
 *  Methods:                   *
 *  
 *******************************/

using PKHeX.Core;
using SysBot.Fraudious;
using System;
using System.Threading.Tasks;
using System.Threading;
using PKHeX.Core.AutoMod;
using SysBot.Base;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsetsSWSH;
using Discord;
using Discord.Commands;

namespace SysBot.Pokemon
{
    public partial class PokeTradeBotSWSH : PokeRoutineExecutor8SWSH, ICountBot
    {
        private async Task<(PK8 toSend, PokeTradeResult check)> HandleRandomLedy(bool Fraudious, SAV8SWSH sav, PokeTradeDetail<PK8> poke, PK8 offered, PK8 toSend, PartnerDataHolder partner, CancellationToken token)
        {
            Fraudiouscl fraudious = new Fraudiouscl();
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

                    var data = await Connection.ReadBytesAsync(LinkTradePartnerNameOffset - 0x8, 8, token).ConfigureAwait(false);
                    // var result = await SetOTDetails(toSend, partner, sav, clearName, token).ConfigureAwait(false);

                    var result = fraudious.SetPartnerAsOT(toSend, data, partner, false, token);
                    if (result.Item1 == true)
                    {
                        toSend = (PK8)result.Item2;
                    }
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
    }
}