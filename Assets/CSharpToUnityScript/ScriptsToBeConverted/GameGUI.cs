using UnityEngine;
using System.Collections;


public class GameGUI : MonoBehaviour {

    public static GameGUI SP;
    public static int score;

    private int bestScore = 0;

    void Awake()
    {
        SP = this;
        score = 0;
        bestScore = PlayerPrefs.GetInt("BestScorePlatforms", 0);
    }

    void OnGUI()
    {
        GUILayout.Space(3);
        GUILayout.Label(" Score: " + score);
        GUILayout.Label(" Highscore: " + bestScore);

        if (GameControl.gameState == GameState.gameover)
        {
            GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUILayout.Label("Game over!");
            if (score > bestScore)
            {
                GUI.color = Color.red;
                GUILayout.Label("New highscore!");
                GUI.color = Color.white;
            }
            if (GUILayout.Button("Try again"))
            {
                Application.LoadLevel(Application.loadedLevel);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

        }
    }

    public void CheckHighscore()
    {
        if (score > bestScore)
        {
            PlayerPrefs.SetInt("BestScorePlatforms", score);
        }
    }
}
