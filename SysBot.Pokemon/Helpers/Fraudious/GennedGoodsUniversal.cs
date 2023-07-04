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
using System;
using SysBot.Pokemon;
using System.Buffers.Binary;
using SysBot.Base;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Discord.WebSocket;

namespace SysBot.Fraudious
{
    public class Fraudiouscl
    {
        readonly HttpClient client = new();

        public static string FixHeldItemName (string name)
        {
            name = name.Replace("____", " ");
            name = name.Replace("___", ".");
            name = name.Replace("__", "'");
            name = name.Replace("_", "-");
            return name;
        }

        public static uint GetFormArgument(PKM pk)
        {
            if (pk is not IFormArgument ifo)
                return 0;
            return ifo.FormArgument;
        }
        public static bool GetCanGigantamax(PKM pk)
        {
            if (pk is not IGigantamax ifo)
                return false;
            return ifo.CanGigantamax;
        }
        public static bool RibbonIndex(PKM pk, int Index)
        {
            if (pk is not IRibbonIndex ifo)
                return false;
            return ifo.GetRibbon(Index);
        }

        public static int BallSwapper(int ballItem) => ballItem switch
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
        public static string NameClearer(PKM toSend)
        {
            PKM cln = toSend.Clone();
            cln.Nickname = cln.ClearNickname();

            if (cln.IsEgg)
            {
                cln.IsNicknamed = true;
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

        public async Task<(bool result, PKM toSend)> SetPartnerAsOT(PKM original, byte[] partnerData, PartnerDataHolder partner, bool nameClear)
        {
            bool canGMax = GetCanGigantamax(original);
            PKM cln = original.Clone();
            uint SID7 = 0, TID7 = 0, FormArgument = GetFormArgument(original);
            int trainerGender = 0, trainerLanguage = 0, trainerVersion = 0, eggMetLocation = 0;

            switch (original.Version)
            {
                case (int)GameVersion.SW or (int)GameVersion.SH:
                    var tidsid = BitConverter.ToUInt32(partnerData, 0);
                    TID7 = tidsid % 1_000_000;
                    SID7 = tidsid / 1_000_000;
                    trainerVersion = partnerData[4];
                    trainerLanguage = partnerData[5];
                    trainerGender = partnerData[6];
                    eggMetLocation = 60002;
                    break;
                case (int)GameVersion.PLA:
                    break;
                case (int)GameVersion.BD or (int)GameVersion.SP:
                    eggMetLocation = 60010;
                    break;
                case (int)GameVersion.SL or (int)GameVersion.VL:
                    SID7 = BinaryPrimitives.ReadUInt32LittleEndian(partnerData.AsSpan(0)) / 1_000_000;
                    TID7 = BinaryPrimitives.ReadUInt32LittleEndian(partnerData.AsSpan(0)) % 1_000_000;
                    trainerVersion = partnerData[4];
                    trainerLanguage = partnerData[6];
                    trainerGender = partnerData[5];
                    eggMetLocation = 30023;
                    break;
                default:
                    break;
            }

            bool result = OTChangeAllowed(original, trainerVersion);  // Verify if changing OT is allowed

            if (result) // if OT is allowed, change the data
            {
                cln.TrainerTID7 = TID7;
                cln.TrainerSID7 = SID7;
                cln.OT_Name = partner.TrainerName;
                cln.OT_Gender = trainerGender;
                cln.Language = trainerLanguage;
                cln.Version = trainerVersion;

                if (cln.IsEgg)
                {
                    cln.HT_Name = "";
                    cln.HT_Gender = 0;
                    cln.ClearMemories();
                    cln.CurrentHandler = 0;
                    if (trainerVersion == (int)GameVersion.BD || trainerVersion == (int)GameVersion.SP)
                        cln.Met_Location = 65535;
                    else
                        cln.Met_Location = 0;
                    cln.Egg_Location = eggMetLocation;
                    cln.EggMetDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));   // sets egg met date to whatever 'yesterday' is
                }

                if (nameClear)
                    cln.Nickname = NameClearer(cln);

                cln.PID = ShinyKeeper(original, cln);
            }

            cln.EncryptionConstant = ECKeeper(cln);  // add protection for moushold and dundunsparce
            cln.RefreshChecksum();
            string msg = "**Pokémon:** ";
            if (cln.IsShiny)
                if (cln.ShinyXor == 0)
                    msg += "■ shiny ";
                else msg += "★ shiny ";
            else msg += "";
            msg += $"{(Species)cln.Species}\n";
            msg += $"**OT_Name:** {cln.OT_Name}   **OT_Gender:** {(Gender)cln.OT_Gender}\n";
            msg += $"**TID:** {cln.TrainerTID7:D6}   **SID:** {cln.TrainerSID7:D4}\n";
            msg += $"**Lang:** {(LanguageID)(cln.Language)}   **Game:** {(GameVersion)(cln.Version)}\n";
            msg += $"**PID:** {cln.PID:X}   **EC:** {cln.EncryptionConstant:X}";

            await EmbedPokemonMessage(cln, canGMax, FormArgument, msg, $"{partner.TrainerName}, hope you enjoy this Pokémon:").ConfigureAwait(false);

            return (result, cln);
        }

        public static bool OTChangeAllowed(PKM offered, int trainerVersion)
        {
            bool changeAllowed = true;

            switch (offered.Version)
            {
                case (int)GameVersion.SW or (int)GameVersion.SH:
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
                    
                    //Stops mons with Specific OT from changing to User's OT
                    switch (offeredSWSH.OT_Name)
                    {
                        case "blaines":
                            changeAllowed = false;
                            break;
                    }
                    break;

                case (int)GameVersion.BD or (int)GameVersion.SP:
                    PB8 offeredBDSP = (PB8)offered.Clone();
                    break;

                case (int)GameVersion.PLA:
                    PA8 offeredPLA = (PA8)offered.Clone();
                    break;

                case (int)GameVersion.SL or (int)GameVersion.VL:
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

                default:
                    break;
            }

            return changeAllowed;
        }
        public static uint ShinyKeeper(PKM original, PKM toSend)
        {
            var cln = toSend.Clone();
            ushort needOW8 = NeedSpecialPID(original);
            if (original.IsShiny)
            {
                if (original.ShinyXor != 0)
                {
                    if (needOW8 == (ushort)SpecialPID.SWSH)
                        cln.PID = (((uint)(cln.TID16 ^ cln.SID16) ^ (cln.PID & 0xFFFF) ^ 0) << 16) | (cln.PID & 0xFFFF);
                    else if (needOW8 == (ushort)SpecialPID.BDSP)
                        cln.PID = cln.PID;
                    else if (needOW8 == (ushort)SpecialPID.SV)
                        cln.PID = (((uint)(cln.TID16 ^ cln.SID16) ^ (cln.PID & 0xFFFF) ^ 0) << 16) | (cln.PID & 0xFFFF);
                    else
                    {
                        do
                        {
                            cln.SetShiny();
                        } while (cln.ShinyXor == 0);
                    }
                }
                else
                {
                    if (needOW8 == (ushort)SpecialPID.SWSH)
                        cln.PID = (((uint)(cln.TID16 ^ cln.SID16) ^ (cln.PID & 0xFFFF) ^ 0) << 16) | (cln.PID & 0xFFFF);
                    else if (needOW8 == (ushort)SpecialPID.BDSP)
                        cln.PID = cln.PID;
                    else if (needOW8 == (ushort)SpecialPID.SV)
                        cln.PID = (((uint)(cln.TID16 ^ cln.SID16) ^ (cln.PID & 0xFFFF) ^ 0) << 16) | (cln.PID & 0xFFFF);
                    else
                    {
                        do
                        {
                            cln.SetShiny();
                        } while (cln.ShinyXor != 0);
                    }
                }
            }
            else if (needOW8 != (ushort)SpecialPID.SWSHGBird)
            {
                cln.SetShiny();
                cln.SetUnshiny();
            }

            return cln.PID;
        }
        public static uint ECKeeper(PKM toSend)
        {
            var cln = toSend.Clone();

            if (cln.Species == (ushort)Species.Dunsparce || cln.Species == (ushort)Species.Tandemaus)
            {
                if (cln.EncryptionConstant % 100 == 0) // Keep EC fully divisible to maintain future form
                    cln.EncryptionConstant = ECKeepModable(cln);
                else if (cln.Met_Location != 30024 || cln.Met_Location != 162) cln.SetRandomEC(); // Keep EC for raidmon
            }
            else
                cln.SetRandomEC();
            return cln.EncryptionConstant;
        }
        public static uint ECKeepModable(PKM pk)
        {
            pk.SetRandomEC();

            uint delta = pk.EncryptionConstant % 100;
            pk.EncryptionConstant -= delta;

            return pk.EncryptionConstant;
        }
        public static ushort NeedSpecialPID(PKM pk)
        {
            ushort needSpecialPID = 0;
            switch (pk.Version)
            {
                case (int)GameVersion.SW or (int)GameVersion.SH:
                    if (RibbonIndex(pk, (int)Marks.MarkFishing))
                        needSpecialPID = (ushort)SpecialPID.SWSH;
                    if (!pk.IsShiny && (pk.Species == (ushort)Species.Zapdos && pk.Form == 1) || (pk.Species == (ushort)Species.Moltres && pk.Form == 1) || (pk.Species == (ushort)Species.Articuno && pk.Form == 1))
                        needSpecialPID = (ushort)SpecialPID.SWSHGBird;
                    if (pk.Species == (ushort)Species.Cobalion || pk.Species == (ushort)Species.Terrakion || pk.Species == (ushort)Species.Virizion)
                        needSpecialPID = (ushort)SpecialPID.SWSH;
                    break;
                case (int)GameVersion.BD or (int)GameVersion.SP:

                    break;
                case (int)GameVersion.SL or (int)GameVersion.VL:
                    if (pk.Met_Location == 30024)
                        needSpecialPID = (ushort)SpecialPID.SV;
                    break;
            }
            return needSpecialPID;
        }

        public async Task EmbedPokemonMessage(PKM toSend, bool CanGMAX, uint formArg, string msg, string msgTitle)
        {
            EmbedAuthorBuilder embedAuthor = new()
            {
                IconUrl = "https://raw.githubusercontent.com/PhantomL98/HomeImages/main/Ballimg/50x50/" + ((Ball)toSend.Ball).ToString().ToLower() + "ball.png",
                Name = msgTitle,
            };

            string embedThumbUrl = await EmbedImgUrlBuilder(toSend, CanGMAX, formArg.ToString("00000000")).ConfigureAwait(false);

            Color embedMsgColor = new((uint)Enum.Parse(typeof(embedColor), Enum.GetName(typeof(Ball), toSend.Ball)));

            EmbedFooterBuilder embedFtr = new()
            {
                Text = $"Traded at: {DateTime.Now.ToShortTimeString()}.",
                IconUrl = "https://raw.githubusercontent.com/PhantomL98/HomeImages/main/approvalspheal.png"
            };

            EmbedBuilder embedBuilder = new()
            {
                Color = embedMsgColor,
                ThumbnailUrl = embedThumbUrl,
                Description = msg,
                Author = embedAuthor,
                Footer = embedFtr
            };

            Embed embedMsg = embedBuilder.Build();

            EchoUtil.EchoEmbed("", embedMsg);
        }
        public async Task EmbedAlertMessage(PKM toSend, bool CanGMAX, uint formArg, string msg, string msgTitle)
        {
            string embedThumbUrl = await EmbedImgUrlBuilder(toSend, CanGMAX, formArg.ToString("00000000")).ConfigureAwait(false);

            EmbedAuthorBuilder embedAuthor = new()
            {
                IconUrl = "https://raw.githubusercontent.com/PhantomL98/HomeImages/main/alert.png",
                Name = msgTitle,
            };

            EmbedBuilder embedBuilder = new()
            {
                Color = Color.Red,
                ThumbnailUrl = embedThumbUrl,
                Description = msg,
                Author = embedAuthor
            };

            Embed embedMsg = embedBuilder.Build();

            EchoUtil.EchoEmbed("<a:SidSalute:1090091589013082154>", embedMsg);
        }
        public static Embed EmbedCDMessage(TimeSpan cdAbuse, double cd, string msg, string msgTitle)
        {
            string embedThumbUrl = "https://raw.githubusercontent.com/PhantomL98/HomeImages/main/yamper.png";

            EmbedAuthorBuilder embedAuthor = new()
            {
                IconUrl = "https://raw.githubusercontent.com/PhantomL98/HomeImages/main/alert.png",
                Name = msgTitle,
            };

            EmbedFooterBuilder embedFtr = new()
            {
                Text = $"Last encountered {cdAbuse.TotalMinutes:F1} minutes ago.\nIgnored the {cd} minute trade cooldown.",
                IconUrl = "https://raw.githubusercontent.com/PhantomL98/HomeImages/main/approvalspheal.png"
            };


            EmbedBuilder embedBuilder = new()
            {
                Color = Color.Red,
                ThumbnailUrl = embedThumbUrl,
                Description = msg,
                Author = embedAuthor,
                Footer = embedFtr
            };

            Embed embedMsg = embedBuilder.Build();

            return embedMsg;
        }
        public static EmbedBuilder EmbedCDMessage2(double cd, string msg, string msgTitle)
        {
            string embedThumbUrl = "https://raw.githubusercontent.com/PhantomL98/HomeImages/main/yamper.png";

            EmbedAuthorBuilder embedAuthor = new()
            {
                IconUrl = "https://raw.githubusercontent.com/PhantomL98/HomeImages/main/alert.png",
                Name = msgTitle,
            };

            EmbedFooterBuilder embedFtr = new()
            {
                Text = $"Brought to you by Fraudious Co.",
                IconUrl = "https://raw.githubusercontent.com/PhantomL98/HomeImages/main/approvalspheal.png"
            };


            EmbedBuilder embedBuilder = new()
            {
                Color = Color.Red,
                ThumbnailUrl = embedThumbUrl,
                Description = msg,
                Author = embedAuthor,
                Footer = embedFtr
            };

            return embedBuilder;
        }
        public async Task<string> EmbedImgUrlBuilder(PKM mon, bool canGMax, string URLFormArg)
        {
            string URLStart = "https://raw.githubusercontent.com/PhantomL98/HomeImages/main/Sprites/200x200/poke_capture";
            string URLString, URLGender;
            string URLGMax = canGMax ? "g" : "n";
            string URLShiny = mon.IsShiny ? "r.png" : "n.png";

            if (mon.Gender < 2)
                URLGender = "mf";
            else
                URLGender = "uk";

            URLString = URLStart + "_" + mon.Species.ToString("0000") + "_" + mon.Form.ToString("000") + "_" + URLGender + "_" + URLGMax + "_" + URLFormArg + "_f_" + URLShiny;

            if (mon.IsEgg)
                URLString = "https://raw.githubusercontent.com/PhantomL98/HomeImages/main/Sprites/512x512/egg.png";

            try
            {
                using HttpResponseMessage response = await client.GetAsync(URLString);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                if (mon.Gender == 0)
                    URLGender = "md";
                else
                    URLGender = "fd";

                URLString = URLStart + "_" + mon.Species.ToString("0000") + "_" + mon.Form.ToString("000") + "_" + URLGender + "_" + URLGMax + "_" + URLFormArg + "_f_" + URLShiny;

                try
                {
                    using HttpResponseMessage response = await client.GetAsync(URLString);
                    response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException ex1) when (ex1.StatusCode == HttpStatusCode.NotFound)
                {
                    if (mon.Gender == 0)
                        URLGender = "mo";
                    else
                        URLGender = "fo";

                    URLString = URLStart + "_" + mon.Species.ToString("0000") + "_" + mon.Form.ToString("000") + "_" + URLGender + "_" + URLGMax + "_" + URLFormArg + "_f_" + URLShiny;
                }
            }

            return URLString;
        }
    }
}