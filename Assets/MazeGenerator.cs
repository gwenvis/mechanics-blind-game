using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

namespace QTea
{
    public class MazeGenerator
    {
        private struct SlowGenerateData
        {
            internal Stack<int> indexStack;
            internal int nextCell;
            internal bool done;
            internal int generateStep;
        }
        
        private readonly int columns;
        private readonly int rows;
        private readonly int entrance;
        private readonly int exit;
        private readonly int removeWalls;
        private readonly IRandom random;
        private readonly Cell[] cells;

        private bool slowGenerate = false;
        private SlowGenerateData slowGenerateData;

        private readonly Dictionary<Direction, Direction> opposites = new Dictionary<Direction, Direction>()
        {
            {Direction.North, Direction.South}, {Direction.South, Direction.North},
            {Direction.East, Direction.West}, {Direction.West, Direction.East}
        };

        public MazeGenerator(int columns, int rows, int entrance, int exit, IRandom random, int removeWalls = 0)
        {
            this.columns = columns;
            this.rows = rows;
            this.entrance = entrance;
            this.exit = exit;
            this.removeWalls = removeWalls;
            this.random = random;

            Cell[] cells = new Cell[columns * rows];
            InitializeArray(ref cells);
            this.cells = cells;
        }

        /// <summary>
        /// Starts regenerating instantly
        /// </summary>
        /// <param name="maxLoop">Safe break for the loop (so the program doesn't hang). set 0 for no limit.</param>
        /// <returns></returns>
        public Maze Generate(int maxLoop = 50_000)
        {
            if (slowGenerate)
            {
                throw new Exception("no");
            }
            
            Stack<int> visitedCells = new Stack<int>();

            int currentCell = GetIndex(0, entrance);
            bool done = false;
            int currentLoop = 0;

            while (!done)
            {
                var result = CarveGreedy(currentCell, visitedCells);
                done = maxLoop == 0 ? result.done : currentLoop > maxLoop || result.done;
                currentCell = result.nextCell;
                currentLoop++;
            }

            return new Maze(cells, columns, rows, true, currentLoop);
        }

        /// <summary>
        /// Starts a generator which calls the callback after every generation
        /// </summary>
        /// <param name="mazeCallback">Callback when a single step is generated</param>
        public Maze StartSlowGenerate()
        {
            slowGenerateData = new SlowGenerateData()
            {
                indexStack = new Stack<int>(),
                nextCell = GetIndex(0, entrance)
            };

            slowGenerate = true;
            return new Maze(cells, columns, rows, false, 0);
        }

        public MazeUpdate NextSlowGenerate()
        {
            if (!slowGenerate)
            {
                throw new Exception("Call StartSlowGenerate first!");
            }
            else if (slowGenerateData.done)
            {
                throw new Exception("Generation is already completed.");
            }


            int lastCell = slowGenerateData.nextCell;
            (bool done, int nextCell) = CarveGreedy(slowGenerateData.nextCell, slowGenerateData.indexStack);
            slowGenerateData.generateStep++;
            slowGenerateData.done = done;
            slowGenerateData.nextCell = nextCell;
            
            (int, Cell)[] updatedCells = new (int, Cell)[2];
            updatedCells[0] = (lastCell, cells[lastCell]);
            updatedCells[1] = (nextCell, cells[nextCell]);

            return new MazeUpdate(updatedCells, slowGenerateData.generateStep, slowGenerateData.done);
        }

        private (bool done, int nextCell) CarveGreedy(int currentCell, Stack<int> visitedCells)
        {
            cells[currentCell].Visited = true;
            
            // get an unvisited neighbor
            (bool result, int nextCell, Direction direction) = GetUnvisitedNeighbor(currentCell);

            // there is still a neighbor, carve between the two.
            if (result)
            {
                // carve the walls and set self to visited
                cells[currentCell].Walls &= ~direction;
                cells[nextCell].Walls &= ~opposites[direction];

                // add self to stack
                visitedCells.Push(currentCell);
                return (false, nextCell);
            }
            else if (visitedCells.Count > 0)
            {
                int poppedCell = visitedCells.Pop();
                return (false, poppedCell);
            }

            return (true, 0);
        }

        private void InitializeArray(ref Cell[] cell)
        {
            for (int i = 0; i < cell.Length; i++)
            {
                cell[i].Walls = (Direction) 0b1111;
            }
        }

        private int GetIndex(int row, int col) => row + col * rows;
        private (int row, int col) GetPosition(int index) => (index % rows, index / rows);

        /// <summary>
        /// Get cells in the order of NESW
        /// </summary>
        public (bool exists, int index, Direction direction)[] GetRelativeCells(int index)
        {
            Direction[] directions = (Direction[]) Enum.GetValues(typeof(Direction));
            (bool, int, Direction)[] relativeCells = new (bool, int, Direction)[4];

            for (int i = 0; i < directions.Length; i++)
            {
                if (GetRelativeCell(index, directions[i], out int cellIndex))
                {
                    relativeCells[i] = (true, cellIndex, directions[i]);
                }
            }

            return relativeCells;
        }

        private (bool result, int index, Direction direction) GetUnvisitedNeighbor(int index, bool random = true)
        {
            // limit the list to only cells that exist and cells that have not been visited.
            var relativeCells = GetRelativeCells(index);
            (int, Direction)[] indices = relativeCells
                .Where(x => x.exists && !cells[x.index].Visited)
                .Select(x => (x.index, x.direction))
                .ToArray();

            if (indices.Length == 0) return default;
            // if not random, or there is only one element just return the first.
            if (!random || indices.Length == 1) return (true, indices[0].Item1, indices[0].Item2);
            int randomIndex = this.random.Range(indices.Length);
            return (true, indices[randomIndex].Item1, indices[randomIndex].Item2);
        }

        private bool GetRelativeCell(int index, Direction direction, out int outIndex)
        {
            bool exists = false;
            int relativeIndex = -1;

            void SetCell(int r, int c)
            {
                if (r < 0 || c < 0 || c >= columns || r >= rows) return;
                int index = GetIndex(r, c);

                exists = true;
                relativeIndex = index;
            }

            (int row, int col) = GetPosition(index);

            switch (direction)
            {
                case Direction.North:
                    SetCell(row, col + 1);
                    break;
                case Direction.East:
                    SetCell(row + 1, col);
                    break;
                case Direction.South:
                    SetCell(row, col - 1);
                    break;
                case Direction.West:
                    SetCell(row - 1, col);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }

            outIndex = relativeIndex;
            return exists;
        }
    }

    public struct Cell
    {
        public bool Visited;
        public Direction Walls; // what walls are on? (Flag enum)
    }

    [Flags]
    public enum Direction
    {
        North = 1,
        East = 2,
        South = 4,
        West = 8
    }

    public readonly struct Maze
    {
        public readonly Cell[] MazeCells;
        public readonly int Columns;
        public readonly int Rows;
        public readonly bool Done;
        public readonly int Steps;

        public Maze(Cell[] mazeCells, int columns, int rows, bool done, int steps)
        {
            MazeCells = mazeCells;
            Columns = columns;
            Rows = rows;
            Done = done;
            Steps = steps;
        }
    }

    public readonly struct MazeUpdate
    {
        public readonly (int, Cell)[] CellUpdate;
        public readonly int Steps;
        public readonly bool Done;

        public MazeUpdate((int, Cell)[] cellUpdate, int steps, bool done)
        {
            CellUpdate = cellUpdate;
            Steps = steps;
            Done = done;
        }
    }

    public class SystemRandom : IRandom
    {
        private readonly System.Random random = new System.Random();
        public int Range(int exclusiveMax) => random.Next(exclusiveMax);
        public int Range(int inclusiveMin, int exclusiveMax) => random.Next(inclusiveMin, exclusiveMax);
    }
}