using UnityEngine;
using System.Collections;

public class MovingObject{
    public DirectionEnum direction; //-1 or 1;
    public Transform transform;

    public MovingObject(DirectionEnum dir, Transform trans)
    {
        direction = dir;
        transform = trans;
    }
    
}

[System.Serializable]
public enum DirectionEnum{left = -1, right= 1}

public class ObjectSpawner : MonoBehaviour {

    public Transform objectPrefab;
    public DirectionEnum spawnDirection = DirectionEnum.right;
    public static ObjectSpawner SP;

    private float farLeft;
    private float farRight;    
    private float lastSpawnTime;
    private float spawnInterval;   


	void Awake () {
        SP = this;
        
        spawnInterval = Random.Range(3.5f, 5.5f);
        lastSpawnTime = Time.time + Random.Range(0.0f, 1.5f);
	}

    
	void Update () {
        //Spawn new object..
        if ((lastSpawnTime + spawnInterval) < Time.time)
        {
            SpawnObject();
        }      
	}

    void SpawnObject()
    {        
        lastSpawnTime = Time.time;
        spawnInterval *= 0.99f;//Speed up spawning

        DirectionEnum direction = spawnDirection; //-1 or 1

        Transform newObj = (Transform)Instantiate(objectPrefab, transform.position, transform.rotation);
        MovingObject movObj = new MovingObject(direction, newObj);        
        GameManager.SP.AddTarget( movObj );
    }

 
}
