using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace uSimFramework.SiegeEngines {


	public class SlingHandler : MonoBehaviour {
		
		public HingeJoint detachingAnchorage;
		public GameObject releaseCollider;
		public Rigidbody trowingArm;
		public bool locked;
		public Vector3 lockedPos;
		public Vector3 angularv;
		float armAngularVelocity;
		float lastArmAngularVelocity;
		public float releaseTime;
		float timer;
		Transform initialParent;
		Quaternion initialRot;
		// Use this for initialization
		void Start () {
			initialParent = transform.parent;
			initialRot = transform.rotation;
		}
		
		// Update is called once per frame
		void FixedUpdate () {

			if (locked) {

				transform.parent = trowingArm.transform;
				transform.localPosition = lockedPos;
				transform.localRotation = initialRot;
				GetComponent<Rigidbody> ().isKinematic = true;

			} else {
				armAngularVelocity = trowingArm.GetRelativePointVelocity (detachingAnchorage.transform.position).y;
				if (lastArmAngularVelocity > Mathf.Abs (armAngularVelocity) && !locked)
					timer += Time.deltaTime;
				if (timer > releaseTime) {
					detachingAnchorage.gameObject.SetActive (false);
					releaseCollider.gameObject.SetActive (false); 
				}
				

				lastArmAngularVelocity = armAngularVelocity;
			}
		}

		public void Reset (){

			detachingAnchorage.gameObject.SetActive (true);
			releaseCollider.gameObject.SetActive (true);
			locked = false;
			timer = 0f;
			transform.parent = initialParent;
			GetComponent<Rigidbody> ().isKinematic = false;
		}
	}
}
