using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class MazeItemPlacer : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _trapDoor;
    [SerializeField] private CollisionDispatcher _key;
    [SerializeField] private float _minDistance;
    [SerializeField] private float _keyMinX = 5;

    [SerializeField, Tooltip("The chance the trapdoor gets placed before the key has been retrieved")]
    private float _trapPlaceChance = 0.5f;

    private bool _trapDoorPlaced;
    private MazeController.GeneratedMaze _maze;

    private void Awake()
    {
        _trapDoor.gameObject.SetActive(false);
    }

    public void MazeGenerated(MazeController.GeneratedMaze maze)
    {
        _maze = maze;
        if(Random.Range(0f, 1f) < _trapPlaceChance) PlaceTrapDoor(maze);
        Vector3 keyPosition = GetRandomPosition(maze.Columns, maze.Rows);
        keyPosition.x = Mathf.Clamp(keyPosition.x, _keyMinX, maze.Rows);
        _key.transform.position = keyPosition;
        
        _key.OnTrigger += OnKeyTriggerEvent;
    }

    private void PlaceTrapDoor(MazeController.GeneratedMaze maze)
    {
        const int maxTries = 50;
        
        bool validPlaceFound = false;
        int tries = 0;
        Vector3 position = Vector3.zero;
        
        while (!validPlaceFound && tries < maxTries)
        {
            position = GetRandomPosition(maze.Columns, maze.Rows);
            validPlaceFound = (position - _key.transform.position).sqrMagnitude > _minDistance * _minDistance &&
                              (position - _player.position).sqrMagnitude > _minDistance * _minDistance;
            tries++;
        }
        
        if(tries >= maxTries) Debug.LogError("Could not ");

        _trapDoor.position = position;
        _trapDoorPlaced = true;
        _trapDoor.gameObject.SetActive(true);
    }

    private void OnKeyTriggerEvent(CollisionDispatcher.State arg1, Collider2D arg2)
    {
        if (arg1 == CollisionDispatcher.State.Enter && !_trapDoorPlaced && arg2.CompareTag("Player"))
        {
            PlaceTrapDoor(_maze);
        }
    }

    public void OnDestroy()
    {
        _key.OnTrigger -= OnKeyTriggerEvent;
    }

    public Vector2 GetRandomPosition(int col, int rows)
    {
        return new Vector2(Random.Range(0, rows) , Random.Range(0, col) );
    }
}
