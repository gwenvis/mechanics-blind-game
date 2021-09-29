using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    private enum SpawnerType
    {
        Player,
        Enemy
    }
    
    [SerializeField] private float zOffset;
    [SerializeField] private SpawnerType spawnerType = SpawnerType.Player;
    
    /// <summary>
    /// Places the position on the entrance of the maze.
    /// </summary>
    public void ImportMaze(MazeController.GeneratedMaze generatedMaze)
    {
        int col = spawnerType == SpawnerType.Player ? generatedMaze.Entrance : generatedMaze.Exit;
        int row = spawnerType == SpawnerType.Player ? 0 : generatedMaze.Rows - 1;
        transform.position = new Vector3(row, generatedMaze.Entrance, zOffset);
    }
}
