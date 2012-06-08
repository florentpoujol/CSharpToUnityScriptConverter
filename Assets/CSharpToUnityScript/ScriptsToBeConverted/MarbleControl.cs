using UnityEngine;
using System.Collections;

public class MarbleControl : MonoBehaviour {

    public float movementSpeed = 6.0f;
	
	void Update () {
        Vector3 movement = (Input.GetAxis("Horizontal") * -Vector3.left * movementSpeed) + (Input.GetAxis("Vertical") * Vector3.forward *movementSpeed);
        rigidbody.AddForce(movement, ForceMode.Force);
	}

    void OnTriggerEnter  (Collider other  ) {
        if (other.tag == "Pickup")
        {
            MarbleGameManager.SP.FoundGem();
            Destroy(other.gameObject);
        }
        else
        {
            //Other collider.. See other.tag and other.name
        }        
    }

}
