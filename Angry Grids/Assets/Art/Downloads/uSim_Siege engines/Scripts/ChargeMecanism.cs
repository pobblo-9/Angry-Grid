using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace uSimFramework.SiegeEngines {
	
	public class ChargeMecanism : MonoBehaviour {

		public Rigidbody trowingArm;
		public ReleaseMecanism releasingMecanism;
		public SlingHandler sling;
		public float movementArc;
		public float currentArc;
		public bool actionCharge;
	
		// Use this for initialization
		void Start () {
			
		}
		
		// Update is called once per frame
		void FixedUpdate () {

			if (actionCharge && !charging)
				StartCoroutine (Charge ());

		}

		bool charging;
		IEnumerator Charge(){

			charging = true;

			if (GetComponent<AudioSource> () != null)
				GetComponent<AudioSource> ().Play ();

			if (releasingMecanism != null){
				if (releasingMecanism.released) {
					Reset ();
					releasingMecanism.released = false;
					if (sling != null)
						sling.locked = true;
					currentArc = 0f;
				}

			float chargingDuration = 1.5f;
			trowingArm.isKinematic = true;


			do{
				if(currentArc >= movementArc){
					if(releasingMecanism != null)
						releasingMecanism.released = false;
					if(sling != null)
						sling.Reset();
					break;
				}
				Vector3 eulers = trowingArm.transform.localEulerAngles;
				float chargeRate = 10 * Time.deltaTime;
				trowingArm.transform.Rotate(Vector3.right * chargeRate,Space.Self);
				currentArc += chargeRate;
			//	if(currentArc > 180f)
			//		currentArc = -360 + currentArc;
				chargingDuration -= Time.deltaTime;
				//trowingArm.transform.localEulerAngles = eulers;
				transform.Rotate(Vector3.right * chargeRate * 3f,Space.Self);
				yield return new WaitForFixedUpdate();
			}
			while (chargingDuration > 0f);

			actionCharge = false;

		
			}

			charging = false;
		}

		public void Reset(){

			Vector3 eulersFinal = trowingArm.transform.localEulerAngles;
			eulersFinal.x =  0f;
			trowingArm.transform.localEulerAngles = eulersFinal;
		}

	}

}