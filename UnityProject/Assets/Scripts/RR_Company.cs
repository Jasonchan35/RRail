using UnityEngine;
using System.Collections;

public class RR_Company : MonoBehaviour {

	[System.Serializable]
	public	class SaveData {
		public	string	name;
		public	long	money;
	}

	public	SaveData	saveData = new SaveData();

	public static TkNamedGameObject	group = new TkNamedGameObject("_Company");

	public static RR_Company Create() {
		var obj = new GameObject("Company");
		Tk.SetParent( obj, group.gameObject, false );

		return obj.AddComponent< RR_Company >();
	}
}
