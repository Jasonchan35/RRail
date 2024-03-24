using UnityEngine;
using System.Collections;

public class RR_StationCollider : MonoBehaviour {
	void OnClick() {
//		Debug.Log("station click");

		var s = this.transform.parent.GetComponent< RR_Station >();
		if( s ) s.OnClick();
	}

	void OnDrag() {
		RR_Camera.instance.PassDragControlToCamera();
	}
}
