using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace uSimFramework.SiegeEngines {

	public class MunitionHandler : MonoBehaviour {

		public Transform rootTranform;
		public Vector3 offset;
		public Transform currentPickup;


		// Use this for initialization
		void Start () {
			
		}

		void Update () {

			if (Input.GetKey (KeyCode.E) && currentPickup != null)
				Release ();

		}
		
		public void Pickup (Transform obj) {
			currentPickup = obj;
			currentPickup.GetComponent<Rigidbody> ().isKinematic = true;
			currentPickup.transform.parent = rootTranform;
			currentPickup.localPosition = offset;
		}

		public void Release () {

			currentPickup.transform.parent = null;
			currentPickup.GetComponent<Rigidbody> ().isKinematic = false;


		}
	}
}
