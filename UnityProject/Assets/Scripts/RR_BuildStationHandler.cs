using UnityEngine;
using System.Collections;

public class RR_BuildStationHandler : MonoBehaviour {

			static	RR_BuildStationHandler	instance_;
	public	static	RR_BuildStationHandler	instance { get{ return instance_; } }

	public	static	RR_BuildStationHandler	Create( RR_Tile startTile ) {
		Cancel ();

		var o = Tk.LoadGameObject( "UI/Handlers/BuildStationHandler");
		if( !o ) return null;
		o.name = "_BuildStationHandler";
		o.transform.position = startTile.worldPos;

		var comp = o.GetComponent<RR_BuildStationHandler>();
		if( ! comp ) {
			Debug.LogError("RR_BuildStationHandler component is missing");
		}

		return comp;
	}

	public	static	bool Cancel() { return instance_ ? instance_._Cancel() : false; }
	bool _Cancel() {
		Object.Destroy( this.gameObject );
		return true;
	}

	public	static	bool Confirm() { return instance_ ? instance_._Confirm() : false; }	
	bool _Confirm() {
		var o = Tk.LoadGameObject( "UI/Handlers/BuildStationHandler");
		if( !o ) return false;

		o.transform.position = this.transform.position;
		TkMaterial.SetPropertyRecursively( o, "_Color", Color.cyan );

		return true;
	}
		
	void Awake() {
		instance_ = this;
	}
	
	void OnDrag( Vector2 delta ) {
		//		Debug.Log( this.name + " OnDrag " + delta );		

		if( UICamera.touchCount != 1 ) return;

		var touch = UICamera.currentTouch;		
		var tile = RR_Map.instance.GetTileFromGUIPoint( touch.pos );
		
		if( tile != null ) {
			this.transform.position = tile.worldPos;
		}
	}
}
