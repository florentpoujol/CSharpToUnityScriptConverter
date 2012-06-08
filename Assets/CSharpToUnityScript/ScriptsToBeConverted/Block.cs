using UnityEngine;
using System.Collections;

public class Block : MonoBehaviour {

	void OnTriggerEnter () {
        BreakoutGame.SP.HitBlock();
        Destroy(gameObject);
	}
}
