using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace uSimFramework.SiegeEngines {

	public class RopeSolver : MonoBehaviour {

		public LineRenderer lineRenderer;
		public Transform pointA;
		public Transform pointB;

		// Use this for initialization
		void Start () {
			lineRenderer = GetComponent<LineRenderer> ();
		}
		
		// Update is called once per frame
		void FixedUpdate () {

			lineRenderer.SetPosition (0, pointA.position);
			lineRenderer.SetPosition (1, pointB.position);

		}
	}
}
