#What does it do ?

This extension for Unity3D does a damn good at converting C# scripts into UnityScript scripts.
More precisely, the extension provides a very good quality mass-conversion solution to translate C# code into UnityScript.

Current version : 1.0
Last edit of this manual on the 30th of November 2012.

#How to install

Extract the CSharpToUnityScriptConverter.unitypackage in your Asset folder.
It will have created the "Assets/CSharpToUnityScript" folder.

Open the extension via the menu "Script Converters > C# To UnityScript".

#How to use

The extension will convert any C# scripts found in the "source" directory and create their UnityScript conterpart in the "target" directory.

By default, the source directory is "Assets/CSharpToUnityScript/Source" and the target directory will be "Assets/CSharpToUnityScript/ConvertedScripts". You may change these paths via the appropriate fields.

Put the C# scripts in your source directory then hit the "convert" button.
The extension tab shows you how many files are left to be converted.
The console display the name of the files when their conversion is completed.

If you have many files being converted and want to abord the conversion, just hit the "Abord" button.
This won't stops the conversion of the current file but it won't convert any more file after that.
If you hit the convert button again, it will convert again the whole source directory.

The conversion speed is about 10Ko or a couple hundreds of lines per second.

#Options



==================================================
TAKE NOTES OF :
==================================================

cast without parenthesis stop at the first non alpha numeric character or parenthesis
(float) castwithoutspace


- multile variable declaration will be messed up if 
	- a semi-colon is used in a value


- casting with parenthesis will only convert one pattern fr each


==================================================
WHAT DOES NOT WORK (HAS NO EQUIVALENT) IN UNITYSCRIPT
==================================================


-	Enums does not accepts negative values

-	"abstract" keyword
	Abstract classes/methods are converted into regular classes/methods (the keyword is stripped)

-	You can not have a method parameter nammed "get"

-	static properties can't access another static property (get the error as the accessed property is non-static)

-	you can't create assembly aliases (as in : using Alias=Assembly;   such pattern is not converted)


- Keyword "params" in method parameters.
They are left, so they pop errors in the Unity console
The solution is to remove the "params" keyword from the method declaration,
then wrap the parameters in an array in the method call.

C# :
	void Method( params int[] values ) {}
	Method( 1, 2 );
	Method( 1, 2, 3 );

	void Method( string firstParam, params int[] values ) {}
	Method( "", 1, 2 );
	Method( "", 1, 2, 3 );

UnityScript :
	function Method( values: int[] ) {}
	Method( [1, 2] );
	Method( [1, 2, 3] );

	function Method( firestParam: String, values: int[] ) {}
	Method( "", [1, 2] );
	Method( "", [1, 2, 3] );



- "delegate" and "event" does not exists in UnityScript but can be simulated to some extends.

You can't create custom-nammed delegate or callable types as in C# or Boo but you can express specific method signatures as Boo does.
ie :

C# :
	delegate string DelegateName(int arg);
	DelegateName variabmeName = MethodName;

Boo :
	CallableName as callable(int) as string
	variableName as CallableName = MethodName

UnityScript :
	"Function" is a global callable type, that match any signatures. You should be able to use it wherever you use a delegate in C#
	But you can also be more specific :

	function(int): String
	function(String, boolean) // same as    function(String, boolean): void
	var variableName: function(int): String = MethodName;

You also still have access to .Net's Action<> and Func<> generic delegates.


As of v0.1, the converter convert every occurence of a delegate name to its US conterpart.
ie : Every occurence of "DelegateName" would be replaced by "function(int)"


Events does not exists, but they are just a specialized collection of method, something you can easily reproduce in UnityScript, while it require some code refactoring.

C# :
	delegate void FooBarSignature(int data);
	// the event that will store the methods
	event FooBarSignature foobarMethods;

	[...]

	// a method to be called when the event is thrown
	void AMethod( int param ) {}

	// registering methods
	foobarMethods += AMethod;

	// throwing an event
	foobarMethods( 1 );

UnityScript :
	// the event can be simulated by any kind of list
	var foobarMethodsList: List.<function(int)> = new List.<function(int)>();

	function foorbarMethods( param: int ) {
		for (var method: function(int) in foobarMethodsList)
			method( param );
	}

	[...]

	// a method to be called when the event is thrown
	function AMethod( param: int ) {}

	// registering methods, this is what you need to go over and change yourself
	foobarMethodsList.Add( AMethod );

	// throwing an event
	foorbarMethods( 1 );


As of v0.1, nothing is done by the converter, everything is left out in the code.


- Arrays
	Single dimentionnal arrays convert just fine.
	But :

	The syntax of a multidimensionnal array in C#, become a jagged array in US :
	C# 	{ {0}, {1} } 	is of type int[,]
	US 	[ [0], [1] ]	is of type int[][] (same case in Boo)


	Here is some examples of what works, what doesn't :

	Jagged arrays :
		var array: int[][];			=>	display the error UCE0001: ';' expected. Insert a semicolon at the end.
		var array = new int[1][2];   	=>  display the error : IndexOutOfRangeException: Array index is out of range.
		var array = new int[2][1];	=>	array is of type int ...

		creating an empty two level jagged array is done :
		var array = array.<int[]>(10);	=>	array is of type int[][]  <=>  int[10][]

		Using "array.<type[]>" as a type does not work, thought.

		You can also set the values
		var array = [ [0], [1] ];		=>	array is of type int[][]
		
		
	MultiDimentionnal arrays :
		var array: int[,];					=> 	Works
		var array = new int[1,1]; 			=>	Works
		var array: int[,] = new int[1,1]; 	=>	Works

		You can also use Boo's syntax :
		var array = matrix(int, 1, 1);
		var array: int[,] = matrix(int, 1, 1);

		It seems that you can't set a multidimentionnal array while declaring it;
  
  
- "out" and "ref" keywords.
In UnityScript, you don't need these keywords when you call a C# method (ie : the 'hitInfo' parameter of Physics.Raycast()).
But there is no way in UnityScript to create such behavior in the method declaration.
Remember that you can still use C# classes from UnityScript scripts if they (the C# scripts) are compiled first.

There are no easy way to pass a value type as reference in UnityScript but here is one hack : You use an intermediary variable which contains the value.

C# :
	void RefMethod(ref int arg) {
		arg = 20;
	}

	void Start() {
		int refVar = 5;

		RefMethod(ref refVar);

		Debug.Log(refVar); // will display 20
	}

UnityScript :
	var ref = Array();

	function RefMethod() {
		ref["refVar"] = 20;
	}

	function Start() {
		var refVar = 5;

		ref["refvar"] = refVar;
		RefMethod();
		refVar = ref["refvar"];

		print(refVar); // will print 20
	}






==================================================

==================================================

