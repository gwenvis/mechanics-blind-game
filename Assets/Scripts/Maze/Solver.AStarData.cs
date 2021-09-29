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
            /// Previous node. Null if start node.
            /// </summary>
            public Node? Parent;
            
            /// <summary>
            /// The current state of the node.
            /// </summary>
            public NodeState State;
            
            /// <summary>
            /// Location of the node
            /// </summary>
            public readonly int Index;

            /// <summary>
            /// Is this node walkable?
            /// </summary>
            public readonly bool Walkable;
            
            /// <summary>
            /// Movement cost to from from the starting point to here
            /// </summary>
            public readonly float G;
            
            /// <summary>
            /// Guesstimate from given spot to the final destination
            /// </summary>
            public readonly float H;

            /// <summary>
            /// Total cost
            /// </summary>
            public readonly float F;

            public Node(Node? parent, bool walkable, int index, float g, float h)
            {
                Index = index;
                G = g;
                H = h;
                F = G + H;
                State = NodeState.None;
                Walkable = walkable;
                Parent = parent;
            }

            public override bool Equals(object obj)
            {
                if (obj is Node node)
                {
                    return Index == node.Index && 
                           State != NodeState.Null && node.State != NodeState.Null;
                }

                return false;
            }
        }
    }
}