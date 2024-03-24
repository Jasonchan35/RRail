using UnityEngine;
using System.Collections;

[SelectionBase]
public class RR_City : MonoBehaviour {
	public	int		buildingCount = 20;
	public	float	radius = 10;
	public	float	testRadius = 5;

	TkVec2i	tilePos_;

	public	TkVec2i	tilePos { get{ return tilePos_; } }

	void OnDrawGizmosSelected() {
		Gizmos.color = Color.green;
		Gizmos.matrix = Matrix4x4.TRS( transform.position + Vector3.up * 0.5f, Quaternion.identity, new Vector3(1,0,1) );
		Gizmos.DrawWireSphere( Vector3.zero, radius );
	}

	void Awake() {
	}

	void Start() {
		tilePos_ = RR_Tile.WorldToTilePos( transform.position );
	}

	void Update() {
//		RandomBuildings();
	}
	/*
	public void OnClick() {
		switch( RR_UI.inputMode ) {
			case RR_InputMode.None:{
				RR_UI.inputMode = RR_InputMode.CityPanel;
				RR_CityPanel.instance.city = this;
			}break;
		}
	}
	*/

	public void OnDrag( Vector2 delta ) {
	}

	bool randomBuildingNeeded = true;

	public	void RandomBuildingNeeded() {
		randomBuildingNeeded = true;
	}

	GameObject	buildingGroup;
	public	void RandomBuildings() {
		if( ! randomBuildingNeeded ) return;
		randomBuildingNeeded = false;
		
		Tk.Destroy( buildingGroup );

		buildingGroup = new GameObject("Buildings");
		Tk.SetParent( buildingGroup.transform, this.transform, false );

		var map = RR_Map.instance;
		if( !map ) return;

		for( int i=0; i<buildingCount; i++ ) {

			for( int t=0; ; t++ ){
				if( t > 100 ) return;

				var d = Random.value;
				d = d*d;
				var wpos = transform.position + TkRandom.rotateY * new Vector3( 0, 0, d * radius );

				var pos = RR_Tile.WorldToTilePos( wpos );
				var tile = map.GetTile( pos );	

				if( tile == null ) continue;
				if( tile.blocked ) continue;
				if( tile.building ) continue;

				GameObject model = null;
				if( d * radius < testRadius ) {
					model = Tk.LoadGameObject("Buildings/TestBuilding");
				}else{
					model = Tk.LoadGameObject("Buildings/TestHouse");
				}

				model.transform.parent = buildingGroup.transform;
				model.transform.position = tile.worldPos;
//				model.transform.rotation = TkRandom.rotateY;
				model.transform.localScale = Vector3.one * 0.4f;

				tile.building = model;

				break;
			}
		}
	}
}
