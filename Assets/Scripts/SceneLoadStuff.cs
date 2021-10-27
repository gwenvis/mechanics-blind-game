using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadStuff : MonoBehaviour
{
    [SerializeField] private int _scene;
    [SerializeField] private LoadSceneParameters _params;
    private AsyncOperation _asyncOperation;

    public void LoadScene()
    {
        _asyncOperation = SceneManager.LoadSceneAsync(_scene, _params);
        _asyncOperation.allowSceneActivation = false;
    }

    public void ActivateScene()
    {
        _asyncOperation.allowSceneActivation = true;
    }
}
