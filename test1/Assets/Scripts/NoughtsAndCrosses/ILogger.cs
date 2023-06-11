using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoughtsAndCrosses
{
    public interface ILogger
    {
        /// <summary>
        /// Start logging
        /// </summary>
        void Start();

        /// <summary>
        /// Finish logging
        /// </summary>
        void Finish();

        /// <summary>
        /// Print GameBoard as a standalone unit or as part of a traverse tree
        /// </summary>
        /// <param name="board">board to print</param>
        /// <param name="comment">comment to print nearby the board</param>
        /// <param name="level">level/depth of traverse subtree, starting 1</param>
        /// <returns>Optionally returns StringBuilder for adding more information about the board. If implementation returns null, it means adding more information after printing is not supported.</returns>
        StringBuilder PrintBoard(GameBoard board, string comment, int level);
    }
}
