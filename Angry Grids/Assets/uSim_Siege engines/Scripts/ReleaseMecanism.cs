using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace uSimFramework.SiegeEngines {

	public class ReleaseMecanism : MonoBehaviour {

		public bool locked;
		public bool releaseCommand;
		public bool released;
		public Rigidbody trowingArm;


		// Use this for initialization
		void Start () {
			
		}
		
		// Update is called once per frame
		void Update () {
			
			if (!locked && releaseCommand && !released) {
				Release ();
			}
		}

		public void Release () {

			trowingArm.isKinematic = false;
			released = true;
			releaseCommand = false;
			if (GetComponent<AudioSource> () != null)
				GetComponent<AudioSource> ().Play ();
		}
	}
}
