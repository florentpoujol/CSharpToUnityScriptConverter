using UnityEngine;
using System.Collections;

public class Playermovement : MonoBehaviour
{
    public float movementSpeed = 5.0f;
    private bool isGrounded = false;


    void Update() {
        rigidbody.velocity = new Vector3(0, rigidbody.velocity.y, 0); //Set X and Z velocity to 0
 
        transform.Translate(Input.GetAxis("Horizontal") * Time.deltaTime * movementSpeed, 0, 0);

        /*if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump(); //Manual jumping
        }*/
	}

    void Jump()
    {
        if (!isGrounded) { return; }
        isGrounded = false;
        rigidbody.velocity = new Vector3(0, 0, 0);
        rigidbody.AddForce(new Vector3(0, 700, 0), ForceMode.Force);        
    }

    void FixedUpdate()
    {
        isGrounded = Physics.Raycast(transform.position, -Vector3.up, 1.0f);
        if (isGrounded)
        {
            Jump(); //Automatic jumping
        }
    }
       

}
