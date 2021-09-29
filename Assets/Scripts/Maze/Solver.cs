using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QTea.MazeGeneration
{
    public static partial class Solver 
    {
        public static Path<Vector2Int> Solve(Maze maze, Vector2Int start, Vector2Int end, int limit)
        {
            Node[] world = new Node[maze.Columns * maze.Rows];
            var path = new SimplePriorityQueue<Node>(new NodeComparer());

            int rows = maze.Rows;
            int currentIndex = GetIndex(start, rows);
            var startingNode = new Node(null, true, currentIndex, 0, GuessCost(start, end));
            
            path.Push(startingNode);
            world[currentIndex] = startingNode;

            int walkedAmount = 0;

            while (path.Count > 0)
            {
                if (walkedAmount > limit)
                {
                    return new Path<Vector2Int>(new[] { start }, 1);
                }
                
                Node currentNode = path.Pop();
                currentNode.State = NodeState.Closed;

                if (GetPosition(currentNode.Index, rows) == end)
                {
                    return BuildPath(currentNode, rows);
                }
                
                // get all neighbors
                Node[] walkableNeighbors = GetWalkableAdjacentNodes(maze, world, currentNode, end);
                if (walkableNeighbors == null) continue;
                Node lowestGScore;

                foreach (Node candidate in walkableNeighbors)
                {
                    switch (candidate.State)
                    {
                        case NodeState.Closed:
                            continue;
                        case NodeState.None:
                            world[candidate.Index].State = NodeState.Open;
                            path.Push(candidate);
                            break;
                        default:
                            continue;
                    }
                }

                walkedAmount++;
            }
            
            return new Path<Vector2Int>(new[] { start }, 1);
        }

        private static Path<Vector2Int> BuildPath(Node startNode, int rows)
        {
            var path = new Stack<Vector2Int>();
            Node currentNode = startNode;
            path.Push(GetPosition(currentNode.Index, rows));
            
            do
            {
                currentNode = currentNode.Parent.Value;
                path.Push(GetPosition(currentNode.Index, rows));
                
            } while (currentNode.Parent.HasValue);

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
            Node[] nodes = GetAdjacentNodes(maze, world, current, end);
            return nodes.Where(x => x.Walkable).ToArray();
        }
        
        private static Node[] GetAdjacentNodes(Maze maze, Node[] world, Node current, Vector2Int end)
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
                
                Node newNode = new Node(current, !maze.MazeCells[current.Index].Walls.HasFlag(direction), index,
                    current.G + 1, GuessCost(offset, end));
                world[index] = newNode;
                return newNode;
            }

            void TryAdd(Node? node, List<Node> list)
            {
                if (node.HasValue) list.Add(node.Value);
            }

            var nodes = new List<Node>();
            TryAdd(GetNeighborNode(Direction.North), nodes);
            TryAdd(GetNeighborNode(Direction.East), nodes);
            TryAdd(GetNeighborNode(Direction.South), nodes);
            TryAdd(GetNeighborNode(Direction.West), nodes);
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