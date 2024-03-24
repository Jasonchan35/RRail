using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[SelectionBase]
public class RR_TileChunk : MonoBehaviour {

	public static	TkNamedGameObject	group = new TkNamedGameObject("_TileChunks");

	public		List< RR_TrackInTile >	tracks;
	bool		_trackMeshDirty = false;
	GameObject	trackMesh;

	void Awake() {
		tracks = new List<RR_TrackInTile>();
	}

	public void Add( RR_TrackInTile t ) {
		tracks.Add(t);
		_trackMeshDirty = true;
	}

	void Update() {
		UpdateTrackMesh();
	}

	void UpdateTrackMesh() {
		if( !_trackMeshDirty ) return;
		_trackMeshDirty = false;

		Tk.Destroy( trackMesh );
		trackMesh = new GameObject("TrackMesh");
		trackMesh.transform.parent = this.transform;

		foreach( var t in tracks ) {
			var mesh = t.LoadMesh();
			mesh.transform.parent = trackMesh.transform;
		}

		TkMesh.CombineChildrenMeshByMaterial( trackMesh );

		foreach( var t in tracks ) {
			Tk.Destroy( t.model );
		}
	}

	void OnDrawGizmosSelected() {
		if( tracks != null ) {
			foreach( var b in tracks ) {
				b.OnDrawGizmos();
			}
		}
	}
}
