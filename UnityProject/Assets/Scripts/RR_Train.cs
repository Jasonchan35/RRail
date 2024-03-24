using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[SelectionBase]
public class RR_Train : MonoBehaviour {
	public	float				acceleration	= 8; 		// seconds to Max Speed
//	public	float				deceleration	= 2.5f; 	// 

	public	float				maxSpeed		= 40.0f;	//! mile/h
	public	float				speed			= 0;		//! current speed
	public	float				targetSpeed		= 0;

	public	float				trainHeadOffset	= 1.6f;
	public	float				trainTailOffset	= 1.6f;
	
	public	float				hardStopDistance = 0.5f;

	public	float				trainLength		= 0;
	public	float				carLength		= 1;

	[HideInInspector]
	public	RR_TrainHUD			hud;

	public	readonly static	Vector3	carSize			= new Vector3( 0.33f, 0.36f, 1.01f );
	public	readonly static	Vector3	carPivot		= new Vector3( 0,      0.2f, 0.395f );

	public	List< RR_TrainCar >			engines;
	public	List< RR_TrainCar >			cars;
	public	List< RR_TrainCar.Cargo >	carryoverCargos; // cargo which target station don't needed, we try to carry to next station

	public	float				posOnPath = 20;

	public	int					headNode;
	public	int					tailNode;

	const	int					slowDownBeforeTarget = 10;
	const	int					slowDownBeforeStop 	 = 10; 
	
	public	int					waitingNode;
	public	RR_Train			waitingTrain;
	public	float				waitingDistance;

	TkCircularBuffer< RR_RoutePath.Node >	lockedTracks;

//	RR_Station					targetStation;
//	RR_RoutePath				path;

			RR_RouteStation		routeStation_;
	public	RR_RouteStation		routeStation {
		get{ return routeStation_; }
		set{ 
			if( routeStation_ == value ) return;

			routeStation_ = value;
			path_ = null;

			if( routeStation_ ) {
				path_ = routeStation_.path;
			}

			if( path_ != null ) {
				action = Action.EnterStation;
			}
		}
	}

	private	RR_RoutePath		path_; 
	public	RR_RoutePath		path { get{ return path_; } }

	public	RR_Route			route { get { return route_; } }
			RR_Route			route_;

	int	targetCheckPoint_;
	int	targetCheckPoint {
		get { return targetCheckPoint_; }
		set { 
			targetCheckPoint_ 	= value;

			if( path != null ) {
				if( value >= path.checkPointNodes.Length ) {
					Debug.LogError( value + " " + path.checkPointNodes.Length );
				}			
			}
		}
	}

	RR_RoutePath.Node 			targetCheckPointNode 	{ get{ return path.checkPointNodes[ targetCheckPoint_ ]; } }
	RR_RoutePath.CheckPointType	targetCheckPointType 	{ get{ return path.checkPointNodes[ targetCheckPoint_ ].checkPointType; } }

	public enum Action {
		None,
		EnterStation,
		UnloadInStation,
		LoadInStation,
		UTurnIn,
		UTurning,
		UTurnOut,
		Go,
		StopAndWait,
		GiveWay,
	}

	public	float	actionTime;

	public	Action	action_;
	public	Action	action {
		get{ return action_; }
		set{ 
			if( value == action_ ) return;
			if( path == null ) return;

			switch( value ) {

			case Action.EnterStation:	{
				targetCheckPoint = 0;
				posOnPath = targetCheckPointNode.pointIndex;

				speed = 0;
				targetSpeed = 0;
				headNode = 0;
				tailNode = 0;

				UpdateTrainPos();
			}break;

			case Action.LoadInStation: {
				UpdateTrainPos();
			}break;

			case Action.UnloadInStation: {
			}break;

			case Action.Go: {
			}break;

			case Action.StopAndWait: {
			}break;

			case Action.GiveWay: {
//				Debug.Log( this.name + " give way to " + waitingTrain.name );
			}break;

			case Action.UTurnIn: {
			}break;

			case Action.UTurnOut: {
			}break;

			} // switch

			action_ = value;
			actionTime = 0;
		}
	}

	void DoAction_UTurnIn() {
		if( actionTime < 2 ) {
			actionTime += RR_Game.fixedDeltaTime;
			return;
		}

		action = Action.UTurnOut;
		
		targetCheckPoint++;

		if( targetCheckPointNode.checkPointType != RR_RoutePath.CheckPointType.UTurnOut ) {
			Debug.LogError("UTurnOut check point missing after UTurnIn");
		}

		posOnPath = targetCheckPointNode.pointIndex;
		speed = 0;
		targetSpeed = 0;

		//keep the train model in this side, but update the head/tail node to lock another side
		UpdateHeadTailNode();
	}

	void DoAction_UTurning() {
		if( ! LockTracks() ) return;

		action = Action.UTurnOut;
	}

	void DoAction_UTurnOut() {
		if( actionTime < 1 ) {
			actionTime += RR_Game.fixedDeltaTime;
			return;
		}

		action = Action.Go;
		targetCheckPoint++;
	}

	void DoAction_None() {
		if( route.stationList.Count < 2 ) return;
	}

	void DoAction_EnterStation() {
		if( ! LockTracks() ) return;
		action = Action.LoadInStation;
	}

	RR_TrainCar	LoadEngineObject( int index ) {
		var carObj = new GameObject("Engine " + index );		
		var car = carObj.AddComponent< RR_TrainCar >();
		
		car.Init( this, true, null );		
		trainLength += carLength;
		
		return car;
	}

	RR_TrainCar	LoadCarModel( int index, RR_TrainCar.Cargo cargo ) {

		var carObj = new GameObject("Cargo " + index );		
		var car = carObj.AddComponent< RR_TrainCar >();

		car.Init( this, false, cargo );
		trainLength += carLength;

		return car;
	}

	public	void AddCargo( RR_TrainCar.Cargo cargo ) {
		route.saveData.currentCargos.Add( cargo );
		var idx = cars.Count;
		var car = LoadCarModel( idx, cargo );
		cars.Add( car );
	}

	void DoAction_LoadInStation() {
		const float loadTimePerCar = 0.5f;

		if( actionTime < loadTimePerCar ) {
			actionTime += RR_Game.fixedDeltaTime;
			return;
		}

		actionTime -= loadTimePerCar;

		var carIdx = cars.Count;

		if( carIdx < routeStation.cargos.Count ) {
			var station = routeStation.startStation;
			var cargoType = routeStation.cargos[ carIdx ].type;

			int		carryoverIndex = -1;
			float	carryoverDistance = 0;

			// find carry over cargo which has max distance ( max value )
			for( int i=0; i<carryoverCargos.Count; i++ ) {
				var a = carryoverCargos[i];
				if( a.type != cargoType ) continue;

				var dis = Vector3.Distance( station.transform.position, a.pickupStation.transform.position );
				if( dis > carryoverDistance ) {
					carryoverIndex = i;
				}
			}

			RR_TrainCar.Cargo	cargo = null;

			if( carryoverIndex >= 0 ) {
				cargo = carryoverCargos[ carryoverIndex ];
				carryoverCargos.RemoveAt( carryoverIndex );
			}else{
				cargo = station.TrainCollectCargo( cargoType );
			}

			// create empty cargo
			if( cargo == null ) {
				cargo = new RR_TrainCar.Cargo();
				cargo.type = cargoType;
			}

			AddCargo( cargo );
			UpdateTrainPos();

			return;
		}

		carryoverCargos.Clear ();

		action = Action.Go;
		targetCheckPoint = 1;
	}

	void DoAction_UnloadInStation() {
		const float unloadTimePerCar = 0.5f;

		if( actionTime < unloadTimePerCar ) {
			actionTime += RR_Game.fixedDeltaTime;
			return;
		}

		actionTime -= unloadTimePerCar;

		if( cars.Count > 0 ) {
			var car = Tk.SafeGetLastElement( cars, 0 );

			if( car.cargo.pickupStation != null ) {
				var station = routeStation.targetStation;
				long money;
				if( ! station.TrainDeliverCargo( car.cargo, out money ) ) {
					carryoverCargos.Add( car.cargo );
				}else{
					RR_Game.instance.playerCompany.saveData.money += money;
					route.saveData.profit += money;

					RR_MoneyFloatingText.Create( money, car.transform.position );
				}
			}

//			lastCar.cargoType

			Tk.Destroy( car.gameObject );

			var lastIdx = cars.Count - 1;
			route.saveData.currentCargos.RemoveAt( lastIdx );
			cars.RemoveAt( lastIdx );

			trainLength -= carLength;

			UpdateTrainPos();

			return;
		}

		trainLength = 0;
		if( cars.Count != 0 || route.saveData.currentCargos.Count != 0 ) {
			Debug.LogError("some cargo remain ? ");
		}

		cars.Clear();
		route.saveData.currentCargos.Clear();

		if( route.stationList.Count < 2 ) {
			UnlockAllTracks();

			action = Action.None;
			routeStation = null;
			return;
		}

		int idx = GetStationIndex();
		if( idx < 0 ) {
//			Debug.LogError("station not found !!");
			idx = 0;
		}

		idx++;

		routeStation = route.stationList[ idx % route.stationList.Count ];

		if( routeStation == null || routeStation.path == null ) return;

		UnlockAllTracks();
	}

	public	int GetStationIndex() {
		return route.stationList.FindIndex( a=> a==routeStation );
	}

	void DoAction_StopAndWait() {
		if( LockTracks() ) {
			action = Action.Go;
		}

		if( actionTime > 5 ) {
			action = Action.GiveWay;
			return;
		}
		actionTime += RR_Game.fixedDeltaTime;

		if( waitingDistance < hardStopDistance ) {
//			Debug.LogWarning( this.name + " too close ! " + waitingDistance );
			speed = 0;
		}

		targetSpeed = 0;
		UpdateTrainPos();
	}

	public void SetAlpha( float a ) {		
		foreach( var c in cars ) {
			c.SetAlpha( a );
		}
	}

	void DoAction_GiveWay() {
		if( LockTracks() ) {
			action = Action.Go;
			SetAlpha( 1 );
			return;
		}

		SetAlpha( 0.5f );

		UnlockAllTracks();

		targetSpeed = 0;
		speed = 0;
	}

	void DoAction_Go() {
		if( ! LockTracks() ) {
			action = Action.StopAndWait;
		}

		targetSpeed = maxSpeed;

		waitingTrain = null;

		UpdateTrainPos();
		UnlockTailTracks();
	}

	public void MyUpdate_Maintenance() {
		const double	maintenanceTime = (double) 3 / 12;

		var lastMaintain = (int)( route.saveData.age / maintenanceTime );
		route.saveData.age += RR_Game.fixedDeltaYear;
		
		var thisMaintain = (int)( route.saveData.age / maintenanceTime );
		
		var maintain = thisMaintain - lastMaintain;
		
		if( maintain <= 0 ) return;

		var engineData = RR_Game.data[ route.saveData.engineType ];
		var maintainCost = -(long)( maintain * engineData.MaintenanceCost );

		RR_MoneyFloatingText.Create( maintainCost, this.transform.position );

		route.saveData.profit += maintainCost;
	}

	public void MyUpdate () {
		if( route == null ) return;

		MyUpdate_Maintenance();

		if( routeStation == null ) {
			if( route.stationList.Count > 1 ) {
				routeStation = route.stationList[0];
			}
		}
		if( routeStation == null ) return;
		if( path == null ) {
			return;
		}
		

		switch( action ) {
		case Action.None:				DoAction_None();				break;
		case Action.EnterStation: 		DoAction_EnterStation();	 	break;
		case Action.LoadInStation: 		DoAction_LoadInStation();	 	break;
		case Action.UnloadInStation: 	DoAction_UnloadInStation();		break;
		case Action.UTurnIn:			DoAction_UTurnIn();				break;
		case Action.UTurning:			DoAction_UTurning();			break;
		case Action.UTurnOut:			DoAction_UTurnOut();			break;
		case Action.Go: 				DoAction_Go();					break;
		case Action.StopAndWait:		DoAction_StopAndWait();			break;
		case Action.GiveWay:			DoAction_GiveWay();				break;
		default: Debug.LogError("unhandled train action"); break;
		}
	}

	
	public	static	RR_Train Create( RR_Route route ) {
		var obj = new GameObject("Train_" + route.name );
		obj.layer = RR.trainLayerId;
		Tk.SetParent( obj, route.gameObject, false );
		
		var train = obj.AddComponent< RR_Train >();		
		train.route_ = route;

		train.hud = RR_UI.LoadPanel< RR_TrainHUD >();
		train.hud.SetTrain( train );

		return train;
	}

	void UnlockAllTracks() {
		while( lockedTracks.Count > 0 ) {
			var tail = lockedTracks.Dequeue();
			tail.Unlock( this );
		}
	}
	
	void UnlockTailTracks() {
		
		while( lockedTracks.Count > 0 ) {
			var t = lockedTracks.Peek();
			
			if( t.index >= tailNode ) {
				break;
			}
			
			t = lockedTracks.Dequeue();
			t.Unlock( this );
		}
	}
	
	bool LockTracks() {		
		waitingTrain = null;
		waitingNode  = -1;
		
		for( int i=tailNode; i <= headNode; i++ ) {
			var p = path.nodes[ i ];
			
			var prevLock = p.Lock( this );
			
			if( prevLock == this ) continue; // was locked by itself
			
			if( prevLock != null ) {
				waitingTrain = prevLock;
				waitingNode  = i;
				
				waitingDistance = p.pointIndex - posOnPath;
				
				return false;
			}
			
			//new lock
			lockedTracks.Enqueue( p );
		}
		
		return true;
	}
	
	void Awake() {
		trainLength = 0;
		lockedTracks = new  TkCircularBuffer<RR_RoutePath.Node>(256);

		cars 	= new List<RR_TrainCar>(16);
		engines = new List<RR_TrainCar>(2);
		carryoverCargos = new List<RR_TrainCar.Cargo>(16);

		engines.Add( LoadEngineObject( 0 ) );
	}

	public	float GetProgress() {
		if( path == null ) return 0;

		var s = path.stationStartIndex;
		var e = path.stationEndIndex;

		var n = e-s;
		if( n <= 0 ) return 0;
		
		var f = (posOnPath - s) / n;
		return f;
	}

	void UpdateTrainPos() {

		if( speed < targetSpeed ) {
			speed = Mathf.Min( speed + maxSpeed * RR_Game.fixedDeltaTime / acceleration, targetSpeed );

		}else if( speed > targetSpeed ) {
			speed = Mathf.Max( speed - maxSpeed * RR_Game.fixedDeltaTime / (acceleration * 5 ), targetSpeed );

		}	

		speed = Mathf.Clamp( speed, 0, maxSpeed );

		posOnPath = path.GetOffset( posOnPath, speed * RR_Game.fixedDeltaYear * RR_Game.data.gameGlobal.TrainSpeed );

		var targetPoint = targetCheckPointNode.pointIndex;

		if( posOnPath >= targetPoint ) {
			posOnPath = targetPoint;

			switch( targetCheckPointType ) {
			case RR_RoutePath.CheckPointType.UTurnIn:		action = Action.UTurnIn;			break;
			case RR_RoutePath.CheckPointType.StationEnd: 	action = Action.UnloadInStation;	break;
			}
		}

	//-----------
		transform.position = path.GetPos( posOnPath );		

//		var lastPos = path.GetBackwardPosInRadius( posOnPath, carFrontWheel );
		var lastPos = posOnPath;

		foreach( var c in engines ) {
			c.UpdateCarPos( ref lastPos );
		}

		foreach( var c in cars ) {
			c.UpdateCarPos( ref lastPos );
		}

		UpdateHeadTailNode();
	}

	void UpdateHeadTailNode() {
		var headPt	= path.GetOffsetIndex( posOnPath, trainHeadOffset );
		var tailPt	= path.GetOffsetIndex( posOnPath, -( trainLength + trainTailOffset ) );
		
		headNode	= path.FindForwardNode( headPt, headNode );
		tailNode	= path.FindForwardNode( tailPt, tailNode );
	}

	public bool ReplaceEngine( RR_LocomotiveType type ) {
		var data = RR_Game.data[ type ];
		
		var player = RR_Game.instance.playerCompany;
		
		if( player.saveData.money < data.Cost ) {
			return false;
		}
		
		player.saveData.money -= (long) data.Cost;	
		
		route.saveData.engineType 		= type;
		route.saveData.age = 0;

		return true;
	}

	void OnDrawGizmos() {
		if( path != null ) {
			if( waitingNode >= 0 ) {
				Gizmos.color = Color.red;
				var node = path.nodes[ waitingNode ];
				Gizmos.DrawLine( transform.position, node.worldPos );
			}
		}
	}
	
	void OnDrawGizmosSelected() {
		if( path != null ) {
			path.OnDrawGizmosSelected();

			foreach( var a in lockedTracks.ToArray() ) {
				a.track.DrawGizmos_TrainLock( true, a.useTrackA, ! a.useTrackA );
			}

			var h = path.nodes[ headNode ];
			var t = path.nodes[ tailNode ];

			Gizmos.color = Color.green;
			Gizmos.DrawCube( h.worldPos, Vector3.one * 0.2f );

			Gizmos.color = Color.cyan;
			Gizmos.DrawCube( t.worldPos, Vector3.one * 0.2f );

			/*
			if( waitingTrain != null ) {
				Gizmos.color = Color.red;
				Gizmos.DrawLine( transform.position, waitingTrain.transform.position );
			}
			*/
		}
	}
}
