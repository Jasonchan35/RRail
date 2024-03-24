using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//AStar Path Finding
public class RR_PathFinding {
	const int maxTestSteps = 1024 * 8;
	const int maxTurn = 1;

	public class Tile {
		public	RR_Tile	mapTile;

		public	int		G0;// local cost
		public	int		G; // total cost
		public	int		F;
		public	int		H; // heuristic
		public	Tile	prev;

		public	RR_Direction	dir;
		public	bool	tested;
		public	bool	opened;
		public	bool	closed;

		public	void	Reset() {
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

		public	int		trackCountInMap { get{ return mapTile.trackCount; } }
	}

	List<Tile>	openList   = new List<Tile>( maxTestSteps );
	List<Tile>	testedList = new List<Tile>( maxTestSteps );

	RR_Map		map;
	Tile 		startTile;
	Tile 		endTile;

	public	int totalTestedSteps = 0;

	static	Stack<Tile>	trashList;
	static	public	Tile	GetTile( RR_Tile t ) {
		if( t == null ) return null;
		if( t._pfTile == null ) {
			if( trashList == null ) {
				trashList = new Stack<Tile>( maxTestSteps );
			}

			var p = trashList.Count > 0 ? trashList.Pop() : new Tile();

			t._pfTile = p;
			p.mapTile = t;
			p.Reset();
		}
		return t._pfTile;
	}

	void AddTested( Tile t ) {
		if( t.tested ) return;
		t.tested = true;
		testedList.Add(t);
	}

	void Clear() {
		foreach( var p in testedList ) {
			p.mapTile._pfTile = null;
			p.Reset();
			trashList.Push( p );
		}		
		testedList.Clear();
		openList.Clear();
		totalTestedSteps = 0;
	}

	int	Heuristic( Tile a, Tile b ) {
		var d = TkMath.Abs( a.mapTile.pos - b.mapTile.pos );
		return d.x + d.y;
	}

	Tile NextTileFromOpenList() {
		var n = openList.Count;
		if( n == 0 ) return null;
		
		Tile t = null;
		int idx = -1;
		var minF = float.MaxValue;

		for( int i=0; i<n; i++ ) {
			var a = openList[i];

			if( a.F < minF ) {
				minF = a.F;
				idx = i;
				t = a;
			}
		}

		Tk.RemoveElementBySwapLast( openList, idx );
		return t;
	}

//-----------------
	public RR_TrackInTile[]	FindTrackPath( RR_Tile startMapTile, RR_Tile endMapTile ) {
		{
			var ret = _FindTrackPath( startMapTile, endMapTile );
			Clear();
			if( ret != null ) return ret;
		}

		#if true
		{ // try to find by swap start/end
			var ret = _FindTrackPath( endMapTile, startMapTile );
			Clear();
			if( ret != null ) return ret;
		}
		#endif

		return new RR_TrackInTile[0];
	}

	RR_TrackInTile[]	_FindTrackPath( RR_Tile startMapTile, RR_Tile endMapTile ) {
		startTile = GetTile( startMapTile );
		endTile   = GetTile( endMapTile   );

		if( startTile == null || endTile == null ) return null;
		if( startTile == endTile ) return null;
		if( startTile.mapTile.blocked || endTile.mapTile.blocked ) return null;

		map = RR_Map.instance;		if( map == null ) return null;

		AddTested( startTile );
		FindTrackPath_AStarStep( startTile );

		bool found = false;		
		for(;;) {
			var t = NextTileFromOpenList();
			if( t == null ) break;
			if( t == endTile ) {
				found = true;
				break;
			}
			FindTrackPath_AStarStep(t);
		}

//		Debug.Log("totalTestedSteps="+totalTestedSteps);

		RR_TrackInTile[]	tracks = null;

		if( found ) {
			var stack = new Stack< RR_TrackInTile >(256);
			var p = endTile;

			RR_TrackInTile prev = null;

			while( p != null ) {
				var t = new RR_TrackInTile();
				stack.Push( t );			

				t.mapTile	= p.mapTile;
				t.pos		= p.mapTile.pos;
				t.inDir 	= RR.DirOpposite( p.dir );
				t.outDir	= RR_Direction.None;

				if( prev != null ) {
					t.outDir = RR.DirOpposite( prev.inDir );
				}

				prev = t;
				p = p.prev;
			}
//			map.SetDebugTextureDirty();

			tracks = stack.ToArray();

			if( tracks.Length > 1 ) {
				var last = tracks.Length - 1;
				tracks[0].inDir     = startMapTile.CanConnectOrBranchDir( tracks[0].outDir );
				tracks[last].outDir =   endMapTile.CanConnectOrBranchDir( tracks[last].inDir );
			}
		}

		return tracks;
	}

	void FindTrackPath_AStarStep( Tile src ) {
		src.closed = true;		
		for( int i=0; i<8; i++ ) {
			FindTrackPath_AStarStep2( src, (RR_Direction) i );
		}
	}

	void FindTrackPath_AStarStep2( Tile src, RR_Direction dir ) {
		if( totalTestedSteps > maxTestSteps ) return;

		var opDir = RR.DirOpposite(dir);
		var tileOffset = RR.DirToInt2(dir);

		var diagonal = RR.IsDirDiagonal( dir );

		var dstMapTile = src.mapTile.GetNextTile( dir );
		if( dstMapTile == null ) return;
		if( dstMapTile == startTile.mapTile ) return;
		if( dstMapTile.blocked ) return;
		
		int turn = 0;
		int G0 = 10;

		if( diagonal ) { // if diagonal then block next to it must be non-blocked
			G0 += 4; // sqrt(2) = 1.414, so we add 4 to become 14
			{ // x
				var tmp = map.GetTile ( src.mapTile.pos + new TkVec2i( tileOffset.x, 0) );
				if( tmp == null || tmp.blocked ) return;	
				
				var tmpDir = RR.DirFrom( new TkVec2i( -tileOffset.x, tileOffset.y ) );
				if( tmp.GetTrackHasDir(tmpDir) != null ) {
					G0 += 40; // across another diagonal track
				}
			}
			{ // y
				var tmp = map.GetTile ( src.mapTile.pos + new TkVec2i( 0, tileOffset.y ) );
				if( tmp == null || tmp.blocked ) return;
				
				var tmpDir = RR.DirFrom( new TkVec2i( tileOffset.x, -tileOffset.y ) );
				if( tmp.GetTrackHasDir(tmpDir) != null ) {
					G0 += 40; // across another diagonal track
				}
			}
		}

		if( src == startTile && src.mapTile.trackInfo != null ) {
			if( src.mapTile.CanConnectDir(dir) == RR_Direction.None ) {
				if( src.mapTile.CanBranchDir(dir) == RR_Direction.None ) {
					return;
				}
			}
		}

		if( dstMapTile == endTile.mapTile && dstMapTile.trackInfo != null ) {
			if( dstMapTile.CanConnectDir(opDir) == RR_Direction.None ) {
				if( dstMapTile.CanBranchDir(opDir) == RR_Direction.None ) {
					return;
				}
			}
		}

		if( src.dir != RR_Direction.None ) {
			if( src.dir != dir ) {
				turn = RR.DirDiff( src.dir, dir );	
				if( Mathf.Abs(turn) > 1 ) return;
			}			
		}
		
		var existingTrackCount = 0;

		if( dstMapTile != endTile.mapTile ) {
			if( dstMapTile.trackInfo != null ) {
				foreach( var p in dstMapTile.trackInfo.tracks ) {
					if( p.IsEnd ) return;
					existingTrackCount++;
				}
			}
		}

		if( existingTrackCount >= 2 ) return;

		var dst = GetTile( dstMapTile );
		G0 += existingTrackCount * 20; // need more cost when across another track

		AddTested(dst);
		totalTestedSteps++;

		var G = src.G + G0;
		var H = Heuristic(dst,endTile);
		var F = G + H;
		
		if( dst.opened || dst.closed ) {
			if( dst.F <= F ) return;
		}
		
		if( ! dst.opened ) {
			dst.opened = true;
			openList.Add( dst );
		}
		
		dst.prev 	= src;
		dst.G0	 	= G0;
		dst.G 		= G;
		dst.H 		= H;
		dst.F 		= F;
		dst.dir		= dir;
	}

}



