using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QTea.MazeGeneration
{
    public static partial class Solver
    {
        public static Path<Vector2Int> Solve(this Maze maze, Vector2Int start, Vector2Int end, int limit = 10_000)
        {
            Node[] world = new Node[maze.Columns * maze.Rows];
            var path = new SimplePriorityQueue<Node>(new NodeComparer());

            int rows = maze.Rows;
            int currentIndex = GetIndex(start, rows);
            var startingNode = new Node(-1, maze.MazeCells[currentIndex].Walls, currentIndex, 0, GuessCost(start, end));

            path.Push(startingNode);
            world[currentIndex] = startingNode;

            int walkedAmount = 0;

            while (path.Count > 0)
            {
                if (walkedAmount > limit)
                {
                    Debug.Log("walkedAmount over limit");
                    return new Path<Vector2Int>(new[] { start });
                }

                Node currentNode = world[path.Pop().Index];
                world[currentNode.Index].State = NodeState.Closed;

                if (GetPosition(currentNode.Index, rows) == end)
                {
                    return BuildPath(currentNode, world, rows);
                }

                var pos = GetPosition(currentNode.Index, rows);
                // get all neighbors
                Node[] walkableNeighbors = GetWalkableAdjacentNodes(maze, world, currentNode, end);
                if (walkableNeighbors == null) continue;
                Node lowestGScore;

                foreach (Node candidate in walkableNeighbors)
                {
                    if (candidate.State != NodeState.None)
                    {
                        if (candidate.State == NodeState.Open)
                        {
                            if (candidate.ParentIndex != currentNode.Index)
                            {
                                world[candidate.Index] = world[candidate.Index].ChangeParent(currentNode.Index,
                                    currentNode.G + 1, GuessCost(pos, GetPosition(candidate.Index, rows)));
                            }
                        }

                        continue;
                    }

                    world[candidate.Index].State = NodeState.Open;

                    path.Push(candidate);
                }

                walkedAmount++;
            }

            Debug.Log("Couldn't find a path");
            return new Path<Vector2Int>(new[] { start }, 1);
        }

        private static Path<Vector2Int> BuildPath(Node startNode, Node[] world, int rows)
        {
            var path = new Stack<Vector2Int>();
            Node currentNode = startNode;
            path.Push(GetPosition(currentNode.Index, rows));

            do
            {
                currentNode = world[currentNode.ParentIndex];
                path.Push(GetPosition(currentNode.Index, rows));
            } while (currentNode.ParentIndex != -1);

            return new Path<Vector2Int>(path, path.Count);
        }

        #region Helper Functions

        private static Vector2Int OffsetPosition(Vector2Int input, Direction direction) => direction switch
        {
            Direction.North => new Vector2Int(input.x, input.y + 1),
            Direction.East => new Vector2Int(input.x + 1, input.y),
            Direction.South => new Vector2Int(input.x, input.y - 1),
            Direction.West => new Vector2Int(input.x - 1, input.y),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };

        private static bool IsInsideBounds(Vector2Int input, int rows, int columns) =>
            (input.x >= 0 && input.x < rows) && (input.y >= 0 && input.y < columns);

        private static Node[] GetWalkableAdjacentNodes(Maze maze, Node[] world, Node current, Vector2Int end)
        {
            int rows = maze.Rows;
            Vector2Int position = GetPosition(current.Index, rows);

            Node? GetNeighborNode(Direction direction)
            {
                Vector2Int offset = OffsetPosition(position, direction);
                if (!IsInsideBounds(offset, rows, maze.Columns)) return null;
                int index = GetIndex(offset, rows);
                if (world[index].State != NodeState.Null)
                {
                    return world[index];
                }

                Node newNode = new Node(current.Index, maze.MazeCells[index].Walls, index,
                    current.G + 1, GuessCost(offset, end));
                return newNode;
            }

            void TryAdd(Direction direction, List<Node> list)
            {
                Node? node = GetNeighborNode(direction);
                bool walkable = !current.Walls.HasFlag(direction);
                if (node.HasValue && walkable)
                {
                    list.Add((node.Value));
                    world[node.Value.Index] = node.Value;
                }
            }

            var nodes = new List<Node>();
            TryAdd(Direction.North, nodes);
            TryAdd(Direction.East, nodes);
            TryAdd(Direction.South, nodes);
            TryAdd(Direction.West, nodes);
            return nodes.ToArray();
        }

        private static Vector2Int GetPosition(int index, int rows) => new Vector2Int(index % rows, index / rows);
        private static int GetIndex(int x, int y, int rows) => y * rows + x;
        private static int GetIndex(Vector2Int pos, int rows) => GetIndex(pos.x, pos.y, rows);

        private static int GuessCost(Vector2Int start, Vector2Int end) =>
            Mathf.Abs(end.x - start.x) + Mathf.Abs(end.y - start.y);

        #endregion


        private class NodeComparer : IComparer<Node>
        {
            public int Compare(Node a, Node b)
            {
                return a.F > b.F ? 1 : a.F < b.F ? -1 : 0;
            }
        }
    }

    public class SimplePriorityQueue<T>
    {
        private readonly IComparer<T> comparer;
        private readonly List<T> elements;

        public SimplePriorityQueue(IComparer<T> comparer)
        {
            this.comparer = comparer;
            elements = new List<T>();
        }

        public int Count => elements.Count;

        public void Push(T element)
        {
            int count = Count;
            elements.Add(element);

            if (count == 0)
            {
                return;
            }

            int offset = 0;
            for (; offset < count; offset++)
            {
                if (comparer.Compare(element, elements[offset]) > 0)
                {
                    // insert it here
                    InsertElement(element, offset);
                    break;
                }
            }
        }

        private void InsertElement(T element, int position)
        {
            T previous = elements[position];
            for (int i = position + 1; i < Count; i++)
            {
                // move the previous element to the next
                (elements[i], previous) = (previous, elements[i]);
            }

            elements[position] = element;
        }

        public T Pop()
        {
            if (elements.Count == 0)
            {
                throw new InvalidOperationException("The queue is empty.");
            }

            T element = elements[Count - 1];
            elements.RemoveAt(Count - 1);
            return element;
        }
    }
}