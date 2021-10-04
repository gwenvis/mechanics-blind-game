namespace QTea.MazeGeneration
{
    public partial class Solver
    {
        private enum NodeState
        {
            Null = 0,
            None = 1,
            Open,
            Closed
        }

        private struct Node
        {
            /// <summary>
            /// Previous node index. Null if start node.
            /// </summary>
            public int ParentIndex;

            /// <summary>
            /// The current state of the node.
            /// </summary>
            public NodeState State;

            /// <summary>
            /// Location of the node
            /// </summary>
            public readonly int Index;

            /// <summary>
            /// The walls this node has
            /// </summary>
            public readonly Direction Walls;

            /// <summary>
            /// Movement cost to from from the starting point to here
            /// </summary>
            public readonly int G;

            /// <summary>
            /// Guesstimate from given spot to the final destination
            /// </summary>
            public readonly int H;

            /// <summary>
            /// Total cost
            /// </summary>
            public int F => G + H;

            private readonly UnityEngine.Vector2Int _pos;

            public Node(int parentIndex, Direction walls, int index, int g, int h)
            {
                Index = index;
                G = g;
                H = h;
                State = NodeState.None;
                Walls = walls;
                ParentIndex = parentIndex;
                _pos = new UnityEngine.Vector2Int(Index % 8, Index / 8);
            }

            public Node ChangeParent(int parent, int g, int h)
            {
                return new Node(parent, Walls, Index, g, h)
                {
                    State = State
                };
            }

            public override bool Equals(object obj)
            {
                if (obj is Node node)
                    return Index == node.Index &&
                           State != NodeState.Null && node.State != NodeState.Null;

                return false;
            }
        }
    }
}