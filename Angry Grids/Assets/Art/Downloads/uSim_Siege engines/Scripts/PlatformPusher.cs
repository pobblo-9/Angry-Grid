using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace uSimFramework.SiegeEngines {

	public class PlatformPusher : MonoBehaviour {

		public bool pushing;
		public SimplePlayerMovement pusher;
		public PlatformMover mover;

		// Use this for initialization
		void Start () {
			
		}
		
		// Update is called once per frame
		void Update () {

			if (pushing && pusher != null && mover != null) {
				pusher.canMove = false;
				pusher.transform.parent = mover.transform.root;
				pusher.GetComponent<Rigidbody> ().isKinematic = true;
				mover.movementForward = pusher.inputV;
				mover.movementSideways = pusher.inputH ;

			} 
			if (mover == null)
				return;


				if(!mover.handled) {

				pusher.canMove = true;
				pusher.transform.parent = null;
				pusher.GetComponent<Rigidbody> ().isKinematic = false;
				mover = null;
				pushing = false;
			}
		}
	}

}
