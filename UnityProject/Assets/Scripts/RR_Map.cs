using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RR_Map : MonoBehaviour {
	const	float	RR_TerrainHeight = 32;
	const	int		tileGroupSize = 8;

	public	int				gameStartYear = 1800;
	public	RR_Game.Month	gameStartMonth;
	public	double			gameStartMoney;

	public	TkVec3i		mapZoneOffset;
	public	TkVec3i		mapZoneSize;

	private	Terrain		_terrain;
	public	Terrain		GetTerrain()		{ if( !_terrain ) { _terrain = GetComponent<Terrain>(); } return _terrain; }
	public	TerrainData	GetTerrainData() 	{ var t = GetTerrain(); return t ? t.terrainData : null; }

	private	int	_width;
	private	int	_height;

	public	int	width 	{ get{ return _width;  } }
	public	int	height	{ get{ return _height; } }

	const float	tileSize = 1.0f;

	public	Material	debugMaterial;
	public	Texture2D	debugTexture;

	[System.NonSerialized]
	public	RR_Tile[]	_tiles;

	public	RR_TileChunk[,]	tileChunks;

	public	RR_TileChunk GetTileChunk( TkVec2i pos ) {
		var s = pos / tileGroupSize;
		return tileChunks[ s.x, s.y ];
	}

	public	RR_Tile	GetTile( TkVec2i pos ) {
		if( pos.x < 0 || pos.x >= width  ) return null;
		if( pos.y < 0 || pos.y >= height ) return null;
		return _tiles[pos.y * width + pos.x];
	}
	
	static 			RR_Map	instance_;
	static	public	RR_Map	instance {
		get{
			if( instance_ == null ) {
				instance_ = Object.FindObjectOfType<RR_Map>();
			}
			return instance_; 		
		}
	}

	public	GameObject	TracksGroup {
		get{
			return Tk.GetOrAddGameObject( this.gameObject, "Tracks" );
		}
	}

	[System.NonSerialized]
	public	List< GameObject > 	trackList;

	void Awake() {
		trackList = new List<GameObject>();
		UpdateDebugTexture();
	}

	void Start() {
	}

	public	static Vector3	Vec3_NaN = new Vector3( float.NaN,  float.NaN,  float.NaN );

	public	bool	TerrainRaycast( Ray ray, out RaycastHit hit, float distance ) {
		hit = new RaycastHit();
		var t = GetTerrain();							if( !t ) return false;
		var c = t.GetComponent< TerrainCollider >();	if( !c ) return false;
		var b = c.Raycast( ray, out hit, distance );
		return b;
	}

	public	float	GetSampleSlope( Vector3 pt ) {
		var hit = new RaycastHit();
		var t = GetTerrain();								if( !t ) return 0;
		var c = t.GetComponent< TerrainCollider >();		if( !c ) return 0;

		var ray = new Ray( pt + new Vector3( 0, 10000, 0 ),  Vector3.down );
		if( ! c.Raycast( ray, out hit, float.PositiveInfinity ) ) {
			return 0;
		}

		return Vector3.Dot( hit.normal, Vector3.up );
	}

	public	Vector3	GUIToGroundPoint( Vector2 pt ) {
		var m = Camera.main;
		if( !m ) return Vec3_NaN;
		
		var ray = m.ScreenPointToRay( pt );

		var plane = new Plane( Vector3.up, new Vector3(0, RR_Tile.waterLevel, 0) );
		float dis;
		if( plane.Raycast( ray, out dis ) ) {
			return ray.GetPoint( dis );
		}
		return Vec3_NaN;
	}

	public	Vector3	GUIToTerrainPoint( Vector2 pt ) {
		var m = Camera.main;
		if( !m ) return Vec3_NaN;
		
		var ray = m.ScreenPointToRay( pt );
		
		var tn = GetTerrain();
		
		var tc = tn.GetComponent<TerrainCollider>();
		if( !tc ) return Vec3_NaN;
		
		
		var hit = new RaycastHit();
		if( tc.Raycast( ray, out hit, float.PositiveInfinity ) ) {
			return hit.point;
		}
		return Vec3_NaN;
	}


	public  RR_Tile GetTileFromGUIPoint( Vector2 pt ) {
		var groundPt = GUIToTerrainPoint(pt);
		if( float.IsNaN( groundPt.x ) ) return null;
		return GetTileFromWorldPos( groundPt );
	}

	public	RR_Tile	GetTileFromWorldPos( Vector3 pos ) {
		var tilePos = RR_Tile.WorldToTilePos( pos );		
		return GetTile ( tilePos );
	}

	public	string	Validate() {
		var terrain = GetTerrain();
		if( !terrain ) return "Terrain is missing";
		
		var td = GetTerrainData();
		if( !td ) return "Terrain Data is missing";

		var scale = td.heightmapScale;
		if( scale.x != 1 || scale.z != 1 ) {
			return "Terrain Width/Height must same as Heightmap Resolution-1";
		}

		if( scale.y != RR_TerrainHeight ) {
			return "Terrain scale must = " + RR_TerrainHeight;
		}

		if( td.heightmapWidth < 1 || td.heightmapHeight < 1 ) {
			return "Heightmap size < 1";
		}

		var w = td.heightmapWidth-1;
		var h = td.heightmapHeight-1;

		if( td.alphamapWidth != w || td.alphamapHeight != h ) {
			return "Control Texture Resolution must same as Heightmap resolution-1";
		}

		return "";
	}

	public	void UpdateHeightMap() {
		var td = GetTerrainData();			if( !td ) return;

		var w = td.heightmapWidth;
		var h = td.heightmapHeight;

		var map = td.GetHeights(0,0,w,h);

		for( int y=0; y<h; y++ ) {
			for( int x=0; x<w; x++ ) {
				map[y,x] = Mathf.Floor( map[y,x] * RR_TerrainHeight ) / RR_TerrainHeight;
			}
		}

		td.SetHeights( 0,0, map );
	}

	public	void UpdateTileTypes() {
//		UpdateHeightMap();

		var terrain = GetTerrain();		if( !terrain ) return;
		var td = GetTerrainData();		if( !td ) return;

		var w = td.alphamapWidth;
		var h = td.alphamapHeight;

		_width  = w;
		_height = h;

		if( _tiles == null || _tiles.Length != (w*h) ) {
			_tiles = new RR_Tile[w*h];
		}

		{
			var nw = w/tileGroupSize + 1;
			var nh = h/tileGroupSize + 1;

			tileChunks = new RR_TileChunk[nw,nh];
			for( int y=0; y<nh; y++ ) {
				for( int x=0; x<nw; x++ ) {
					var o = new GameObject("X" + x + "_Y" + y);
					o.transform.parent = RR_TileChunk.group.transform;
					tileChunks[ x,y ] = o.AddComponent< RR_TileChunk >();
				}
			}
		}
		
		var mapLayers = td.alphamapLayers;
		var map 	  = td.GetAlphamaps(0,0,w,h);

		for( var y=0; y<h;y++ ) {
			for( var x=0; x<w; x++ ) {
				var idx = y*w + x;
				if( _tiles[idx] == null ) _tiles[idx] = new RR_Tile();
				var tile = _tiles[idx];

				tile.Init( new TkVec2i(x,y) );

				tile.worldY = terrain.SampleHeight( tile.worldPos );
				tile.slope	= GetSampleSlope( tile.worldPos );

				var wy = tile.worldY-0.5f;

				if( wy < RR_Tile.waterLevel  ) {
					tile.type = RR_Tile.Type.water;

				}else if( wy >= RR_Tile.mountainLevel ) {
					tile.type = RR_Tile.Type.mountain;

				}else if( tile.slope < TkMath.cos15 ) {
					tile.type = RR_Tile.Type.cliff;

				}else{
					int detailType = 0;
					tile.type = RR_Tile.Type.plain;

					for( int i=1; i<mapLayers; i++ ) {
						var s = map[y,x,i];
						if( s > 0.5f ) {
							tile.type = (RR_Tile.Type)( (int)RR_Tile.Type.plain + i );
							detailType = i;
						}				
					}

					for( int i=0; i<mapLayers; i++ ) {
						map[y,x,i] = ( i == detailType ) ? 1 : 0;
					}
				}
			}
		}

//		td.SetAlphamaps( 0,0, map );
	}

	public	void UpdateDebugTexture() {
		transform.localPosition = new Vector3( -0.5f, 0, -0.5f );

		UpdateTileTypes();
		if( _tiles == null ) return;

		if( ! debugMaterial ) {
			Debug.LogWarning("debugMaterial is missing");

			var mat = Tk.LoadMaterial("Materials/RR_TerrainEdit");
			debugMaterial = new Material( mat );
			debugMaterial.name = mat.name + "(Clone)";

			return;
		}

		if( debugTexture ) {
			Tk.Destroy( debugTexture );
		}
		debugTexture = new Texture2D( width, height, TextureFormat.RGBA32, true, false );
		debugTexture.hideFlags  = HideFlags.DontSave;
		debugTexture.filterMode = FilterMode.Point;
		
		var pixels	 = new Color32[ width * height ];
		
		int i=0;
		foreach( var t in _tiles ) {
			var c = new Color(0,0,0,1);
			/*
			if( t.blocked ) {
				c.r = 255;
			}else if( t.pfTested ) {
				c.b = 100;
			}
			*/
			//			c.r = (byte)( i % 256 );
			//			c.g = (byte)( i / 256 );

			var dy = 1 - (float) ( (int) t.worldY - RR_Tile.waterLevel ) / (RR_Tile.mountainLevel - RR_Tile.waterLevel);
			dy = dy * 0.5f + 0.5f;

			switch( t.type ) {
			case RR_Tile.Type.water: 	{ c = new Color( .0f, .3f, .5f ); }break;
			case RR_Tile.Type.mountain: { c = new Color( .15f, .1f, 0f ); }break;

			case RR_Tile.Type.cliff: 	{ c = new Color( .4f, .4f, .3f ) * dy; }break;

			case RR_Tile.Type.plain:	{ c = new Color( .4f, .7f, .4f ) * dy; }break;
			case RR_Tile.Type.forest:	{ c = new Color(  0,   .5f,  0 ) * dy; }break;
			case RR_Tile.Type.derset:	{ c = new Color( .9f,  .8f,  0 ) * dy; }break;
			case RR_Tile.Type.other2:	{ c = new Color( .7f,  0,   .7f) * dy; }break;
			}

//			c.a = (byte) ( ( y/RR_TerrainHeight ) * 255 );
			pixels[i] = c;
			i++;
		}
		
		debugTexture.SetPixels32( pixels );
		debugTexture.Apply(true);

		var terrain = GetTerrain();
		if( terrain ) {
			debugMaterial.SetTexture("_DebugTex", debugTexture );
			terrain.materialTemplate = debugMaterial;
		}
	}

	public	void	ShowGridLine( bool b ) {
		var t = GetTerrain();
		if( !t ) return;
		t.materialTemplate.SetFloat( "_GridLine", b?1:0 );
	}

	public	void	ShowDebugTexture( bool b ) {
		var t = GetTerrain();
		if( !t ) return;
		t.materialTemplate.SetFloat( "_DebugColor", b?1:0 );
	}

	void OnDrawGizmos() {
		var offset = ( mapZoneOffset + mapZoneSize / 2 ).ToVector3() - Vector3.one * 0.5f;
		var size   = mapZoneSize.ToVector3();

		offset.y = 16;

		Gizmos.color = Color.white;
		for( int i=0; i<=4; i++ ) {
			size.y = i * 8;
			Gizmos.DrawWireCube( offset, size );
		}
	}
}
