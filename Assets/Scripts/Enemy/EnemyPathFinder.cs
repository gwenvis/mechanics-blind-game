using System;
using System.Collections;
using System.Collections.Generic;
using QTea;
using QTea.MazeGeneration;
using UnityEngine;

public class EnemyPathFinder : MonoBehaviour
{
    [SerializeField] private Transform player;
    
    private bool mazeImported = false;
    private Maze maze;
    private Path<Vector2Int> path;

    public void ImportMaze(MazeController.GeneratedMaze generatedMaze)
    {
        mazeImported = true;
        maze = generatedMaze.Maze;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) && mazeImported)
        {
            Vector2 enemyPos = transform.position;
            Vector2 playerPos = player.position;
            Vector2Int start = new Vector2Int(Mathf.FloorToInt(enemyPos.x), Mathf.FloorToInt(enemyPos.y));
            Vector2Int end = new Vector2Int(Mathf.FloorToInt(playerPos.x), Mathf.FloorToInt(playerPos.y));
            path = Solver.Solve(maze, start, end, 10000);

            if (path.Count == 1)
            {
                Debug.LogError("Generated a maze with 1 path");
            }
        }

        if (path.Count > 1)
        {
            Vector2Int previous = path.Next();
            
            while (true)
            {
                if (!path.TryNext(out Vector2Int pos))
                {
                    break;
                }

                Debug.DrawLine((Vector2)previous, (Vector2)pos, Color.magenta, Time.deltaTime, false);
                previous = pos;
            }

            path.Reset();
        }
    }
}
