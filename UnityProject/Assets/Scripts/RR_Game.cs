using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RR_Game : MonoBehaviour {
			static	RR_Game	instance_;
	public	static	RR_Game	instance {
		get{ return instance_; }
	}

	static	private	RR_GameData		data_;
	static	public	RR_GameData		data {
		get{
			if( data_ == null ) {
				data_ = Tk.LoadResource< RR_GameData >( "_AutoGen_/GameData" );
			}
			return data_;
		}
	}

	public	enum Month { Jan, Feb, Mar, Apr, May, Jun, Jul, Aug, Sep, Oct, Nov, Dec }

	// 1800.083 = 1800 Feb
	public	double	gameDate		{ get{ return saveData.gameDate; } }
	public	int		gameYear 		{ get{ return (int) gameDate; } }
	public  int     gameMonthInt	{ get{ return (int)((gameDate - (int)gameDate) * 12); } }
	public	Month	gameMonth		{ get{ return (Month)gameMonthInt; } }

	static	public	float	gameSpeed = 1;
	static	public	bool	gamePause = false;
//	static	public	float	deltaTime			{ get{ return gamePause ? 0 : gameSpeed * Time.deltaTime; } }
	static	public	float	fixedDeltaTime		{ get{ return gamePause ? 0 : gameSpeed * Time.fixedDeltaTime; } }
	static	public	float	fixedDeltaYear		{ get{ return fixedDeltaTime * data.gameGlobal.GameYearPerSeconds; } }

	public	const	int		kMaxCarPerTrain = 8;

//	[System.NonSerialized]
	public	bool	leftHandedTrack = true;

	public	List< RR_Route >	routeList;
	public	List< RR_Station >	stationList;

	static	bool			_firstStart = true;
	static	RR_SaveData		_loadedData;

	public	RR_Company		playerCompany;

	public	static	void	Restart() {
		Debug.Log("Restart");
		Application.LoadLevel( Application.loadedLevel );
	}

	public	static	string	saveFileFolder { 
		get { 
			#if UNITY_EDITOR
				return "./";
			#else
				return Application.persistentDataPath + "/";
			#endif
		}
	}

	static public string saveDataFilename {
		get{ return "GameSave/"+ Application.loadedLevelName +"_SaveData0.json"; }
	}


	public	static	void	Save() {
		var filename = saveFileFolder + saveDataFilename;
		
		Debug.Log("SaveGame to [" + filename +"]" );
		TkJson.EncodeFile( instance.saveData, filename );		
	}


	public	static	void	Load() {
		Debug.Log("LoadGame");
		_LoadData();
		Restart();
	}

	public	static	bool	_TryLoadData() {
		var filename = saveFileFolder + saveDataFilename;

		if( ! System.IO.File.Exists( filename ) ) {
			return false;
		}
		
		TkJson.DecodeFile( out _loadedData, filename );
		if( _loadedData == null ) {
			Debug.LogError("Error load game");
			return false;
		}

		return true;
	}

	public	static	bool	_LoadData() {
		var filename = saveFileFolder + saveDataFilename;

		if( ! _TryLoadData() ) {
			Debug.LogError("cannot load [" + filename +"]" );
			return false;
		}
		return true;
	}

	public	void	_OnLoadGameData() {
		if( Application.isLoadingLevel ) return;

		if( _loadedData == null ) return;
//		Debug.Log("LoadGame Data");
		
		var data = _loadedData;
		_loadedData = null;
		
		var map = RR_Map.instance;

		saveData.nextRouteId	= data.nextRouteId;
		saveData.gameDate		= data.gameDate;

		saveData.playerCompany.money	= data.playerCompany.money;
		saveData.playerCompany.name		= data.playerCompany.name;

		foreach( var src in data.tracks ) {
			var tile = map.GetTile( src.pos );
			if( tile == null ) {
				Debug.LogError("Cannot get tile " + src.pos );
				continue;
			}
			tile._AddTrack( src );
		}

		foreach( var src in data.routes ) {

			var r = RR_Route.Create( src.Id );

			foreach( var srcCargo in src.currentCargos ) {
				if( string.IsNullOrEmpty( srcCargo.pickupStationName ) ) continue;

				srcCargo.pickupStation = RR_Station.FindByName( srcCargo.pickupStationName );
				if( ! srcCargo.pickupStation ) {
					Debug.LogError("Cannot find station " + srcCargo.pickupStationName );
					continue;
				}
				r.train.AddCargo( srcCargo );
			}

			foreach( var srcStation in src.stationList ) {
				var station = RR_Station.FindByName( srcStation.stationName );
				if( ! station ) {
					Debug.LogError("Cannot find station " + srcStation.stationName );
					continue;
				}
				var rs = r.AddStation( station );
				if( ! rs ) continue;

				foreach( var cargos in srcStation.cargos ) {
					rs.AddCargo( cargos.type );
				}
			}

			r.saveData.profit 		= src.profit;
			r.saveData.age 			= src.age;
			r.saveData.engineType 	= src.engineType;
		}

		RR_RouteListPanel.Reload();
	}

	void Awake() {
		instance_	= this;
		routeList	= new List<RR_Route>();
		stationList = new List<RR_Station>();

		saveData.gameDate = 1800;

		playerCompany = RR_Company.Create();
		saveData.playerCompany = playerCompany.saveData;

		playerCompany.name = "Player";
		playerCompany.saveData.name = "Player";
	}

	void Start() {
		var map = RR_Map.instance;
		saveData.gameDate = map.gameStartYear + (float)map.gameStartMonth / 12;
		playerCompany.saveData.money = (long) map.gameStartMoney;

//		Debug.Log("Game Start");
		if( _firstStart ) {
			_firstStart = false;
			_TryLoadData();
		}
	}

	void Update() {
		_OnLoadGameData();
	}

	void FixedUpdate() {
		saveData.gameDate += fixedDeltaYear;
	}

	[System.NonSerialized]
	public	RR_SaveData	saveData = new RR_SaveData();
}
