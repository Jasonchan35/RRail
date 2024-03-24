using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class RR_SaveData {
	public	long							nextRouteId = 101;
	public	double							gameDate;

	public	RR_Company.SaveData				playerCompany = new RR_Company.SaveData();

	public	List< RR_Route.SaveData >		routes = new List<RR_Route.SaveData>();
	public	List<RR_TrackInTile>			tracks = new List<RR_TrackInTile>();
}
