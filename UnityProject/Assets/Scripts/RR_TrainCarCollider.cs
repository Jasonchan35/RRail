using UnityEngine;
using System.Collections;

public class RR_TrainCarCollider : MonoBehaviour {
	public	RR_Train	train;

	void OnClick() {
		//		Debug.Log("Click Train " + train.name );
		RR_UI.SelectRoute( train.route );
	}

	void OnDrag() {
		RR_Camera.instance.PassDragControlToCamera();
	}
}
