using UnityEngine;
using System. Collections ;
using System. Collections . Generic;

interface Int1 {}
interface Int2 {}
interface Int3 {}

// CLASSES DECLARATION
public class TestInheritance : MonoBehaviour {}
public class TestInterface : Int1 {
	public TestInterface () {}
	public TestInterface (string aTruc) {}
	public TestInterface (bool aTruc, bool aTruc2) {}
}
public class TestInterfaces : Int1 ,Int2 {}
public class TestInterfaces2 : Int1 ,Int2 , Int3 {}
public class aaTest : TestInterface, Int1 ,Int2 {

	// CONSTRUCTORS
/*	public aaTest (string aTruc) 
		: base (aTruc) {
		// struff there 1
	}

	public aaTest () : base () {
		// struff there 2
	}

	public aaTest (bool aTruc) 
		: this (aTruc.ToString()) {
		// struff there 3
	}

	public aaTest (string mahcin) 
		: this () 
	{
		// struff there 4
	}
*/

	// ATTRIBUTES
/*
	
	
	[RequireComponent ( typeof(Material) )]
	[DrawGizmo (GizmoType.NotSelected | GizmoType.Pickable)]
	[AddComponentMenu("Transform/Follow Transform")]
	[ContextMenu ("Do Something")]
	[MenuItem ("MyMenu/Do Something")]
	[ExecuteInEditMode]
	[System.Serializable]
*/


	// VARIABLES

	// multiple variable declaration
/*	double aVar9= 3.5f, aVar10, aVar11=5.0 ;
	string aVar1 = "un ; espace", aVar2;
	TestInterface avar = new TestInterface(), avar2 = new TestInterface (untruc);
*/	

	// VAR DECLARATION WITHOUT VALUE
	string[] withoutValue;
	private bool withoutValue1;
	private static string withoutValue2;
	static private Dictionary<string, bool> withoutValue3;


	// VAR DECLARATION WITH VALUE
	[HideInInspector]
	public string withValue1 = "mach ; in";
	public static string withValue2 = @"machin \. "+	" machin chose"	;
	public static char withValue7 = 'a';
	static public bool withValue3 = false;
	protected float[] withValue4 = {3.5f, 0f};
//	protected static double withValue5 = Method (machin);
	static protected TestInterface withValue6 = new TestInterface (withValue3, withValue3);

	
	// VAR DECLARATION IN FOREACH LOOP
	// se in function
	

	// CASTING
//	string varCast1 = (string)ReturnInt(out withValue5);


	// ARRAYS

	string[] array1;
	string[,] array2 = new string[5,6];

	string[,,] array3 = new string[1,1,1] { { {""} }   };
	bool[] array4 = { false, true};

	float[] array5 = new float[] { 0.0f,
		3.5f,
		5f};
	int[] array6 = { 0,
		3,
		5};

	// multi array
	int[,,] array7 = new int[1,2,3];



	// FUNCTIONS
	[RPC]
	public void ReturnVoid () {
		
		// VAR DELCARATION IN FOREACH LOOP
		foreach (string _string in array1) {}
		foreach (KeyValuePair<string, bool> kvPair in withoutValue3) {}

		string testVar = "";
		if (true) {}
		else
			testVar = " ";

		//string _var = ReturnString (withValue3);
	}

	//private string ReturnString (ref bool testarg) { return "";}

	static protected bool ReturnBool (out string arg1) {
		/*
		if (new Rect()) { // sould not be converted

		}
		else if (false) { // should not be converted

		}
*/
		arg1 = "";
		return false;
	}

	public override string ToString () {
		return "ToString()";
	}

	IEnumerator ACoroutine (bool isTrue) {
		if (isTrue) {
			yield return null;
		}
		else {
			yield return new WaitForSeconds (3.5f);
			// yield WaitForSeconds (3.5);
		}
		
	}



	// PROPERTIES
	private int prop1 = 5;
	public int Prop1 { get; set; }

	private int prop2;
	public int Prop2 {
		get { return prop2*2; 
		}
	}

	private string prop3;
	public string Prop3 {
		set 
		{ 
			prop3 = value+" ";
		}
	}

	private double prop4;
	public double Prop4 {
		get { return prop4; }
		protected set {
			prop4 += value;
		}
	}


	// Use this for initialization
	void Start () {
		
		

		
	}


}

struct AStruct
{
	
}