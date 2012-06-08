using UnityEngine;
using System.Collections;

public class Ball : MonoBehaviour {

    public float maxVelocity = 20;
    public float minVelocity = 15;

	void Awake () {
        rigidbody.velocity = new Vector3(0, 0, -18);
	}

	void Update () {
        //Make sure we stay between the MAX and MIN speed.
        float totalVelocity = Vector3.Magnitude(rigidbody.velocity);
        if(totalVelocity>maxVelocity){
            float tooHard = totalVelocity / maxVelocity;
            rigidbody.velocity /= tooHard;
        }
        else if (totalVelocity < minVelocity)
        {
            float tooSlowRate = totalVelocity / minVelocity;
            rigidbody.velocity /= tooSlowRate;
        }

        //Is the ball below -3? Then we're game over.
        if(transform.position.z <= -3){            
            BreakoutGame.SP.LostBall();
            Destroy(gameObject);
        }
	}
}
