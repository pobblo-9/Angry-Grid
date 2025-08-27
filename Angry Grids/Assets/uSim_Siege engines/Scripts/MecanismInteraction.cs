using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace uSimFramework.SiegeEngines {

	public class MecanismInteraction : MonoBehaviour {

		bool actionInput;
		bool secondButton;

		public Text infoTextUI;
		string infoText;

		// Use this for initialization
		void Start () {
			
		}

		void Update (){

			actionInput = Input.GetMouseButton (0);
			secondButton = Input.GetMouseButton (1);
		}

		void FixedUpdate () {

			infoText = "";

		
				Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
				RaycastHit hit;
				if (Physics.Raycast (ray, out hit, 30f)) {
			
					if (hit.collider.GetComponent<ChargeMecanism> () != null) {
						infoText = "Left click to charge shot";
						if(actionInput)
						hit.collider.GetComponent<ChargeMecanism> ().actionCharge = true;						
					}
					if (hit.collider.GetComponent<Munition> () != null) {
						infoText = "Left click to pickup / E key to release";
					if (GetComponent<MunitionHandler> () != null && actionInput)
						GetComponent<MunitionHandler> ().Pickup (hit.transform);

					}
					if (hit.collider.GetComponent<ReleasingHandler> () != null) {
						infoText = "Left click to release catapult";
						if(actionInput)
						hit.collider.GetComponent<ReleasingHandler> ().Release ();

					}
					if (hit.collider.GetComponent<CounterWeightHandler> () != null) {
					infoText = "Left click to remove weight | Right click to add weight\n" + Mathf.RoundToInt (hit.collider.GetComponent<Rigidbody>().mass).ToString() + " kg";
						if (secondButton) {							
							hit.collider.GetComponent<CounterWeightHandler> ().AddWeight (10f);
						} 
						if (actionInput) {						
							hit.collider.GetComponent<CounterWeightHandler> ().RemoveWeight (10f);
						}
					}
					if (hit.collider.GetComponent<PlatformMover> () != null) {
						infoText = "Left click and hold to push platform with player movement";
						if (actionInput) {
							GetComponent<PlatformPusher> ().pushing = true;
							GetComponent<PlatformPusher> ().mover = hit.collider.GetComponent<PlatformMover> ();
							hit.collider.GetComponent<PlatformMover> ().handled = true;
						}
					}
				}
			
			infoTextUI.text = infoText;
		}
	}
}