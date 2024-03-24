using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RR_GameData : ScriptableObject {

	[System.Serializable]
	public	class GameGlobal {
		public	float		GameYearPerSeconds;
		public	float		TrainSpeed;	//( mile/h ) to ( Tile/GameYear )
	}
	public	GameGlobal	gameGlobal;

	[System.Serializable]
	public	class LocomotiveType {
		[HideInInspector]
		public	string		Name;

		[HideInInspector]
		[TkCSVParser.NonSerialize]
		public	RR_LocomotiveType	type;

		public	float		MaxSpeed;
		public	float		Acceleration;
		public	float		FreeLoads;
		public	float		ExtraLoadsK;
		public	float		SlopeK;
	//-----
		public	double		Cost;
		public	double		MaintenanceCost;
		public	double		FuelCost;
	}
	public	LocomotiveType[]	locomotiveType;
	public	LocomotiveType		this[ RR_LocomotiveType t ] { get{ return locomotiveType[(int) t]; } }

	public	void	ResetLocomotiveTypeArray() {
		var values = Tk.EnumValues< RR_LocomotiveType >();
		var N = values.Length;
		var newArr = new LocomotiveType[N];
		for( int i=0; i<N; i++ ) {
			var o = Tk.SafeGetElement( locomotiveType, i );
			if( o == null ) o = new LocomotiveType();
			newArr[i] = o;
			var t = values[i];
			o.Name = t.ToString();
			o.type = t;
		}
		locomotiveType = newArr;
	}

	[System.Serializable]
	public class FactoryType {
		[HideInInspector]
		public	string			Name;
		public	double			Year;
		public	double			BuildCost;
	}
	public	FactoryType[]	factoryType;
	public	FactoryType		this[ RR_FactoryType t ] { get{ return factoryType[(int) t]; } }

	public	void	ResetFactoryTypeArray() {
		var values = Tk.EnumValues< RR_FactoryType >();
		var N = values.Length;
		var newArr = new FactoryType[N];
		for( int i=0; i<N; i++ ) {
			var o = Tk.SafeGetElement( factoryType, i );
			if( o == null ) o = new FactoryType();
			newArr[i] = o;
			var t = values[i];
			o.Name = t.ToString();
		}
		factoryType = newArr;
	}

	[System.Serializable]
	public	class ResourceCargo { 
		[HideInInspector]
		public	string		name;
		public	float		rate;	// GenerationRate
		public	float		cap;	// StorageCapacity
		public	float		demand;
	}

	[System.Serializable]
	public	class ResourceSize {
		[HideInInspector]
		public	string				name;
		public	ResourceCargo[]		cargos;
		public	ResourceCargo		this[ RR_CargoType t ] { get{ return cargos[(int) t]; } } 
	}

	[System.Serializable]
	public	class ResourceType {
		[HideInInspector]
		public	string				name;
		public	void				SetName ( string s ) { name = s; }

		public	ResourceSize[]		sizes;
		public	ResourceSize		this[ RR_ResourceSize t ] { get{ return sizes[(int) t]; } }
	}
	
	public	ResourceType[]		resourceType;
	public	ResourceType		this[ RR_ResourceType t ] { get{ return resourceType[(int) t]; } }

	[System.Serializable]
	public	class CargoType {
		[HideInInspector]
		public	string				Name;
		public	double				money_min;
		public	double				money_max;
		public	double				v_min;
		public	double				v_max;
	}
	public	CargoType[]			cargoType;
	public	CargoType			this[ RR_CargoType t ] { get{ return cargoType[(int) t]; } }

	public	void	ResetCargoTypeArray() {
		var values = Tk.EnumValues< RR_CargoType >();
		var N = values.Length;
		var newArr = new CargoType[N];
		for( int i=0; i<N; i++ ) {
			var o = Tk.SafeGetElement( cargoType, i );
			if( o == null ) o = new CargoType();
			newArr[i] = o;
			var t = values[i];
			o.Name = t.ToString();
		}
		cargoType = newArr;
	}

	public	void	ResetResourceTypeArray() {
		var resourceValues 	= Tk.EnumValues< RR_ResourceType >();
		var sizeValues		= Tk.EnumValues< RR_ResourceSize >();
		var cargoValues		= Tk.EnumValues< RR_CargoType >();

		var R = resourceValues.Length;
		var S = sizeValues.Length;
		var C = cargoValues.Length;

		var newArr = new ResourceType[ R ];

		for( int ri=0; ri<R; ri++ ) {
			var r = Tk.SafeGetElement( resourceType, ri );
			if( r == null ) r = new ResourceType();
			newArr[ri] = r;

			r.name = resourceValues[ri].ToString();
			r.sizes = new ResourceSize[ S ];

			for( int si=0; si<S; si++ ) {
				var s = Tk.SafeGetElement( r.sizes, si );
				if( s == null ) s = new ResourceSize();
				r.sizes[ si ] = s;

				s.name = sizeValues[ si ].ToString();

				s.cargos = new ResourceCargo[ C ];

				for( int ci=0; ci<C; ci++ ) {
					var c = Tk.SafeGetElement( s.cargos, ci );
					if( c == null ) c = new ResourceCargo();
					c.name = cargoValues[ci].ToString();

					s.cargos[ci] = c;
				}
			}
		}
		resourceType = newArr;
	}
}



