import UnityEngine;
import System.Collections;
import System.Collections.Generic;

interface Int1 {}
interface Int2 {}
interface Int3 {}

// CLASSES DECLARATION
public class TestInheritance extends MonoBehaviour {}
public class TestInterface implements Int1 {
	public function TestInterface() {}
	public function TestInterface( aTruc: String) {}
	public function TestInterface( aTruc: boolean, aTruc2: boolean) {}
}
public class TestInterfaces implements Int1, Int2 {}
public class TestInterfaces2 implements Int1, Int2 , Int3 {}
public class aaTest extends TestInterface implements  Int1 ,Int2 {

	// CONSTRUCTORS
/*	public function aaTest( aTruc: String) {
super(aTruc);
		// struff there 1
	}

	public function aaTest() {
super();
		// struff there 2
	}

	public function aaTest( aTruc: boolean) {
aaTest(aTruc.ToString());
		// struff there 3
	}

	public function aaTest( mahcin: String) 
	{
aaTest();
		// struff there 4
	}
*/

	// ATTRIBUTES
/*
	
	
	@script RequireComponent(Material)
	@DrawGizmo(GizmoType.NotSelected | GizmoType.Pickable)
	@script AddComponentMenu("Transform/Follow Transform")
	@script ContextMenu("Do Something")
	@script MenuItem("MyMenu/Do Something")
	@script ExecuteInEditMode
	@script System.Serializable
*/


	// VARIABLES

	// multiple variable declaration
/*	var aVar9: double=var aVar11:  3.5, aVar10,=5.0 ;
	var aVar1: String = "un ;var aVar2:  espace",;
	var avar: TestInterface = new TestInterface()var avar2: , = new TestInterface (untruc);
*/	

	// VAR DECLARATION WITHOUT VALUE
	var withoutValue: String[];
	private var withoutValue1: boolean;
	private static var withoutValue2: String;
	static private var withoutValue3: Dictionary.<String, boolean>;


	// VAR DECLARATION WITH VALUE
	@HideInInspector
	public var withValue1: String = "mach ; in";
	public static var withValue2: String = "machin \\. "+	" machin chose"	;
	public static var withValue7: char = 'a'[0];
	static public var withValue3: boolean = false;
	protected var withValue4: float[] = [3.5, 0];
//	protected static var withValue5: double = Method (machin);
	static protected var withValue6: TestInterface = new TestInterface (withValue3, withValue3);

	
	// VAR DECLARATION IN FOREACH LOOP
	// se in function
	

	// CASTING
//	var varCast1: String = ReturnInt(withValue5);


	// ARRAYS

	var array1: String[];
	var array2: String[,] = new String[5,6];

	var array3: String[,,] = [ [ [""] ]   ];
	var array4: boolean[] = [ false, true];

	var array5: float[] = [ 0.0,
		3.5,
		5];
	var array6: int[] = [ 0,
		3,
		5];

	// multi array
	var array7: int[,,] = new int[1,2,3];



	// FUNCTIONS
	@RPC
	public function ReturnVoid() {
		
		//function for(var _string: String in array1):  VAR DELCARATION IN FOREACH LOOP {}
		for (var kvPair: KeyValuePair.<String, boolean> in withoutValue3) {}

		var testVar: String = "";
		if (true) {}
		else testVar = " ";

		//var _var: String = ReturnString (withValue3);
	}

	//private function ReturnString( testarg: boolean): String { return "";}

	static protected function ReturnBool( arg1: String): boolean {
		/*
		if (new Rect()) { // sould not be converted

		}
		else if (false) { // should not be converted

		}
*/
		arg1 = "";
		return false;
	}

	public override function ToString(): String {
		return "ToString()";
	}

	function ACoroutine( isTrue: boolean): IEnumerator {
		if (isTrue) {
			yield ;
		}
		else {
			yield  WaitForSeconds (3.5);
			// yield WaitForSeconds (3.5);
		}
		
	}



	// PROPERTIES
	private var prop1: int = 5;
	public int Prop1 { get; set; }

	private var prop2: int;
	public int Prop2 {
		get { return prop2*2; 
		}
	}

	private var prop3: String;
	public string Prop3 {
		set 
		{var prop3:   = value+" ";
		}
	}

	private var prop4: double;
	public double Prop4 {
		get {return prop4; }
		protected set {
			prop4 += value;
		}
	}


	// Use this for initialization
	function Start() {
		
		

		
	}


}

class AStruct extends System.ValueType {
	
}