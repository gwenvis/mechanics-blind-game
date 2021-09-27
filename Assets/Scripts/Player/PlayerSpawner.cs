using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private float zOffset;
    
    /// <summary>
    /// Places the position on the entrance of the maze.
    /// </summary>
    public void ImportMaze(MazeController.GeneratedMaze generatedMaze)
    {
        transform.position = new Vector3(0, generatedMaze.Entrance, zOffset);
    }
}
