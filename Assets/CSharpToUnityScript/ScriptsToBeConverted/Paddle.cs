using UnityEngine;
using System.Collections;

public class Paddle : MonoBehaviour {

    public float moveSpeed = 15;
		
	void Update () {
        float moveInput = Input.GetAxis("Horizontal") * Time.deltaTime * moveSpeed;
        transform.position += new Vector3(moveInput, 0, 0);

        float max = 14.0f;
        if (transform.position.x <= -max || transform.position.x >= max)
        {
            float xPos = Mathf.Clamp(transform.position.x, -max, max); //Clamp between min -5 and max 5
            transform.position = new Vector3(xPos, transform.position.y, transform.position.z);
        }
	}

    void OnCollisionExit(Collision collisionInfo ) {
        //Add X velocity..otherwise the ball would only go up&down
        Rigidbody rigid = collisionInfo.rigidbody;
        float xDistance = rigid.position.x - transform.position.x;
        rigid.velocity = new Vector3(rigid.velocity.x + xDistance/2, rigid.velocity.y, rigid.velocity.z);
    }
}
