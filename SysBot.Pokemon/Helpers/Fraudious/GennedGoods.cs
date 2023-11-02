using PKHeX.Core;
using SysBot.Fraudious;
using Discord;
using System;
using System.Threading.Tasks;
using System.Threading;
using static SysBot.Pokemon.PokeDataOffsetsSWSH;
using SysBot.Base;
using System.Net.NetworkInformation;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace SysBot.Pokemon
{
    /*******************************
     * Sword / Shield Section
     *******************************
     * Methods:
     *      
     ***/

    public partial class PokeTradeBotSWSH : PokeRoutineExecutor8SWSH, ICountBot
    {
        public async Task<(bool, PK8)> SetOTDetails(PK8 toSend, PartnerDataHolder partner, SAV8SWSH sav, bool clearName, CancellationToken token)
        {
            Fraudiouscl Fraudious = new();

            var data = await Connection.ReadBytesAsync(LinkTradePartnerNameOffset - 0x8, 8, token).ConfigureAwait(false);
            var tidsid = BitConverter.ToUInt32(data, 0);
            var cln = toSend.Clone();

            cln.OT_Name = partner.TrainerName;
            cln.TrainerTID7 = tidsid % 1_000_000;
            cln.TrainerSID7 = tidsid / 1_000_000;
            cln.Version = data[4];
            cln.Language = data[5];
            cln.OT_Gender = data[6];

            if (cln.IsEgg)
            {
                cln.HT_Name = "";
                cln.HT_Language = 0;
                cln.HT_Gender = 0;
                cln.CurrentHandler = 0;
                cln.Met_Location = 0;
                cln.Egg_Location = 60002;
                cln.EggMetDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));
            }

           // if (clearName)
           //     cln.Nickname = Fraudiouscl.NameClearer(cln, fraudConnection);

            //cln.PID = Fraudiouscl.ShinyKeeper(toSend, cln); // If shiny, change PID to same shiny type as before for OT change.

            cln.SetRandomEC();
            cln.RefreshChecksum();

            // Log Creation of OT Change

            var msg = $"Pokémon: {(Species)cln.Species}";
            msg += $"\nOT_Name: {cln.OT_Name}";
            msg += $"\nGender: {(Gender)cln.OT_Gender}";
            msg += $"\nTID: {cln.TrainerTID7:D6}       SID: {cln.TrainerSID7:D4}";
            msg += $"\nLang: {(LanguageID)(cln.Language)}     Game: {(GameVersion)(cln.Version)}";
            msg += $"\nPID: {cln.PID:X}     EC: {cln.EncryptionConstant:X}";
            msg += $"\nShiny: {cln.IsShiny}      Was Shiny: {toSend.IsShiny}";

            await Fraudious.EmbedPokemonMessage(cln, cln.CanGigantamax, cln.FormArgument, msg, $"{partner.TrainerName}, your requested Pokémon was:").ConfigureAwait(false);

            var la = new LegalityAnalysis(cln);
            if (la.Valid)
            {
                Log($"Pokemon is valid, used trade partnerInfo");
                return (true, cln);
            }
            else
            {
                Log($"Pokemon not valid, do nothing to trade Pokemon");
            }

            return (true, toSend);
        }
        private static bool OTChangeAllowed(PK8 toSend, byte[] trainerData)
        {
            // Check if OT change is allowed for different pokemon
            var changeAllowed = true;
            int trainerVersion = trainerData[4];

            // Check certain species of pokemon in different situations
            switch (toSend.Species)
            {
                // Zacian on Shield
                case (ushort)Species.Zacian:
                    if (!toSend.IsShiny && trainerVersion == (int)GameVersion.SH)
                        changeAllowed = false;
                    break;

                // Zamazenta on Sword
                case (ushort)Species.Zamazenta:
                    if (!toSend.IsShiny && trainerVersion == (int)GameVersion.SW)
                        changeAllowed = false;
                    break;

                    // 

            }

            //Stops mons with Specific OT from changing to User's OT
            switch (toSend.OT_Name)
            {
                case "blaines":
                    changeAllowed = false;
                    break;
            }

            return changeAllowed;
        }
    }

    /*******************************
     * BDSP Section
     *******************************
     * Methods:
     *      
     ***/


    /*******************************
    * PLA Section
    *******************************
    * Methods:
    *      
    ***/


    /*******************************
    * SV Section
    *******************************
    * Methods:
    *      
    ***/

    public partial class PokeTradeBotSV : PokeRoutineExecutor9SV, ICountBot
    {
        public async Task<PK9> SetOTDetails(PK9 toSend, SAV9SV sav, bool clearName, CancellationToken token)
        {
            var cln = toSend.Clone();
            var tradepartner = await GetTradePartnerInfo(token).ConfigureAwait(false);
            var changeallowed = OTChangeAllowed(toSend, tradepartner);

            if (changeallowed)
            {
                var msg = "Changing OT info to:\r\n";
                cln.OT_Name = tradepartner.TrainerName;
                cln.TrainerTID7 = Convert.ToUInt32(tradepartner.TID7);
                cln.TrainerSID7 = Convert.ToUInt32(tradepartner.SID7);
                cln.Version = tradepartner.Game;
                cln.Language = tradepartner.Language;
                cln.OT_Gender = tradepartner.Gender;

                if (cln.HeldItem > -1 && cln.Species != (ushort)Species.Finizen) cln.SetDefaultNickname(); //Block nickname clear for item distro, Change Species as needed.
                if (cln.HeldItem > 0 && cln.RibbonMarkDestiny == true) cln.SetDefaultNickname();

                msg += $"OT_Name: {cln.OT_Name}\r\n";
                msg += $"TID: {cln.TrainerTID7}\r\n";
                msg += $"SID: {cln.TrainerSID7}\r\n";
                msg += $"Gender: {(Gender)cln.OT_Gender}\r\n";
                msg += $"Language: {(LanguageID)(cln.Language)}\r\n";
                msg += $"Game: {(GameVersion)(cln.Version)}\r\n";

                Log(msg);

                if (clearName)
                    cln.ClearNickname();

                if (toSend.IsShiny)
                {
                    if (toSend.ShinyXor == 0)
                    {
                        do
                        {
                            cln.SetShiny();
                        } while (cln.ShinyXor != 0);
                    }
                    else
                    {
                        do
                        {
                            cln.SetShiny();
                        } while (cln.ShinyXor != 1);
                    }

                }
                else
                    cln.SetUnshiny();

                if (cln.Species == (ushort)Species.Dunsparce || cln.Species == (ushort)Species.Tandemaus) //Keep EC to maintain form
                {
                    if (cln.EncryptionConstant % 100 == 0)
                        cln = KeepECModable(cln);
                }
                else
                    if (cln.Met_Location != 30024) cln.SetRandomEC(); //OT for raidmon
                cln.RefreshChecksum();
                Log("NPC user has their OT now.");
            }

            var tradesv = new LegalityAnalysis(cln); //Legality check, if fail, sends original PK9 instead

            //return tradesv.Valid;
            return cln;
        }
        private static bool OTChangeAllowed(PK9 toSend, TradePartnerSV trader1)
        {
            var changeallowed = true;

            // Check if OT change is allowed for different situations
            switch (toSend.Species)

            {
                //Miraidon on Scarlet, no longer needed
                case (ushort)Species.Miraidon:
                    if (trader1.Game == (int)GameVersion.SL)
                        changeallowed = false;
                    break;
                //Koraidon on Violet, no longer needed
                case (ushort)Species.Koraidon:
                    if (trader1.Game == (int)GameVersion.VL)
                        changeallowed = false;
                    break;
                //Ditto will not OT change unless it has Destiny Mark
                case (ushort)Species.Ditto:
                    if (toSend.RibbonMarkDestiny == true)
                        changeallowed = true;
                    else
                        changeallowed = false;
                    break;
            }
            switch (toSend.OT_Name) //Stops mons with Specific OT from changing to User's OT
            {
                case "Blaines":
                case "New Year 23":
                case "Valentine":
                    changeallowed = false;
                    break;
            }
            return changeallowed;
        }
        private static PK9 KeepECModable(PK9 eckeep) //Maintain form for Dunsparce/Tandemaus
        {
            eckeep.SetRandomEC();

            uint ecDelta = eckeep.EncryptionConstant % 100;
            eckeep.EncryptionConstant -= ecDelta;

            return eckeep;
        }
    }
}