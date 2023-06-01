/*******************************
 *  Universal Methods          *
 *******************************
 *  Methods:                   *
 *      BallSwapper            *
 *      ShinyKeeper            *      
 *******************************/

using PKHeX.Core;

namespace SysBot.Pokemon
{
    public partial class PokeTradeBot : PokeRoutineExecutor8, ICountBot
    {
        private int BallSwapper(int ballItem) => ballItem switch
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

        private uint ShinyKeeper(PKM toSend)
        {
            PKM cln = toSend.Clone();
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
            return cln.PID;
        }

        private string NameClearer(PKM toSend)
        {
            PKM cln = toSend.Clone();
            return cln.Nickname;
        }
    }
}