
/*
 * variables declaration
 */

// variable declaration without type declaration or value setting (ie : var _aVariable;) does not get converted;

// variable with type declaration but no value setting

	var _typeNoVal: String ;
	var _typeNoVal4:char;
	var _typeNoVal2 : boolean;
	var _typeNoVal3 :float;
	

// variable with type declaration and value setting
	
	var _typeVal: String =  " &";
	var _typeVal2:String='& ';
	var _typeVal3 : char = 'a' [0];
	var _typeVal4:char =  "a"[0];
	var _typeVal5:boolean=true;
	var _typeVal6: int = 7;
	var _typeVal7 :float=   3.5;


// variable with value setting but no type declaration. Have to guess the type with the value's look

	// String
	var _noTypeVal   =   "_ "  ;
	var _noTypeVal2= ' _' ;
	// Char
	var _noTypeVal3 = 'a'[ 0];
	var _noTypeVal4="a"  [0 ] ;

	var _noTypeVal5=false;
	var _noTypeVal6 = 6  ;
	var _noTypeVal7=3.5;


// array with type declaration
	
	var _array: String [ ] ;
	var _array2:char[] ;
	var _array3 : boolean [ ];
	var _array4 : int [];
	var _array5 :float [ ];


// array with type declaration and value setting
	
	// String
	var _array111:String[] = [ "1", "2"];
	var _array112: String [] = [ "1", '2'];
	var _array113: String[ ] = [ '1', '2'];
	// char
	var _array121:char[]=['a'[0 ],'b'[0]];
	var _array122:char[]= ['a' [0],"b"[ 0] ];
	var _array123:char[]=[ "a"[0] ,"b"[0]];

	var _array13 : boolean [ ] =[true,false];
	var _array14 : int[ ]  = [ 1, 2 ,3 , 4 ];
	var _array15 :float [ ]= [ 3.5, 1.2];


// arrays with value setting but no type declaration. Have to guess the type with the value's look
	
	// String
	var _array21 = [ "1", "2"];
	var _array212=[ "1", '2'];
	var _array213 = [ '1', '2'] ;

	// char
	var _array221 = ['a' [0], 'b'[0 ]];
	var _array222 =['a' [0], "b" [0]] ;
	var _array223= [ "a"[0] , "b"[ 0 ]];
	
	var _array23 = [true,false];
	var _array24 = [ 1, 2 ,3 , 4 ] ;
	var _array25 = [ 3.5, 1.2];
	
// empty arrays declarations   
	
	// without type
	var _array31=new String[5];
	var _array32 = new char[ 1 ] ;
	var _array33 = new boolean [11];
	var _array34=new int [ 99] ;
	var _array35 = new float[53 ];

	// with type
	var _array41: String []=new String[5];
	var _array42:char[] = new char[ 1 ]  ;
	var _array43 : boolean [ ] = new boolean [11];
	var _array44 : int[ ]=new int [ 99] ;
	var _array45 :float [ ]= new float[53 ];


// generic collections
	
	// without new keyword
	var _coll11 = List. <double>(); 
	public var _coll12 = Dictionary .<boolean, long> ( ); 
	private var _coll13: Dictionary . <String, List.< boolean> > = Dictionary. <String, List.<boolean> > ( ) ;
	// with new keyword
	protected var _coll14 = new List.< float >();
	var _coll15: Dictionary.< String , String > = new Dictionary .<  String,String> ();


// collections

	static var _coll21 = Hashtable ();
	static public var _coll22: Array = Array (); // gets converted to ArrayList
	static private var _coll23 = new ArrayList();
	static protected var _coll24: Queue = new Queue();


// custom classes

	var _class11 = _Parent () ;
	public static var _class12: _Parent = _Parent ();
	private static var _class13 = new _Parent( " ");
	protected static var _class14: _Parent = new _Parent( );


//methods

	// see other test file



/* 
 * enums declaration
 * nothing change from UnityScript
 */
	 

enum AEnum {
	Var1,
	Var2 = 1, Var3
}

enum 
AnotherEnum 
{ Var1=0, Var2 }
