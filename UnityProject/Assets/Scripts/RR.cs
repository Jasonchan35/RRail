using UnityEngine;
using System.Collections;

public static class RR {
	public	static int trainLayerId = Tk.LayerId("Train");
		
	public static TkVec2i[] _dirToInt2 = new TkVec2i[] {
		new TkVec2i( 0, 1 ),
		new TkVec2i( 1, 1 ),
		new TkVec2i( 1, 0 ),
		new TkVec2i( 1,-1 ),
		new TkVec2i( 0,-1 ),
		new TkVec2i(-1,-1 ),
		new TkVec2i(-1, 0 ),
		new TkVec2i(-1, 1 )
	};
	
	public static TkVec2i	DirToInt2( RR_Direction dir ) {
		var i = (int)dir;
		if( i<0 || i >=8 ) return TkVec2i.zero;
		return _dirToInt2[i];
	}
	
	public static Vector3	DirToVector3( RR_Direction dir ) {
		var o = DirToInt2(dir);
		return new Vector3(o.x,0,o.y);
	}
	
	public static RR_Direction DirFrom( TkVec2i d ) {
		if( d.y < 0 ) {
			if( d.x < 0 ) return RR_Direction.SW;
			if( d.x > 0 ) return RR_Direction.SE;
			return RR_Direction.S;
		}else if( d.y > 0 ) {
			if( d.x < 0 ) return RR_Direction.NW;
			if( d.x > 0 ) return RR_Direction.NE;
			return RR_Direction.N;
		}else{
			if( d.x < 0 ) return RR_Direction.W;
			if( d.x > 0 ) return RR_Direction.E;
			return RR_Direction.None;
		}
	}

	public static bool IsDirDiagonal( RR_Direction dir ) {
		return (int)dir % 2 == 1;
	}

	public static RR_Direction	DirOpposite( RR_Direction dir ) {
		var i = (int)dir;
		if( i<0 || i >=8 ) return RR_Direction.None;
		return (RR_Direction)( ( i+4 ) % 8 );
	}

	public static RR_Direction	DirAdd( RR_Direction dir, int t ) {
		return (RR_Direction)( ((int)dir + t + 8 ) % 8 );
	}

	public static int DirDiff( RR_Direction from, RR_Direction to ) {
		var t = (int)to - (int)from;
		if( t >  4 ) return t-8;
		if( t < -4 ) return t+8;
		return t;
	}

	public static int DirDiffAbs( RR_Direction from, RR_Direction to ) {
		return Mathf.Abs( DirDiff( from,to ) );
	}

	public static void	  SnapToTile( Transform t ) {
		t.position = SnapToTile( t.position );
	}

	public static Vector3 SnapToTile( Vector3 pos ) {
		pos.x = Mathf.RoundToInt( pos.x );
		pos.z = Mathf.RoundToInt( pos.z );		
		
		var map = RR_Map.instance;
		if( map ) {
			var t = map.GetTerrain();
			if( t ) {
				pos.y = t.SampleHeight( pos );
			}
		}
		
		return pos;
	}
}
