using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoughtsAndCrosses
{
    public class PlayerTraverse : IPlayer
    {
        private enum Score { 
            None, // no result, the move is invalid
            Unknown, // the move is valid, but the result is unknown
            Loss, // if we do this move, the best strategy after that gives us Loss
            Draw, // if we do this move, the best strategy after that gives us Draw
            Win // if we do this move, the best strategy after that gives us Win
        };

        private struct Move
        {
            public int X;
            public int Y;
            public Score Score;
        }

        /// <summary>
        /// How many ticks (100 nanosecond intervals) it is allowed to spend on search of the solution
        /// </summary>
        private readonly int TimeLimitTicks;

        private long HardStopTicks;
        private int moves_counter;
        private int level;

        public ILogger logger = null;

        public PlayerTraverse(int timeLimitTicks)
        {
            TimeLimitTicks = timeLimitTicks;
        }

        public void MakeMove(GameBoard game)
        {
            HardStopTicks = DateTime.Now.Ticks + TimeLimitTicks;
            moves_counter = 0;
            level = 1;
            StringBuilder loggerBoardComment = null;
            if (logger != null)
            {
                logger.Start();
                loggerBoardComment = logger.PrintBoard(game, "", level);
            }
            Move move = FindBestMove(game);
            if (logger != null)
            {
                if (loggerBoardComment != null)
                {
                    loggerBoardComment.Append(string.Format("Best Move: ({0},{1}), score={2}", move.X, move.Y, move.Score));
                }
                logger.Finish();
            }

            long ticks_elapsed = DateTime.Now.Ticks - HardStopTicks + TimeLimitTicks;
            Console.WriteLine("Ticks elapsed: {0}; Move: X={1},Y={2},Score={3}",
                ticks_elapsed, move.X, move.Y, move.Score);
            Console.WriteLine("Moves tried: {0}", moves_counter);

            game.Move(move.X, move.Y);
        }

        private Move FindBestMove(GameBoard game)
        {
            level++;
            GameWinner anticipated_winner = game.NextMove == Mark.Cross ? GameWinner.Cross : GameWinner.Nought;
            Mark[,] board = game.GetBoard();
            bool board_is_empty = true;
            Move best = new Move { Score = Score.None };
            for (int y = 0; y < game.SizeY && best.Score < Score.Win; y++)
                for (int x = 0; x < game.SizeX && best.Score < Score.Win; x++)
                    if (board[x, y] == Mark.None)
                    {
                        if (best.Score == Score.None)
                        {
                            best.X = x;
                            best.Y = y;
                            best.Score = Score.Unknown;
                        }
                        GameBoard game_branch = new GameBoard(game);
                        game_branch.Move(x, y);
                        moves_counter++;
                        //game_branch.PrintBoard(true);

                        StringBuilder loggerBoardComment = null;
                        if (logger != null)
                        {
                            loggerBoardComment = logger.PrintBoard(game_branch, "[" + moves_counter + "]", level);
                        }

                        if (game_branch.NextMove == Mark.None)
                        {
                            Score score = game_branch.Winner == anticipated_winner ? Score.Win : (game_branch.Winner == GameWinner.Draw ? Score.Draw : Score.Loss);
                            if (loggerBoardComment != null)
                            {
                                loggerBoardComment.Append(score.ToString());
                            }
                            if (best.Score < score)
                            {
                                best.X = x;
                                best.Y = y;
                                best.Score = score;
                            }
                        }
                        else
                        {
                            // If we have time, go into depth, otherwise finish current phase
                            if (DateTime.Now.Ticks < HardStopTicks)
                            {
                                Move opponent_best_move = FindBestMove(game_branch);
                                Score score = opponent_best_move.Score == Score.Loss ? Score.Win : (opponent_best_move.Score == Score.Win ? Score.Loss : opponent_best_move.Score);
                                if (loggerBoardComment != null)
                                {
                                    loggerBoardComment.Append(score.ToString());
                                }
                                if (best.Score < score)
                                {
                                    best.X = x;
                                    best.Y = y;
                                    best.Score = score;
                                }
                            }
                        }
                    }
                    else
                    {
                        board_is_empty = false;
                    }

            // On empty board making move to the center
            if (board_is_empty)
            {
                best.X = game.SizeX / 2;
                best.Y = game.SizeY / 2;
            }

            level--;
            return best;
        }
    }
}
