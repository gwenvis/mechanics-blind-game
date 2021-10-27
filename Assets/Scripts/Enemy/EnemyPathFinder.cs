using System;
using System.Collections;
using System.Collections.Generic;
using QTea;
using QTea.MazeGeneration;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class EnemyPathFinder : MonoBehaviour
{
    public UnityEvent<Path<Vector2Int>> _pathGeneratedEvent;

    [SerializeField] private Transform _player;
    [SerializeField] private WarnUI _warnUI;

    private bool _mazeImported = false;
    private Maze _maze;
    private Path<Vector2Int> _path;
    private Vector2Int _start;
    private Vector2Int _end;

    private bool _newPath = false;

    public void ImportMaze(MazeController.GeneratedMaze generatedMaze)
    {
        _mazeImported = true;
        _maze = generatedMaze.Maze;
    }

    private void Update()
    {
        if (_path.Count > 1)
        {
            Vector2Int previous = _path.Next();

            while (true)
            {
                if (!_path.TryNext(out Vector2Int pos)) break;

                if (_newPath)
                {
                    if (GetFromVector(_player.transform.position) == pos)
                    {
                        _warnUI.StartWarn();
                        _newPath = false;
                    }
                }
                
                Debug.DrawLine((Vector2)previous, (Vector2)pos, Color.magenta, Time.deltaTime, false);
                previous = pos;
            }

            _newPath = false;

            _path.Reset();
        }
    }

    public Path<Vector2Int> GetRandomPath()
    {
        // set a random path
        Vector2Int start = GetFromVector(transform.position);
        var randomEnd = new Vector2Int(Random.Range(0, _maze.Rows), Random.Range(0, _maze.Columns));
        var newPath = _maze.Solve(start, randomEnd);
        _path = newPath.Clone();
        _newPath = true;
        this._start = start;
        _end = randomEnd;
        return newPath;
    }

    public Path<Vector2Int> GetPathTo(Vector2Int end)
    {
        Vector2Int start = GetFromVector(transform.position);
        var newPath = _maze.Solve(start, end);
        _path = newPath.Clone();
        _newPath = true;
        _start = start;
        _end = end;
        return newPath;
    }

    public Path<Vector2Int> GetPathTo(Vector2 end)
    {
        return GetPathTo(GetFromVector(end));
    }

    private Vector2Int GetFromVector(Vector2 vector)
    {
        return new Vector2Int(Mathf.FloorToInt(vector.x + 0.5f), Mathf.FloorToInt(vector.y + 0.5f));
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere((Vector2)_start, 0.5f);
        Gizmos.DrawWireSphere((Vector2)_end, 0.5f);
    }
}