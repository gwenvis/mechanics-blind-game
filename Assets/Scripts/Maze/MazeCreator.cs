using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using QTea;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace QTea
{
    public class MazeCreator : MonoBehaviour
    {
#region Structs
        private enum DrawMode
        {
           Instance,
           GPUInstance
        }

        private struct GPUBatch
        {
            internal MaterialPropertyBlock PropertyBlock;
            internal Matrix4x4[] TransformMatrices;
            internal int Count;
        }

        [Serializable]
        private struct InitializeDrawModeSettings
        {
            [SerializeField] internal GameObject WallObject;
            [SerializeField] internal GameObject BackgroundObject;
        }

        [Serializable]
        private struct GPUDrawModeSettings
        {
            [SerializeField] internal Mesh Mesh;
            [SerializeField] internal Material Material;
        }
        
        private struct CellView
        {
            internal readonly IReadOnlyList<GameObject> walls;
            internal readonly GameObject background;
            internal bool Visited;
            internal int GPUWalls; 

            private SpriteRenderer spriteRenderer;

            internal void SetImage(Color color)
            {
                if (!spriteRenderer) spriteRenderer = background.GetComponentInChildren<SpriteRenderer>();
                if(spriteRenderer) spriteRenderer.color = color;
            }

            public CellView(IReadOnlyList<GameObject> walls, GameObject background)
            {
                this.walls = walls;
                this.background = background;
                Visited = false;
                spriteRenderer = null;
                GPUWalls = 0b1111;
            }
        }
#endregion
        [SerializeField] private new Camera camera;
        [SerializeField] private bool followCurrentCell;
        [SerializeField, ShowIf("followCurrentCell")] private float zoomLevel;
        [SerializeField] private DrawMode drawMode = DrawMode.GPUInstance;

        [SerializeField, TitleGroup("GPU Instance Drawing Settings")]
        [ShowIf("@drawMode == DrawMode.GPUInstance"), HideLabel]
        private GPUDrawModeSettings gpuDrawSettings;

        [SerializeField, TitleGroup("Instance Drawing Settings")]
        [ShowIf("@drawMode == DrawMode.Instance"), HideLabel]
        private InitializeDrawModeSettings instanceDrawSettings;

        [Title("Maze Settings")] [SerializeField]
        private bool dontSelfGenerate = true;
        [SerializeField] private float emptySpace = 1.0f;
        [SerializeField] private float wallThickness = 0.1f;
        [HideIf("dontSelfGenerate")]
        [SerializeField, HorizontalGroup] private int mazeColumns = 7, mazeRows = 7;
        [HideIf("dontSelfGenerate")]
        [SerializeField, MinValue(0)] private int removeWallAmount = 0;
        [HideIf("dontSelfGenerate")]
        [SerializeField] private bool slowGenerate;
        [HideIf("dontSelfGenerate")]
        [SerializeField, ShowIf("slowGenerate"), MinValue(0.0)]
        private float generateDelay = 0.2f;
        [HideIf("dontSelfGenerate")]
        [SerializeField, ShowIf("slowGenerate"), MinValue(1)]
        private int stepsPerUpdate = 1;

        [Title("Colours")]
        [SerializeField]
        private Color defaultGroundColor = Color.red;
        [SerializeField]
        private Color currentGroundColor = Color.cyan;
        [SerializeField]
        private Color visitedGroundColor = Color.magenta;
        [SerializeField] private Color wallColor;

        private int generateState = -1;

        [ReadOnly, SerializeField]
        private int generateStep = 0;

        private CellView[] cellViews;
        private MazeGenerator mazeGenerator;
        private Coroutine mazeGenerationCoroutine;
        private int currentCell;

        private ComputeBuffer argsBuffer;
        private ComputeBuffer meshPropertiesBuffer;
        private MeshProperties[] meshProperties;
        private static readonly int Properties = Shader.PropertyToID("_Properties");

        private int latestGroundUpdate1 = -1;
        private int latestGroundUpdate2 = -1;
        private int latestWallUpdate1 = -1;
        private int latestWallUpdate2 = -1;
        private bool done = false;

        private void Start()
        {
            if (!slowGenerate && !dontSelfGenerate)
            {
                mazeGenerator = new MazeGenerator(mazeColumns, mazeRows, 2, 4, new UnityRandom(), removeWallAmount);
                BuildMazeBase(mazeColumns, mazeRows);
                //UpdateMaze(mazeGenerator.Generate(0));
                done = true;
            }
        }

        [HideIf("dontSelfGenerate")]
        [ShowIf("@slowGenerate && generateState == -1 && UnityEngine.Application.isPlaying")]
        [Button]
        private void InitializeMaze()
        {
            mazeGenerator = new MazeGenerator(mazeColumns, mazeRows, 0, 4, new UnityRandom(), removeWallAmount);
            BuildMazeBase(mazeColumns, mazeRows);
            generateState = 0;
        }

        public void ImportMaze(MazeController.GeneratedMaze generatedMaze)
        {
            mazeRows = generatedMaze.Rows;
            mazeColumns = generatedMaze.Columns;
            BuildMazeBase(mazeColumns, mazeRows);
            UpdateMaze(generatedMaze.Maze);
        }

        private void BuildMazeBase(int columns, int rows)
        {
            cellViews = new CellView[columns * rows];

            switch (drawMode)
            {
                case DrawMode.Instance:
                    InstantiateMaze(columns, rows);
                    break;
                case DrawMode.GPUInstance:
                    InitializeGPUMaze(columns, rows);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (camera)
            {
                Vector3 cam = camera.transform.position;
                cam.x = rows * emptySpace / 2;
                cam.y = columns * emptySpace / 2 - emptySpace / 2;
                camera.orthographicSize = cam.y + emptySpace / 2;
                camera.transform.position = cam;
            }
        }

        private void Update()
        {
            if (drawMode == DrawMode.GPUInstance && generateState >= 0) DrawGPUInstanced();
            float zoomLerpSpeed = 10f;
            float positionLerpSpeed = 2.5f;

            if (followCurrentCell && generateState == 3 && camera)
            {
                Vector3 wantedPosition = new Vector3(currentCell % mazeRows, currentCell / mazeRows,
                    camera.transform.position.z);
                camera.orthographicSize = Mathf.Lerp(camera.orthographicSize, zoomLevel, zoomLerpSpeed * Time.deltaTime);
                camera.transform.position =
                    Vector3.Lerp(camera.transform.position, wantedPosition, positionLerpSpeed * Time.deltaTime);
            }
            else if (followCurrentCell && generateState != 3 && generateState != -1 && camera)
            {
                Vector3 wantedPosition = camera.transform.position;
                wantedPosition.x = mazeRows * emptySpace / 2;
                wantedPosition.y = mazeColumns * emptySpace / 2 - emptySpace / 2;
                camera.orthographicSize = Mathf.Lerp(camera.orthographicSize, wantedPosition.y + emptySpace / 2, zoomLerpSpeed * Time.deltaTime);
                camera.transform.position = 
                    Vector3.Lerp(camera.transform.position, wantedPosition, positionLerpSpeed * Time.deltaTime);

            }
        }

        private void DrawGPUInstanced()
        {
            Vector3 bounds = new Vector3(mazeRows * emptySpace, mazeRows * emptySpace, 1f);
            Graphics.DrawMeshInstancedIndirect(gpuDrawSettings.Mesh, 0, 
                gpuDrawSettings.Material, 
                new Bounds(Vector3.zero, bounds * 4), 
                argsBuffer);
        }

        private void OnDisable()
        {
            if (drawMode == DrawMode.GPUInstance && generateState > -1)
            {
                argsBuffer.Dispose();
                meshPropertiesBuffer.Dispose();
            }
        }

        private struct MeshProperties
        {
            public Matrix4x4 Mat;
            public Vector4 Color;

            public static int Size => sizeof(float) * 4 * 4 + sizeof(float) * 4;
        }

        private void InitializeGPUMaze(int columns, int rows)
        {
            uint[] args = { 0, 0, 0, 0, 0 };
            args[0] = gpuDrawSettings.Mesh.GetIndexCount(0);
            args[1] = (uint)(columns * rows + columns * rows * 4);
            args[2] = gpuDrawSettings.Mesh.GetIndexStart(0);
            args[3] = gpuDrawSettings.Mesh.GetBaseVertex(0);
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);

            int length = columns * rows + columns * rows * 4;
            meshProperties = new MeshProperties[length];
            
            // ground
            for (int j = 0; j < columns*rows; j ++)
            {
                meshProperties[j].Color = defaultGroundColor;
                meshProperties[j].Mat = new Matrix4x4();
                float pos = emptySpace * j;
                float xPos = j % rows * emptySpace;
                float yPos = j / rows * emptySpace;
                meshProperties[j].Mat.SetTRS(
                    new Vector3(xPos, yPos, 0), 
                    Quaternion.identity, 
                    new Vector3(emptySpace, emptySpace, 0));
            }
            
            // walls
            for (int i = 0; i < length - mazeColumns*mazeRows; i+=4)
            {
                int index = i + mazeColumns * mazeRows;
                for (int j = 0; j < 4; j++)
                {
                    meshProperties[index+j].Color = wallColor;
                    meshProperties[index+j].Mat = new Matrix4x4();

                    Direction dir = (Direction)(1 << j);
                    Vector3 pos = GetWallPosition(i / 4, dir);
                    meshProperties[index+j].Mat.SetTRS(pos, Quaternion.identity, GetWallScale(dir));
                }
            }

            meshPropertiesBuffer = new ComputeBuffer(length, MeshProperties.Size, ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
            meshPropertiesBuffer.SetData(meshProperties);
            gpuDrawSettings.Material.SetBuffer(Properties, meshPropertiesBuffer);
        }

        private void InstantiateMaze(int columns, int rows)
        {
            int length = columns * rows;
            for (int i = 0; i < length; i++)
            {
                int r = i % rows;
                int c = i / rows;
                Vector2 cellPosition = new Vector2(r * emptySpace, c * emptySpace);
                Direction[] directions = (Direction[])Enum.GetValues(typeof(Direction));

                GameObject[] wallObjects = new GameObject[4];

                GameObject backGround = Instantiate(instanceDrawSettings.BackgroundObject, transform);
                backGround.transform.position = new Vector3(cellPosition.x, cellPosition.y, 0.1f);
                backGround.name = $"(r{r}, c{c})";

                for (int index = 0; index < directions.Length; index++)
                {
                    Direction direction = directions[index];
                    wallObjects[index] =
                        PlaceWall(i, direction, (Direction)0b1111, backGround.transform);
                }

                cellViews[i] = new CellView(wallObjects, backGround);
            }
        }

        private void UpdateMaze(Maze maze)
        {
            for (int i = 0; i < maze.MazeCells.Length; i++)
            {
                UpdateWall(maze.MazeCells[i], i);
            }

            if (meshProperties != null)
            {
                meshPropertiesBuffer.SetData(meshProperties);
                gpuDrawSettings.Material.SetBuffer(Properties, meshPropertiesBuffer);
            }
        }

        private void UpdateWall(Cell cell, int index)
        {
            Direction[] directions = (Direction[])Enum.GetValues(typeof(Direction));
            foreach (Direction direction in directions)
            {
                UpdateWallDirection(index, direction, cell.Walls);
            }

            if (drawMode == DrawMode.Instance)
            {
                if (done) cellViews[index].SetImage(defaultGroundColor);
                
                cellViews[index].SetImage((currentCell == index && (generateState != -1 || generateState != 1)) ? currentGroundColor :
                    cellViews[index].Visited ? visitedGroundColor : defaultGroundColor);
            }
            else
            {
                meshProperties[index].Color = currentCell == index ? currentGroundColor :
                    cellViews[index].Visited ? visitedGroundColor : defaultGroundColor;
                if (latestGroundUpdate1 >= 0) latestGroundUpdate2 = index;
                else latestGroundUpdate1 = index;
            }
            
            if (currentCell == index) cellViews[index].Visited = true;
        }

        [ShowIf("@slowGenerate && generateState == 1")]
        [Button(ButtonSizes.Small)]
        private void StartAutoGenerate()
        {
            generateState = 3;
            mazeGenerationCoroutine = StartCoroutine(AutoGenerateCoroutine());
        }

        private bool NextGenerateStep()
        {
            bool done = false;
            for (int i = 0; i < stepsPerUpdate; i++)
            {
                MazeUpdate maze = mazeGenerator.NextSlowGenerate();
                currentCell = maze.CellUpdate[1].Item1;
                
                generateStep = maze.Steps;
                done = maze.Done;
                
                foreach ((int index, Cell cell) in maze.CellUpdate)
                {
                    UpdateWall(cell, index);
                }
                
                UpdateIndex(latestWallUpdate1);
                UpdateIndex(latestWallUpdate2);
                UpdateIndex(latestGroundUpdate1);
                UpdateIndex(latestGroundUpdate2);
                UpdateIndex(currentCell);
                latestWallUpdate1 = latestGroundUpdate1 = latestGroundUpdate2 = latestWallUpdate2 = -1;

                if (!maze.Done) continue;
                this.done = true;
                generateState = 2;
                break;
            }

            return done;
        }

        private void UpdateIndex(int index)
        {
            if (index == -1 || meshPropertiesBuffer == null) return;
            
            var a = meshPropertiesBuffer.BeginWrite<MeshProperties>(index, 1);
            a[0] = meshProperties[index];
            meshPropertiesBuffer.EndWrite<MeshProperties>(1);
        }

        [ShowIf("@slowGenerate && generateState == 1")]
        [Button(ButtonSizes.Small)]
        private void NextGenerateStepButton() => NextGenerateStep();

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
                done = NextGenerateStep();
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
            OnDisable();
            
            mazeGenerator = null;
            generateState = -1;
            generateStep = 0;

            for (int i = 0; i < cellViews.Length; i++)
            {
                Destroy(cellViews[i].background);
            }

            cellViews = null;
        }

        private Vector3 GetWallPosition(int index, Direction direction)
        {
            int row = index % mazeRows;
            int col = index / mazeRows;
            
            Vector2 position = direction switch
            {
                Direction.South => new Vector3(row * emptySpace, col * emptySpace - emptySpace / 2 + wallThickness / 2),
                Direction.West => new Vector3(row * emptySpace - emptySpace / 2 + wallThickness / 2, col * emptySpace),
                Direction.East => new Vector3(row * emptySpace + emptySpace / 2 - wallThickness / 2, col * emptySpace),
                Direction.North => new Vector3(row * emptySpace, col * emptySpace + emptySpace / 2 - wallThickness / 2),
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
            return position;
        }

        private Vector3 GetWallScale(Direction direction)
        {
            Vector3 scale = direction switch
            {
                Direction.North => new Vector3(emptySpace, wallThickness, 1),
                Direction.South => new Vector3(emptySpace, wallThickness, 1),
                Direction.East => new Vector3(wallThickness, emptySpace, 1),
                Direction.West => new Vector3(wallThickness, emptySpace, 1),
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
            return scale;
        }

        private GameObject PlaceWall(int index, Direction direction, Direction wallFlag, Transform parent)
        {
            Vector2 position = GetWallPosition(index, direction);
            Vector3 scale = GetWallScale(direction);

            GameObject wall = Instantiate(instanceDrawSettings.WallObject, position, Quaternion.identity);
            wall.transform.localScale = scale;
            wall.transform.SetParent(parent, true);
            wall.SetActive(wallFlag.HasFlag(direction));
            wall.name = $"{parent.name} {direction}";
            return wall;
        }

        private void UpdateWallDirection(int index, Direction direction, Direction wallFlag)
        {
            if (drawMode == DrawMode.Instance)
            {
                var cellView = cellViews[index];
                GameObject gameObject = cellView.walls[Array.IndexOf(Enum.GetValues(direction.GetType()), direction)];
                gameObject.SetActive(wallFlag.HasFlag(direction));
            }
            else
            {
                // get the wall and just make it invisible?
                int directionIndex = Array.IndexOf(Enum.GetValues(direction.GetType()), direction);
                int wallIndex = mazeRows * mazeColumns + index * 4 + directionIndex;
                if (!wallFlag.HasFlag(direction) && meshProperties[wallIndex].Color != Vector4.zero)
                {
                    meshProperties[wallIndex].Color = Vector4.zero;
                    if (latestWallUpdate1 >= 0) latestWallUpdate2 = wallIndex;
                    else latestWallUpdate1 = wallIndex;
                }
            }
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
}