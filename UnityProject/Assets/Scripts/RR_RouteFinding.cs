using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RR_RouteFinding {
	const int kUTurnExtraDistance = 12 * 10;
	const int kMaxTestSteps = 1024 * 1;

	List<RR_TrackInTile>	openList   = new List<RR_TrackInTile>( kMaxTestSteps );
	List<RR_TrackInTile>	testedList = new List<RR_TrackInTile>( kMaxTestSteps );

	RR_Tile	startMapTile;
	RR_Tile	endMapTile;

	RR_TrackInTile	endTrack;
	public	int totalTestedSteps = 0;

	int	Heuristic( RR_Tile a, RR_Tile b ) {
		var d = TkMath.Abs( a.pos - b.pos );
		return d.x + d.y;
	}

	void ClearTestedList() {
		foreach( var p in testedList ) {
			p.rf.Reset();
		}
		testedList = null;
	}
	
	void AddTested( RR_TrackInTile t ) {
		if( t.rf.tested ) return;
		t.rf.tested = true;
		testedList.Add(t);
	}


	RR_TrackInTile NextFromOpenList() {
		var n = openList.Count;
		if( n == 0 ) return null;
		
		RR_TrackInTile t = null;
		int idx = -1;
		var minF = float.MaxValue;
		
		for( int i=0; i<n; i++ ) {
			var a = openList[i];
			
			if( a.rf.F < minF ) {
				minF = a.rf.F;
				idx = i;
				t = a;
			}
		}
		
		Tk.RemoveElementBySwapLast( openList, idx );
		return t;
	}

	Stack<RR_TrackInTile> tempExtraTrackStack = new Stack<RR_TrackInTile>(20);

	bool GetExtraTrackForUTurn( Stack< RR_TrackInTile > stack, RR_Tile tile, RR_Direction inDir, int neededDistance ) {
		var opDir = RR.DirOpposite( inDir );

		var dis = RR.IsDirDiagonal( inDir ) ? 14 : 10;

		if( tile.trackInfo == null ) return false;

		foreach( var p in tile.trackInfo.tracks ) {
			if( p.IsEnd ) continue;

			var nextDir = RR_Direction.None;
			if( p.inDir == opDir ) {
				nextDir = p.outDir;

			}else if( p.outDir == opDir ) {
				nextDir = p.inDir;

			}else{
				continue;
			}

			stack.Push( p );
			if( dis >= neededDistance ) return true;

			var nextTile = tile.GetNextTile( nextDir );
			if( nextTile != null ) {
				if( GetExtraTrackForUTurn( stack, nextTile, nextDir, neededDistance - dis ) ) {
					return true;
				}
			}

			stack.Pop();
		}
		return false;
	}

	void GetExtendTrackForStation( Stack< RR_RoutePath.Node > stack, RR_Tile tile, RR_Direction dir ) {
		if( dir == RR_Direction.None ) {
			Debug.LogError("GetExtendTrackForStation dir == None");
			return;
		}

		if( tile.trackInfo == null ) return;
		var s = tile.trackInfo.station;
		if( s == null ) return;

		var t = tile.GetNextTile( dir );

		while( t != null ) {
			if( t.trackCount == 0 ) break;
			if( t.trackStation != s ) break;

			var q = new RR_RoutePath.Node();
			q.track = t.trackInfo.tracks[0];
			stack.Push( q );

			t = t.GetNextTile( dir );
		}
	}


	public RR_RoutePath FindRoutePath( RR_Tile startMapTile, RR_Tile endMapTile ) {
		var ret = _FindRoutePath( startMapTile, endMapTile );
		ClearTestedList();
		return ret;
	}
	
	RR_RoutePath _FindRoutePath( RR_Tile startMapTile, RR_Tile endMapTile ) {
		if( startMapTile == null || endMapTile == null ) return null;
		var map = RR_Map.instance;		if( map == null ) return null;

		if( startMapTile.trackInfo == null ) return null;
		if( endMapTile.trackInfo   == null ) return null;

		if( startMapTile == endMapTile ) return null;

		this.startMapTile	= startMapTile;
		this.endMapTile		= endMapTile;

		{
			if( startMapTile.trackInfo != null ) {
				foreach( var p in startMapTile.trackInfo.tracks ) {
					openList.Add( p );
					AddTested( p );
				}
			}
		}

		bool found = false;		
		for(;;) {
			var t = NextFromOpenList();
			if( t == null ) break;
			if( t.mapTile == endMapTile ) {
				found = true;
				endTrack = t;
				break;
			}
			FindRoutePath_AStarStep(t);
		}


		if( found ) {
			var stack = new Stack< RR_RoutePath.Node >(256);
			var p = endTrack;

			var stationTrack = new Stack< RR_RoutePath.Node >( 32 );
			GetExtendTrackForStation( stationTrack, p.mapTile,  p.rf.dir );

			foreach( var t in stationTrack ) {
				stack.Push( t );
			}

			var station = startMapTile.trackInfo.station;
			var tryToFindStationStart = ( station != null );

			for( ; p != null; p = p.rf.prev ) {
				{
					var q = new RR_RoutePath.Node();
					q.track = p;

					if( tryToFindStationStart ) {
						if( p.mapTile.trackStation == station ) {
							q.checkPointType = RR_RoutePath.CheckPointType.StationStart;
							tryToFindStationStart = false;
						}
					}

					stack.Push( q );
				}

				var prev = p.rf.prev;
				if( prev != null && prev.rf.dir ==  RR.DirOpposite( p.rf.dir ) ) { //U-Turn

//					stack.Peek().checkPointType = RR_RoutePath.CheckPointType.UTurnOut;

					tempExtraTrackStack.Clear();
					if( GetExtraTrackForUTurn( tempExtraTrackStack, prev.mapTile, prev.rf.dir, kUTurnExtraDistance ) ) {

						var arr = tempExtraTrackStack.ToArray();
						System.Array.Reverse( arr );

						var t0 = true;
						foreach( var t in  arr ) {
							var q = new RR_RoutePath.Node();
							q.track = t;
							stack.Push( q );

							if( t0 ) {
								t0 = false;
								q.checkPointType = RR_RoutePath.CheckPointType.UTurnOut;
							}
						}


						System.Array.Reverse( arr );

						t0 = true;
						foreach( var t in  arr ) {
							var q = new RR_RoutePath.Node();
							q.track = t;
							stack.Push( q );

							if( t0 ) {
								t0 = false;
								q.checkPointType = RR_RoutePath.CheckPointType.UTurnIn;
							}
						}

						stack.Pop();

					}else{
						Debug.LogError("No Extra Track Bug ?? " + prev.mapTile.pos + " " + p.rf.dir );
					}
				}
			}

			if( stack.Count >= 2 ) {
				var last = stack.Pop();
				p = stack.Peek().track;
				stack.Push( last );

				GetExtendTrackForStation( stack, last.track.mapTile, RR.DirOpposite( p.rf.dir ) );

				var output = stack.ToArray();
				int c = 0;
				foreach( var o in output ) {
					o.index = c;
					c++;
				}

				Tk.SafeGetLastElement( output, 0 ).checkPointType = RR_RoutePath.CheckPointType.StationEnd;
				return new RR_RoutePath( output );
			}
		}
		
		return null;
	}

	
	void FindRoutePath_AStarStep( RR_TrackInTile src ) {
		src.rf.closed = true;

		if( src.rf.dir == RR_Direction.None ) {
			for( int i=0; i<8; i++ ) {
				FindRoutePath_AStarStep2( src, (RR_Direction) i );
			}
		}else{
			var opDir = RR.DirOpposite( src.rf.dir );

			if( src.inDir == opDir ) {
				FindRoutePath_AStarStep2( src, src.outDir );

			}else if( src.outDir == opDir ) {
				FindRoutePath_AStarStep2( src, src.inDir  );

			}else{
				Debug.LogError("invalid dir in track route finding");
				//bug ??
			}

			FindRoutePath_AStarStep2( src, opDir, true ); // U turn
		}
	}

	void FindRoutePath_AStarStep2( RR_TrackInTile src, RR_Direction dir, bool uTurn = false ) {
		var dstMapTile = src.mapTile.GetNextTile( dir );
		if( dstMapTile == null ) return;
		if( dstMapTile == startMapTile ) return;
		
		if( dstMapTile.trackInfo == null ) return; 

		var op_dir = RR.DirOpposite( dir );

		if( dstMapTile.trackInfo == null ) return;

		foreach( var dst in dstMapTile.trackInfo.tracks ) {
			if( dst.IsEnd ) continue;
			if( ! dst.HasDir( op_dir ) ) continue;
			
			FindRoutePath_AStarStep3( src, dst, dir, uTurn );
		}
	}
	
	void FindRoutePath_AStarStep3( RR_TrackInTile src, RR_TrackInTile dst, RR_Direction dir, bool uTurn ) {
		if( totalTestedSteps > kMaxTestSteps ) return;

		var G0 = RR.IsDirDiagonal(dir) ? 14 : 10;

		if( uTurn ) {
			//valid extra track exists
			tempExtraTrackStack.Clear();

			var opDir = RR.DirOpposite( dir );
			if( ! GetExtraTrackForUTurn( tempExtraTrackStack, src.mapTile, opDir, kUTurnExtraDistance ) ) {
				return;
			}

			G0 += kUTurnExtraDistance * 2 + 50; // (extra_forward + extra_backward) + turn_over_time
		}
		
		AddTested( dst );
		totalTestedSteps++;		

		var G = src.rf.G + G0;
		var H = Heuristic( dst.mapTile, endMapTile );
		var F = G + H;
		
		if( dst.rf.opened || dst.rf.closed ) {
			if( dst.rf.F <= F ) return;
		}
		
		if( ! dst.rf.opened ) {
			dst.rf.opened = true;
			openList.Add( dst );
		}

		dst.rf.prev 	= src;
		dst.rf.G0	 	= G0;
		dst.rf.G 		= G;
		dst.rf.H 		= H;
		dst.rf.F 		= F;
		dst.rf.dir		= dir;
	}

}
