using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoughtsAndCrosses
{
    public class LoggerSVG : ILogger
    {
        public string fileName = "board.html";
        public int maxLevelLimit = 10;
        public int fontSize = 12;
        public int gridSize = 20;
        public int textCenterX = 10;
        public int textBottomY = 15;
        public int spanBetweenBoards = 30;
        public int spanBetweenBunches = 20;
        public int boardUplinkHeight = 30;
        public int levelUplinkHeight = 30;

        private StringBuilder[] buf;
        private int[] nextBoardOffsetX;
        private int[] currentBunchOffsetX;
        private bool[] currentBunchOffsetXFreeze;
        private Dictionary<int, GameBoard> nextBoard;
        private StringBuilder[] nextBoardComment;
        private int boardSizeX;
        private int boardSizeY;
        private int maxLevel;
        private int previousAddedBoardLevel;
        private int boardWidth;
        private int boardHeight;

        public void Start()
        {
            previousAddedBoardLevel = 0;
            boardSizeX = 0;
            boardSizeY = 0;
            maxLevel = 0;
            boardWidth = 0;
            boardHeight = 0;

            buf = new StringBuilder[maxLevelLimit];
            nextBoardOffsetX = new int[maxLevelLimit];
            currentBunchOffsetX = new int[maxLevelLimit];
            currentBunchOffsetXFreeze = new bool[maxLevelLimit];
            nextBoard = new Dictionary<int, GameBoard>(maxLevelLimit);
            nextBoardComment = new StringBuilder[maxLevelLimit];
        }
        public void Finish()
        {
            CloseLevelsUpTo(0);
            int width = nextBoardOffsetX.Max();
            int height = boardHeight + boardUplinkHeight + (boardHeight + levelUplinkHeight + boardUplinkHeight) * (maxLevel - 1);
            using (StreamWriter file = new StreamWriter(fileName))
            {
                file.WriteLine("<html><body><svg width=\"{0}\" height=\"{1}\">", width, height);
                file.WriteLine("<g font-size=\"{0}\" font-family=\"sans - serif\" fill=\"black\" stroke=\"none\" text-anchor=\"middle\">", fontSize);
                foreach (StringBuilder sb in buf)
                {
                    if (sb != null)
                    {
                        file.WriteLine(sb.ToString());
                    }
                }
                file.WriteLine("</g></svg></body></html>");
            }
        }

        public StringBuilder PrintBoard(GameBoard board, string comment, int level)
        {
            if (level <= 0 || level > maxLevelLimit)
            {
                throw new IndexOutOfRangeException("Level for logging is out of allowed limits");
            }

            if (boardWidth == 0)
            {
                boardWidth = board.SizeX * gridSize;
                boardSizeX = board.SizeX;
            }
            if (boardHeight == 0)
            {
                boardHeight = board.SizeY * gridSize;
                boardSizeY = board.SizeY;
            }

            if (maxLevel < level)
            {
                maxLevel = level;
            }

            if (buf[level] == null)
            {
                buf[level] = new StringBuilder();
            }

            if (level > 1 && level > previousAddedBoardLevel && nextBoardOffsetX[level]< nextBoardOffsetX[level-1])
            {
                nextBoardOffsetX[level] = nextBoardOffsetX[level-1];
                currentBunchOffsetX[level] = nextBoardOffsetX[level];
            }

            CloseLevelsUpTo(level);
            // drawing previously memorized board at current level if any
            if (nextBoard.ContainsKey(level))
            {
                currentBunchOffsetXFreeze[previousAddedBoardLevel] = true;
                DrawNextBoard(level);
            }
            // remember board for future drawing
            nextBoard[level] = new GameBoard(board);
            nextBoardComment[level] = new StringBuilder(comment);
            previousAddedBoardLevel = level;

            return nextBoardComment[level];
        }

        /// <summary>
        /// Closes all levels up to specified one. Draws all buffered boards at that levels.
        /// </summary>
        /// <param name="level"></param>
        private void CloseLevelsUpTo(int level)
        {
            while (level < previousAddedBoardLevel)
            {
                // drawing board that was waitng for draw at that level
                DrawNextBoard(previousAddedBoardLevel);

                // drawing uplink of the bunch
                int summaryLineFromX = currentBunchOffsetX[previousAddedBoardLevel] + boardWidth / 2;
                int summaryLineToX = nextBoardOffsetX[previousAddedBoardLevel] - spanBetweenBoards - boardWidth / 2;
                int summaryLineY = (boardHeight + levelUplinkHeight + boardUplinkHeight) * (previousAddedBoardLevel - 1);
                int uplinkX = (summaryLineFromX + summaryLineToX) / 2;
                buf[previousAddedBoardLevel].AppendLine(
                    Line(summaryLineFromX, summaryLineY, summaryLineToX, summaryLineY).
                    Line(uplinkX, summaryLineY-levelUplinkHeight, uplinkX, summaryLineY).
                    EndPath());

                nextBoardOffsetX[previousAddedBoardLevel] += (spanBetweenBoards + spanBetweenBunches);
                currentBunchOffsetX[previousAddedBoardLevel] = nextBoardOffsetX[previousAddedBoardLevel];
                currentBunchOffsetXFreeze[previousAddedBoardLevel] = false;
                previousAddedBoardLevel--;

                // moving board on the upper level to the uplink line
                if (nextBoardOffsetX[previousAddedBoardLevel] < uplinkX - boardWidth / 2)
                {
                    nextBoardOffsetX[previousAddedBoardLevel] = uplinkX - boardWidth / 2;
                    if (!currentBunchOffsetXFreeze[previousAddedBoardLevel])
                    {
                        currentBunchOffsetX[previousAddedBoardLevel] = nextBoardOffsetX[previousAddedBoardLevel];
                    }
                }
            }
        }

        public void DrawNextBoard(int level)
        {
            if (nextBoard.ContainsKey(level))
            {
                Mark[,] board = nextBoard[level].GetBoard();
                int moveX = nextBoard[level].LatestMoveX;
                int moveY = nextBoard[level].LatestMoveY;
                int baseX = nextBoardOffsetX[level];
                int baseY = boardUplinkHeight + (boardHeight + levelUplinkHeight + boardUplinkHeight) * (level - 1);

                // drawing grid
                StringBuilder sbGrid = new StringBuilder();
                for (int y = 0; y <= boardSizeY; y++)
                {
                    sbGrid.Append(Line(baseX, baseY + y * gridSize, baseX + boardWidth, baseY + y * gridSize));
                }
                for (int x = 0; x <= boardSizeX; x++)
                {
                    sbGrid.Append(Line(baseX + x * gridSize, baseY, baseX + x * gridSize, baseY + boardHeight));
                }
                // drawing uplink line of the board
                sbGrid.Append(Line(baseX + boardWidth / 2, baseY - boardUplinkHeight, baseX + boardWidth / 2, baseY));
                buf[level].AppendLine(sbGrid.ToString().EndPath());

                // drawing contents of the grid
                for (int x = 0; x < boardSizeX; x++)
                {
                    for (int y = 0; y < boardSizeY; y++)
                    {
                        if (board[x, y] != Mark.None)
                        {
                            buf[level].AppendLine(string.Format("<text x=\"{0}\" y=\"{1}\"{3}>{2}</text>",
                                baseX + x * gridSize + textCenterX,
                                baseY + y * gridSize + textBottomY,
                                board[x, y] == Mark.Cross ? 'X' : 'O',
                                x == moveX && y == moveY ? " fill=\"red\"" : ""));
                        }
                    }
                }

                // drawing win line(s)
                foreach (LineOfMarks line in nextBoard[level].WinLines)
                {
                    buf[level].AppendLine(Line(
                        baseX + line.FromX * gridSize + gridSize / 2,
                        baseY + line.FromY * gridSize + gridSize / 2,
                        baseX + line.ToX * gridSize + gridSize / 2,
                        baseY + line.ToY * gridSize + gridSize / 2)
                        .EndPath("blue"));
                }

                // drawing comments over the uplink line
                buf[level].AppendLine(string.Format("<text x=\"{0}\" y=\"{1}\">{2}</text>",
                    baseX + boardWidth / 2,
                    baseY - boardUplinkHeight / 2,
                    nextBoardComment[level].ToString()));


                nextBoardOffsetX[level] = baseX + boardWidth + spanBetweenBoards;

                nextBoard.Remove(level);
            }
        }


        public static string Line(int x1, int y1, int x2, int y2)
        {
            return string.Format(" M{0} {1} L{2} {3}", x1, y1, x2, y2);
        }
    }

    static class LocalExtensions
    {
        public static string Line(this string line, int x1, int y1, int x2, int y2)
        {
            return line + LoggerSVG.Line(x1, y1, x2, y2);
        }
        public static string EndPath(this string line, string color="black")
        {
            return string.Format("<path d=\"{0} \" stroke=\"{1}\" fill=\"none\"/>", line, color);
        }
    }
}
