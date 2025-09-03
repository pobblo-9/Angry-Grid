using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace uSimFramework.SiegeEngines {

	public class FixedWheelAxis : MonoBehaviour {

		public Transform mobil;
		public Transform axis;
		public Transform wheelLeft;
		public Transform wheelRight;
		public float wheelsRadius;
		public Transform contactL;
		RaycastHit hitL;
		public Transform contactR;
		RaycastHit hitR;

		//transform reads
		public float transformSpeed;
		private Vector3 lastPosition;

		// Use this for initialization
		void Start () {
			
		}
		
		// Update is called once per frame
		void FixedUpdate () {

			transformSpeed = GetTransformSpeed ();
			lastPosition = transform.position;

			float rpm = transformSpeed / ((2 * 3.14f) * (wheelsRadius)) * 60f;		
			wheelLeft.Rotate ((rpm * wheelsRadius * 2f) * 60 / 360, 0f, 0f);	
			wheelRight.Rotate ((rpm * wheelsRadius * 2f) * 60 / 360, 0f, 0f);
			Ray rayA = new Ray (wheelLeft.position, transform.TransformDirection (Vector3.down));
			if (Physics.Raycast (rayA, out hitL)) {
				contactL.position = hitL.point;
			}

			Ray rayB = new Ray (wheelRight.position, transform.TransformDirection (Vector3.down));
			if (Physics.Raycast (rayB, out hitR)) {
				contactR.position = hitR.point;
			}

			Vector3 centerPos = (contactL.position + contactR.position) / 2;
			Vector3 pos = centerPos;
			pos += axis.up * wheelsRadius;
			transform.position = pos;
			Quaternion rotation = Quaternion.LookRotation (contactL.position - contactR.position, axis.up);		
			axis.rotation = rotation;
			transform.rotation = Quaternion.LookRotation (-axis.right, axis.up);
		}


		public float GetTransformSpeed (){

			return transform.InverseTransformDirection (transform.position - lastPosition).z * 60f;

		}
	}


}
