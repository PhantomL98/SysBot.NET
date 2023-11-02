/************************************************************
 *  Universal Methods:                                      *
 ************************************************************
 *      BallSwapper(int ballItem)                           *
 *      NameClearer(PKM toSend)                             *
 *      OTChangeAllowed(PKM offered, int trainerVersion)    *
 *      SetPartnerAsOT (PKM original, PKM toSend)           *
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
        public static string NameClearer(PKM toSend, IConsoleConnectionAsync fraudConnetion)
        {
            PKM cln = toSend.Clone();
            if (cln.Met_Location != 30001)
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

        public Task<(bool result, PKM toSend)> SetPartnerAsOT(PKM original, byte[] partnerData, PartnerDataHolder partner, bool nameClear, IConsoleConnectionAsync fraudConnetion)
        {
            var rnd = new Random();

            // checks if mon is able to Gigantamax
            bool canGMax = GetCanGigantamax(original);

            PKM cln = original.Clone();

            var la = new LegalityAnalysis(cln);

            bool CopiumOT = false;

            // Initializes TID & SIDs
            uint SID7 = 0, TID7 = 0;

            // Gets the Form Argument of the mon
            uint FormArgument = GetFormArgument(original);

            // Initializes OT and Egg-related variables
            int trainerGender = 0, trainerLanguage = 0, trainerVersion = 0, eggMetLocation = 0;

            switch (original.Version)
            {
                // Sets variables for Sword and Shield
                case (int)GameVersion.SW or (int)GameVersion.SH:
                    var tidsid = BitConverter.ToUInt32(partnerData, 0);
                    TID7 = tidsid % 1_000_000;
                    SID7 = tidsid / 1_000_000;
                    trainerVersion = partnerData[4];
                    trainerLanguage = partnerData[5];
                    trainerGender = partnerData[6];
                    eggMetLocation = 60002;
                    fraudConnetion.Log($"Partner's game: {GameInfo.GetStrings(1).gamelist[trainerVersion]}");
                    break;
                // Sets variables for Legends Arceus
                case (int)GameVersion.PLA:
                    fraudConnetion.Log($"Partner's game: {GameInfo.GetStrings(1).gamelist[trainerVersion]}");
                    break;
                // Sets variables for Brilliant Diamond and Shining Pearl
                case (int)GameVersion.BD or (int)GameVersion.SP:
                    eggMetLocation = 60010;
                    fraudConnetion.Log($"Partner's game: {GameInfo.GetStrings(1).gamelist[trainerVersion]}");
                    break;
                // Sets variables for Scarlet and Violet
                case (int)GameVersion.SL or (int)GameVersion.VL:
                    SID7 = BinaryPrimitives.ReadUInt32LittleEndian(partnerData.AsSpan(0)) / 1_000_000;
                    TID7 = BinaryPrimitives.ReadUInt32LittleEndian(partnerData.AsSpan(0)) % 1_000_000;
                    trainerVersion = partnerData[4];
                    trainerLanguage = partnerData[6];
                    trainerGender = partnerData[5];
                    eggMetLocation = 30023;
                    fraudConnetion.Log($"Partner's game: {GameInfo.GetStrings(1).gamelist[trainerVersion]}");
                    break;
                default:
                    break;
            }

            switch (cln.Species)
            {
                case (ushort)Species.Zacian when trainerVersion == (ushort)GameVersion.SH:
                case (ushort)Species.Zamazenta when trainerVersion == (ushort)GameVersion.SW:
                case (ushort)Species.Dialga when trainerVersion == (ushort)GameVersion.SP:
                case (ushort)Species.Palkia when trainerVersion == (ushort)GameVersion.BD:
                case (ushort)Species.Koraidon when trainerVersion == (ushort)GameVersion.VL:
                case (ushort)Species.Miraidon when trainerVersion == (ushort)GameVersion.SV:
                    CopiumOT = true;
                    break;
            }

            // Verify if changing OT is allowed

            bool result = OTChangeAllowed(original, trainerVersion, fraudConnetion, CopiumOT);

            // When changing OT is allowed, let's change the data
            if (result)
            {
                if (CopiumOT) //Box Legends OT
                {
                    cln.TrainerTID7 = (ushort)rnd.Next(106894, 999999); // Sets random TID7
                    cln.TrainerSID7 = (ushort)rnd.Next(1649, 4294); // Sets random SID7
                }
                else
                {
                    cln.TrainerTID7 = TID7; // Sets SID7 to partner's
                    cln.TrainerSID7 = SID7; // Sets SID7 to partner's
                }
                cln.OT_Name = partner.TrainerName;
                cln.OT_Gender = trainerGender;
                cln.Language = trainerLanguage;
                cln.Version = trainerVersion;

                // For ehh, 
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
                    cln.Nickname = NameClearer(cln, fraudConnetion);

                cln.PID = ShinyKeeper(original, cln, la, fraudConnetion);
            }

            cln.EncryptionConstant = ECKeeper(cln, la, fraudConnetion);  // add protection for moushold and dundunsparce
            cln.RefreshChecksum();

            return Task.FromResult((result, cln));
        }

        // Return True/False if changing the OT is allowed based on game version
        public static bool OTChangeAllowed(PKM offered, int trainerVersion, IConsoleConnectionAsync fraudConnetion, bool copium = false)
        {
            bool changeAllowed = true;

            switch (offered.Version)
            {
                case (int)GameVersion.SW or (int)GameVersion.SH:
                    PK8 offeredSWSH = (PK8)offered.Clone();

                    /* Check for situations where one cannot be the OT of a pokemon
                    switch (offeredSWSH.Species)
                    {
                        // Non-shiny Zacian on Shield
                        case (ushort)Species.Zacian when copium == false:
                            if (!offeredSWSH.IsShiny && trainerVersion == (int)GameVersion.SH)
                                changeAllowed = false;
                            break;

                        // Non-shiny Zamazenta on Sword
                        case (ushort)Species.Zamazenta when copium == false:
                            if (!offeredSWSH.IsShiny && trainerVersion == (int)GameVersion.SW)
                                changeAllowed = false;
                            break;
                    } */
                    
                    //Stops mons with Specific OT from changing to User's OT
                    switch (offeredSWSH.OT_Name)
                    {
                        case "blaines":
                            changeAllowed = false;
                            break;
                    }

                    if (changeAllowed)
                        LogUtil.LogText($"OT changing allowed");
                    else
                        LogUtil.LogText($"OT changing NOT allowed");
                    break;

                case (int)GameVersion.BD or (int)GameVersion.SP:
                    PB8 offeredBDSP = (PB8)offered.Clone();
                    break;

                case (int)GameVersion.PLA:
                    PA8 offeredPLA = (PA8)offered.Clone();
                    break;

                case (int)GameVersion.SL or (int)GameVersion.VL:
                    PK9 offeredSV = (PK9)offered.Clone();

                    /*switch (offeredSV.Species)
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
                        */
                    //Ditto will not OT change unless it has Destiny Mark
                    /*case (ushort)Species.Ditto:
                        if (toSend.RibbonMarkDestiny == true)
                            changeAllowed = true;
                        else
                            changeAllowed = false;
                        break;
            } */

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
        public static uint ShinyKeeper(PKM original, PKM toSend, LegalityAnalysis originalLA, IConsoleConnectionAsync fraudConnetion)
        {
            var cln = toSend.Clone();

            //OT for Overworld8 (Galar Birds/Swords of Justice/Marked mons/Wild Grass)
            if (originalLA.Info.PIDIV.Type == PIDType.Overworld8)
            {
                fraudConnetion.Log($"PID is Overworld8");
                if (original.IsShiny)
                {
                    fraudConnetion.Log($"Request is for a shiny");
                    cln.PID = (((uint)(cln.TID16 ^ cln.SID16) ^ (cln.PID & 0xFFFF) ^ 0) << 16) | (cln.PID & 0xFFFF);
                }
                else
                {
                    fraudConnetion.Log($"Request is for a non shiny");
                    cln.PID = cln.PID; //Do nothing as non shiny
                }
            }
            else
            {
                fraudConnetion.Log($"PID is not Overworld8)");
                if (original.IsShiny)
                {
                    if (original.ShinyXor == 0) //Ensure proper shiny type is rerolled
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
                    if (toSend.Met_Location == 244)  //Dynamax Adventures
                    {
                        do
                        {
                            cln.SetShiny();
                        } while (cln.ShinyXor != 1);
                    }
                }
                else if (toSend.Met_Location != 162 && toSend.Met_Location != 244) //If not Max Raid, reroll PID for non shiny 
                {
                    cln.SetShiny();
                    cln.SetUnshiny();
                }
            }
                return cln.PID;
        }
        public static uint ECKeeper(PKM toSend, LegalityAnalysis toSendLA, IConsoleConnectionAsync fraudConnetion)
        {
            var cln = toSend.Clone();

            if (cln.Species == (ushort)Species.Dunsparce || cln.Species == (ushort)Species.Tandemaus)
            {
                if (cln.EncryptionConstant % 100 == 0) // Keep EC fully divisible to maintain future form
                    cln.EncryptionConstant = ECKeepModable(cln, fraudConnetion);
                else if (cln.Met_Location != 30024 || cln.Met_Location != 162) cln.SetRandomEC(); // Keep EC for raidmon
            }
            else
                switch (cln.Version)
                {
                    case ((int)GameVersion.SH or (int)GameVersion.SW):
                        if ((toSendLA.Info.PIDIV.Type != PIDType.Overworld8) && (cln.Met_Location != 162) && (cln.Met_Location != 244))
                            cln.SetRandomEC();
                        break;
                }
                   
                
            return cln.EncryptionConstant;
        }
        public static uint ECKeepModable(PKM pk, IConsoleConnectionAsync fraudConnetion)
        {
            pk.SetRandomEC();

            uint delta = pk.EncryptionConstant % 100;
            pk.EncryptionConstant -= delta;

            return pk.EncryptionConstant;
        }

        public static short CheckOfferedSpecies(PK8 offered)
        {
            short tradeevolve = 0;
            switch (offered.Species)
            {
                // Poliwhirl, Slowpoke need to be holding a King’s Rock
                case (ushort)Species.Poliwhirl:
                case (ushort)Species.Slowpoke:
                    tradeevolve = 222;
                    break;
                // Dusclops needs to be holding a Reaper's Cloth
                case (ushort)Species.Dusclops:
                    tradeevolve = 326;
                    break;
                // Feebas needs to be holding a Prism Scale
                case (ushort)Species.Feebas:
                    tradeevolve = 538;
                    break;
                // Scyther, Onix: needs to be holding a Metal Coat
                case (ushort)Species.Onix:
                case (ushort)Species.Scyther:
                    tradeevolve = 234;
                    break;
                // Swirlix needs to be holding a Whipped Dream
                case (ushort)Species.Swirlix:
                    tradeevolve = 647;
                    break;
                // Spritzee needs to be holding a Satchet
                case (ushort)Species.Spritzee:
                    tradeevolve = 648;
                    break;
                // Rhydon needs to be holding a Protector
                case (ushort)Species.Rhydon:
                    tradeevolve = 322;
                    break;
                // Karrablast and Shelmet needs the other to be traded
                case (ushort)Species.Karrablast:
                case (ushort)Species.Shelmet:
                    tradeevolve = -2;
                    break;
                //Seadra: needs to be holding a Dragon Scale
                case (ushort)Species.Seadra:
                    tradeevolve = 236;
                    break;
                //Porygon: needs to be holding an Upgrade
                case (ushort)Species.Porygon:
                    tradeevolve = 253;
                    break;
                //Porygon2: needs to be holding a Dubious Disc
                case (ushort)Species.Porygon2:
                    tradeevolve = 325;
                    break;
                // Electabuzz needs to be holding an Electirizer
                case (ushort)Species.Electabuzz:
                    tradeevolve = 323;
                    break;
                // Magmar needs to be holding a Magmarizer
                case (ushort)Species.Magmar:
                    tradeevolve = 324;
                    break;
                // Machoke, Haunter, Boldore, Pumpkaboo, Phantump, Kadabra, Gurdurr need to just be traded
                case (ushort)Species.Machoke:
                case (ushort)Species.Haunter:
                case (ushort)Species.Boldore:
                case (ushort)Species.Pumpkaboo:
                case (ushort)Species.Phantump:
                case (ushort)Species.Kadabra:
                case (ushort)Species.Gurdurr:
                    tradeevolve = -1;
                    break;
            }

            return tradeevolve;
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