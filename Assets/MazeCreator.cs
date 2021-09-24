using System;
using System.Collections;
using System.Collections.Generic;
using QTea;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class MazeCreator : MonoBehaviour
{
    private readonly struct CellView
    {
        internal readonly IReadOnlyList<GameObject> walls;
        internal readonly GameObject background;

        public CellView(IReadOnlyList<GameObject> walls, GameObject background)
        {
            this.walls = walls;
            this.background = background;
        }
    }

    [SerializeField] private new Camera camera;
    [SerializeField] private GameObject wallObject;
    [SerializeField] private GameObject backgroundObject;
    [SerializeField] private float emptySpace = 1.0f;
    [SerializeField] private float wallThickness = 0.1f;
    [SerializeField, HorizontalGroup] private int mazeColumns = 7, mazeRows = 7;
    [SerializeField] private bool slowGenerate;
    [SerializeField, ShowIf("slowGenerate")]
    private float generateDelay = 0.2f;

    private int generateState = -1;
    
    [ReadOnly, SerializeField]
    private int generateStep = 0;
       
    private CellView[] cellViews;
    private MazeGenerator mazeGenerator;
    private Coroutine mazeGenerationCoroutine;
    private int currentCell;

    private void Start()
    {
        if (!slowGenerate)
        {
            mazeGenerator = new(mazeColumns, mazeRows, 2, 4, new UnityRandom());
            BuildMazeBase(mazeColumns, mazeRows);
            UpdateMaze(mazeGenerator.Generate());
        }
    }

    [ShowIf("@slowGenerate && generateState == -1 && UnityEngine.Application.isPlaying")]
    [Button]
    private void InitializeMaze()
    {
        mazeGenerator = new(mazeColumns, mazeRows, 2, 4, new UnityRandom());
        BuildMazeBase(mazeColumns, mazeRows);
        generateState = 0;
    }

    private void BuildMazeBase(int columns, int rows)
    {
        cellViews = new CellView[columns * rows];
        
        for (int col = 0; col < columns; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                Vector2 cellPosition = new Vector2(row * emptySpace, col * emptySpace);

                Direction[] directions = (Direction[]) Enum.GetValues(typeof(Direction));

                GameObject[] wallObjects = new GameObject[4];

                GameObject backGround = Instantiate(backgroundObject, transform); 
                backGround.transform.position = new Vector3(cellPosition.x, cellPosition.y, 0.1f);
                backGround.name = $"(r{row}, c{col})";
                
                for (int index = 0; index < directions.Length; index++)
                {
                    Direction direction = directions[index];
                    wallObjects[index] = PlaceWall(cellPosition, direction, (Direction) 0b1111, backGround.transform);
                }

                CellView cellView = new(wallObjects, backGround);
                cellViews[row * columns + col] = cellView;
            }
        }

        if (camera)
        {
            Vector3 cam = camera.transform.position;
            cam.x = rows * emptySpace / 2;
            cam.y = columns * emptySpace / 2;
            camera.orthographicSize = cam.y;
            camera.transform.position = cam;
        }
    }

    private void UpdateMaze(Maze maze)
    {
        for (int col = 0; col < maze.Columns; col++)
        {
            for (int row = 0; row < maze.Rows; row++)
            {
                int index = maze.Columns * row + col;
                Direction[] directions = (Direction[]) Enum.GetValues(typeof(Direction));
                foreach (Direction direction in directions)
                {
                    UpdateWall(cellViews[index], direction, maze.MazeCells[index].Walls);
                }

                cellViews[index].background.GetComponentInChildren<SpriteRenderer>().color =
                    currentCell == index ? Color.cyan : Color.red;
            }
        }
    }

    [ShowIf("@slowGenerate && generateState == 1")]
    [Button(ButtonSizes.Small)]
    private void StartAutoGenerate()
    {
        generateState = 3;
        mazeGenerationCoroutine = StartCoroutine(AutoGenerateCoroutine());
    }
    
    [ShowIf("@slowGenerate && generateState == 1")]
    [Button(ButtonSizes.Small)]
    private void NextGenerateStep()
    {
        Maze maze = mazeGenerator.NextSlowGenerate(out currentCell);
        generateStep = maze.Steps;
        if (maze.Done) generateState = 2;
        UpdateMaze(maze);
    }

    [ShowIf("@generateState == 3")]
    [Button(ButtonSizes.Small)]
    private void StopGeneratingMaze()
    {
        StopCoroutine(mazeGenerationCoroutine);
        generateState = 1;
    }

    private IEnumerator AutoGenerateCoroutine()
    {
        bool done = false;
        while (!done)
        {
            if (generateDelay > 0) yield return new WaitForSeconds(generateDelay);
            else yield return null;
            Maze maze = mazeGenerator.NextSlowGenerate(out currentCell);
            generateStep = maze.Steps;
            UpdateMaze(maze);
            done = maze.Done;
        }

        generateState = 2;
    }

#if UNITY_EDITOR
    [ShowIf("@slowGenerate && generateState == 0 && UnityEngine.Application.isPlaying")]
#else
    [ShowIf("@slowGenerate && generateState == 0")]
#endif
    [Button(ButtonSizes.Small)]
    private void StartGenerate()
    {
        mazeGenerator.StartSlowGenerate();
        generateState = 1;
    }

    [ShowIf("@slowGenerate && generateState == 2")]
    [Button]
    private void Reset()
    {
        mazeGenerator = null;
        generateState = -1;
        generateStep = 0;

        for (int i = 0; i < cellViews.Length; i++)
        {
            Destroy(cellViews[i].background);
        }

        cellViews = null;
    }

    private GameObject PlaceWall(Vector2 cellPosition, Direction direction, Direction wallFlag, Transform parent)
    {
        Vector2 position = direction switch
        {
            Direction.South or Direction.West => new Vector2(cellPosition.x, cellPosition.y),
            Direction.East => new Vector2(cellPosition.x + emptySpace - wallThickness, cellPosition.y),
            Direction.North => new Vector2(cellPosition.x, cellPosition.y + emptySpace - wallThickness),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };

        Vector2 scale = direction switch
        {
            Direction.North or Direction.South => new Vector2(emptySpace, wallThickness),
            Direction.East or Direction.West => new Vector2(wallThickness, emptySpace),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };

        GameObject wall = Instantiate(wallObject, position, Quaternion.identity);
        wall.transform.localScale = scale;
        wall.transform.SetParent(parent, true);
        wall.SetActive(wallFlag.HasFlag(direction));
        wall.name = $"{parent.name} {direction}";
        return wall;
    }

    private void UpdateWall(CellView cellView, Direction direction, Direction wallFlag)
    {
        GameObject gameObject = cellView.walls[Array.IndexOf(Enum.GetValues(direction.GetType()), direction)];
        gameObject.SetActive(wallFlag.HasFlag(direction));
    }
    
    public class UnityRandom : IRandom
    {
        public int Range(int exclusiveMax)
        {
            return UnityEngine.Random.Range(0, exclusiveMax);
        }

        public int Range(int inclusiveMin, int exclusiveMax)
        {
            return UnityEngine.Random.Range(inclusiveMin, exclusiveMax);
        }
    }
}
