/************************************************************
 *  Universal Methods:                                      *
 ************************************************************
 *      BallSwapper(int ballItem)                           *
 *      NameClearer(PKM toSend)                             *
 *      OTChangeAllowed(PKM offered, int trainerVersion)    *
 *      SetPartnerAsOT (PKM original, PKM toSend)
 *      ShinyKeeper(PKM original, PKM toSend)               *
 ************************************************************/

using PKHeX.Core;
using Discord;
using Discord.Commands;
using System;
using SysBot.Pokemon;
using static SysBot.Pokemon.PokeRoutineExecutor8SWSH;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Buffers.Binary;
using SysBot.Base;
using Discord.Rest;
using System.Threading.Tasks;
using System.Threading;
using PKHeX.Core.Searching;
using SysBot.Base;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SysBot.Fraudious
{
    public class Fraudiouscl
    {
        public int BallSwapper(int ballItem) => ballItem switch
        {
            1 => 1,
            2 => 2,
            3 => 3,
            4 => 4,
            5 => 5,
            6 => 6,
            7 => 7,
            8 => 8,
            9 => 9,
            10 => 10,
            11 => 11,
            12 => 12,
            13 => 13,
            14 => 14,
            15 => 15,
            492 => 17,
            493 => 18,
            494 => 19,
            495 => 20,
            496 => 21,
            497 => 22,
            498 => 23,
            499 => 24,
            576 => 25,
            851 => 26,
            _ => 0,
        };
        public string NameClearer(PKM toSend)
        {
            PKM cln = toSend.Clone();
            cln.Nickname = cln.ClearNickname();

            if (cln.IsEgg)
            {
                cln.Nickname = cln.Language switch
                {
                    1 => "タマゴ",
                    3 => "Œuf",
                    4 => "Uovo",
                    5 => "Ei",
                    7 => "Huevo",
                    8 => "알",
                    9 or 10 => "蛋",
                    _ => "Egg",
                };
            }

            return cln.Nickname;
        }
        public bool OTChangeAllowed(PKM offered, int trainerVersion)
        {
            bool changeAllowed = true;

            switch (offered.Generation)
            {
                case < 8:
                    break;

                case 8:
                    PK8 offeredSWSH = (PK8)offered.Clone();
                    // Check for situations where one cannot be the OT of a pokemon
                    switch (offeredSWSH.Species)
                    {
                        // Non-shiny Zacian on Shield
                        case (ushort)Species.Zacian:
                            if (!offeredSWSH.IsShiny && trainerVersion == (int)GameVersion.SH)
                                changeAllowed = false;
                            break;

                        // Non-shiny Zamazenta on Sword
                        case (ushort)Species.Zamazenta:
                            if (!offeredSWSH.IsShiny && trainerVersion == (int)GameVersion.SW)
                                changeAllowed = false;
                            break;
                    }
                    break;

                case 9:
                    PK9 offeredSV = (PK9)offered.Clone();

                    switch (offeredSV.Species)
                    {
                        //Miraidon on Scarlet, no longer needed
                        case (ushort)Species.Miraidon:
                            if (trainerVersion == (int)GameVersion.SL)
                                changeAllowed = false;
                            break;
                        //Koraidon on Violet, no longer needed
                        case (ushort)Species.Koraidon:
                            if (trainerVersion == (int)GameVersion.VL)
                                changeAllowed = false;
                            break;
                        //Ditto will not OT change unless it has Destiny Mark
                        /*case (ushort)Species.Ditto:
                            if (toSend.RibbonMarkDestiny == true)
                                changeAllowed = true;
                            else
                                changeAllowed = false;
                            break;*/
                    }

                    switch (offeredSV.OT_Name) //Stops mons with Specific OT from changing to User's OT
                    {
                        case "Blaines":
                        case "New Year 23":
                        case "Valentine":
                            changeAllowed = false;
                            break;
                    }
                    break;
            }
            return changeAllowed;
        }
        public (bool, PKM) SetPartnerAsOT(PKM original, byte[] partnerData, PartnerDataHolder partner, bool nameClear, CancellationToken token)
        {
            bool result = false;
            string msg = "";
            
            uint SID7 = 0, TID7 = 0;
            int trainerGender = 0, trainerLanguage = 0, trainerVersion = 0;

            switch (original.Version)
            {
                case (int)GameVersion.SW or (int)GameVersion.SH:
                    var tidsid = BitConverter.ToUInt32(partnerData, 0);
                    TID7 = tidsid % 1_000_000;
                    SID7 = tidsid / 1_000_000;
                    trainerVersion = partnerData[4];
                    trainerLanguage = partnerData[5];
                    trainerGender = partnerData[6];
                    break;
                case (int)GameVersion.BDSP:
                    break;
                case (int)GameVersion.PLA:
                    break;
                case (int)GameVersion.SL or (int)GameVersion.VL:
                    SID7 = BinaryPrimitives.ReadUInt32LittleEndian(partnerData.AsSpan(0)) / 1_000_000;
                    TID7 = BinaryPrimitives.ReadUInt32LittleEndian(partnerData.AsSpan(0)) % 1_000_000;
                    trainerVersion = partnerData[4];
                    trainerLanguage = partnerData[6];
                    trainerGender = partnerData[5];
                    break;
                default:
                    break;
            }

            PKM cln = original.Clone();

            cln.TrainerTID7 = TID7;
            cln.TrainerSID7 = SID7;
            cln.OT_Name = partner.TrainerName;
            cln.OT_Gender = trainerGender;
            cln.Language = trainerLanguage;
            cln.Version = trainerVersion;



            cln.PID = ShinyKeeper(original, cln);

            //msg = "Attempting to change Pokémon info to:\r\n";
            msg = $"Pokémon: {(Species)cln.Species}";
            //msg += $"\nShiny: {cln.IsShiny}             Shiny Type: {cln.ShinyXor}";
            //msg += $"\nOT_Name: {cln.OT_Name}               Gender: {(Gender)cln.OT_Gender}";
            //msg += $"\nTID: {cln.TrainerTID7}               SID: {cln.TrainerSID7}";
            //msg += $"\nLanguage: {(LanguageID)(cln.Language)}               Game: {(GameVersion)(cln.Version)}";
            //msg += $"\nPID: {cln.PID}               EC: {cln.EncryptionConstant}";
            //msg += $"\nChecksum Valid: {cln.ChecksumValid}";

            EmbedBuilder builder = new EmbedBuilder
            {
                //Optional color
                Color = Color.Blue,
                Description = msg,
                ThumbnailUrl = "https://img.pokemondb.net/sprites/home/normal/spheal.png",
                Title = "Attempting to change Pokémon info to:",

            };
            Embed embedMsg = builder.Build();
            EchoUtil.EchoEmbed(embedMsg);
            //toSend.

            return (result, cln);
        }
        public uint ShinyKeeper(PKM original, PKM toSend)
        {
            PKM cln = toSend.Clone();
            LogUtil.LogText($"toSend Shiny is: {toSend.IsShiny}");
            if (original.IsShiny)
            {
                if (original.ShinyXor == 0)
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
            LogUtil.LogText($"cln Shiny is: {cln.IsShiny}");
            return cln.PID;
        }
    }
}