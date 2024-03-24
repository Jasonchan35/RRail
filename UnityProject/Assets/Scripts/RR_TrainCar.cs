using UnityEngine;
using System.Collections;

public class RR_TrainCar : MonoBehaviour {

	public	float		carFrontWheelOffset 	= 0.3f;
	public	float		carBackWheelOffset	 	= 0.8f; // distance from front wheel

	public	Vector3		frontWheelWorldPos;
	public	Vector3		backWheelWorldPos;

	public	RR_Train	train;
	public	GameObject	colliderObject;
	public	GameObject	model;

	[System.Serializable]
	public	class Cargo {
		public	RR_CargoType	type;
		public	string			pickupStationName;
		public	double			pickupDate;

		[System.NonSerialized]
		public	RR_Station		pickupStation;
	}

	public	Cargo	cargo;

	public	void	UpdateCarPos( ref float posOnPath ) {		
		var p0 = train.path.GetBackwardPosInRadius( posOnPath,	carFrontWheelOffset );
		var p1 = train.path.GetBackwardPosInRadius( p0, 		carBackWheelOffset  );
		
		if( p1 == 0 ) {
			this.gameObject.SetActive( false );
			return;
		}else{
			this.gameObject.SetActive( true );
		}

		var v0 = train.path.GetPos( p0 );
		var v1 = train.path.GetPos( p1 );
		
		frontWheelWorldPos	= v0;
		backWheelWorldPos	= v1;
		
		var rot = Quaternion.LookRotation( v1-v0 );
		
		transform.position = v0;
		transform.rotation = rot;

		posOnPath = p1;
	}

	public void Init( RR_Train train, bool isEngine, Cargo	cargo ) {
		this.gameObject.SetActive( false );
		this.train = train;
		this.cargo = cargo;
		
		gameObject.layer = RR.trainLayerId;

		GameObject	payload = null;

		Tk.SetParent( this.gameObject, train.gameObject, false );

		if( isEngine ) {
			model = Tk.LoadGameObject( "_AutoGen_/Trains/TestEngine");
		}else{
			model = Tk.LoadGameObject( "_AutoGen_/Trains/TestCar");
			payload = Tk.FindChild( model, "Payload", true );
			if( payload ) {
				payload.SetActive( cargo.pickupStation != null );
			}
		}

		if( ! model ) {
			model = GameObject.CreatePrimitive( PrimitiveType.Cube );
		}
		
		Tk.SetParent( model, this.gameObject, false );

//		var mat = Tk.LoadMaterial("Materials/RR_TestCar");

		if( isEngine ) {
			foreach( var mr in model.GetComponentsInChildren< MeshRenderer >( true ) ) {
				var oldColor = mr.sharedMaterial.color;
//				mr.material = mat;
				mr.material.color = oldColor;
			}
		}else{
			var color = RR_UI.settings[ cargo.type ].color;

			foreach( var mr in model.GetComponentsInChildren< MeshRenderer >( true ) ) {
//				var oldColor = mr.sharedMaterial.color;
//				mr.material = mat;
				mr.material.color = color;
			}

			if( payload ) {
				var payloadColor = Color.Lerp( color, Color.white, 0.4f );
				foreach( var mr in payload.GetComponentsInChildren< MeshRenderer >( true ) ) {
//					var oldColor = mr.sharedMaterial.color;
//					mr.material = mat;
					mr.material.color = payloadColor;
				}
			}
		}

		// for touch pickup only
		colliderObject = Tk.LoadGameObject( "Colliders/TrainCarCollider" );
		var co = colliderObject.GetComponent< RR_TrainCarCollider >();
		co.train = train;

		Tk.SetParent( colliderObject, this.gameObject, false );
	}

	void _OnDrawGizmos_Car( bool selected ) {
		if( gameObject.activeSelf ) {			
			switch( train.action ) {
			case RR_Train.Action.StopAndWait:	Gizmos.color = Color.red; 	break;
			case RR_Train.Action.GiveWay:		Gizmos.color = Color.cyan;	break;
			default: {
				Gizmos.color = selected ? Color.green : new Color( 0.75f, 0.75f, 0.75f );		
			}break;
			} //switch
			
			Gizmos.matrix = transform.localToWorldMatrix;
			Gizmos.DrawWireCube( RR_Train.carPivot, RR_Train.carSize * ( selected ? 1.02f : 0.98f ) );
			Gizmos.matrix = Matrix4x4.identity;
		}
	}

	void OnDrawGizmos() {
		_OnDrawGizmos_Car( false );
	}

	void OnDrawGizmosSelected() {		
		_OnDrawGizmos_Car( true );

		Gizmos.color = Color.green;
		Gizmos.DrawCube( frontWheelWorldPos, Vector3.one * 0.08f );
		
		Gizmos.color = Color.cyan;
		Gizmos.DrawCube( backWheelWorldPos,  Vector3.one * 0.08f );
	}

	public void SetAlpha( float a ) {
		if( ! model ) return;
		var mr = model.GetComponent<MeshRenderer>();
		if( !mr ) return;
		var c = mr.material.color;
		c.a = a;
		mr.material.color = c;
	}
}
