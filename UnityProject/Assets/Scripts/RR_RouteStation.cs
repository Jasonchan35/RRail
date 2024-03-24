using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RR_RouteStation : MonoBehaviour {
	public	RR_Route			route;
	public	RR_Station			startStation;
	public	RR_Station			targetStation;
	public	RR_RoutePath		path;

	public	const	int		maxCargos = 8;

	[System.Serializable]
	public	class Cargo {
		[System.NonSerialized]
		public	RR_RouteStation		routeStation;
		public	RR_CargoType		type;
	}

	[System.Serializable]
	public class SaveData {
		public	string		stationName = string.Empty;
		public	List<Cargo>	cargos		= new List<Cargo>();
	}
	public SaveData	saveData = new SaveData();

	public	void Init( RR_Route route, RR_Station station ) {
		this.route = route;
		this.startStation = station;
		saveData.stationName = station.name;

		route.stationList.Add( this );
		route.StationListUpdated();
	}

	void OnDestroy() {
		Tk.Destroy( this.gameObject );
	}

	public	List<Cargo>		cargos { get{ return saveData.cargos; } } 

	public	RR_RouteStation.Cargo	AddCargo( RR_CargoType t ) {
		if( saveData.cargos.Count >= maxCargos ) {
			return null;
		}

		var c = new Cargo();
		c.routeStation = this;
		c.type = t;

		saveData.cargos.Add( c );
		return c;
	}

	public	void	RemoveCargo( RR_RouteStation.Cargo cargo ) {
		saveData.cargos.Remove( cargo );
	}
}
