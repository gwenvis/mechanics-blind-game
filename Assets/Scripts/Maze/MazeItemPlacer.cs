using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeItemPlacer : MonoBehaviour
{
    [SerializeField] private Transform _trapDoor;
    [SerializeField] private Transform _key;
    
    public void MazeGenerated(MazeController.GeneratedMaze maze)
    {
        _trapDoor.position = GetRandomPosition(maze.Columns, maze.Rows);
        _key.position = GetRandomPosition(maze.Columns, maze.Rows);
    }

    public Vector2 GetRandomPosition(int col, int rows)
    {
        return new Vector2(Random.Range(0, rows) , Random.Range(0, col) );
    }
}
