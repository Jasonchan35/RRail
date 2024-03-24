using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class RR_RoutePath {
	public	enum CheckPointType {
		None,
		StationStart,
		StationEnd,
		UTurnIn,
		UTurnOut,
	}


	[System.Serializable]
	public class Node {
		public	int					index;
		public	RR_TrackInTile		track;
		public	bool				useTrackA;
		public	int					pointIndex;
		public	CheckPointType		checkPointType;

		public	Vector3		worldPos {
			get{ return useTrackA ? track.trackA_midPoint : track.trackB_midPoint; }
		}

		public	RR_Train	Lock( RR_Train train ) {
			return track.info.Lock( train, track, useTrackA );
		}

		public	bool	Unlock( RR_Train train ) {
			return track.info.Unlock( train, track, useTrackA );
		}
	}

	public	Node[]			nodes;
	public	Node[]			checkPointNodes;

	public	Vector3[]		points;
	public	float[]			pointDistance;

	public	int				stationStartIndex;
	public	int				stationEndIndex;

	public	int	FindForwardNode( float posOnPath, int fromNode ) {
		var pi = Mathf.FloorToInt( posOnPath );

		for( int i=fromNode; i<nodes.Length; i++ ) {
			if( pi > nodes[i].pointIndex ) continue;
			return i;
		}
		return nodes.Length - 1;
	}

	public	int		GetOffsetIndex( float posOnPath, float distance ) {
		int 	i = Mathf.FloorToInt( posOnPath );
		int		n = points.Length;

		if( i >= n-1 ) return n - 1;

		if( distance >= 0 ) {

			float	f = posOnPath - i;
			float 	d = pointDistance[i] * (1-f);

			i++;
			for( ;; i++ ) {
				if( i >= n-1 ) return n - 1;

				d += pointDistance[i];
				if( d >= distance ) return i;
			}
		}else{
			float	f = posOnPath - i;
			float 	d = pointDistance[i] * -f;
			
			i++;
			for( ;; i-- ) {
				if( i < 0 ) return 0;
				
				d -= pointDistance[i];
				if( d <= distance ) return i;
			}
		}
	}

	public float	GetOffset( float posOnPath, float distance ) {
		int 	i = Mathf.FloorToInt( posOnPath );
		float	f = posOnPath - i;

		if( i < 0 ) return 0;

		int n = points.Length;
		for(;;) {
			if( i >= n-1 ) {
				return n-1;
			}

			var d = pointDistance[i];
			var r = (1-f) * d;

			if( r > distance ) {
				f = 1- ( ( r - distance ) / d );
				return i + f;
			}

			distance -= r;
			i++;
			f = 0;
		}
	}

	public Vector3	GetPos( float posOnPath ) {
		if( float.IsNaN( posOnPath ) ) return Vector3.zero;

		int 	i = Mathf.FloorToInt( posOnPath );
		float	f = posOnPath - i;

		var n = points.Length;

		if( i < 0 || i >= n-1 ) {
			if( n < 1 ) {
				return new Vector3( float.NaN, float.NaN, float.NaN );
			}
			return points[n-1];
		}

		var a = points[i];
		var b = points[i+1];

		return  Vector3.Lerp( a,b, f );
	}

	public	float GetBackwardPosInRadius( float posOnPath, float radius ) {
		var pos = GetPos( posOnPath );
		int i = Mathf.FloorToInt( posOnPath );
		for( ; i>0; i-- ) {
			var p = CircleSegmentIntersect( i, pos, radius );
			if( ! float.IsNaN(p) ) return (i+p);
		}
		return 0;
	}

	public	float	CircleSegmentIntersect( int segment, Vector3 center, float radius ) {
		if( segment >= points.Length-1 ) return float.NaN;

		var dis = pointDistance[ segment ];
		if( dis == 0 ) return float.NaN;

		var a = points[ segment ];
		var b = points[ segment+1 ];
		var c = center;

		var v1 = b - a;
		var v2 = c - a;

		if( dis == 0 ) return float.NaN;

		var dot = Vector3.Dot( v1, v2 );

		var proj = ( dot / (dis*dis) ); // project on segment

		var projPt = v1 * proj;

		var pp = projPt + a;

		float disToC2 = (pp-c).sqrMagnitude;
		var r2 = radius * radius;

		if( disToC2 > r2 ) return float.NaN; // out of circle

		float q = proj; // when touch the circle ( disToC == 0 )

		if( disToC2 < r2 ) {
			var h = Mathf.Sqrt( r2 - disToC2 ) / dis;
			q = proj - h; //
//			q = proj + h; // another solution point
		}

		if( q < 0 ) return float.NaN;
		if( q > 1 ) return float.NaN;

		return (float) q;
	}

	void CreatePoints( RR_RoutePath path ) {
		var tmpCheckPointNodes 	= new List<Node>(16);
		var tmpPoints 			= new List<Vector3>( 512 );
		var tmp 				= new List<Vector3>(64);

		if( path.nodes.Length < 2 ) return;

		Node last = null;

		for( int c=0; c<path.nodes.Length; c++ ) {
			var p = path.nodes[c];

			if( p.checkPointType != CheckPointType.None ) {
				tmpCheckPointNodes.Add( p );
			}

			if( p.checkPointType == CheckPointType.StationStart ) {
				stationStartIndex = tmpPoints.Count;
			}else if( p.checkPointType == CheckPointType.StationEnd ) {
				stationEndIndex = tmpPoints.Count;
			}

			var inDir = RR_Direction.None;

			if( c == 0 ) {
				inDir = RR.DirFrom(  path.nodes[1].track.pos - p.track.pos );
			}else{
				inDir = RR.DirFrom( p.track.pos - last.track.pos );
			}

			var opDir = RR.DirOpposite( inDir );

			var leftHandedTrack = RR_Game.instance.leftHandedTrack;

			if( opDir == p.track.inDir ) {
				p.useTrackA = ! leftHandedTrack;

			}else if( opDir == p.track.outDir ) {
				p.useTrackA =   leftHandedTrack;
			}else{
				//U-Turn
				if( last != null ) {
					p.useTrackA = ! last.useTrackA;
				}
//				Debug.LogError( "Dir is missing in path node " + c + " " + p.track + " " + opDir + " " + last.track.pos );
			}

			if( ! p.track.doubleTrack ) {
				p.useTrackA = true; // always use path A for single track
			}

			tmp.Clear();
			if( p.useTrackA ) {
				tmp.AddRange( p.track.trackA );
			}else{
				tmp.AddRange( p.track.trackB );
			}

			if( p.useTrackA == leftHandedTrack ) {
				tmp.Reverse();
			}

			if( tmp.Count == 0 ) {
				Debug.LogError( "no points on track" );
				continue;
			}

			var midIndex = tmp.Count / 2;
			Tk.RemoveLastElement( tmp );

			p.pointIndex = tmpPoints.Count + midIndex;
			tmpPoints.AddRange( tmp );

			last = p;
		}
		points = tmpPoints.ToArray();

		#if false //debug points
		for( int i=0; i<points.Length; i++ ) {
			points[i].y += i * 0.05f;
		}
		#endif

		checkPointNodes = tmpCheckPointNodes.ToArray();

		pointDistance = new float[ points.Length ];
		for( int i=0; i<points.Length-1; i++ ) {
			pointDistance[i] = Vector3.Distance( points[i], points[i+1] );
		}
	}

	public	RR_RoutePath( Node[] nodes ) {
		this.nodes = nodes;
		CreatePoints( this );
	}

	void _OnDrawGizmos( bool selected ) {
		if( selected ) {
			Gizmos.color = Color.green;
			var offset = new Vector3(0,0.1f,0);
			TkGizmos.DrawWireCubes( points, Vector3.one * 0.05f, offset );
			TkGizmos.DrawLines( points, offset );

		}else{
//			Gizmos.color = new Color( 0.25f, 0.55f, 0.55f );
			Gizmos.color = new Color( 0.5f, 0.5f, 0.15f );
			TkGizmos.DrawLines( points );
		}


		Gizmos.color = Color.yellow;		
		if( checkPointNodes != null ) {
			foreach( var cp in checkPointNodes ) {
				Gizmos.DrawCube( points[ cp.pointIndex ], Vector3.one * 0.25f );
			}
		}
	}

	public void OnDrawGizmos() {
		foreach( var p in nodes ) {
			p.track.DrawGizmos_TrainLock( false );
		}
		_OnDrawGizmos( false );
	}

	public void OnDrawGizmosSelected() {
		_OnDrawGizmos( true );
	}
}

