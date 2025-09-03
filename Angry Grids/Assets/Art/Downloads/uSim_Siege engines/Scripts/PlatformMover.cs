using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace uSimFramework.SiegeEngines {

	public class PlatformMover : MonoBehaviour {

		public Transform platform;
		public bool handled;
		public float movementForward;
		public float movementSideways;

		// Use this for initialization
		void Start () {
			
		}
		
		// Update is called once per frame
		void FixedUpdate () {

			if (!handled)
				return;

			if (Input.GetMouseButtonUp (0))
				handled = false;
			
			platform.Translate (Vector3.forward * movementForward * Time.deltaTime);
			platform.Rotate (Vector3.up, movementSideways * Time.deltaTime);

		}

	}

}
