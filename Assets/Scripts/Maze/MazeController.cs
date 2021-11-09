using System;
using System.Collections;
using System.Collections.Generic;
using QTea;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class MazeController : MonoBehaviour
{
    public readonly struct GeneratedMaze
    {
        public readonly Maze Maze;
        public readonly int Entrance;
        public readonly int Exit;
        public readonly int Columns;
        public readonly int Rows;

        public GeneratedMaze(Maze maze, int entrance, int exit, int columns, int rows)
        {
            Maze = maze;
            Entrance = entrance;
            Exit = exit;
            Columns = columns;
            Rows = rows;
        }
    }

    [SerializeField] private UnityEvent<GeneratedMaze> mazeCompletedAction;

    [SerializeField] private int rows;
    [SerializeField] private int columns;
    [SerializeField] private int removeWalls = 0;
    [SerializeField] private bool removeDeadEnds = false;

    private void Start()
    {
        var random = new MazeCreator.UnityRandom();
        int entrance = random.Range(columns - 1);
        int exit = random.Range(columns - 1);
        var mazeGenerator =
            new MazeGenerator(columns, rows,
                entrance, exit,
                random, 
                removeWalls: removeWalls, removeDeadEnds: removeDeadEnds);

        Maze maze = mazeGenerator.Generate(0);

        var generatedMaze = new GeneratedMaze(maze, entrance, exit, columns, rows);
        mazeCompletedAction?.Invoke(generatedMaze);
    }
}