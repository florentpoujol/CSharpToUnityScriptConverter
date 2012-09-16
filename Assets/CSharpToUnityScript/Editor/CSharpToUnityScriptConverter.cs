/// <summary>
/// CSharpToUnityScriptConverter class for Unity3D
///
/// This class handle the convertion of code from C# to UnityScript.
/// Used by the "C# to UnityScript" extension for Unity3D.
///
/// Created by Florent POUJOL aka Lion on Unity's forums
/// florent.poujol@gmail.com
/// http://www.florent-poujol.fr/en
/// Profile on Unity's forums : http://forum.unity3d.com/members/23148-Lion
/// </summary>


/// <summary>
/// Use instructions :
/// 
/// Put this script anywhere in your project asset folder and attach it to a GameObject
///
/// Create a folder "[your project]/Assets/ScriptsToBeConverted".
/// You may put in this folder any .js file (and the folder they may be in) to be converted.
/// 
/// Run the scene. One script is converted per frame, but the convertion of one script may often takes longer than 1/60 seconds. The convertion speed is///approximately* 20 files/seconds.
/// A label on the "Game" view shows the overall progress of the convertion and each convertion is logged in the console.
/// When it's complete, refresh the project tab for the new files/folder to be shown (right-click on the "Project" tab, then click on "Refresh" (or hit Ctrl+R on Windows)) 
///
/// Upon convertion, a folder "[your project]/Assets/ConvertedScripts" is created with all converted scripts (and their folder hyerarchie)
/// </summary>


using UnityEngine; 
using UnityEditor; // EditorGUILayout
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions; // Regex.Replace(), Match, Matches, MatchCollection...
using System.IO; // Directory Directory StreamReader/Writer


public class CSharpToUnityScriptConverter: RegexUtilities {

    // a list of classes that exists in the pool of files that will be converted
    //private List<string> classesList = new List<string> ();

    // a list of items (variable or functions) and their coresponding type
    //private Dictionary<string, string> itemsAndTypes = new Dictionary<string, string> ();

    // list of classes and their items (variable or function) and corresponding type
    //private Dictionary<string, Dictionary<string, string>> projectItems = new Dictionary<string, Dictionary<string, string>> ();

    private static List<string> projectClasses = new List<string> ();
    public static List<string> importedAssemblies = new List<string> ();

    public static bool convertMultipleVarDeclaration = false;

    public static bool removeRefKeyword = true;
    public static bool removeOutKeyword = true;


    
    // ----------------------------------------------------------------------------------


    /// <summary>
    /// Constructor and main method
    /// </summary>
    public CSharpToUnityScriptConverter (string inputCode) : base (inputCode) {
        importedAssemblies.Clear ();
        projectClasses.Clear ();
        Convert ();
    }


    // ----------------------------------------------------------------------------------

    /// <summary>
    ///  main method
    /// </summary>
    public void Convert (string inputCode) {
        convertedCode = inputCode;
        Convert ();
    }

    public void Convert () {
        // GENERIC COLLECTIONS

        // Add a dot before the opening chevron  List.<float>
        patterns.Add (genericCollections+optWS+"<");
        replacements.Add ("$1$2.<");

        // Add a whitespace between two closing chevron   Dictionary.<string,List<string> > 
        patterns.Add ("("+genericCollections+optWS+"<.+)>>");
        replacements.Add ("$1> >");


        // LOOPS

        // foreach (in) > for (in)
        patterns.Add ("foreach("+optWS+"\\(.+in"+oblWS+".+\\))");
        replacements.Add ("for$1");


        // GETCOMPONENT (& Co)

        // GetComponent<T>() => GetComponent.<T>()
        patterns.Add ("((GetComponent|GetComponents|GetComponentInChildren|GetComponentsInChildren)"+optWS+")(?<type><"+optWS+commonChars+optWS+">)");
        replacements.Add ("$1.${type}");


        // ABSTRACT (remove the keyword)
        patterns.Add ("((public|private|protected|static)"+oblWS+")abstract"+oblWS);
        replacements.Add ("$1");


        // YIELDS
            // must remove the return keyword after yield
        patterns.Add ("yield"+optWS+"return"+optWS+"(null|0|new)");
        replacements.Add ("yield ");

        // yield return  ;
        /*patterns.Add ("yield("+oblWS+commonChars+optWS+";)");
        replacements.Add ("yield return $2;");
    
        // yield return new WaitForSeconds(3.5f);
        patterns.Add ("yield"+oblWS+commonChars+optWS+"\\(");
        replacements.Add ("yield return new $2$3(");

        patterns.Add ("yield return new");
        replacements.Add ("yield return new");*/



        DoReplacements ();
    

        // CLASSES
        Classes ();

        // VARIABLES
        Variables ();
        
        // FUNCTIONS
        Functions ();
        
        // PROPERTIES
        // why after Functions() ? => to let the function pattern be converted
        Properties ();

        // VISIBILITY
        //AddVisibility ();

        // STRING BOOL CONVERSION
            // string      
            patterns.Add ( "("+commonName+optWS+":"+optWS+")?string(?<end>("+optWS+"(\\[[0-9,\\s]*\\])+)?"+optWS+"(=|;|,|\\)|in|{))" );
            replacements.Add ( "$1String${end}" );

            // bool
            patterns.Add ( "("+commonName+optWS+":"+optWS+")?bool(?<end>("+optWS+"\\[([0-9,\\s]*\\])+)?"+optWS+"(=|;|,|\\)|in|{))" );
            replacements.Add ( "$1boolean${end}" );

        // with arrays
            // string
            //patterns.Add ( "(new"+oblWS+")string(?<end>("+optWS+"\\[[0-9,]+\\])?"+optWS+"(=|;|,|\\)|in|{))" );
            //replacements.Add ( "$1String${end}" );

        // with generic collections
            // string
            patterns.Add ( "((<|,)"+optWS+")string(?<end>("+optWS+"\\[[,\\s]*\\])?"+optWS+"(>|,))" );
            replacements.Add ( "$1String${end}" );

            // bool
            patterns.Add ( "((<|,)"+optWS+")bool(?<end>("+optWS+"\\[[,\\s]*\\])?"+optWS+"(>|,))" );
            replacements.Add ( "$1boolean${end}" );


        // #region
        //patterns.Add ("\\#(region|REGION)"+oblSpaces+commonName+"("+oblSpaces+commonName+")*");
        patterns.Add ("\\#(region|REGION)"+oblSpaces+"*");
        replacements.Add ("");
        patterns.Add ("\\#(endregion|ENDREGION)");
        replacements.Add ("");

        // define
        patterns.Add ("\\#(define|DEFINE)"+oblSpaces+commonName+"("+oblSpaces+commonName+")*");
        replacements.Add ("");


        DoReplacements ();

        //convertedCode = "#pragma strict"+EOL+convertedCode;

    } // end of method CSharpToUnityScriptConverter


    // ----------------------------------------------------------------------------------

    /// <summary> 
    /// Convert stuffs related to classes : declaration, inheritance, parent constructor call, Assembly imports
    /// </summary>
    void Classes () {
        pattern = "\\bclass"+oblWS+commonName+"\\b";
        List<Match> allClasses = ReverseMatches (convertedCode, pattern);

        foreach (Match aClass in allClasses)
            projectClasses.Add (aClass.Groups[2].Value);

        // classes declarations with inheritance
        patterns.Add ("(\\bclass"+oblWS+commonName+")"+optWS+":"+optWS+"(?<parent>"+commonName+optWS+"{)");
        replacements.Add ("$1 extends ${parent}");
        // if parent is an Interface, extends will be converted below

        // class that inherits a parent class and implements at least one interace
        patterns.Add ("(\\bclass"+oblWS+commonName+")"+optWS+":"+optWS+"(?<parent>"+commonName+")"+
        "(?<interfaces>("+optWS+","+optWS+commonName+")+)"+
        "(?<end>"+optWS+"{)");
        replacements.Add ("$1 extends ${parent} implements ${interfaces}${end}");
        // lieave the first interface after implemtns with a coma, to be removed below
        // if parent is an Interface, extends will be converted below
        
        // if the "parent" begins by a I, consider it as an interface
        patterns.Add ("\\bextends(?<interface>"+oblWS+"I"+commonName+optWS+"(implements|{))");
        replacements.Add ("implements${interface}");

        // remove the coma before the first interface after the keyword implements
        patterns.Add ("\\bimplements"+optWS+",");
        replacements.Add ("implements ");

        // implements [interface] implements [interfaces] => implements [interface], [interface]
        patterns.Add ("(\\bimplements"+oblWS+"I"+commonName+")"+oblWS+"implements\\b");
        replacements.Add ("$1,");


        // if parent class begins by a I and have alredy been encountered during the conversion, they will be in projectClasses
        foreach (string aClass in projectClasses) {
            patterns.Add ("implements("+oblWS+aClass+optWS+"{)");
            replacements.Add ("extends$1");

            patterns.Add ("implements("+oblWS+aClass+")"+optWS+",");
            replacements.Add ("extends$1 implements ");
        }

        DoReplacements ();


        // now convert parent and alternate constructor call

        // loop the classes declarations in the file
        pattern = "\\bclass"+oblWS+"(?<blockName>"+commonName+")"+
        "("+oblWS+"extends"+oblWS+commonName+")?[^{]*{";
        allClasses = ReverseMatches (convertedCode, pattern);
        
        foreach (Match aClass in allClasses) {
            Block classBlock = new Block (aClass, convertedCode);
            classBlock.newText = classBlock.text;

            if (classBlock.isEmpty)
                continue;

            List<Match> allConstructors = new List<Match> ();

            // look for constructors in the class that call the parent constructor
            if (classBlock.declaration.Contains ("extends")) { // if the class declaration doesn't contains "extends", a constructor has no parent to call
                
                pattern = "\\bpublic"+optWS+"(?<blockName>"+classBlock.name+")"+optWS+"\\(.*\\)(?<base>"+optWS+":"+optWS+"base"+optWS+"\\((?<args>.*)\\))"+optWS+"{";
                allConstructors = ReverseMatches (classBlock.text, pattern); // all constructors in that class

                foreach (Match aConstructor in allConstructors) {
                    Block constructorBlock = new Block (aConstructor, classBlock.newText);

                    // first task : add "super();" to the constructor's body
                   constructorBlock.newText = constructorBlock.text.Insert (1, EOL+"super("+aConstructor.Groups["args"]+");");
                   classBlock.newText = classBlock.newText.Replace (constructorBlock.text, constructorBlock.newText);

                   // second tacks : remove ":base()" in the constructor declaration
                   constructorBlock.newDeclaration = constructorBlock.declaration.Replace (aConstructor.Groups["base"].Value, "");
                   classBlock.newText = classBlock.newText.Replace (constructorBlock.declaration, constructorBlock.newDeclaration);
                }
            }


            // look for constructors in the class that call others constructors (in the same class)
            pattern = "\\bpublic"+optWS+"(?<blockName>"+classBlock.name+")"+optWS+"\\(.*\\)(?<this>"+optWS+":"+optWS+"this"+optWS+"\\((?<args>.*)\\))"+optWS+"{";
            allConstructors.Clear ();
            allConstructors = ReverseMatches (classBlock.newText, pattern); // all constructors in that class

            foreach (Match aConstructor in allConstructors) {
                Block constructorBlock = new Block (aConstructor, classBlock.newText);

                // first task : add "classname();" to the constructor's body
               constructorBlock.newText = constructorBlock.text.Insert (1, EOL+classBlock.name+"("+aConstructor.Groups["args"]+");");
               classBlock.newText = classBlock.newText.Replace (constructorBlock.text, constructorBlock.newText);

               // second tacks : remove ":this()" in the constructor declaration
               constructorBlock.newDeclaration = constructorBlock.declaration.Replace (aConstructor.Groups["this"].Value, "");
               classBlock.newText = classBlock.newText.Replace (constructorBlock.declaration, constructorBlock.newDeclaration);
            }
            
            // we won't do more search/replace for this class 
            // now replace in convertedCode, the old classBlock.text by the new
            convertedCode = convertedCode.Replace (classBlock.text, classBlock.newText);
        } // end looping through classes in that file


        //--------------------


        // Attributes
        string path = Application.dataPath+"/CSharpToUnityScript/Editor/Attributes.txt";
        if (File.Exists (path)) {
            StreamReader reader = new StreamReader (path);
            List<string> attributesList = new List<string> ();

            while (true) {
                string line = reader.ReadLine ();
                if (line == null)
                    break;

                if (line.StartsWith ("#") || line.Trim () == "")
                    continue;

                attributesList.Add (line.Trim ());
            }

            foreach (string attr in attributesList) {
                if (attr == "RPC") {
                    patterns.Add ("\\["+optWS+"RPC"+optWS+"\\]");
                    replacements.Add ("@RPC");
                    continue;
                }

                if (attr == "HideInInspector") {
                    patterns.Add ("\\["+optWS+"HideInInspector"+optWS+"\\]");
                    replacements.Add ("@HideInInspector");
                    continue;
                }

                if (attr == "RequireComponent") {
                    patterns.Add ("\\["+optWS+"RequireComponent"+optWS+"\\("+optWS+"typeof"+optWS+"\\((?<type>"+commonName+")\\)"+optWS+"\\)"+optWS+"\\]");
                    replacements.Add ("@script RequireComponent(${type})");
                    continue;
                }

                if (attr == "DrawGizmo") {
                    patterns.Add ("\\["+optWS+"DrawGizmo"+optWS+"(?<params>\\(.*\\))"+optWS+"\\]");
                    replacements.Add ("@DrawGizmo${params}");
                    continue;
                }

                patterns.Add ("\\["+optWS+attr+optWS+"(?<params>\\(.*\\))?"+optWS+"\\]");
                replacements.Add ("@script "+attr+"${params}");
            }
        }
        else
            Debug.LogError ("Attributes.txt does not exist, not converting attributes !");

        
        // struct
        // in JS, the way to define struct is to makes a public class inherits from System.ValueType
        patterns.Add ("\\bstruct"+oblWS+commonName+optWS+"{");
        replacements.Add ("class $2 extends System.ValueType {");


        // base. => this.      
        patterns.Add ("\\bbase"+optWS+"\\.");
        replacements.Add ("super$1.");


        // Assembly imports
        patterns.Add ("\\busing("+oblWS+commonNameWithSpace+optWS+";)");
        replacements.Add ("import$1");

        DoReplacements ();

        
        // in UnityScript, each assembly has to be imported once per project, or it will throw a warning in he Unity console for each duplicate assembly import !
        // so keep track of the assemblies already imported in the project (in one of the previous file) and comment out the duplicate
        pattern = "\\bimport"+oblWS+"(?<assemblyName>"+commonNameWithSpace+")"+optWS+";";
        List<Match> allImports = ReverseMatches (convertedCode, pattern);

        foreach (Match import in allImports) {
            string oldAssemblyName = import.Groups["assemblyName"].Value; // remove spaces
            
            // remove spaces
            string assemblyName = oldAssemblyName.Replace (" ", "");
            convertedCode = convertedCode.Replace (oldAssemblyName, assemblyName);

            // won't work if 
            // System. Collections is written after
            // System. Collections. Generic for instace
            // because "System.Collections" will already be replaced and "System. Collections. Generic" won't exist anymore

            if (importedAssemblies.Contains (assemblyName)) {
                convertedCode = convertedCode.Insert (import.Index, "//");
                //Debug.Log ("inserting comment on import ");
            }
            else
                importedAssemblies.Add (assemblyName);
        }

        DoReplacements ();
    } // end of method Classes


    // ----------------------------------------------------------------------------------

    /// <summary> 
    /// Convert stuffs related to variable declarations
    /// </summary>
    void Variables () {
        // MULTIPLE INLINE VARIABLE DECLARATION
        if (convertMultipleVarDeclaration) {
            /*string[] valuePatterns = {
                "new"+oblWS+genericCollections+optWS+"<.*>"+optWS+"\\(.*\\)", // generic collection
                "(new"+oblWS+")?"+commonName+optWS+"\\(.*\\)", // method or class instanciation with at least two parameters
                "(\"|').*(\"|')", // string
                "[^,;]+" // general case
            };*/


            // multiple inline var declaration of the same type : "Type varName, varName = foo;"

            pattern = "\\b(?<varType>"+commonChars+")"+oblWS+"(?<varList>"+commonName+optWS+"(="+optWS+"[^,]+"+optWS+")?,{1}"+optWS+"[^;]*)+"+optWS+";";
            List<Match> allDeclarations = ReverseMatches (convertedCode, pattern);

            foreach (Match aDeclaration in allDeclarations){
                // split the varlist using the coma
                string[] varList = aDeclaration.Groups["varList"].Value.Split (',');
                string varType = aDeclaration.Groups["varType"].Value;

                string newSyntax = "";

                foreach (string varName in varList) {
                    if (varName.Contains ("=")) {
                        // add the varType beetween the varName and the equal sign
                        string varDeclaration = varName.Replace ("=", ": "+varType+" =");
                        newSyntax += "var "+varDeclaration.Trim ()+";"+EOL;
                    }
                    else 
                        newSyntax += "var "+varName.Trim ()+" :"+varType+";"+EOL;
                }

                convertedCode = convertedCode.Replace (aDeclaration.Value, newSyntax);
            }
        } // end if convertMultipleVarDeclaration
 

        // " as AType;" => ";"
        // "as aType" might appear in other location
        patterns.Add (oblWS+"as"+oblWS+commonCharsWithSpace+optWS+";");
        replacements.Add (";");


        // VAR DECLARATION WITHOUT VALUE
        patterns.Add ("(?<visibility>"+visibilityAndStatic+optWS+")?(?<varType>"+commonCharsWithSpace+")"+oblWS+"(?<varName>"+commonName+")(?<end>"+optWS+";)"); 
        replacements.Add ("${visibility}var ${varName}: ${varType}${end}");


        // VAR DECLARATION WITH VALUE
        //patterns.Add ("(?<visibility>"+visibilityAndStatic+optWS+")?(?<varType>"+commonCharsWithSpace+")"+oblWS+"(?<varName>"+commonName+")(?<varValue>"+optWS+"="+optWS+".+;)");
        patterns.Add ("(?<visibility>"+visibilityAndStatic+optWS+")?(?<varType>"+commonCharsWithSpace+")"+oblWS+"(?<varName>"+commonName+")(?<varValue>"+optWS+"=)");
        replacements.Add ("${visibility}var ${varName}: ${varType}${varValue}");
        // will mess up if the string is on several lines


        // VAR DECLARATION IN FOREACH LOOP
        patterns.Add ("\\b"+"(?<varType>"+commonCharsWithSpace+")"+oblWS+"(?<varName>"+commonName+")(?<in>"+oblWS+"in"+oblWS+")");
        replacements.Add ("var ${varName}: ${varType}${in}");


        

        // PATCHING   converting var declaration leads to some garbage
           // assembly imports
           patterns.Add ("\\bvar"+oblWS+commonName+optWS+":"+optWS+"import"+optWS+";"); // will mess up if a custom class is named "import"...
           replacements.Add ("import $2;");

           // returned values
           patterns.Add ("\\bvar"+oblWS+commonName+optWS+":"+optWS+"return"+optWS+";");
           replacements.Add ("return $2;");

           // "else aVar = aValue;" got converted in  "var aVar: else = aValue;"
           patterns.Add ("\\bvar"+oblWS+commonName+optWS+":"+optWS+"else"+optWS+"=");
           replacements.Add ("else $2 =");

           // yield return null;  or  yield return 0;   got converted into  var null: yield return;
           patterns.Add ("\\bvar"+oblWS+"(?<value>null|0)"+optWS+":"+optWS+"yield"+oblWS+"return"+optWS+";");
           replacements.Add ("yield return ${value};");

           // some value setting got treated like var declaration   variable = value; => var variable: = value
           patterns.Add ("\\bvar"+oblWS+"(?<varName>"+commonName+optWS+"):(?<end>"+optWS+"=)");
           replacements.Add ("${varName}${end}");

           //in for loop :   i < variable;   got converted in    var variable: i <;
           patterns.Add ("\\bvar"+oblWS+"(?<varName>"+commonName+")"+optWS+":(?<start>"+optWS+commonName+optWS+"(<|>|<=|>=))");
           replacements.Add ("${start}${varName}");



        // CASTING
        // we can make it a little more secure by checking that the to commonChars are the same
        patterns.Add ("(var"+oblWS+commonName+optWS+":"+optWS+commonCharsWithSpace+optWS+"="+optWS+")\\("+optWS+commonCharsWithSpace+optWS+"\\)(?<afterCast>"+optWS+commonName+optWS+";)");
        replacements.Add ("$1${afterCast}");

        patterns.Add ("((\\(|=)"+optWS+")\\("+optWS+commonCharsWithSpace+optWS+"\\)(?<afterCast>"+optWS+commonName+")"); // match if and loop without brackets !!
        replacements.Add ("$1${afterCast}");


        // ARRAYS
            // string[] array1; => var array1: String[] (already done)
            // string[,] array2 = new string[5,6];  => var array2: Strint[] = new String[5,6];

            //string[] array3 = new string[] { "machin", "machin"};
            //bool[] array4 = { false, true};
                // both => var array3: String[] = { }

            // string[,] array2 = new string[5,6];  => var array2: Strint[] = new String[5,6];
            //patterns.Add ("\\b(?<type>"+commonName+optWS+"\\[,*\\])"+oblWS+"(?<name>"+commonName+")"+optWS+"=");
            // replacements.Add ("");

            // = new String[] {}; => = {};
            patterns.Add ("(?<equal>="+optWS+")?new"+oblWS+commonName+optWS+"(\\[[0-9, ]*\\])+(?<end>"+optWS+"{)");
            replacements.Add ("${equal}${end}");

        DoReplacements ();

            // replace curly brackets by square bracket
            pattern = "(?s)(="+optWS+"){(?<values>.*)}(?<end>"+optWS+";)";
            foreach (Match _pattern in ReverseMatches (convertedCode, pattern)) {
                string text = _pattern.Value;
                string newText = text.Replace ("{", "[").Replace ("}", "]");

                convertedCode = convertedCode.Replace (text, newText);
            }

            


            // jagged arrays
            // array[][]


        // LITTERAL CHAR 'a' => 'a'[0]
        patterns.Add ("'[a-zA-z0-9]{1}'");
        replacements.Add ("$0[0]");


        // remove f to Float values
        // actually not needed, it works with the f
        patterns.Add ("([0-9]{1}(\\.[0-9]{1})?)(f|F)");
        replacements.Add ("$1");


        // BOOL AND STRING
        // convert bool to boolean and string to String
        // see in Convert ()the constructor

        DoReplacements ();


        // remove @ in  'string aVariable = @"a string";"  and add a extra \ to the existing \
        pattern = "(="+optWS+")@(?<text>"+optWS+"\"[^;]+;)";
        foreach (Match _pattern in ReverseMatches (convertedCode, pattern)) {

            string text = _pattern.Value;
            string newText = text.Replace ("@\"", "\"").Replace ("\\", "\\\\");
            //Debug.Log (text);
            convertedCode = convertedCode.Replace (text, newText);
        }


    } // end of method Variable


    // ----------------------------------------------------------------------------------

    /// <summary> 
    /// Convert stuffs related to functions : declaration
    /// </summary>
    void Functions () {
        
        // function declaration ...
        // using a simple pattern/replacement regex as below match way too much things it shouldn't
        // So I need to check each match before allowing the replacement

        // patterns.Add (commonChars+oblWS+commonName+optWS+"(\\(.*\\))"+optWS+"{"); // here I don't care if the method is public, private, static, abstract or whatever since a signature is always composed of a type followed by the name of the method
        // replacements.Add ("function $3$5: $1 {");

        pattern = "("+visibilityAndStatic+oblWS+")?(?<returnType>"+commonCharsWithSpace+")"+oblWS+"(?<functionName>"+commonName+")"+optWS+"(\\("+argumentsChars+"\\))("+optWS+"{)"; // match two words followed by a set of parenthesis followed by an opening curly bracket
        List<Match> allFunctionsDeclarations = ReverseMatches (convertedCode, pattern);

        foreach (Match aFunctionDeclaration in allFunctionsDeclarations) {
            string returnType = aFunctionDeclaration.Groups["returnType"].Value.Replace ("[", Regex.Escape ("["));
            string functionName = aFunctionDeclaration.Groups["functionName"].Value;
            

            //Debug.Log ("returnType="+returnType+" | functionName="+functionName);

            if (returnType == "else" && functionName == "if") // do not match else if () statement
                continue;

            if (returnType == "new") // do not match class instanciatton inside an if statement   ie : new Rect ()) {}  => it shouldn't anymore anyway, thanks to argumentsChars instead of ".*"
                continue;

            if (returnType.Contains ("override"))
                returnType = returnType.Replace ("override", "").Trim ();  // this will left the override keyword after the prefix

            // if we are there, it's really a function declaration that has to be converted
            patterns.Add ("("+returnType+")"+oblWS+"("+functionName+")"+optWS+"(\\("+argumentsChars+"\\))("+optWS+"{)"); 
            
            switch (returnType) {
                case "void" : replacements.Add ("function $3$5$7"); continue;
                case "string" : replacements.Add ("function $3$5: String$7"); continue;
                case "string[]" : replacements.Add ("function $3$5: String[]$7"); continue; 
                case "bool" : replacements.Add ("function $3$5: boolean$7"); continue; 
                case "bool[]" : replacements.Add ("function $3$5: boolean[]$7"); continue; 
                case "public" /* it's a constructor */ : replacements.Add ("$1 function $3$5$7"); continue;
            }

            // if we are there, it's that the functiondeclaration has nothing special
            replacements.Add ("function $3$5: $1$7");
        }


        // remove out keyword in arguments
        if (removeOutKeyword) {
            patterns.Add ("(,|\\()"+optWS+"out"+oblWS+"("+commonCharsWithoutComma+")");
            replacements.Add ("$1$4");
        }

        // remove ref keyword in arguments  => let in so it will throw an error and the dev can figure out what to do 
        if (removeRefKeyword) {
            patterns.Add ("(,|\\()"+optWS+"ref"+oblWS+"("+commonCharsWithoutComma+")");
            replacements.Add ("$1$4");
        }


        // arguments declaration      if out and ref keyword where not removed before this point it would also convert them ("out hit" in Physics.Raycast() calls) or prevent the convertion ("ref aType aVar" as function argument)
        string refKeyword = "";
        if (removeRefKeyword)
            refKeyword = "(ref"+oblWS+")?";

        patterns.Add ("(?<begining>(\\(|,){1}"+optWS+")"+refKeyword+"(?<type>"+commonCharsWithoutComma+")"+oblWS+"(?<name>"+commonName+")"+optWS+"(?<end>\\)|,){1}");
        replacements.Add ("${begining} ${name}: ${type}${end}");
        // as regex doesn't overlap themselves, only half of the argument have been converted
        // I need to run the regex  a second time
        patterns.Add ("(?<begining>(\\(|,){1}"+optWS+")"+refKeyword+"(?<type>"+commonCharsWithoutComma+")"+oblWS+"(?<name>"+commonName+")"+optWS+"(?<end>\\)|,){1}");
        replacements.Add ("${begining}${name}: ${type}${end}");


        DoReplacements ();


        // loop through functions and search for variable declaration that happend several times
        // leave only the first declaration
        pattern = "function"+oblWS+"(?<blockName>"+commonName+")"+optWS+"\\("+argumentsChars+"\\)("+optWS+":"+optWS+commonCharsWithSpace+")?"+optWS+"{"; 
        allFunctionsDeclarations = ReverseMatches (convertedCode, pattern);

        foreach (Match aFunctionDeclaration in allFunctionsDeclarations) {
            Block functionBlock = new Block (aFunctionDeclaration, convertedCode);

            pattern = "var"+oblWS+"(?<varName>"+commonName+")"+optWS+":"+optWS+"(?<varType>"+commonChars+")"+optWS+"(?<ending>(=|;))"; // don't match var declaration in foreach loop
            List<Match> allVariablesDeclarations = ReverseMatches (functionBlock.text, pattern);

            foreach (Match aVariableDeclaration in allVariablesDeclarations) {
                string varName = aVariableDeclaration.Groups["varName"].Value;
                string varType = aVariableDeclaration.Groups["varType"].Value.Replace ("[", Regex.Escape ("["));
                //varType = varType.Escape ("[");
                string ending = aVariableDeclaration.Groups["ending"].Value; 

                // how many time this variable is (still) declared in the function ?
                pattern = "var"+oblWS+varName+optWS+":"+optWS+varType;
                int declarationsCount = Regex.Matches (functionBlock.newText, pattern).Count;

                if (declarationsCount <= 1) // no need to go forward with this particular variable
                    continue;
                
                // it's at least the second time that variable is declared in the function
                // that will throw an error in the Unity console
                // so replace the declaration by the var name, if a value is set at the same time (var aVar: aType = whatever;), or just delete the declaration (var aVar: aType;)
                
                // here I can't replace the declaration in functionBlock with String.Replace because it could match sevral declaration at the same time
                // I have to use Insert and Remove, that's why the function and variable declaration are looped backward

                // remove old declaration
                functionBlock.newText = functionBlock.newText.Remove (aVariableDeclaration.Index, aVariableDeclaration.Length);

                // add the new one (if needed)
                if (ending == "=")
                    functionBlock.newText = functionBlock.newText.Insert (aVariableDeclaration.Index, varName+" "+ending);

                //varList.Add (varName, declarationsCount--);
            } // end loop variable declarations

            // replace old function text by new one in convertedCode
            convertedCode = convertedCode.Replace (functionBlock.text, functionBlock.newText);
        } // end lopp function declarations
    } // end of method Functions


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Convert Properties declarations
    /// </summary>
    public void Properties () {
            
        // find properties
        pattern = "(?<visibility>"+visibilityAndStatic+oblWS+")?(?<blockType>"+commonCharsWithSpace+")"+oblWS+"(?<blockName>"+commonName+")"+optWS+"{";
        List<Match> allProperties = ReverseMatches (convertedCode, pattern);

        foreach (Match aProp in allProperties) {
            // first check if this is really a property declaration
            string[] forbiddenBlockTypes = {"enum", "class", "extends", "implements", "new", "else", "struct", "interface"};
            
            //List<string> forbiddenBlockTypes = new List<string> (types);
            //if (forbiddenBlockTypes.Contains (aProp.Groups["blockType"].Value))
            //    continue;

            bool isAPropDeclaration = true;
            string blockType = aProp.Groups["blockType"].Value;
           
            foreach (string type in forbiddenBlockTypes) {
                if (blockType.Contains (type))
                    isAPropDeclaration = false;
            }

            if ( ! isAPropDeclaration)
                continue;
 

            string[] forbiddenBlockNames = {"get", "set", "else", "if"};
            
            //List<string> forbiddenBlockNames = new List<string> (names);
            //if (forbiddenBlockNames.Contains (aProp.Groups["blockName"].Value))
            //    continue;

            foreach (string type in forbiddenBlockNames) {
                if (aProp.Groups["blockName"].Value.Contains (type))
                    isAPropDeclaration = false;
            }

            if ( ! isAPropDeclaration)
                continue;


            //--------------------
            // Ok now we are sure this is a property declaration

            Block PropBlock = new Block (aProp, convertedCode);
            //Debug.Log ("property : "+aProp.Value+" | "+PropBlock.text);

            string property = "";
            string visibility = "";

            // search getters
            pattern = "get"+optWS+"({|;)";
            Match getterMatch = Regex.Match (PropBlock.text, pattern);

            if (getterMatch.Success) {
                visibility = aProp.Groups["visibility"].Value; // getter has the visibilty of the property
                if (visibility != "")
                    visibility += " ";

                // if the match value contains a curly bracket, it's not an empty getter, so I have to get it's content
                if (getterMatch.Value.Contains ("{")) {
                    Block getterBlock = new Block (getterMatch, PropBlock.text);

                    property += visibility+"function get "+PropBlock.name+"(): "+PropBlock.type+" ";
                    property += getterBlock.text+EOL;
                }
                else { // empty getter    "get;"
                    property += 
                    visibility+"function get "+PropBlock.name+"(): "+PropBlock.type+" {"+EOL
                        +"\treturn "+PropBlock.name.ToLower ()+";"+EOL
                    +"}"+EOL;
                }
                //
            }

            // now search for the setter
            pattern = "((?<visibility>protected|private|public)"+oblWS+")?set"+optWS+"({|;)";
            Match setterMatch = Regex.Match (PropBlock.text, pattern);

            if (setterMatch.Success) {
                visibility = setterMatch.Groups["visibility"].Value;
                if (visibility == "")
                    visibility = "public ";
                else
                    visibility += " ";

                if (setterMatch.Value.Contains ("{")) {
                    Block setterBlock = new Block (setterMatch, PropBlock.text);

                    property +=  visibility+"function set "+PropBlock.name+"(value: "+PropBlock.type+") ";
                    property += setterBlock.text+EOL;
                }
                else { // empty setter    "set;"
                    property += 
                    visibility+"function set "+PropBlock.name+"(value: "+PropBlock.type+") {"+EOL
                        +"\t"+PropBlock.name.ToLower ()+" = value;"+EOL
                    +"}"+EOL;
                }
            }

            //Debug.Log ("new prop : "+property);
            string cSharpProperty = aProp.Value.Replace ("{", PropBlock.text);

            convertedCode = convertedCode.Replace (cSharpProperty, property); // replace property block by the new(s) function(s)
        } // end lopping on properties
    } // end of method Properties


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Add the keyword public when no visibility (or just static) is set (the default visibility is public in JS but private in C#)
    /// Works also for functions, classes and enums
    /// </summary>
    void AddVisibility () {
        // the default visibility for variable and functions is public in JS but private in C# => add the keyword private when no visibility (or just static) is set 
        patterns.Add ("([;{}\\]>/]+"+optWS+")((var|function|enum|class)"+oblWS+")");
        replacements.Add ("$1private $3");

        patterns.Add ("(\\*"+optWS+")((var|function|enum|class)"+oblWS+")"); // add a / after \\*
        replacements.Add ("$1private $3");

        patterns.Add ("(//.*"+optWS+")((var|function|enum|class)"+oblWS+")");
        replacements.Add ("$1private $3");

        patterns.Add ("((\\#else|\\#endif)"+oblWS+")((var|function|enum|class)"+oblWS+")");
        replacements.Add ("$1private $4");


        // static
        patterns.Add ("([;{}\\]]+"+optWS+")static"+oblWS+"((var|function)"+oblWS+")");
        replacements.Add ("$1private static $4");

        patterns.Add ("(\\*"+optWS+")static"+oblWS+"((var|function)"+oblWS+")");  // add a / after \\*
        replacements.Add ("$1private static $4");

        patterns.Add ("(//.*"+optWS+")static"+oblWS+"((var|function)"+oblWS+")");
        replacements.Add ("$1private static $4");

        patterns.Add ("((\\#else|\\#endif)"+oblWS+")((var|function)"+oblWS+")");
        replacements.Add ("$1private static $4");

        DoReplacements ();


        // all variables gets a public or static public visibility but this shouldn't happend inside functions, so remove that

        pattern = "function"+oblWS+"(?<blockName>"+commonName+")"+optWS+"\\(.*\\)("+optWS+":"+optWS+commonCharsWithSpace+")?"+optWS+"{";
        List<Match> allFunctions = ReverseMatches (convertedCode, pattern);

        foreach (Match aFunction in allFunctions) {
            Block function = new Block (aFunction, convertedCode);

            if (function.isEmpty)
                continue;

            patterns.Add ("private"+oblWS+"(static"+oblWS+"var)");
            replacements.Add ("$2");
            patterns.Add ("(static"+oblWS+")?private"+oblWS+"var");
            replacements.Add ("$1var");

            function.newText = DoReplacements (function.text);
            convertedCode = convertedCode.Replace (function.text, function.newText);
        }
    } // end AddVisibility ()
} // end of class CSharpToUnityScript_Main
