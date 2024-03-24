using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RR_Station : MonoBehaviour {
	[System.NonSerialized]	public	GameObject	model;
	[System.NonSerialized]	public	RR_Tile		centerTile;

	[System.NonSerialized]	public	readonly	Vector3		cityPlatformSize 		= new Vector3(9,1,3);
	[System.NonSerialized]	public	readonly	Vector3		resourcePlatformSize 	= new Vector3(3,1,2);
	[System.NonSerialized]	public	readonly	Color		cityPlatformColor 		= new Color( 0.8f, 0.8f, 0.8f );
	[System.NonSerialized]	public	readonly	Color		resourcePlatformColor 	= new Color( 0.8f, 0.6f, 0.2f );

	public	Vector3		platformSize 	{ get{ return isCity ? cityPlatformSize  : resourcePlatformSize; } }
	public	Color		platformColor 	{ get{ return isCity ? cityPlatformColor : resourcePlatformColor; } }

	public	bool			buildWhenGameStart;

	public	RR_Direction	orientation;

	[HideInInspector]
	public	RR_StationHUD	hud;

	public	RR_ResourceType		resourceType;
	public	RR_ResourceSize		resourceSize;

	public	bool	isCity { get{ return resourceType == RR_ResourceType.City; } }		

	[System.Serializable]
	public class FactorySlot {
		public	RR_FactoryType		type;
		public	RR_FactoryType[]	avaliableType;
	}

	public	FactorySlot	facorySlot0;
	public	FactorySlot	facorySlot1;
	public	FactorySlot	facorySlot2;

	[System.Serializable]
	public	class CargoStatus {
		public	float	supply;
//		public	float	demand;
		public	float	storage;
	}
	[System.NonSerialized]
	private	CargoStatus[]	_cargoStatus_ = null;

	public	RR_TrainCar.Cargo	TrainCollectCargo( RR_CargoType t ) {
		var st = GetCargoStatus( t );
		if( st.supply < 1 ) return null;
		st.supply -= 1;

		var cargo = new RR_TrainCar.Cargo();
		cargo.type = t;
		cargo.pickupDate = RR_Game.instance.gameDate;
		cargo.pickupStation = this;
		cargo.pickupStationName = this.name;
		
		return cargo;
	}

	public	bool	TrainDeliverCargo( RR_TrainCar.Cargo cargo, out long money ) {
		money = 0;
		if( cargo.pickupStation == null ) return false;

		var t = cargo.type;
		if( ! HasDemand( t ) ) return false;

		var st = GetCargoStatus( t );
		st.storage++;


	//---
		var ab = cargo.pickupStation.transform.position - this.transform.position;
		ab.y = 0;

		var distance = ab.magnitude;

		var data = RR_Game.data[t];

		var maxDistance = 1000;

		var m = distance / maxDistance * data.money_max + data.money_min;
		var time = RR_Game.instance.gameDate - cargo.pickupDate;
		
		var v = TkMath.Div( distance, time );

		v = TkMath.NormalizeInRange( v, data.v_min, data.v_max );
		v = TkMath.Clamp01( v );

		money = (long)( m * v );
		return true;
	}

	public	CargoStatus		GetCargoStatus( RR_CargoType t ) { 
		if( _cargoStatus_ == null ) {
			Tk.NewObjectArray( ref _cargoStatus_, Tk.EnumCount< RR_CargoType >() );
		}
		return _cargoStatus_[ (int) t ]; 
	}

	public	bool	HasSupply( RR_CargoType t ) { return gameData[ t ].cap 		> 0; }
	public	bool	HasDemand( RR_CargoType t ) { return gameData[ t ].demand	> 0; }

	public	string	GetSupplyDisplayText( RR_CargoType t ) {
		return Mathf.FloorToInt( GetCargoStatus(t).supply ).ToString();
	}

	public	RR_GameData.ResourceSize gameData {
		get{
			return RR_Game.data[ resourceType ][ resourceSize ];
		}
	}


	public	Vector3	platformCenter {
		get{
			return transform.position + Quaternion.Euler( 0, 45*(int)orientation, 0 ) * new Vector3( 0,0, platformSize.z / 2 + 0.5f );
		}
	}

	public static	RR_Station	FindByName( string name ) {
		var list = RR_Game.instance.stationList;

		foreach( var s in list ) {
			if( s.name == name ) return s;
		}
		return null;
	}

	void Awake() {
	}

	void Start() {
		RR_Game.instance.stationList.Add( this );

		model = new GameObject();
		model.name = "Model";
		if( model ) {
			var size = platformSize;

			Tk.SetParent( model, this.gameObject, false );
			model.transform.rotation = Quaternion.Euler( 0, 45*(int)orientation, 0 );

			model.AddComponent< RR_StationCollider >();

			var c = model.AddComponent< BoxCollider > ();
			c.size	 = size;
			c.center = new Vector3( 0, 0, size.z/2 + 0.5f );

			var cube = GameObject.CreatePrimitive( PrimitiveType.Cube );
			Tk.SetParent( cube, model, false );
			cube.transform.localScale = size;
			cube.transform.localPosition = 	new Vector3( 0,0, size.z/2 + 0.5f );

			var mr = cube.GetComponent<MeshRenderer>();
			mr.material.color = platformColor;

			Object.Destroy( cube.collider );

		}

		BuildStationTrack();
		AddToTiles();
	}

	void AddToTiles() {
		if( ! model ) return;
		
		var co = model.collider;
		if( ! co ) {
			Debug.LogError("no collider");
			return;
		}

		var size = platformSize;

		//----- 
		var corner = new Vector3( size.x / 2, 0, size.z );

		var radius 	 = corner.magnitude + 2;
		var radiusSq = radius * radius;

		//		var test = GameObject.CreatePrimitive( PrimitiveType.Sphere );
		//		Tk.SetParent( test, this.gameObject, false );
		//		test.transform.localScale = Vector3.one * corner.magnitude * 2;


		var sx = -corner.x * 2;
		var ex =  corner.x * 2;

		var sz = -corner.z * 2;
		var ez =  corner.z * 2;

		var ray = new Ray();
		ray.direction = Vector3.down;

		var rayOffset = new Vector3[4];

		var d = 0.45f;

		rayOffset[0] = transform.position + new Vector3(  d, 1000,  d );
		rayOffset[1] = transform.position + new Vector3( -d, 1000,  d );
		rayOffset[2] = transform.position + new Vector3(  d, 1000, -d );
		rayOffset[3] = transform.position + new Vector3( -d, 1000, -d );

		var hit = new RaycastHit();

		var map = RR_Map.instance;

		for( var z = sz; z <= ez; z++ ) {
			for( var x = sx; x <=ex; x++ ) {
				var pt = new Vector3( x, 0, z );
				if( pt.sqrMagnitude > radiusSq ) continue;

				bool b = false;
				foreach( var offset in rayOffset ) {
					ray.origin = pt + offset;

					if( co.Raycast( ray, out hit, float.PositiveInfinity ) ) {
						b = true;
						break;
					}
				}

				if( ! b ) continue;
				var wp = pt + transform.position;

				var tile = map.GetTileFromWorldPos( wp );
				if( tile != null ) {
					tile.building = this.gameObject;
				}
			}
		}			
			
	}

	void BuildStationTrack() {
		var n = (int)platformSize.x;
		if( RR.IsDirDiagonal( orientation ) ) {
			n = (int)( platformSize.x / TkMath.sqrt2 );
		}		

		var tilePos		= RR_Tile.WorldToTilePos( this.transform.position );
		var tileDir  	= RR.DirAdd( orientation,  2 );
		var tileOffset	= RR.DirToInt2( tileDir ) * (n/2 + 1);
		
		var map = RR_Map.instance;
		
		this.centerTile	= map.GetTile( tilePos );
		var startTile	= map.GetTile( tilePos + tileOffset );
		var endTile		= map.GetTile( tilePos - tileOffset );
		
		var t = RR_BuildTrackHandler.Create( startTile );
		t.SetEnd( endTile );
		t.SetStationTrack( this );
		t.DoConfirm();
		
		hud = RR_UI.LoadPanel< RR_StationHUD >();
		hud.SetStation( this );
	}

	void OnDrawGizmos() {
		Gizmos.matrix = Matrix4x4.TRS( transform.position, Quaternion.Euler( 0, 45*(int)orientation, 0 ), Vector3.one );

		var size = platformSize;

		var color = platformColor;
		color.a = 0.25f;
		Gizmos.color = color;
		Gizmos.DrawCube( new Vector3(0,0, size.z / 2 + 0.5f ), new Vector3( size.x, 0.95f, size.z ) );
		
		Gizmos.color = new Color(1,0,1,0.25f);
		Gizmos.DrawWireCube( new Vector3(0,0,0), new Vector3( size.x, 0.5f, 1 ) );
	}	

	public void OnClick() {
		RR_UI.SelectStation( this );
	}

	void FixedUpdate() {
		var cargoTypes = Tk.EnumValues< RR_CargoType >();
		foreach( var t in cargoTypes ) {
			var d = gameData[t];
			if( d.cap <= 0 ) continue;

			var st = GetCargoStatus( t );
			st.supply += d.rate * RR_Game.fixedDeltaYear;
			if( st.supply > d.cap ) {
				st.supply = d.cap;
			}
		}
	}
}
