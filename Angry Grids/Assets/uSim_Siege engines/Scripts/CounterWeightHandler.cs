using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace uSimFramework.SiegeEngines {

	public class CounterWeightHandler : MonoBehaviour {


		public void AddWeight (float weight){

			if(GetComponent<Rigidbody> ().mass < 4000)
			GetComponent<Rigidbody> ().mass += weight;

		}

		public void RemoveWeight (float weight){

			if(GetComponent<Rigidbody> ().mass > 0)
			GetComponent<Rigidbody> ().mass -= weight;
		}
	}
}
