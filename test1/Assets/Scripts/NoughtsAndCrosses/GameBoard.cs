using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoughtsAndCrosses
{
    /// <summary>
    /// Marks on the game board
    /// </summary>
    public enum Mark : byte {None, Cross, Nought};

    public enum GameWinner : byte { None, Cross, Nought, Draw };

    /// <summary>
    /// Represents a line of marks on the board
    /// </summary>
    public class LineOfMarks
    {
        public int FromX;
        public int FromY;
        public int ToX;
        public int ToY;
        public int length;
        /// <summary>
        /// Number of open ends (empty positions at the end)
        /// </summary>
        public int openEnds;
    }

    /// <summary>
    /// Implements game board of specified size.
    /// </summary>
    public class GameBoard
    {
        /// <summary>
        /// X size of the board
        /// </summary>
        public int SizeX { get; private set; }

        /// <summary>
        /// Y size of the board
        /// </summary>
        public int SizeY { get; private set; }

        /// <summary>
        /// Leinght of the line of marks that makes one side a winner in the game
        /// </summary>
        public int WinLineSize { get; private set; }

        /// <summary>
        /// Allow one last move for Nought after Cross builded a winning line
        /// </summary>
        public bool AllowEqualMoves { get; private set; }

        public List<LineOfMarks> WinLines { get; }

        /// <summary>
        /// Current state of the game board
        /// </summary>
        private readonly Mark[,] Board;
        
        /// <summary>
        /// X coordinate of the latest move
        /// </summary>
        public int LatestMoveX { get; private set; }

        /// <summary>
        /// Y coordinate of the latest move
        /// </summary>
        public int LatestMoveY { get; private set; }

        /// <summary>
        /// Counter of free fields on the board. Used to detect draw.
        /// </summary>
        public int FreeFields { get; private set; }

        /// <summary>
        /// Provides access to the array representing current state on the game board, but does not allow changing it
        /// </summary>
        /// <returns>Copy of the array with game state</returns>
        public Mark[,] GetBoard()
        {
            return (Mark[,])Board.Clone();
        }

        /// <summary>
        /// Which side has to make next move. <see cref="Mark.None"/> means the game is over.
        /// </summary>
        public Mark NextMove { get; private set; }

        /// <summary>
        /// Who is the winner. 
        /// <see cref="GameWinner.None"/> means either the game is in progress.
        /// </summary>
        public GameWinner Winner { get; private set; }

        /// <summary>
        /// Constructs a game board with given parameters and sets it into initial state for starting new game.
        /// </summary>
        /// <param name="sizeX">X size of the board</param>
        /// <param name="sizeY">Y size of the board</param>
        /// <param name="allowEqualMoves">Allows Nought to make last move after Cross made a winning line</param>
        /// <param name="winLineSize">Leinght of the line of marks that makes one side a winner in the game</param>
        public GameBoard (int sizeX, int sizeY, int winLineSize, bool allowEqualMoves)
        {
            SizeX = sizeX;
            SizeY = sizeY;
            WinLineSize = winLineSize;
            AllowEqualMoves = allowEqualMoves;
            Board = new Mark[SizeX, SizeY];
            NextMove = Mark.Cross;
            Winner = GameWinner.None;
            FreeFields = sizeX * SizeY;
            WinLines = new List<LineOfMarks>();
        }

        /// <summary>
        /// Copy constructor. Copies current state of the given game board.
        /// </summary>
        /// <param name="boardToCopy">Original board to copy from</param>
        public GameBoard (GameBoard boardToCopy)
        {
            SizeX = boardToCopy.SizeX;
            SizeY = boardToCopy.SizeY;
            WinLineSize = boardToCopy.WinLineSize;
            AllowEqualMoves = boardToCopy.AllowEqualMoves;
            Board = boardToCopy.GetBoard();
            NextMove = boardToCopy.NextMove;
            FreeFields = boardToCopy.FreeFields;
            Winner = boardToCopy.Winner;
            WinLines = new List<LineOfMarks>();
            WinLines.AddRange(boardToCopy.WinLines);
        }

        /// <summary>
        /// Performs a move of current player (<see cref="NextMove"/>) to the position specified by parameters. 
        /// Checks if the player won after the move and updates Board state accordingly.
        /// </summary>
        /// <param name="x">X coordinate of the move</param>
        /// <param name="y">Y coordinate of the move</param>
        public void Move(int x, int y)
        {
            if (NextMove == Mark.None)
            {
                throw new InvalidOperationException("The game is over. Cannot do moves anymore.");
            }
            if (Board[x, y] != Mark.None)
            {
                throw new InvalidOperationException(string.Format("The field [{0},{1}] is occupied.", x, y));
            }
            LatestMoveX = x;
            LatestMoveY = y;
            Board[x, y] = NextMove;
            LineOfMarks line = FindWinLine(x, y);
            if (line != null)
            {
                WinLines.Add(line);
                Winner = Winner == GameWinner.None ? (NextMove == Mark.Cross ? GameWinner.Cross : GameWinner.Nought) : GameWinner.Draw;
            }
            NextMove = NextMove == Mark.Cross ? (Winner == GameWinner.None || AllowEqualMoves ? Mark.Nought : Mark.None) : (Winner == GameWinner.None ? Mark.Cross : Mark.None);
            FreeFields--;
            if (FreeFields <= 0)
            {
                NextMove = Mark.None;
                if (Winner == GameWinner.None)
                {
                    Winner = GameWinner.Draw;
                }
            }
        }

        /// <summary>
        /// Checks if win-lines appear after the latest move specified by parameters. Updates <see cref="NextMove"/> and <see cref="Winner"/>.
        /// </summary>
        /// <param name="x">X coordinate of just made move.</param>
        /// <param name="y">Y coordinate of just made move.</param>
        private LineOfMarks FindWinLine(int x, int y)
        {
            LineOfMarks line;
            // vertical
            line = FindLine(x, y, 0, 1, NextMove);
            if (line.length >= WinLineSize)
            {
                return line;
            }
            // horizontal
            line = FindLine(x, y, 1, 0, NextMove);
            if (line.length >= WinLineSize)
            {
                return line;
            }
            // diagonal 1
            line = FindLine(x, y, 1, 1, NextMove);
            if (line.length >= WinLineSize)
            {
                return line;
            }
            // diagonal 2
            line = FindLine(x, y, 1, -1, NextMove);
            if (line.length >= WinLineSize)
            {
                return line;
            }
            return null;
        }

        /// <summary>
        /// Finds all lines of marks passing through the specified point if we pit the mark specified to that position.
        /// </summary>
        /// <param name="x">x coordinate of the point</param>
        /// <param name="y">y coordinate of the point</param>
        /// <param name="mark">mark to try in the position</param>
        /// <returns></returns>
        public LineOfMarks[] FindAllLines(int x, int y, Mark mark)
        {
            LineOfMarks[] result = new LineOfMarks[4];
            // vertical
            result[0] = FindLine(x, y, 0, 1, mark);
            // horizontal
            result[1] = FindLine(x, y, 1, 0, mark);
            // diagonal 1
            result[2] = FindLine(x, y, 1, 1, mark);
            // diagonal 2
            result[3] = FindLine(x, y, 1, -1, mark);
            return result;
        }

        /// <summary>
        /// Finds a line of sequential marks passing through the specified point in specified direction. Searches the direction back and forward.
        /// </summary>
        /// <param name="x">x coordinate of the point</param>
        /// <param name="y">y coordinate of the point</param>
        /// <param name="dx">delta x of the direction</param>
        /// <param name="dy">delta y of the direction</param>
        /// <param name="mark">mark to look for</param>
        /// <returns>The line found</returns>
        private LineOfMarks FindLine(int x, int y, int dx, int dy, Mark mark)
        {
            LineOfMarks line = new LineOfMarks() { FromX = x, FromY = y, ToX = x, ToY = y, length = 1, openEnds = 0 };

            // go forward from the point
            int xx = x;
            int yy = y;
            while (true)
            {
                xx += dx;
                yy += dy;
                if (CheckField(xx, yy, mark))
                {
                    line.ToX = xx;
                    line.ToY = yy;
                    line.length++;
                }
                else
                {
                    break;
                }
            }
            line.openEnds += CheckField(xx, yy, Mark.None) ? 1 : 0;

            // go back from the point
            xx = x;
            yy = y;
            while (true)
            {
                xx -= dx;
                yy -= dy;
                if (CheckField(xx, yy, mark))
                {
                    line.FromX = xx;
                    line.FromY = yy;
                    line.length++;
                }
                else
                {
                    break;
                }
            }
            line.openEnds += CheckField(xx, yy, Mark.None) ? 1 : 0;

            return line;
        }

        /// <summary>
        /// Checks if specified field is in range of the board and has specified mark.
        /// </summary>
        /// <param name="x">x coordinate of the field</param>
        /// <param name="y">y coordinate of the field</param>
        /// <param name="mark">mark that is expected</param>
        /// <returns>True if the field is in range of the board and contains specified mark, otherwise False</returns>
        public bool CheckField(int x, int y, Mark mark)
        {
            return x >= 0 && x < SizeX && y >= 0 && y < SizeY && Board[x, y] == mark;
        }
    }
}
