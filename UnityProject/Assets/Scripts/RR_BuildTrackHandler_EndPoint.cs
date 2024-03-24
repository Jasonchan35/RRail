using UnityEngine;
using System.Collections;

public class RR_BuildTrackHandler_EndPoint : MonoBehaviour {

	public	bool					isEnd;

	const float colorAlpha = 0.75f;

	public void SetStartColor	() { TkMaterial.SetPropertyRecursively( gameObject, "_Color", new Color( 0, 1, 1, colorAlpha ) ); }
	public void SetEndColor		() { TkMaterial.SetPropertyRecursively( gameObject, "_Color", new Color( 0, 1, 1, colorAlpha ) ); }
	public void SetBlockedColor	() { TkMaterial.SetPropertyRecursively( gameObject, "_Color", new Color( 1, 0, 0, colorAlpha ) ); }	

	void Awake() {
		const float scale = 0.8f;
		transform.localScale = Vector3.one * scale;
		var cl = gameObject.AddComponent< BoxCollider >();
		cl.size = new Vector3( 2, 0.25f, 2 );
	}

	void OnDrag( Vector2 delta ) {
//		Debug.Log( this.name + " OnDrag " + delta );

		if( UICamera.touchCount != 1 ) return;

		var touch = UICamera.currentTouch;

		var tile = RR_Map.instance.GetTileFromGUIPoint( touch.pos );

		if( tile != null ) {
			this.transform.position = tile.worldPos;

			if( isEnd ) {
				RR_BuildTrackHandler.instance.SetEnd( tile );
                RR_BuildTrackHandler.instance.UpdateBuildTrackConfirm(true, tile.worldPos);
			}else{
				RR_BuildTrackHandler.instance.SetStart( tile );
			}
		}
	}
}
