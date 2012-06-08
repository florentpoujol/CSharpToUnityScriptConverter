using UnityEngine;
using System.Collections;

public class GameoverTrigger : MonoBehaviour {

    void OnTriggerEnter()
    {
        MarbleGameManager.SP.SetGameOver();
    }
}
