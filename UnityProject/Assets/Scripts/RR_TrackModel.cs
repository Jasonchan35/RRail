using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RR_TrackModel : MonoBehaviour {

	public	GameObject[]	jointsA; 
	public	GameObject[]	jointsB; // for double track

	GameObject[] OnImportJointSet( GameObject m ) {
		if( !m ) return new GameObject[0];

		var j = Tk.FindChildWithPrefix( m, "Joint0", false, false );
				
		var list = new List< GameObject >();
		while( j ) {
			list.Add( j );
			
			if( j.transform.childCount == 0 ) break;
			j = j.transform.GetChild(0).gameObject;
		}

		return list.ToArray();
	}

	public	void	OnImport( bool doubleTrack ) {
		if( doubleTrack ) {
			var ja = Tk.FindChildWithSuffix( this.gameObject, "_A", false, false );
			var jb = Tk.FindChildWithSuffix( this.gameObject, "_B", false, false );

			jointsA = OnImportJointSet( ja );
			jointsB = OnImportJointSet( jb );

		}else{
			jointsA = OnImportJointSet( this.gameObject );
			jointsB = new GameObject[0];
		}
	}
}
