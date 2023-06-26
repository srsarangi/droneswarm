using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class creditsScreenScript : MonoBehaviour
{

    // button is put on credit screne whach enables reverse navigation back to welcome screen
    public void BackToWelcomeScreen()
   {
   	SceneManager.LoadScene("WelcomeScreen");

   }
}
