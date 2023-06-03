using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoughtsAndCrosses
{
    public class PlayerRandom : IPlayer
    {
        private readonly Random random = new Random();
        public void MakeMove(GameBoard game)
        {
            if (game.NextMove != Mark.None && game.FreeFields > 0)
            {
                int num = random.Next(0, game.FreeFields);
                Mark[,] board = game.GetBoard();
                for (int y = 0; y < game.SizeY; y++)
                    for (int x = 0; x < game.SizeX; x++)
                        if (board[x, y] == Mark.None)
                            if (num-- == 0)
                            {
                                game.Move(x, y);
                            }
            }
        }
    }
}
