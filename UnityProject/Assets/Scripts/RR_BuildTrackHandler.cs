using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RR_BuildTrackHandler : MonoBehaviour {

	public	RR_TrackInTile[]	tracks;

	public	RR_BuildTrackHandler_EndPoint	startHandler;
	public	RR_BuildTrackHandler_EndPoint	endHandler;

	public	bool		doubleTrack	= true;
	public	RR_Station	stationTrack;

	public	GameObject	model;

	public	RR_Tile		startTile;
	public	RR_Tile		endTile;

	static			RR_BuildTrackHandler instance_;
	static	public	RR_BuildTrackHandler instance {
		get{ return instance_; }
	}

	public	static	bool Cancel() { return instance_ ? instance_.DoCancel() : false; }

	public	bool DoCancel() {
		Tk.Destroy( this.gameObject );
		if(	RR_BuildTrackPanel.instance ) {
			RR_BuildTrackPanel.instance.OnCancelBuild();
		}
		return true;
	}

	public	static	bool Confirm() { return instance_ ? instance_.DoConfirm() : false; }

	public	bool DoConfirm() {
		if( tracks != null ) {
			var n = tracks.Length;
			
			for( int i=0; i<n; i++ ) {
				var t = tracks[i];
				
				var connectDir = RR_Direction.None;
				if( i == 0 ) {
					connectDir = t.outDir;
				}else if( i == n-1 ) {
					connectDir = t.inDir;
				}

				t.mapTile.AddTrack( t, connectDir );

				if( connectDir == RR_Direction.None ) {
					t.mapTile.trackInfo.station = stationTrack;
				}
			}

			RR_Route.AllRoute_UpdatePath();
		}
		
		DoCancel();
		return true;
	}

	public	static	RR_BuildTrackHandler Create( RR_Tile startTile ) {
		//		Debug.Log("RR_BuildTrackHandler Create");
		Cancel ();

		var o = new GameObject("_RR_BuildTrackHandler");
		var c = o.AddComponent<RR_BuildTrackHandler>();

		var prefabPath = "_AutoGen_/Handlers/TrackEndPoint";

		{	var obj = Tk.LoadGameObject( prefabPath );
			c.startHandler = Tk.GetOrAddComponent< RR_BuildTrackHandler_EndPoint >( obj );
			c.startHandler.isEnd = false;
		}

		{
			var obj =Tk.LoadGameObject( prefabPath );
			c.endHandler = Tk.GetOrAddComponent< RR_BuildTrackHandler_EndPoint >( obj );
			c.endHandler.isEnd = true;
		}
		
		c.startHandler.name = "Start";
		  c.endHandler.name   = "End";
		
		c.startHandler.transform.parent = c.transform;
		  c.endHandler.transform.parent = c.transform;

		c.SetStart( startTile );
		c.SetEnd  ( startTile );

		if(	RR_BuildTrackPanel.instance ) {
			RR_BuildTrackPanel.instance.OnStartBuild();
		}

		return c;
	}

	void Awake() {
		instance_ = this;
	}

	public	void	SetStationTrack( RR_Station s ) {
		stationTrack = s;
	}

	public	void	SetEnd( RR_Tile tile ) {
		if( tile != null ) {
			endHandler.transform.position = tile.worldPos;
		}
		endTile = tile;

		if( tile.blocked ) {
			endHandler.SetBlockedColor();
		}else{
			endHandler.SetEndColor();
		}

		UpdatePath();
	}

	public	void	SetStart( RR_Tile tile ) {
		if( tile != null ) {
			startHandler.transform.position = tile.worldPos;
		}
		startTile = tile;

		if( tile.blocked ) {
			startHandler.SetBlockedColor();
		}else{
			startHandler.SetStartColor();
		}		

		UpdatePath();
	}

	public	void UpdatePath() {
		var p = new RR_PathFinding();
		tracks = p.FindTrackPath( startTile, endTile );
		
		Tk.Destroy( model );

		if( tracks != null ) {
			model = new GameObject("Model");
			model.transform.parent = this.transform;

			foreach( var t in tracks ) {
				t.doubleTrack = true;

				var mesh = t.LoadMesh();
				mesh.transform.parent = model.transform;
			}
		}
		
		//		Debug.Log( "totalTestedSteps=" + p.totalTestedSteps );
	}

	void _OnDrawGizmos() {
		if( tracks != null ) {
			foreach( var t in tracks ) {
				t.OnDrawGizmos();
			}
		}
	}

    public void UpdateBuildTrackConfirm(bool show,Vector3 v)
    {
        RR_BuildTrackPanel.instance.UpdateConfirmObject(show, v);
    }

}
