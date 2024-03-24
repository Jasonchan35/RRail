using UnityEngine;
using System.Collections;

[System.Serializable]
public class RR_TrackInTile {
	public RR_TrackInTile() {
		rf.Reset();
	}

	public	bool			doubleTrack;
	public	RR_Direction	inDir;
	public	RR_Direction	outDir;
	public	TkVec2i			pos;

	public	void	CorrectDirection() {
		bool b = inDir != RR_Direction.None;

		switch( b ? inDir : outDir  ) {
		case RR_Direction.E:
		case RR_Direction.NE:
		case RR_Direction.N:
		case RR_Direction.NW:
			b = !b;
			break;
		}

		if( b ) {
			var tmp = inDir;
			inDir = outDir;
			outDir = tmp;
		}
	}

	[System.NonSerialized]	public	GameObject		model;
	[System.NonSerialized]	public	Vector3[]		trackA;
	[System.NonSerialized]	public	Vector3[]		trackB; // 2nd track in double track

							public	Vector3			trackA_midPoint { get{ return trackA[ trackA.Length / 2 ]; } }
							public	Vector3			trackB_midPoint { get{ return trackB[ trackB.Length / 2 ]; } }


	[System.NonSerialized]	public	int					indexInTile;
	[System.NonSerialized]	public	RR_Tile				mapTile;
	[System.NonSerialized]	public	RR_Tile.TrackInfo	info; // info shared for all track in this mapTile
	[System.NonSerialized]	public	RouteFindingInfo	rf;

	public	bool			HasDir		( RR_Direction dir ) { return inDir == dir || outDir == dir; }
	public	bool			HasDirPair	( RR_Direction dir1, RR_Direction dir2 ) { 
		if( inDir == dir1 && outDir == dir2 ) return true;
		if( inDir == dir2 && outDir == dir1 ) return true;
		return false;
	}
	public	bool			IsEnd { get{ return ( inDir == RR_Direction.None || outDir == RR_Direction.None ); } }
	public	RR_Direction	GetEndDir() {
		if( inDir  == RR_Direction.None ) return outDir;
		if( outDir == RR_Direction.None ) return inDir;
		return RR_Direction.None;
	}

	public	struct RouteFindingInfo {
		public	int		G0;// local cost
		public	int		G; // total cost
		public	int		F;
		public	int		H; // heuristic

		public	RR_TrackInTile	prev;
		public	RR_Direction	dir;

		public	bool	tested;
		public	bool	opened;
		public	bool	closed;

		public void Reset() {
			G = 0;
			//			H = Heuristic(t,endTile);
			//			F = t.pf.G + t.pf.H;
			opened 	= false;
			closed 	= false;
			prev 	= null;
			G0	   	= 0;
			dir		= RR_Direction.None;
			tested 	= false;
		}
	}


	public	GameObject	LoadMesh() {
		if( model ) {
			Tk.Destroy( model );
		}

		var ry = 0.0f;
		var	pos = mapTile.worldPos;
		var diagonal = true;

		var prefabName = "";

		if( inDir == RR_Direction.None ) {
			prefabName = "Half";
			diagonal = RR.IsDirDiagonal(outDir);
			ry = (int)outDir/2 * 90;
			
		}else if( outDir == RR_Direction.None ) {
			prefabName = "Half";
			diagonal = RR.IsDirDiagonal(inDir);
			ry = (int)inDir/2 * 90;
			
		}else{
			var t = RR.DirDiff( inDir, RR.DirOpposite(outDir) );
			if( t<0 ) {
				prefabName = "Left";
			}else if( t>0 ) {
				prefabName = "Right";
			}else{ // t == 0
				prefabName = "Full";
			}
			diagonal = RR.IsDirDiagonal(inDir);
			ry = (int)inDir/2 * 90;
		}

		if( doubleTrack ) {
			prefabName = "_AutoGen_/TrackSet/Double/DoubleTrack_" + ( diagonal ? "Dia" : "" ) + prefabName;
		}else{
			prefabName = "_AutoGen_/TrackSet/Single/SingleTrack_" + ( diagonal ? "Dia" : "" ) + prefabName;
		}

		var go = Tk.LoadGameObject( prefabName );
		if( !go ) return null;
		
		go.name = go.name + "_" + outDir.ToString();
//		go.transform.parent = parent.transform;
		go.transform.rotation = Quaternion.Euler(0,ry,0);
		go.transform.position = pos + new Vector3(0,.1f,0);

		UpdateJoints( go );

		model = go;

		return go;
	}

	public	void UpdateJoints( GameObject go ) {
		var map 	= RR_Map.instance;						if( ! map ) return;
		var terrain = map.GetTerrain();						if( ! terrain ) return;
		var m 		= go.GetComponent< RR_TrackModel >();	if( ! m ) return;
		
		trackA = _UpdateJointSet( terrain, m.jointsA );
		trackB = _UpdateJointSet( terrain, m.jointsB );
	}

	Vector3[] _UpdateJointSet( Terrain terrain, GameObject[] joints ) {
		if( joints == null ) return new Vector3[0];

		var path = new Vector3[joints.Length];

		int i = 0;
		foreach( var jt in joints ) {
			var pt0 = jt.transform.position;
			var pt1 = jt.transform.TransformPoint( new Vector3(0,0,0.1f) );

			pt0.y = terrain.SampleHeight( pt0 );
			pt1.y = terrain.SampleHeight( pt1 );

			jt.transform.position = pt0;
			jt.transform.rotation = Quaternion.LookRotation( pt1 - pt0 );

			path[i] = pt0;
			i++;
		}
		return path;
	}

	public override string ToString() {
		return "Track[" + pos + " " + inDir + " " + outDir + "]";
	}

	
	public void OnDrawGizmos() {
		var pos = mapTile.worldPos;
		
		pos.y += indexInTile * 0.2f;
		
		if( inDir != RR_Direction.None ) {
			Gizmos.color = Color.red;
			Gizmos.DrawRay( pos, RR.DirToVector3( inDir ) * 0.4f );
		}
		
		if( outDir != RR_Direction.None ) {
			Gizmos.color = Color.green;
			Gizmos.DrawRay( pos, RR.DirToVector3( outDir) * 0.4f );
		}

		//		DrawGizmos_TrainLock();
	}

	public	void DrawGizmos_TrainLock( bool selected, bool useTrackA=true, bool useTrackB=true ) {
		var scale = 0.1f;
		if( selected ) {
			Gizmos.color = Color.magenta;
			scale = 0.15f;
		}else{
			Gizmos.color = new Color( 0.25f, 0.55f, 0.55f );
		}

//		Gizmos.color = Color.green;
//		TkGizmos.DrawLines( trackA );
//		Gizmos.color = Color.red;
//		TkGizmos.DrawLines( trackB );

		if( useTrackA && info.trackLockA != null ) {
			var t = info.trackLockA;
			Gizmos.DrawCube( t.trackA_midPoint, Vector3.one * scale );
		}
		
		if( useTrackB && info.trackLockB != null ) {
			var t = info.trackLockB;
			Gizmos.DrawCube( t.trackB_midPoint,  Vector3.one * scale );
		}
	}
}



