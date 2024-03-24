using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class RR_Camera : MonoBehaviour {

	public	Vector3	angle    = new Vector3(45,0,0);
	public	float	distance = 25;

	public	float	povDistance = 8;
	public	float	povOffsetY	= 2;
	public	float	povFOV		= 35;

	float	startFOV;

	public	bool	enableRotation = true;

	public	Camera	mainCamera;
	public	Camera	purchaseEngineCamera;

	public	static	void	SetAim( GameObject o ) {
		if( instance ) {
			instance.aimObject = o;
		}
	}

	public	enum Mode {
		None,
		Map,
		TrainPOV,
	}
	private	Mode	mode_;
	public	Mode	mode {
		get{
			return mode_;
		}
		set{
			if( mode_ == value ) return;

			switch( value ) {

			case Mode.TrainPOV: {
				mainCamera.fieldOfView = povFOV;
			}break;

			default: {
				mainCamera.fieldOfView = startFOV;
			}break;

			}//switch
			mode_ = value;
		}
	}

	GameObject	aimObject;
	Vector3		aimVelocity;

	Vector3 debugPt;
	Vector3 debugLineA;
	Vector3 debugLineB;
	float	debugAngle;

	RR_Tile	startTile;
	RR_Tile	endTile;

	int		raycastDefaultLayerMask;
	int		raycastHandlerLayerMask;

			static	RR_Camera	instance_;
	public	static	RR_Camera	instance { get{ return instance_; } }

	void Awake() {
		instance_ = this;
		raycastDefaultLayerMask = ~Tk.LayerBits("UI","UI 2D","UI 2D (only show)","NGUI", "Terrain");
		raycastHandlerLayerMask =  Tk.LayerBits("Handler");

		UICamera.extraRaycast = ExtraRaycast;
		UICamera.fallThrough = this.gameObject;

		{
			var o = Tk.FindChild( this.gameObject, "Main Camera", true );
			if( o ) mainCamera = o.GetComponent< Camera >();
		}

		{
			var o = Tk.FindChild( this.gameObject, "PurchaseEngine Camera", true );
			if( o ) purchaseEngineCamera = o.GetComponent< Camera >();
		}
	}

	void Start() {
		startFOV = mainCamera.fieldOfView;
	}

	bool ExtraRaycast( Vector3 inPos, out RaycastHit hit ) {

		var ray = GUIPointToRay( inPos );

		var mask = raycastDefaultLayerMask;
		if( RR_UI.inputMode == RR_InputMode.BuildTrack ) {
			mask = raycastHandlerLayerMask;
		}

		if( Physics.Raycast( ray, out hit, float.PositiveInfinity, mask ) ) {
//			Debug.Log("Hit " + hit.collider.name );
			return true;
		}

		hit = new RaycastHit();		
		return false;
	}

	Vector2	screenPixelCenter { get{ return new Vector2( Camera.main.pixelWidth, Camera.main.pixelHeight ) / 2; } } 	

	Vector2	startDragPoint0;
	Vector2 startDragPoint1;
	Vector3	startDragHitPoint0;
	Vector3	startDragHitPoint1;
	Vector3	startDragAimHitPoint;
	Vector3	startDragAimPos;
	Vector3 startDragAngle;
	float	startDragDistance;

	const	float	rotateThreshold = 5;
			bool	passRotateThreshold = false;


	public	void OnClick() {
	}

	bool OnPress_UIInput( bool isPressed ) {
		switch( RR_UI.inputMode ) {
			case RR_InputMode.BuildTrack:{
                if (isPressed)
                {
                    var h = RR_BuildTrackHandler.instance;
                    if (!h)
                    {
                        var touch0 = UICamera.GetTouchByOrder(0);
                        var startTile = RR_Map.instance.GetTileFromGUIPoint(touch0.pos);

                        if (startTile != null && startTile.trackCount > 0)
                        {
                            h = RR_BuildTrackHandler.Create(startTile);
                            if (!h) return false;

                            touch0.dragged = h.endHandler.gameObject;

                            return true;
                        }
                    }
                }
			}break;

			case RR_InputMode.BuildStation: {
				if( isPressed ) {
					var h = RR_BuildStationHandler.instance;
					if( !h ) {
						var touch0 = UICamera.GetTouchByOrder(0);
						var startTile = RR_Map.instance.GetTileFromGUIPoint( touch0.pos );
						if( startTile != null ) {
							h = RR_BuildStationHandler.Create( startTile );
							if( !h ) return false;

							touch0.dragged = h.gameObject;
							return true;
						}
					}
				}
			}break;
		}

		return false;
	}

	void StartDrag_OneTouch( UICamera.MouseOrTouch touch ) {
		startDragAimPos		 = this.transform.position;
		startDragAimHitPoint = RR_Map.instance.GUIToGroundPoint( screenPixelCenter );
		startDragHitPoint0 	 = RR_Map.instance.GUIToGroundPoint( touch.pos );
	}

	public	void PassDragControlToCamera() {
		OnPress( true );
		UICamera.currentTouch.dragged = RR_Camera.instance.gameObject;
	}

	public	void OnPress( bool isPressed ) {
		if( ! Application.isPlaying ) return;

		if( RR_UI.inputMode == RR_InputMode.PurchaseEngine ) return;

		switch( UICamera.touchCount ) {
		case 1:{
			if( OnPress_UIInput( isPressed ) ) return;

			var touch0 = UICamera.GetTouchByOrder(0);
			StartDrag_OneTouch( touch0 );
		}break;
			
		case 2:{
			passRotateThreshold = false;
			var touch0 = UICamera.GetTouchByOrder(0);
			var touch1 = UICamera.GetTouchByOrder(1);

			if( ! isPressed ) {
				if( UICamera.currentTouch == touch0 ) {
					StartDrag_OneTouch( touch1 );
				}else{
					StartDrag_OneTouch( touch0 );
				}
			}else{
				startDragPoint0		= touch0.pos;
				startDragPoint1		= touch1.pos;

				startDragAimPos		= this.transform.position;
				startDragAngle		= this.angle;
				startDragDistance	= this.distance;
					
				startDragAimHitPoint= RR_Map.instance.GUIToGroundPoint( screenPixelCenter );
				startDragHitPoint0 	= RR_Map.instance.GUIToGroundPoint( touch0.pos );
				startDragHitPoint1 	= RR_Map.instance.GUIToGroundPoint( touch1.pos );
			}

		}break;
		}
	}

	public void OnDrag( Vector2 delta ) {
		if( ! Application.isPlaying ) return;

//		Debug.Log("RR_Camera OnDrag " + delta );

		if( RR_UI.inputMode == RR_InputMode.PurchaseEngine ) return;

		if( mode == Mode.TrainPOV ) {
			var touch0 = UICamera.GetTouchByOrder(0);

			mainCamera.transform.RotateAround( this.transform.position, Vector3.up, touch0.delta.x * 0.1f );
			return;
		}
		
		aimObject = null;

		switch( UICamera.touchCount ) {
		case 1: {
			var touch0 = UICamera.GetTouchByOrder(0);
			_OnDragAim( touch0.pos, startDragHitPoint0 );
		}break;

		case 2: {
			var map = RR_Map.instance;
			if( map == null ) return;

			var touch0 = UICamera.GetTouchByOrder(0);
			var touch1 = UICamera.GetTouchByOrder(1);

			//=== distance ===
			{
				var d0 = Vector2.Distance( startDragPoint0, startDragPoint1 );
				var d1 = Vector2.Distance( touch0.pos,		touch1.pos );
				var d = d1-d0;
				
				this.distance = startDragDistance - d * 0.10f;
			}
			
			//=== rotate ===
			if( enableRotation ) {
				var d0 = startDragPoint0 - startDragPoint1;
				var d1 = touch0.pos - touch1.pos;
				
				var r = Quaternion.FromToRotation( Vector3.up, d0 ).eulerAngles;
				var q = Quaternion.FromToRotation( Vector3.up, d1 ).eulerAngles;

				var a = startDragAngle.y - r.z + q.z;

				if( ! passRotateThreshold ) {
					if( Mathf.Abs(a) > rotateThreshold ) {
						passRotateThreshold = true;
					}
				}

				if( passRotateThreshold ) {
					this.angle.y = a;
				}
			}
			// === Aim ===
			{
				var pos = ( touch0.pos + touch1.pos ) / 2;
				var startPos = ( startDragHitPoint0 + startDragHitPoint1 ) / 2;
				_OnDragAim( pos, startPos );		
			}		
		}break;
		}
	}

	void _OnDragAim( Vector2 touchPos, Vector3 startHitPoint ) {
		var map = RR_Map.instance;
		if( map == null ) return;
		
		var a = startDragAimHitPoint - startHitPoint;
		
		var c = map.GUIToGroundPoint( screenPixelCenter );
		var b = map.GUIToGroundPoint( touchPos );

		if( float.IsNaN(c.x) || float.IsNaN(b.x) ) return;

		var newAim = startDragAimPos + c - b - a;
		MoveToFocus ( newAim );
	}

	public void MoveToFocus( Vector3 aim ) {
		this.transform.position = aim;
	}

	void FixedUpdate () {
		TkMath.ClampIt( ref distance, 15, 100 );

		var lastPos = this.transform.position;
		var lastMainCamPos = mainCamera.transform.position;

		var aim 	= lastPos;
		if( aimObject ) {
			this.transform.position = Vector3.SmoothDamp( lastPos, aimObject.transform.position, ref aimVelocity, RR_UI.settings.camera.smoothDampTime );
		}

		switch( mode ) {
		case Mode.TrainPOV: {

			if( UICamera.touchCount == 0 ) {
				var d = lastMainCamPos - this.transform.position;
				d.y = 0;

				var pos = this.transform.position + d.normalized * povDistance;

				TkMath.MaxIt( ref pos.y, this.transform.position.y + povOffsetY );

				mainCamera.transform.position = pos;
				mainCamera.transform.LookAt( this.transform.position );
			}
		}break;

		default: {
			mainCamera.transform.position = aim + Quaternion.Euler( angle ) * Vector3.back * distance;
			mainCamera.transform.LookAt( this.transform.position );
		}break;
		} //switch

	}

	Ray	 GUIPointToRay( Vector2 pt ) {
		return mainCamera.ScreenPointToRay( pt );
	}

}
