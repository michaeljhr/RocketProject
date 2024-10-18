using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuHandler : MonoBehaviour
{
    public bool isFullScreen = true;

    public void StartButton()
    {
        SceneManager.LoadScene("Simulation");
    }

    public void QuitButton()
    {
        Application.Quit();
    }

    public void ToggleFullScreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (isFullScreen)
        {
            Screen.fullScreen = true;
        }
        else
        {
            Screen.fullScreen = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
