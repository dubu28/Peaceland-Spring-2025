using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Yarn.Unity;

public class F26_TownSquareManager : GenericMemManager
{

    //Initializes variables and plays the starting dialogue
    void Start()
    {
        F26_GameManager.Instance.CurrentScene = "Day1TownSquare";
        NextMinigame();
        NextOrder();
    }

    void Update()
    {
    }
}
