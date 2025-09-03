using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace uSimFramework.SiegeEngines {
	
	public class SimplePlayerMovement : MonoBehaviour {

		public Rigidbody playerRigidbody;
		public Transform cameraTransform;

		public float moverForce;
		public float rotSpeed;
		public bool canMove;
		public float inputV;
		public float inputH;
		public float rotV;
		public float rotH;

		// Use this for initialization
		void Start () {
			
		}
		
		// Update is called once per frame
		void Update () {

			inputH = Input.GetAxis ("Horizontal");
			inputV = Input.GetAxis ("Vertical");
			rotH = Input.GetAxis ("Mouse X");
			rotV = Input.GetAxis ("Mouse Y");
		}

		void FixedUpdate (){

			Vector3 rotation = new Vector3 (rotSpeed * -rotV, 0f, 0f);
			cameraTransform.Rotate (rotation);

			if (!canMove)
				return;
			
			Vector3 forces = new Vector3 (moverForce * inputH, 0f, moverForce * inputV);
			playerRigidbody.AddRelativeForce (forces);
			playerRigidbody.AddRelativeTorque (Vector3.up * rotSpeed * rotH);




		}
	}

}
