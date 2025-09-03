using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace uSimFramework.SiegeEngines {

	public class TorsionAxis : MonoBehaviour {

		public Rigidbody torsionAxis;

		// Use this for initialization
		void Start () {
			torsionAxis.maxAngularVelocity = 100f;
		}
		
		// Update is called once per frame
		void Update () {
			Vector3 eulers = torsionAxis.transform.localEulerAngles;
			eulers.y = eulers.z = 0f;
			torsionAxis.transform.localEulerAngles = eulers;

		}
	}
}
