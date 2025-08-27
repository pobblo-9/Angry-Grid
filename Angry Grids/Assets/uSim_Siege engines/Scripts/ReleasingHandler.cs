using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace uSimFramework.SiegeEngines {

	public class ReleasingHandler : MonoBehaviour {

		public Transform pivot;
		public ReleaseMecanism releaseMecanism;

		public void Release () {

			if (!releasing)
				StartCoroutine (ReleaseAnimation ());

		}
		bool releasing;
		float angle = 0f;
		IEnumerator ReleaseAnimation (){
	
			releasing = true;



			do{
				Vector3 eulers = pivot.localEulerAngles;
				eulers.x +=  200f * Time.deltaTime;
				pivot.localEulerAngles = eulers;
				angle = pivot.localEulerAngles.x;
				yield return new WaitForEndOfFrame();
			}
			while(angle < 45f);
			

			releaseMecanism.releaseCommand = true;


			do{
				Vector3 eulers = pivot.localEulerAngles;
				eulers.x -=  200f * Time.deltaTime;
				pivot.localEulerAngles = eulers;
				angle = pivot.localEulerAngles.x;
				if(angle > 180f)
					angle = angle - 360f;
				yield return new WaitForEndOfFrame();
			}
			while(angle > 0f);

			Vector3 eulersFinal = pivot.localEulerAngles;
			eulersFinal.x =  0f;
			pivot.localEulerAngles = eulersFinal;
			angle = pivot.localEulerAngles.x;

			releasing = false;
		}
	}
}