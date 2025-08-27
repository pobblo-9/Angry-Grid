using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace uSimFramework.SiegeEngines {

	public class GravityAxis : MonoBehaviour {

		public Rigidbody gravityAxis;

		// Use this for initialization
		void Start () {
			gravityAxis.maxAngularVelocity = 100f;
			gravityAxis.centerOfMass = transform.position ;
		}
		
		// Update is called once per frame
		void Update () {
			
		}
	}
}
