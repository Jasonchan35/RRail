using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RR_Route : MonoBehaviour {

	public 	static	TkNamedGameObject	group		= new TkNamedGameObject("_Routes");

	public	static	void AllRoute_UpdatePath() {
		foreach( var r in RR_Game.instance.routeList ) {
			if( r != null ) r.UpdatePath();
		}
	}

	[System.NonSerialized]
	public	RR_Train train;

	[System.Serializable]
	public	class SaveData {
		[System.NonSerialized]
		public	RR_Route	route;

		public	long				Id;
		public	long				profit;

		public	RR_LocomotiveType	engineType;
		public	double				age;		

		public	List< RR_RouteStation.SaveData	>	stationList 	= new List< RR_RouteStation.SaveData >();
		public	List< RR_TrainCar.Cargo			>	currentCargos	= new List<RR_TrainCar.Cargo>();
	};
	public	SaveData	saveData = new SaveData();

	public	Color					color {
		get{
			var n = RR_UI.settings.route.colors.Length;
			return RR_UI.settings.route.colors[ this.Id % n ];
		}
	}

	public	long					Id 			{ get { return saveData.Id; } }
	public	List< RR_RouteStation >	stationList = new List<RR_RouteStation>();
	public	GameObject	stationGroup;

	void Awake() {
		saveData		= new SaveData();
		saveData.route	= this;
		
		stationGroup = new GameObject("RouteStations");
		Tk.SetParent( stationGroup, this.gameObject, false );
	}

	public static RR_Route Create( long Id = -1 ) {
		if( Id < 0 ) {
			Id =  RR_Game.instance.saveData.nextRouteId;
			RR_Game.instance.saveData.nextRouteId++;
		}

		var o = new GameObject("Route " + Id );
		var c = o.AddComponent< RR_Route >();

		c.saveData.Id = Id;
		o.transform.parent = group.transform;

		RR_Game.instance.routeList.Add( c );
		RR_Game.instance.saveData.routes.Add( c.saveData );

		c.train = RR_Train.Create( c );

		return c;
	}

	public void	Remove() {
		RR_Game.instance.saveData.routes.Remove( this.saveData );
		RR_Game.instance.routeList.Remove( this );

		var listPanel = RR_RouteListPanel.instance;
		if( listPanel ) {
			listPanel.RemoveItem( this );
		}

		Tk.Destroy( this.gameObject );
	}

	public void StationListUpdated() {
		saveData.stationList.Clear();

		foreach( var s in stationList ) {
			saveData.stationList.Add( s.saveData );
		}
	}

	public bool CanAddStation( RR_Station s ) {
		if( stationList.Count > 0 ) {
			if( stationList[0].startStation == s ) return false;

			var last = Tk.SafeGetLastElement( stationList, 0 );
			if( last.startStation == s ) return false;
		}
		return true;
	}

	public RR_RouteStation AddStation( RR_Station s ) {
		if( ! CanAddStation(s) ) return null;

		var o = new GameObject( s.name );

		Tk.SetParent( o, stationGroup, false );
		o.transform.position = s.transform.position;

		var p = o.AddComponent< RR_RouteStation >();

		p.Init( this, s );
		UpdatePath();
		return p;
	}

	public	void RemoveStation( RR_RouteStation rs ) {
		stationList.Remove( rs );
		UpdatePath();
	}

	void FixedUpdate() {
		if( train ) {
			train.MyUpdate();
		}
	}

	void UpdatePath() {
		if( stationList.Count >= 2 ) {
			for( int i=0; i<stationList.Count; i++ ) {
				var a = stationList[i];
				var b = stationList[(i+1) % stationList.Count ];

				a.targetStation = b.startStation;

				var pf = new RR_RouteFinding();
				a.path = pf.FindRoutePath( a.startStation.centerTile, a.targetStation.centerTile );
				if( a.path == null ) {
//					Debug.Log("cannot find path from [" + a.station.name + "] to [" + b.station.name + "] " + stationList.Count );
				}
			}
		}
	}

	public float GetProgress() {
		if( ! train ) return 0;
		return train.GetProgress();
	}

	void OnDrawGizmos() {
		foreach( var s in stationList ) {
			if( s.path != null ) {
				s.path.OnDrawGizmos();
			}
		}
	}

	void OnDrawGizmosSelected() {
		foreach( var s in stationList ) {
			if( s.path != null ) {
				s.path.OnDrawGizmosSelected();
			}
		}
	}
}
