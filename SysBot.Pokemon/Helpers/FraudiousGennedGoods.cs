using PKHeX.Core;
using PKHeX.Core.Searching;
using SysBot.Base;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsets;
using PKHeX.Core.AutoMod;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Runtime.CompilerServices;

namespace SysBot.Pokemon
{
    public partial class PokeTradeBot : PokeRoutineExecutor8, ICountBot
    {
        /*******************************
         * Sword / Shield Section
         *******************************
         */
        public async Task<(bool, PK8)> SetOTDetails(PK8 toSend, PartnerDataHolder partner, SAV8SWSH sav, bool clearName, CancellationToken token)
        {
            var data = await Connection.ReadBytesAsync(LinkTradePartnerNameOffset - 0x8, 8, token).ConfigureAwait(false);
            var tidsid = BitConverter.ToUInt32(data, 0);
            var cln = toSend.Clone();

            cln.OT_Name = partner.TrainerName;
            cln.TrainerTID7 = tidsid % 1_000_000;
            cln.TrainerSID7 = tidsid / 1_000_000;
            cln.Version = data[4];  
            cln.Language = data[5];
            cln.OT_Gender = data[6];

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

            cln.SetRandomEC();
            cln.RefreshChecksum();

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

        //

        /*******************************
         * BDSP Section
         *******************************
         * Methods:
         *      
         * */


        // PLA Section


        // SV Section


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
    }
}
