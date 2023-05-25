using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoughtsAndCrosses
{
    public static class UtilityExtensions
    {
        public static char ToChar(this Mark mark)
        {
            return (mark == Mark.None) ? '.' : (mark == Mark.Cross ? 'X' : 'O');
        }

        public static void PrintBoard(this GameBoard game, bool pause = false)
        {
            Mark[,] brd = game.GetBoard();
            Console.Write(' ');
            for (int x = 0; x < game.SizeX; x++)
                Console.Write(x);
            Console.WriteLine();
            for (int y = 0; y < game.SizeY; y++)
            {
                Console.Write(y);
                for (int x = 0; x < game.SizeX; x++)
                {
                    Console.Write(brd[x, y].ToChar());
                }
                Console.WriteLine();
            }
            if (pause)
            {
                Console.ReadLine();
            }
        }
    }
}
