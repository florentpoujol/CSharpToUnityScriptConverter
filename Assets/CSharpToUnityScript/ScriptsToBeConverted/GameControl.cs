using UnityEngine;
using System.Collections;

public enum GameState { playing, gameover };

public class GameControl : MonoBehaviour {

    public Transform platformPrefab;
    public static GameState gameState;

    private Transform playerTrans;
    private float platformsSpawnedUpTo = 0.0f;
    private ArrayList platforms;
    private float nextPlatformCheck = 0.0f;

    
	void Awake () {
        playerTrans = GameObject.FindGameObjectWithTag("Player").transform;
        platforms = new ArrayList();

        SpawnPlatforms(25.0f);
        StartGame();
	}

    void StartGame()
    {
        Time.timeScale = 1.0f;
        gameState = GameState.playing;
    }

    void GameOver()
    {
        Time.timeScale = 0.0f; //Pause the game
        gameState = GameState.gameover;
        GameGUI.SP.CheckHighscore();
    }

	void Update () {
        //Do we need to spawn new platforms yet? (we do this every X meters we climb)
        float playerHeight = playerTrans.position.y;
        if (playerHeight > nextPlatformCheck)
        {
            PlatformMaintenaince(); //Spawn new platforms
        }

        //Update camera position if the player has climbed and if the player is too low: Set gameover.
        float currentCameraHeight = transform.position.y;
        float newHeight = Mathf.Lerp(currentCameraHeight, playerHeight, Time.deltaTime * 10);
        if (playerTrans.position.y > currentCameraHeight)
        {
            transform.position = new Vector3(transform.position.x, newHeight, transform.position.z);
        }else{
            //Player is lower..maybe below the cameras view?
            if (playerHeight < (currentCameraHeight - 10))
            {
                GameOver();
            }
        }

        //Have we reached a new score yet?
        if (playerHeight > GameGUI.score)
        {
            GameGUI.score = (int)playerHeight;
        }
	}



    void PlatformMaintenaince()
    {
        nextPlatformCheck = playerTrans.position.y + 10;

        //Delete all platforms below us (save performance)
        for(int i = platforms.Count-1;i>=0;i--)
        {
            Transform plat = (Transform)platforms[i];
            if (plat.position.y < (transform.position.y - 10))
            {
                Destroy(plat.gameObject);
                platforms.RemoveAt(i);
            }            
        }

        //Spawn new platforms, 25 units in advance
        SpawnPlatforms(nextPlatformCheck + 25);
    }


    void SpawnPlatforms(float upTo)
    {
        float spawnHeight = platformsSpawnedUpTo;
        while (spawnHeight <= upTo)
        {
            float x = Random.Range(-10.0f, 10.0f);
            Vector3 pos = new Vector3(x, spawnHeight, 12.0f);

            Transform plat = (Transform)Instantiate(platformPrefab, pos, Quaternion.identity);
            platforms.Add(plat);

            spawnHeight += Random.Range(1.6f, 3.5f);
        }
        platformsSpawnedUpTo = upTo;
    }

}