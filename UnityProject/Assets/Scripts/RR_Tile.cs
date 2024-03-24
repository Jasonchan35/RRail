using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RR_Tile {
	public	float		slope;

	public	TkVec2i		pos;
	public	float		worldY;
	public	Vector3		worldPos { get{ return TileToWorldPos(pos,worldY); } }

	public	const	int	waterLevel = 8;
	public	const	int mountainLevel = 16;

	public	GameObject		building;

	// Seperate property for track into this class to save memory, since not all tile contain tracks
	[System.Serializable]
	public	class TrackInfo {
		public	bool					doubleTrack;

		public	List< RR_TrackInTile >	tracks = new List<RR_TrackInTile>();
		public	RR_Station				station;

		public	RR_TrackInTile			trackLockA;
		public	RR_Train				trainLockA;

		public	RR_TrackInTile			trackLockB;
		public	RR_Train				trainLockB;

		readonly	TkVec2i	debugTileLock = new TkVec2i(-1,-1); //new TkVec2i(214,162);

		public	bool	Unlock	( RR_Train train, RR_TrackInTile track, bool useTrackA ) {
			if( ! doubleTrack ) {
				useTrackA = true;	//always use path A for single track
			}

			if( useTrackA ) {
				if( trackLockA == track && trainLockA == train ) {
					trackLockA = null;
					trainLockA = null;

					if( track.mapTile.pos == debugTileLock ) {
						Debug.Log( "TrackA Unlock " + track + " train " + train.name );
					}
					return true;
				}else{
					var another = trainLockA == null ? "null" : trainLockA.name;
					Debug.LogError("TrackA " + track + " is not locked by train " + train.name + " but " + another );
				}
			}else{
				if( trackLockB == track && trainLockB == train ) {
					trackLockB = null;
					trainLockB = null;
	
					if( track.mapTile.pos == debugTileLock ) {
						Debug.Log( "TrackB Unlock " + track + " train " + train.name );
					}
					return true;
				}else{
					var another = trainLockB == null ? "null" : trainLockB.name;
					Debug.LogError("TrackB " + track + " is not locked by train" + train.name + " but " + another );
				}
			}
			return false;
		}

		bool	CheckIntersect( RR_TrackInTile a, RR_TrackInTile b ) {
			
			var inDiff  = RR.DirDiff( a.inDir,  b.inDir  );
			var outDiff = RR.DirDiff( b.outDir, a.outDir );

			if( RR_Game.instance.leftHandedTrack ) {
				if( inDiff  > 0 ) return true;
				if( outDiff > 0 ) return true;
			}else{
				if( inDiff  < 0 ) return true;
				if( outDiff < 0 ) return true;
			}
			return false;
		}

		// return prev locked train
		public	RR_Train	Lock	( RR_Train train, RR_TrackInTile track, bool useTrackA ) {
			if( ! doubleTrack ) {
				useTrackA = true;	//always use path A for single track
			}

			if( useTrackA ) {
				if( trackLockA != null ) {
					if( trainLockA == null ) Debug.LogError("trackLock / trainLock pair invalid");
					return trainLockA;
				}

				if( trackLockB != null ) {
					if( trainLockB == null ) Debug.LogError("trackLock / trainLock pair invalid");
					if( CheckIntersect( track, trackLockB ) ) {
						return trainLockB;
					}
					if( track.mapTile.pos == debugTileLock ) {
						Debug.Log("TrackA Check Intersect OK A=" + track + " B=" + trackLockB );
					}
				}

				trackLockA	= track;
				trainLockA	= train;
			
				if( track.mapTile.pos == debugTileLock ) {
					Debug.Log( "TrackA Lock   " + track + " train " + train.name );
				}
				return null;

			}else{
				if( trackLockB != null ) {
					if( trainLockB == null ) Debug.LogError("trackLock / trainLock pair invalid");
					return trainLockB;
				}

				if( trackLockA != null ) {
					if( trainLockA == null ) Debug.LogError("trackLock / trainLock pair invalid");
					if( CheckIntersect( trackLockA, track ) ) {
						return trainLockA;
					}

					if( track.mapTile.pos == debugTileLock ) {
						Debug.Log("TrackB Check Intersect OK A=" + trackLockA + " B=" + track );
					}
				}

				trackLockB	= track;
				trainLockB	= train;

				if( track.mapTile.pos == debugTileLock ) {
					Debug.Log( "TrackB Lock   " + track + " train " + train.name );
				}

				return null;
			}
		}

		public	RR_TrackInTile	GetTrackHasDir		( RR_Direction dir ) {
			foreach( var p in tracks ) {
				if( p.HasDir( dir ) ) return p;
			}
			return null;
		}

		public	RR_TrackInTile	GetTrackHasDirPair	( RR_Direction dir1, RR_Direction dir2 ) {
			foreach( var p in tracks ) {
				if( p.HasDirPair( dir1, dir2 ) ) return p;
			}
			return null;
		}
	}

	public	TrackInfo				trackInfo;
	public	RR_Station				trackStation 	{ get { return trackInfo == null ? null : trackInfo.station; } }

	public	RR_Tile	GetNextTile( RR_Direction dir ) {
		if( dir == RR_Direction.None ) return null;
		var map = RR_Map.instance;
		return map.GetTile( pos + RR.DirToInt2(dir) );
	}

	public void AddTrack( RR_TrackInTile newTrack, RR_Direction connectDir ) {
		newTrack.pos = this.pos;

		// connect end
		if( connectDir != RR_Direction.None && trackInfo != null ) {
			var opConnectDir = RR.DirOpposite( connectDir );
		
		// try to connect to another end
			var connected = false;

			if( trackInfo != null ) {
				foreach( var p in trackInfo.tracks ) {
					if( p.IsEnd ) {
						if( p.inDir != RR_Direction.None ) {
							if( RR.DirDiffAbs( opConnectDir, p.inDir ) <= 1 ) {
								p.outDir = connectDir;
								connected = true;
							}
						}else if( p.outDir != RR_Direction.None ) {
							if( RR.DirDiffAbs( opConnectDir, p.outDir ) <= 1 ) {
								p.inDir = connectDir;
								connected = true;
							}
						}
					}
				}
			}

			if( connected ) {
				return; // connected so don't have to append to the list
			}
		}

	// no connect, no branch, so append to the list
		_AddTrack( newTrack );
	}

	public	void	_AddTrack( RR_TrackInTile newTrack ) {
		newTrack.mapTile = this;
		newTrack.CorrectDirection();


		if( trackInfo != null ) {
			if( trackInfo.doubleTrack != newTrack.doubleTrack ) {
				Debug.LogError("only can be single/double track in one tile");
			}

			foreach( var p in trackInfo.tracks ) {
				if( p.HasDirPair( newTrack.inDir, newTrack.outDir ) ) {
					return; // already has the same track
				}
			}
		}else{
			trackInfo = new TrackInfo();
			trackInfo.doubleTrack = newTrack.doubleTrack;
		}
		
		newTrack.info = trackInfo;
		newTrack.LoadMesh();

		newTrack.indexInTile = trackInfo.tracks.Count;
		trackInfo.tracks.Add( newTrack );
		
		var chunck = RR_Map.instance.GetTileChunk( this.pos );
		chunck.Add( newTrack );		

		RR_Game.instance.saveData.tracks.Add( newTrack );
	}

	public	int	trackCount {
		get{ return ( trackInfo == null ) ? 0 : trackInfo.tracks.Count; }
	}

	public	RR_TrackInTile GetTrackHasDir( RR_Direction dir ) {
		if( trackInfo == null ) return null;
		return trackInfo.GetTrackHasDir( dir );
	}

	public	RR_TrackInTile GetTrackHasDirPair( RR_Direction dir1, RR_Direction dir2 ) {
		if( trackInfo == null ) return null;
		return trackInfo.GetTrackHasDirPair( dir1, dir2 );
	}

	public RR_Direction	CanConnectDir( RR_Direction dir ) {
		if( trackInfo == null ) return RR_Direction.None;
		var op = RR.DirOpposite(dir);

		foreach( var p in trackInfo.tracks ) {
			if( p.IsEnd ) {
				if( p.inDir  != RR_Direction.None && RR.DirDiffAbs(op,p.inDir ) <= 1 ) return p.inDir;
				if( p.outDir != RR_Direction.None && RR.DirDiffAbs(op,p.outDir) <= 1 ) return p.outDir;
			}
		}		
		return RR_Direction.None;
	}

	public RR_Direction	CanBranchDir( RR_Direction dir ) {
		if( trackInfo == null ) return RR_Direction.None;
		var op = RR.DirOpposite(dir);

		foreach( var p in trackInfo.tracks ) {
			if( ! p.IsEnd ) {
				if( RR.DirDiffAbs(op,p.inDir ) <= 1 ) return p.inDir;
				if( RR.DirDiffAbs(op,p.outDir) <= 1 ) return p.outDir;
			}
		}		
		return RR_Direction.None;
	}

	public RR_Direction CanConnectOrBranchDir( RR_Direction dir ) {
		var ret = CanConnectDir(dir);
		if( ret != RR_Direction.None ) return ret;
		return CanBranchDir(dir);	
	}

	public	enum Type {
		none,
		water,
		mountain,
		cliff,
		plain,
		forest,
		derset,
		other2,
	}

	public	Type	type;


	[System.NonSerialized]
	public	RR_PathFinding.Tile	_pfTile;

	public	bool	blocked {
		get{
			if( trackStation ) return true;
			if( building ) return true;

			if( trackCount >= 2 ) return true;

			switch( type ) {
			case Type.plain:	return false;
			case Type.forest:	return false;
			case Type.derset:	return false;
			case Type.other2:	return false;
			}
			return true;
		}
	}

	public void Init( TkVec2i pos ) {
		this.pos = pos;
	}

	static public Vector3	TileToWorldPos( TkVec2i pos, float y = 0 ) {
		return new Vector3( pos.x, y, pos.y );
	}

	static public TkVec2i	WorldToTilePos( Vector3 pos ) {
		return new TkVec2i( Mathf.RoundToInt( pos.x ), Mathf.RoundToInt( pos.z ) );
	}
}
