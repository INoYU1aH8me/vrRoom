using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoughtsAndCrosses
{
    public class PlayerOrderedTraverse : IPlayer
    {
        private enum Score
        {
            None, // Not tried
            Loss, // Loss detected as a result of the move
            Unknown, // the result is unknown
            Draw, // Draw detected as a result of the move
            Win // Win detected as a result of the move
        };

        private struct Move
        {
            public int X;
            public int Y;
            public Score Score;
            /// <summary>
            /// Heoristic metric for comparison of two moves. The higher the better.
            /// (max length of lines built with this move and enemy lines prevented by making this move)*100+
            /// (then total length of lines built with this move and enemy lines prevented by making this move)
            /// </summary>
            public int HeuristicMetric;
            public int OpponentHeuristicMetric;
            public Score OpponentBestScore;
            public Score BestNextMoveScore;
        }

        private const int HeuristicThreshold = 300; // minumal heuristic metric is 208 (a field with no lines (1+1)*100+(4+4))

        /// <summary>
        /// How many ticks (100 nanosecond intervals) it is allowed to spend on search of the solution
        /// </summary>
        private readonly int TimeLimitTicks;

        private long HardStopTicks;

        private int level;
        const int MaxDepth = 4;
        private readonly int[] MaxBranches = new int[] { 8, 8, 4 }; // max branches to traverse from each node

        string[] levels;
        int moves_counter;

        public PlayerOrderedTraverse(int timeLimitTicks)
        {
            TimeLimitTicks = timeLimitTicks;
        }

        public void MakeMove(GameBoard game)
        {
            HardStopTicks = DateTime.Now.Ticks + TimeLimitTicks;
            level = 0;
            levels = new string[MaxDepth+1];
            moves_counter = 0;
            Move move = FindBestMove(game);
            long ticks_elapsed = DateTime.Now.Ticks - HardStopTicks + TimeLimitTicks;
            Console.WriteLine("Ticks elapsed: {0}; Move: X={1},Y={2},Score={3},HeuristicMetric={4}",
                ticks_elapsed, move.X, move.Y, move.Score, move.HeuristicMetric);

            Console.WriteLine("Moves tried: {0}", moves_counter);
            /*
            for (int i=1; i<=MaxDepth; i++)
            {
                Console.WriteLine("Level " + i + ": " + levels[i]);
            }
            */

            game.Move(move.X, move.Y);
        }

        private Move FindBestMove(GameBoard game)
        {
            level++;
            GameWinner anticipated_winner = game.NextMove == Mark.Cross ? GameWinner.Cross : GameWinner.Nought;
            Mark[,] board = game.GetBoard();
            Move best = new Move { Score = Score.None, HeuristicMetric = 0 };
            Mark opponent_mark = game.NextMove == Mark.Cross ? Mark.Nought : Mark.Cross;
            Dictionary<Move, GameBoard> branches = new Dictionary<Move, GameBoard>();

            List<Move> possible_moves = new List<Move>();
            for (int y = 0; y < game.SizeY; y++)
                for (int x = 0; x < game.SizeX; x++)
                    if (board[x, y] == Mark.None)
                    {
                        LineOfMarks[] my_lines = game.FindAllLines(x, y, game.NextMove);
                        LineOfMarks[] enemy_lines = game.FindAllLines(x, y, opponent_mark);

                        int metric =
                            (my_lines.Count(line => line.length + line.openEnds > game.WinLineSize) +
                            enemy_lines.Count(line => line.length + line.openEnds > game.WinLineSize)) * 10000 +
                            (my_lines.Count(line => line.length + line.openEnds == game.WinLineSize) +
                            enemy_lines.Count(line => line.length + line.openEnds == game.WinLineSize)) * 1000 +
                            (my_lines.Max(line => line.length) + enemy_lines.Max(line => line.length)) * 100 +
                            my_lines.Sum(line => line.length) + enemy_lines.Sum(line => line.length);

                        Move move = new Move() {
                            X = x,
                            Y = y,
                            Score = Score.None,
                            HeuristicMetric = metric,
                            OpponentHeuristicMetric = 0
                        };
                        moves_counter++;

                        // best score and move score is Unknown at this point
                        if (best.HeuristicMetric < move.HeuristicMetric)
                        {
                            best = move;
                            //if (level == 1)
                            //    Console.WriteLine("Better0: level={0},X={1},Y={2},Score={3},BestNextMoveScore={4},HeuristicMetric={5},OpponentHeuristicMetric={6}", level, best.X, best.Y, best.Score, best.BestNextMoveScore, best.HeuristicMetric, best.OpponentHeuristicMetric);
                        }

                        // if there are no other marks/lines nearby, do not include such field into next stages of search
                        if (metric >= HeuristicThreshold)
                        {
                            possible_moves.Add(move);
                        }
                    }

            // On empty board making move to the center
            if (best.HeuristicMetric < HeuristicThreshold)
            {
                best.X = game.SizeX / 2;
                best.Y = game.SizeY / 2;
            }

            levels[level] += possible_moves.Count + " ";

            // Starting traverse from the moves that produce or prevent longest lines (Max)
            // among equally long lines starting from the moves which produce or prevent more lines (Sum)
            // Trying to make a move to each of pre-selected field and see if the game is over and with which result
            // selecting the branch with best result for us (win or draw)
            // if game is not over with the move, att that "branch of the game" to a list for future consideration
            foreach (Move move in possible_moves.OrderByDescending(mv => mv.HeuristicMetric))
            {
                GameBoard game_branch = new GameBoard(game);
                game_branch.Move(move.X, move.Y);
                //game_branch.PrintBoard(true);
                if (game_branch.NextMove == Mark.None)
                {
                    Score score = game_branch.Winner == anticipated_winner ? Score.Win : (game_branch.Winner == GameWinner.Draw ? Score.Draw : Score.Loss);
                    if (best.Score < score)
                    {
                        best = move;
                        best.Score = score;
                        //if (level == 1)
                        //Console.WriteLine("Better1: level={0},X={1},Y={2},Score={3},BestNextMoveScore={4},HeuristicMetric={5},OpponentHeuristicMetric={6}", level, best.X, best.Y, best.Score, best.BestNextMoveScore, best.HeuristicMetric, best.OpponentHeuristicMetric);
                        //game_branch.PrintBoard();
                    }
                }
                else
                {
                    branches[move] = game_branch;
                }
            }

            // Traversing one by one all game branches where game is not over
            // The traverse might be huge. Limiting it by several parameters: 
            // 1) traverse depth, or level, or number of moves - our (1) - enemy (2) - our (3) - enemy (4) - etc.
            // 2) traverse width, number of branches tried on each level
            // 3) time spent - stopping search when time limit is over
            // TODO: optimize, use two lists instead of dictionary + ordering of initial list
            Score OpponentBestScore = Score.Loss;
            int branch_count = 0;
            int MaxBr = level <= MaxBranches.Length ? MaxBranches[level - 1] : 2;
            foreach (Move move in possible_moves.OrderByDescending(mv => mv.HeuristicMetric))
            {
                if (branches.ContainsKey(move))
                {
                    // limiting width of search tree
                    if (++branch_count > MaxBr)
                    {
                        break;
                    }
                    GameBoard game_branch = branches[move];
                    // If we have time and did not reach depth limit, go into depth, otherwise finish current phase
                    if (DateTime.Now.Ticks < HardStopTicks && level<MaxDepth)
                    {
                        //Console.Write(" [L={0} {1}({2},{3})[ ", level, (game.NextMove == Mark.Cross ? 'X' : 'O'), move.X, move.Y);
                        // trying to guess best move of the opponent
                        Move opponent_best_move = FindBestMove(game_branch);

                        // Calculating best score of the opponent in the branch related to the move to use it as next move score estimation
                        if (OpponentBestScore < opponent_best_move.Score)
                        {
                            OpponentBestScore = opponent_best_move.Score;
                        }

                        //Console.Write(" ]L={0} {1}({2},{3})] ", level, (game.NextMove == Mark.Cross ? 'X' : 'O'), move.X, move.Y);
                        /*
                        Score op_score = opponent_best_move.Score;
                        Score score = op_score == 
                            Score.Unknown ? (opponent_best_move.BestNextMoveScore >= Score.OpponentCanWin ? Score.OpponentCanWin : Score.Unknown) : 
                            ((op_score == Score.Loss || op_score == Score.OpponentCanWin) ? Score.OpponentCanLose : 
                            ((op_score == Score.Win || op_score == Score.OpponentCanLose) ? Score.OpponentCanWin : Score.OpponentCanDraw));
                        */
                        Score score = opponent_best_move.Score == Score.Loss ? Score.Win : (opponent_best_move.Score == Score.Win ? Score.Loss : (opponent_best_move.Score == Score.None ? Score.Unknown : opponent_best_move.Score));
                        //Console.WriteLine("best.score={0} X={1} Y={2}, score={3}, level={4}", best.Score, best.X, best.Y, score, level);
                        if (best.Score < score)
                        {
                            best = move;
                            best.Score = score;
                            best.BestNextMoveScore = opponent_best_move.OpponentBestScore;
                            best.OpponentHeuristicMetric = opponent_best_move.HeuristicMetric;
                            //if (level == 1) 
                            //Console.WriteLine("Better2: level={0},X={1},Y={2},Score={3},BestNextMoveScore={4},HeuristicMetric={5},OpponentHeuristicMetric={6}", level, best.X, best.Y, best.Score, best.BestNextMoveScore, best.HeuristicMetric,best.OpponentHeuristicMetric);
                        }
                        else if (best.Score == score)
                        {
                            // opponent_best_move.OpponentBestScore is best found score of "opponent of opponent", i.e. our best score for next move in that branch
                            if (best.BestNextMoveScore < opponent_best_move.OpponentBestScore)
                            {
                                best = move;
                                best.Score = score;
                                best.BestNextMoveScore = opponent_best_move.OpponentBestScore;
                                best.OpponentHeuristicMetric = opponent_best_move.HeuristicMetric;
                                //if (level == 1)
                                //Console.WriteLine("Better3: level={0},X={1},Y={2},Score={3},BestNextMoveScore={4},HeuristicMetric={5},OpponentHeuristicMetric={6}", level, best.X, best.Y, best.Score, best.BestNextMoveScore, best.HeuristicMetric, best.OpponentHeuristicMetric);
                            }
                            else if (best.BestNextMoveScore == opponent_best_move.OpponentBestScore)
                            {
                                // best.HeuristicMetric cannot be greater than move.HeuristicMetric because of ordering so effectively the condition is "=="
                                if (best.HeuristicMetric >= move.HeuristicMetric
                                    && (best.OpponentHeuristicMetric == 0 || best.OpponentHeuristicMetric > opponent_best_move.HeuristicMetric))
                                {
                                    best = move;
                                    best.Score = score;
                                    best.OpponentHeuristicMetric = opponent_best_move.HeuristicMetric;
                                    best.BestNextMoveScore = opponent_best_move.OpponentBestScore;
                                    //if (level == 1) 
                                    //Console.WriteLine("Better4: level={0},X={1},Y={2},Score={3},BestNextMoveScore={4},HeuristicMetric={5},OpponentHeuristicMetric={6}", level, best.X, best.Y, best.Score, best.BestNextMoveScore, best.HeuristicMetric, best.OpponentHeuristicMetric);
                                }
                            }
                        }
                    }
                }
            }
            best.OpponentBestScore = OpponentBestScore;

            level--;
            return best;
        }
    }
}
