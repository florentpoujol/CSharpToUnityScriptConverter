#What does it do ?

This extension for Unity3D does a damn good at converting C# scripts into UnityScript scripts.
More precisely, the extension provides a very good quality mass-conversion solution to translate C# code into UnityScript.

Main features :
* very thorough and intelligent conversion (this is simply the best converter on the market)
* mass conversion : from one file to whole hierarchies
* ultra fast : 300+ lines/second (try this by hand !)
* dead-easy setup and use : drop files in a folder then hit a button

* Current version : 1.0
* Last edit of this manual on the 02nd of December 2012.

#Contact

* florent dot poujol at gmail dot com
* http://www.florent-poujol.fr/en
* My profile on Unity's forums : http://forum.unity3d.com/members/23148-Lion

#How to buy

You will be able to buy this extension from the offical Unity Asset Store and from GamePrefabs.com at some point during the first week of December 2012.

You want to try before bying ? No problem !
Head over to my website to [test the live demo](http://www.florent-poujol.fr/en/unity3d/c-to-unityscript-converter).
Unlike with the extension, you can convert only 100 lines at a time but the quality of the conversion is identical !

#How to install

Extract the CSharpToUnityScriptConverter.unitypackage in your Asset folder.
It will have created the "Assets/CSharpToUnityScriptConverter" folder.

Open the extension via the menu "Script Converters > C# To UnityScript".

#How to use

The extension will convert any C# scripts found in the "source" directory and create their UnityScript conterpart in the "target" directory.

By default, the source directory is "Assets/CSharpToUnityScriptConverter/Source" and the target directory will be "Assets/CSharpToUnityScriptConverter/ConvertedScripts". You may change these paths via the appropriate fields.

Put the C# scripts in your source directory then hit the "convert" button.
The extension tab shows you how many files are left to be converted.
The console display the name of the files when their conversion is completed.

If you have many files being converted and want to abord the conversion, just hit the "Abord" button.
This won't stops the conversion of the current file but it won't convert any more file after that.
If you hit the convert button again, it will convert again the whole source directory.

The conversion speed is about 10Ko or a couple hundreds of lines per second.

#Options

You may choose if you want the converter to try to convert multiple inline variable declarations.

	int var1, var2 = 2, var3, var4;

Despite all my efforts, the converter may still match a few false positive.
If you are confident that the code you want to convert does not contains such declarations, or only a few of them, you better leave the box unticked.

#Quality of the conversion

Be advised that UnityScript has less features than C#.
That means that in some case, there is just no way to express in UnityScript what the C# does.

In some other cases, you will need to refactor part of the UnityScript code (when it's not done by the converter itself) to make it do the same thing as the C# code.

Finally, in some yet other cases, the conversion may not be performed, depending on the syntactic environnement.

##Be advised

* Casts without parenthesis around the casted expression stops at the first non alphanumeric character.
* Multiple inline variable declaration will be messed up or won't be converted at all if a semi-colon is found anywhere whithin the whole delcaration, before the semi-colon that closes the expression.
Also having curly brackets and/or parenthesis inside values or instance may lead the expression not to be converted.
* Commented code is ignored, and thus does not gets converted. Comments are actually stripped from the files before the conversion then put back in after.
* Don't bother with error message like "RegexUtilities.Block.GetEndOfBlockIndex() : No matching closing bracket has been found ! Returning -1. [...]", it does not necessarily means that the conversion of something is broken.

#Features not supported by UnityScript

This sections shows which features does not work in UnityScript, and try to provide a work around when possible.

In some cases, the converter could deal with the situation itself, but it is just not (yet) implemented.

##Features dealt with by the converter

* abstract keyword : abstract classes/methods are converted into regular classes/methods.
* virtual keyword : it is just removed
* ...

##Features that could be deal with by the converter (but that curently aren't)

If these features are not currently converted by the converter, it does not mens, they will never be.

* You can't have a method parameter nammed "get".
* You can't create assembly aliases (using Alias=Assembly;)
* Operator overloading. [It works like in Boo](http://docs.codehaus.org/display/BOO/Operator+overloading).
* Closures. Excerpt from the Unity Doc :

========

	list.Sort(function(x:String, y:String) {
		return x.CompareTo(y);
	});

	list.Sort(function(x, y) x.CompareTo(y));

##Hopeless features

The converter can not do anything about them (it does in some very specific cases), you have to deal with the situation yourself.

* Linq.
* The where clause.
* Enums do not accept negative values.
* Static properties can't access another static property (you get the same error as if the accessed property is non-static).


###Params keyword

They are left, so they pop errors in the Unity console.
The solution is to remove the "params" keyword from the method declaration, then wrap the parameters in an array in the method call.

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


##Delegates and Events

They does not exists in UnityScript but can be simulated to some extends.

You can't create custom-nammed delegate or callable types as in C# or Boo but you can express specific method signatures as Boo does :

C# :

	delegate string DelegateName(int arg);
	DelegateName variabmeName = MethodName;

Boo :

	CallableName as callable(int) as string
	variableName as CallableName = MethodName

UnityScript :

	// "Function" is a global callable type, that match any signatures. You should be able to use it wherever you use a delegate in C#
	// But you can also be more specific :

	function(int): String
	function(String, boolean) // same as    function(String, boolean): void
	var variableName: function(int): String = MethodName;

You also still have access to .Net's Action<> and Func<> generic delegates.

The converter convert every occurence of a delegate name to its US conterpart.
ie : Every occurence of "DelegateName" would be replaced by "function(int)"

Events does not exists, but they are just a specialized collection of method, something you can reproduce in UnityScript, while it require some code refactoring.

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



As of v1.0, nothing is done by the converter, everything is left untouched in the code.

###Arrays
Single dimentionnal arrays should convert just fine.

The syntax of a multidimensionnal array in C#, becomes a jagged array in US :
C# :

	{ {0}, {1} } // is of type int[,]
US :

	[ [0], [1] ] // is of type int[][] (same case in Boo)


Here is some examples of what works, what doesn't :

Jagged arrays :

	// syntaxes that do not work
	var jaggedArray: int[][];			=>	display the error UCE0001: ';' expected. Insert a semicolon at the end.
	var jaggedArray = new int[1][2];  	=>  display the error : IndexOutOfRangeException: Array index is out of range.
	var jaggedArray = new int[2][1];	=>	jaggedArray is of type int ...

Creating an empty two level jagged array is done like this :

	var jaggedArray = array.<int[]>(10); // jaggedArray is of type int[][]  <=>  int[10][]

The declaration of empty jagged arrays is the only thing that is handled by the converter.

	Type[][] variable = new Type[num][];
	// is converted into
	var variable = array.<Type[]>(num);

But I don't know what is the equivalent of the expression "Type[][]" in UnityScript. So they are left in the code and pop errors.
You can also create a jagged array by setting its value right away :

	var array = [ [0], [1] ]; // array is of type int[][]
		
		
MultiDimentionnal arrays :

	var array: int[,];					// 	Works
	var array = new int[1,1]; 			//	Works
	var array: int[,] = new int[1,1]; 	//	Works

You can also use Boo's syntax :

	var array = matrix(int, 1, 1);
	var array: int[,] = matrix(int, 1, 1);

It seems that you can't set the value of a multidimentionnal array while declaring it.
  
  
###Out and Ref keywords

In UnityScript, you don't need these keywords when you call a C# method (ie : the 'hitInfo' parameter of Physics.Raycast()).
But there is no way in UnityScript to create such behavior in the method declaration.
Remember that you can still use C# classes from UnityScript scripts if they (the C# scripts) are compiled first.

There is no easy way to pass a value type as reference in UnityScript but here is one hugly hack : you can use an intermediary variable which contains the value if you really need to.

C# :

	void RefMethod(ref int arg)
	{
		arg = 20;
	}

	void Start()
	{
		int refVar = 5;

		RefMethod(ref refVar);

		Debug.Log(refVar); // will display 20
	}

UnityScript :

	var ref = Array();

	function RefMethod()
	{
		ref["refVar"] = 20;
	}

	function Start()
	{
		var refVar = 5;

		ref["refvar"] = refVar;
		RefMethod();
		refVar = ref["refvar"];

		print(refVar); // will print 20
	}


#Improving the conversion

Sometimes, someting that convert just fine in most of the scripts will just not convert at all or be messed up in another script, for no apparent reason.
If that happens, please contact me (see section at the top) and give me your script if it's not top-secret.

Such behaviour is often due to a particular syntactic setting that makes the converter not recogize a pattern (or recogize one when it shouldn't).
By gathering scripts where this happends, I may be able to find what triggers the situation and improve the converter.

