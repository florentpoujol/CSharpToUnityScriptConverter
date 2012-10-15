/// <summary>
/// CSharpToUnityScriptConverter class for Unity3D
///
/// This class handle the convertion of code from C# to UnityScript.
/// Used by the "C# to UnityScript" extension for Unity3D.
///
/// Use instructions :
/// 
/// Initiate the converter by creating an instance of it
/// Then call the Convert(string inputCode) method with the input code (in C#) to be converted as parameter
/// The converted code in UnityScript is then available in the public member convertedCode
///
/// Created by Florent POUJOL
/// florent.poujol@gmail.com
/// http://www.florent-poujol.fr/en
/// Profile on Unity's forums : http://forum.unity3d.com/members/23148-Lion
/// </summary>


using UnityEngine; 
using UnityEditor; // EditorGUILayout
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions; // Regex.Replace(), Match, Matches, MatchCollection...
using System.IO; // Directory File StreamReader/Writer


public class CSharpToUnityScriptConverter: RegexUtilities {

    // list of the projct's classes. Filled in the constructor and used in Classes()
    static List<string> projectClasses = new List<string>();

    // list of the Unity API classes, extracted from the file "CSharpToUnityScript/Editor/UnityClasses.txt"
    static List<string> unityClasses = new List<string>();

    // list of the imported assemblies
    // see explanation at the end of Classes()
    public static List<string> importedAssemblies = new List<string>();

    // list of data types, including  the built-in C# data types, the Unity classes and the project classes
    public static string dataTypes = ""; // "regex list" "(data1|data2|data3|...)"

    // conversion options
    public static bool convertMultipleVarDeclaration = false;
    public static bool removeRefKeyword = true;
    public static bool removeOutKeyword = true;


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Constructor and main method
    /// </summary>
    /// <param name="sourceDirectory">The directory where to look for files</param>
    public CSharpToUnityScriptConverter( string sourceDirectory ) {
        importedAssemblies.Clear();
        projectClasses.Clear();
        unityClasses.Clear();
        StreamReader reader;

        // reading unity classes
        string path = Application.dataPath+"/CSharpToUnityScript/Editor/UnityClasses.txt";
        if (File.Exists( path ) ) {
            reader = new StreamReader( path );
            string line = "";

            while( true ) {
                line = reader.ReadLine();
                if (line == null )
                    break;

                unityClasses.Add( line.Trim() );
            }

            reader.Close();
        }
        else
            Debug.LogError( "CSharpToUnityScriptConverter : The file that contains all Unity classes does not exists at path ["+path+"]" );

        // set datatypes
        dataTypes = regularTypes;

        foreach (string _class in unityClasses )
            dataTypes = dataTypes.Replace( ")", "|"+_class+")" );

        // loop trough all poject's file, extract the data types (classes, enums and structs)
        string[] paths = Directory.GetFiles( Application.dataPath+sourceDirectory, "*.cs", SearchOption.AllDirectories );
        
        foreach (string scriptPath in paths ) {
            reader = new StreamReader( scriptPath );
            string scriptContent = reader.ReadToEnd();
            reader.Close();

            pattern = "\\b(?<type>class|interface|struct|enum)"+oblWS+"(?<name>"+commonName+"\\b)";
            MatchCollection allDataTypes = Regex.Matches( scriptContent, pattern );

            foreach (Match aDataType in allDataTypes ) {
                string name = aDataType.Groups["name"].Value;

                // discard results where the first letter is lowercase
                if (name[0] == char.ToLower( name[0] ) )
                    continue;

                dataTypes = dataTypes.Replace( ")", "|"+name+")" );

                if (aDataType.Groups["type"].Value == "class" )
                    projectClasses.Add( name );
            }
        }

       // Debug.Log ("Data types : "+dataTypes);
    }


    // ----------------------------------------------------------------------------------
    
    /// <summary>
    ///  Main method that perform generic conversion and call the other method for specific conversion
    /// </summary>
    /// <param name="inputCode">The code in C# to be converted in UnityScript</param>
    /// <returns>The converted code in UnityScript</returns>
    public string Convert( string inputCode ) {
        convertedCode = inputCode;
        Convert();
        return convertedCode;
    }


    /// <summary>
    ///  Main method that perform generic conversion and call the other method for specific conversion
    /// Assume at the beginning that convertedCode is the C# code to be converted
    /// convertedCode
    /// </summary>
    private void Convert() {
        // GENERIC COLLECTIONS

        // Add a dot before the opening chevron  List.<float>
        patterns.Add (genericCollections+optWS+"<" );
        replacements.Add( "$1$2.<" );

        // Add a whitespace between two closing chevron   Dictionary.<string,List<string> > 
        //patterns.Add( "("+genericCollections+optWS+"\\.<.+)>>" );
        // don't this pattern is used by mask or layerMask related things ? => only the opposite <<
        patterns.Add( ">>" );
        replacements.Add( "> >" );
 

        // LOOPS
        // foreach(bla in bla) => for(bla in bla)
        patterns.Add( "foreach("+optWS+"\\(.+"+oblWS+"in"+oblWS+".+\\))" );
        replacements.Add( "for$1" );


        // GETCOMPONENT (& Co)
        // GetComponent<T>() => GetComponent.<T>()
        //patterns.Add( "(\\b(AddComponent|GetComponent|GetComponents|GetComponentInChildren|GetComponentsInChildren)"+optWS+")(?<type><"+optWS+commonChars+optWS+">)" );
        patterns.Add( "(\\b(AddComponent|GetComponent|GetComponents|GetComponentInChildren|GetComponentsInChildren)"+optWS+")<" );
        replacements.Add( "$1.<" );


        // ABSTRACT
        //patterns.Add( "((public|private|protected|static)"+oblWS+")abstract"+oblWS);
        //replacements.Add( "$1" );
        patterns.Add( "\\babstract"+oblWS+"class\\b" );
        replacements.Add( "class" );
        // abstract methods are dealt with at the beginning of Function().
        // What about abstract variables or properties ?


        // VIRTUAL
        patterns.Add("(\\b(public|private|protected|static)"+oblWS+")virtual\\b" );
        replacements.Add("$1" );


        // YIELDS
        // must remove the return keyword after yield
        // yield return null; => yield ;
        // yield return new WaitForSecond(1.0f); => yield WaitForSecond(1.0f);
        patterns.Add("yield"+optWS+"return"+optWS+"(null|0|new)" );
        replacements.Add("yield " );

        DoReplacements();
    

        // CLASSES
        Classes();

        // VARIABLES
        Variables();
        
        // FUNCTIONS
        Functions();
        
        // PROPERTIES
        // why after Functions() ? => to let the function pattern be converted first
        Properties();

        // VISIBILITY
        AddVisibility();


        // STRING AND BOOL
        
        //patterns.Add ( "("+commonName+optWS+":"+optWS+")?string(?<end>("+optWS+"(\\[[0-9,\\s]*\\])+)?"+optWS+"(=|;|,|\\)|in|{))" );
        patterns.Add( "((:|new)"+optWS+")?string(?<end>("+optWS+"(\\["+commonNameWithSpaceAndComa+"\\])+)?)" );
        replacements.Add( "$1String${end}" );

        patterns.Add( "((:|new)"+optWS+")?bool(?<end>("+optWS+"\\[("+commonNameWithSpaceAndComa+"\\])+)?)" );
        replacements.Add( "$1boolean${end}" );

        // with arrays
        // string
        //patterns.Add ( "(new"+oblWS+")string(?<end>("+optWS+"\\[[0-9,]+\\])?"+optWS+"(=|;|,|\\)|in|{))" );
        //replacements.Add ( "$1String${end}" );

        // with generic collections
        patterns.Add ( "((<|,)"+optWS+")string(?<end>("+optWS+"\\[[,\\s]*\\])?"+optWS+"(>|,))" );
        replacements.Add ( "$1String${end}" );

        patterns.Add ( "((<|,)"+optWS+")bool(?<end>("+optWS+"\\[[,\\s]*\\])?"+optWS+"(>|,))" );
        replacements.Add ( "$1boolean${end}" );


        // #REGION
        patterns.Add( "\\#(region|REGION)"+oblSpaces+commonChars+"("+oblSpaces+commonChars+")*" );
        replacements.Add( "" );
        
        patterns.Add( "\\#(endregion|ENDREGION)" );
        replacements.Add( "" );


        // DEFINE
        patterns.Add( "\\#(define|DEFINE)"+oblSpaces+commonName+"("+oblSpaces+commonName+")*" );
        replacements.Add( "" );

        DoReplacements();

        //convertedCode = "#pragma strict"+EOL+convertedCode;
    } // end of method Convert()

    
    // ----------------------------------------------------------------------------------

    /// <summary> 
    /// Convert stuffs related to classes : declaration, inheritance, parent constructor call, Assembly imports
    /// </summary>
    void Classes () {
        // CLASSES DECLARATION
        pattern = "(\\bclass"+oblWS+commonName+")(?<keyword>"+optWS+":"+optWS+")(?<parent>"+commonName+")"+optWS+"{";
        MatchCollection allClasses = Regex.Matches( convertedCode, pattern );

        foreach (Match aClass in allClasses ) {
            // assume parent is a class unless
            // it is not in UnityClasses nor in projectClasses and its name begin by an uppercase I
            string parent = aClass.Groups["parent"].Value;
            string keyword = " extends ";
            
            if (!unityClasses.Contains(parent) && !projectClasses.Contains(parent) && parent[0] == 'I' )
                keyword = " implements ";

            string newClassDeclaration = aClass.Value.Replace( aClass.Groups["keyword"].Value, keyword );
            convertedCode = convertedCode.Replace( aClass.Value, newClassDeclaration );
        }


        pattern = "(\\bclass"+oblWS+commonName+")(?<keyword>"+optWS+":"+optWS+")(?<parent>"+commonName+")"+
        "(?<interfaces>("+optWS+","+optWS+commonName+")+)"+
        "(?<end>"+optWS+"{)";
        allClasses = Regex.Matches( convertedCode, pattern );

        foreach (Match aClass in allClasses ) {
            string parent = aClass.Groups["parent"].Value;
            string keyword = " extends ";
            
            if (!unityClasses.Contains(parent) && !projectClasses.Contains(parent) && parent[0] == 'I' )
                keyword = " implements ";

            string newClassDeclaration = aClass.Value.Replace( aClass.Groups["keyword"].Value, keyword );

            // if keyword is implements, there is nothing more to do
            // but if keyword is extends, needs to add "implements" after the parent and strip the first coma (between implements and the first interface)
            if (keyword == " extends " ) {
                newClassDeclaration = newClassDeclaration.Replace( parent, parent+" implements " );
                string newInterfaces = aClass.Groups["interfaces"].Value.TrimStart(' ', ','); // remove space and coma
                newClassDeclaration = newClassDeclaration.Replace( aClass.Groups["interfaces"].Value, newInterfaces );
            }

            convertedCode = convertedCode.Replace( aClass.Value, newClassDeclaration );
        }


        // PARENT AND ALTERNATE CONSTRUCTOR CALL

        // loop the classes declarations in the file
        pattern = "\\bclass"+oblWS+"(?<blockName>"+commonName+")"+
        "("+oblWS+"extends"+oblWS+commonName+")?[^{]*{";
        List<Match> allReverseClasses = ReverseMatches( convertedCode, pattern );
        
        foreach (Match aClass in allReverseClasses ) {
            Block classBlock = new Block( aClass, convertedCode );
            classBlock.newText = classBlock.text;

            if (classBlock.isEmpty )
                continue;

            MatchCollection allConstructors;

            // look for constructors in the class that call the parent constructor
            // if the class declaration doesn't contains "extends", a constructor has no parent to call
            if (classBlock.declaration.Contains("extends") ) { 
                // all constructors in this class
                pattern = "\\bpublic"+optWS+"(?<blockName>"+classBlock.name+")"+optWS+"\\([^\\)]*\\)(?<base>"+optWS+":"+optWS+"base"+optWS+"\\((?<args>[^\\)]*)\\))"+optWS+"{";
                allConstructors = Regex.Matches( classBlock.text, pattern ); 

                foreach (Match aConstructor in allConstructors ) {
                    // remove :base() from the constructor declaration
                    string newConstructor = aConstructor.Value.Replace( aConstructor.Groups["base"].Value, "" );
                    // add super(); to the constructor body
                    string super = "{"+EOL+"super("+aConstructor.Groups["args"]+" );";
                    newConstructor = newConstructor.Replace( "{", super );
                    
                    classBlock.newText = classBlock.newText.Replace( aConstructor.Value, newConstructor );
                }
            }


            // look for constructors in this class that call others constructors of the same class
            pattern = "\\bpublic"+optWS+"(?<blockName>"+classBlock.name+")"+optWS+"\\(.*\\)(?<this>"+optWS+":"+optWS+"this"+optWS+"\\((?<args>.*)\\))"+optWS+"{";
            allConstructors = Regex.Matches( classBlock.newText, pattern );

            foreach (Match aConstructor in allConstructors ) {
                // remove :this() from the constructor declaration
                string newConstructor = aConstructor.Value.Replace( aConstructor.Groups["this"].Value, "" );
                // add Classname() to the constructor body
                string super = "{"+EOL+classBlock.name+"("+aConstructor.Groups["args"]+" );";
                newConstructor = newConstructor.Replace( "{", super );
                
                classBlock.newText = classBlock.newText.Replace( aConstructor.Value, newConstructor );
            }
            
            // we won't do more search/replace for this class 
            // now replace in convertedCode, the old classBlock.text by the new
            convertedCode = convertedCode.Replace( classBlock.text, classBlock.newText );
        } // end looping through classes in that file


        // ATTRIBUTES

        // no script, no params
        patterns.Add( "\\["+optWS+"(?<attribute>RPC|HideInInspector|System.NonSerialized|SerializeField)"+optWS+"\\]" );
        replacements.Add( "@${attribute}" );

        // no script, with params
        patterns.Add( "\\["+optWS+"(?<attribute>DrawGizmo|Conditional|MenuItem)"+optWS+"(?<params>\\(.*\\))"+optWS+"\\]" );
        replacements.Add( "@${attribute}${params}" );
        
        // require component  need to remove typeof()
        // why don't we need to do that with CustomEditor ??
        patterns.Add( "\\["+optWS+"RequireComponent"+optWS+"\\("+optWS+"typeof"+optWS+"\\((?<type>"+commonName+")\\)"+optWS+"\\)"+optWS+"\\]" );
        replacements.Add( "@script RequireComponent(${type})" );

        // script + params
        string attributes = "(?<attributes>AddComponentMenu|ContextMenu|ExecuteInEditMode|ImageEffectOpaque|"+
        "ImageEffectTransformsToLDR|NotConvertedAttribute|NotRenamedAttribute|System.Serializable|"+
        "CanEditMultipleObjects|CustomEditor|PostProcessAttribute|PreferenceItem)";
        patterns.Add( "\\["+optWS+attributes+optWS+"(?<params>\\(.*\\))?"+optWS+"\\]" );
        replacements.Add( "@script ${attributes}${params}" );

        
        // STRUCT
        patterns.Add( "\\bstruct"+oblWS+commonName+optWS+"{" );
        replacements.Add( "class $2 extends System.ValueType {" );


        // base. => super.      
        patterns.Add( "\\bbase"+optWS+"\\." );
        replacements.Add( "super$1." );

        DoReplacements();


        // ASSEMBLY IMPORT
        
        // in UnityScript, each assembly has to be imported once per project, or it will throw a warning in he Unity console for each duplicate assembly import
        // so keep track of the assemblies already imported in the project (in one of the previous file) and comment out the duplicate
        pattern = "\\busing"+oblWS+"(?<assemblyName>"+commonNameWithSpace+")"+optWS+";";
        MatchCollection allImports = Regex.Matches( convertedCode, pattern );

        foreach (Match import in allImports ) {
            string assemblyName = import.Groups["assemblyName"].Value.Replace( " ", "" );

            if (importedAssemblies.Contains(assemblyName) ) {
                convertedCode = convertedCode.Replace( import.Value, "// "+import.Value );
            }
            else {
                convertedCode = convertedCode.Replace( import.Value, "import "+assemblyName+";" );
                importedAssemblies.Add(assemblyName);
            }
        }
    } // end of method Classes


    // ----------------------------------------------------------------------------------

    /// </summary>
    /// Used in Variable() method below
    /// check if the first character is a letter or an underscore
    ///     actually this is alredy done by the regex itself, see commonName or commonChars
    /// make sure the name asn't more than tree spaces in it
    /// </summary>
    bool IsAValidName( string text ) {
        text = text.Trim();

        // check the number of spaces
        int spaceCount = 0;        foreach (char letter in text ) {
            if (letter == ' ' )
                spaceCount++;

            if (spaceCount >= 3 ) {
                Log( "IsAValidName() : Invalid name="+text );
                return false;
            }
        }

        return true;
    }

    
    // ----------------------------------------------------------------------------------

    /// <summary> 
    /// Convert stuffs related to variable declarations
    /// </summary>
    void Variables () {
        // MULTIPLE INLINE VARIABLE DECLARATION
            // multiple inline var declaration of the same type : "Type varName, varName = foo;"
            
        if (convertMultipleVarDeclaration) {
            // using dataTypes here, instead of commonChars or commonCharsWithSpace 
            // drastically reduces the number of false positiv returned by the regex
            // the pattern discard all match that have a coma or a semi colon when setting the value of a variable (in a string or a method call)
            pattern = "(?<varType>\\b"+dataTypes+")"+oblWS+"(?<varList>"+"[^,;{}]+"+optWS+"(="+optWS+"[^,;]+"+optWS+")?,{1}"+optWS+"[^;]*)+"+optWS+";";
            MatchCollection allDeclarations = Regex.Matches( convertedCode, pattern );

            foreach (Match aDeclaration in allDeclarations ) {
                //Debug.Log (aDeclaration.Value);
                if (aDeclaration.Value.Contains("\n") ) // discard results on several lines
                    continue;

                // look for method call pattern "method( arg1, arg2 );" to discard
                // will discard a legit match if a variable's value comes from a method with at least two parameters
                pattern = "\\b"+commonName+optWS+"\\(.+,{1}[^\\)]+\\)";
                if (Regex.Matches( aDeclaration.Value, pattern ).Count > 0 ){
                    //Debug.Log ("Discarding : "+aDeclaration.Value);  
                    continue;
                }
                
                // split the varlist using the coma
                string[] varList = aDeclaration.Groups["varList"].Value.Split(',');
                string varType = aDeclaration.Groups["varType"].Value;
                string newSyntax = "";

                foreach (string varName in varList ) {
                    if (varName.Contains("=") ) {
                        // add the varType beetween the varName and the equal sign
                        string varDeclaration = varName.Replace( "=", ": "+varType+" =" );
                        newSyntax += "var "+varDeclaration.Trim()+";"+EOL;
                    }
                    else 
                        newSyntax += "var "+varName.Trim()+": "+varType+";"+EOL;
                }

                convertedCode = convertedCode.Replace( aDeclaration.Value, newSyntax );
            }
        } // end if convertMultipleVarDeclaration
 

        // CONST
        patterns.Add( "(\\bconst"+oblWS+")(?<end>"+commonCharsWithSpace+oblWS+commonName+")" ); 
        replacements.Add( "${end}" );


        // VAR DECLARATION IN FOREACH LOOP
        patterns.Add( "(?<varType>\\b"+commonCharsWithSpace+")"+oblWS+"(?<varName>"+commonName+")(?<in>"+oblWS+"in"+oblWS+")" );
        replacements.Add( "var ${varName}: ${varType}${in}" );

        DoReplacements();


        // VAR DECLARATION WITHOUT VALUE

        // conversion will mess up if the declaration is not preceded by a visibility keyword 
        // but by something else like @HideInInspector or @System.Serialize which will be part of varType
        // that's why I use two patterns here instead of making the "visibility" optionnal in a single pattern
        string[] tempPatterns = { "(?<visibility>"+visibilityAndStatic+oblWS+")(?<varType>"+commonCharsWithSpace+")"+oblWS+"(?<varName>"+commonName+")(?<end>"+optWS+";)",
        "(?<varType>"+commonCharsWithSpace+")"+oblWS+"(?<varName>"+commonName+")(?<end>"+optWS+";)" };

        foreach (string tempPattern in tempPatterns ) {
            MatchCollection allVariables = Regex.Matches( convertedCode, tempPattern );

            foreach (Match aVariable in allVariables ) {
                string visibility = aVariable.Groups["visibility"].Value;
                string type = aVariable.Groups["varType"].Value;
                string name = aVariable.Groups["varName"].Value;
                string end = aVariable.Groups["end"].Value;

                if (!IsAValidName(visibility) || !IsAValidName(type) || !IsAValidName(name) )
                    continue;

                List<string> forbiddenTypes = new List<string>( 
                    new string[] { "import", "return", "yield return", "as", "else", "<", "<=", ">", ">=" }
                );

                if (forbiddenTypes.Contains(type.Trim()) || type.Contains("import ") || type.Contains(" = ") )
                    continue;
                // some value setting got treated like var declaration   variable = value; => var variable: = value;

                // stuff like     othervar as Type;   got converted to   var Type: othervar as;
                if (type.TrimEnd().EndsWith(" as") )
                    continue;

                //
                pattern = aVariable.Value.Replace( "HideInInspector ", "" ).Replace( "System.NonSerialized ", "" );
                type = type.Replace( "HideInInspector ", "" ).Replace( "System.NonSerialized ", "" );

                patterns.Add( EscapeRegexChars( pattern ) );
                replacements.Add( visibility+"var "+name+": "+type+end );
            }

            DoReplacements();
        }


        // VAR DECLARATION WITH VALUE
        tempPatterns = new string[] { "(?<visibility>"+visibilityAndStatic+oblWS+")(?<varType>"+commonCharsWithSpace+")"+oblWS+"(?<varName>\\b"+commonName+")(?<varValue>"+optWS+"="+optWS+"[^;]+;)",
        "(?<varType>"+commonCharsWithSpace+")"+oblWS+"(?<varName>\\b"+commonName+")(?<varValue>"+optWS+"="+optWS+"[^;]+;)" };
        
        foreach (string tempPattern in tempPatterns ) {
            MatchCollection allVariables = Regex.Matches( convertedCode, tempPattern );

            foreach (Match aVariable in allVariables ) {
                string visibility = aVariable.Groups["visibility"].Value;
                string type = aVariable.Groups["varType"].Value.Replace( "HideInInspector", "").Replace( "System.NonSerialized", "" );;
                string name = aVariable.Groups["varName"].Value;
                string varValue = aVariable.Groups["varValue"].Value;

                if (!IsAValidName(visibility) || !IsAValidName(type) || !IsAValidName(name) )
                    continue;

                List<string> forbiddenTypes = new List<string>( 
                    new string[] { "import", "return", "yield return", "as", "else" }
                );

                if (forbiddenTypes.Contains(type.Trim()) )
                    continue;

                pattern = aVariable.Value.Replace( "HideInInspector ", "" ).Replace( "System.NonSerialized ", "" );
                type = type.Replace( "HideInInspector ", "" ).Replace( "System.NonSerialized ", "" );

                patterns.Add( EscapeRegexChars( pattern ) );
                replacements.Add( visibility+"var "+name+": "+type+varValue );
            }

            DoReplacements();
        }
        
        
        // CASTING

        // (int)() => paseInt()
        /*patterns.Add( "\\("+optWS+"int"+optWS+"\\)"+optWS+"\\(?"+optWS+"(?<afterCast>"+commonChars+")"+optWS+"\\)?" );
        replacements.Add( "parseInt(${afterCast})" );

        patterns.Add( "\\("+optWS+"float"+optWS+"\\)"+optWS+"\\(?"+optWS+"(?<afterCast>"+commonChars+")"+optWS+"\\)?" );
        replacements.Add( "parseFloat(${afterCast})" );*/

        //DoReplacements();

        // using dataTypes prevent to match if patterns like "if(var1) var2;"
        tempPatterns = new string[] { 
        "\\("+optWS+"(?<type>"+dataTypes+")"+optWS+"\\)"+optSpaces+"(?<afterCast>\\(?"+optWS+commonChars+optWS+"\\)?)", // don't allow space if parenthesis are optionnal
        "\\("+optWS+"(?<type>"+dataTypes+")"+optWS+"\\)"+optSpaces+"(?<afterCast>\\([^;}]+(;|}))" }; 
        
        for( int i = 0; i < tempPatterns.Length; i++ ) {
            MatchCollection allCasts = Regex.Matches( convertedCode, tempPatterns[i] );

            foreach (Match aCast in allCasts ) {
                string type = aCast.Groups["type"].Value;
                string afterCast = aCast.Groups["afterCast"].Value;

                if (i == 1 ) { // second pattern
                    // find the closing parenthesis
                    int oppenedParenthesis = 1;
                    afterCast = afterCast.TrimStart('(');
                    string tempAfterCast = "";

                    foreach (char letter in afterCast ) {
                        tempAfterCast += letter.ToString();

                        if (letter == '(' ) {
                            oppenedParenthesis++;
                            continue;
                        }

                        if (letter == ')' ) {
                            oppenedParenthesis--;

                            if (oppenedParenthesis == 0 ) // we have reached the final closing parenthesis
                                break;

                            continue;
                        }
                    }

                    afterCast = tempAfterCast.TrimEnd(')');
                }

                if (regularTypes.Contains( type.Trim() ) ) { 
                    // the type is a value type which does not need the "as" keyword to be cast in UnityScript
                    // just keep the "aftercast" without the parenthesis
                    if (afterCast.StartsWith("(") )
                        afterCast = afterCast.Trim('(', ')');

                    if (type.Trim() == "int" )
                        afterCast = "parseInt("+afterCast+")";
                    else if (type.Trim() == "float" )
                        afterCast = "parseFloat("+afterCast+")";

                    convertedCode = convertedCode.Replace( aCast.Value, afterCast );
                }
                else {
                    // (Type)(aftercast) => aftercast as Type
                    if (afterCast.StartsWith("(") )
                        afterCast = afterCast.Trim('(', ')');

                    convertedCode = convertedCode.Replace( aCast.Value, afterCast+" as "+type );
                }
            }
        }


        // ARRAYS

        // string[] array1; => var array1: String[] (already done)
        // string[,] array2 = new string[5,6];  => var array2: Strint[] = new String[5,6];

        //string[] array3 = new string[] { "machin", "machin"};
        //bool[] array4 = { false, true};
            // both => var array3: String[] = { }

        // string[,] array2 = new string[5,6];  => var array2: Strint[] = new String[5,6];
        //patterns.Add( "\\b(?<type>"+commonName+optWS+"\\[,*\\])"+oblWS+"(?<name>"+commonName+")"+optWS+"=" );
        // replacements.Add( "" );

        // = new String[] {}; => = {};
        patterns.Add( "(?<equal>="+optWS+")?new"+oblWS+commonName+optWS+"(\\[[0-9, ]*\\])+(?<end>"+optWS+"{)" );
        replacements.Add( "${equal}${end}" );

        DoReplacements();

        // replace curly brackets by square bracket
        pattern = "(?s)(="+optWS+"){(?<values>.*)}(?<end>"+optWS+";)"; // the (?s) means that the dot represent every character, including new line \n
        MatchCollection allMatches = Regex.Matches( convertedCode, pattern );
        
        foreach (Match aMatch in allMatches ) {
            string newText = aMatch.Value.Replace("{", "[").Replace("}", "]");
            convertedCode = convertedCode.Replace( aMatch.Value, newText );
        }


        // LITTERAL CHAR 'a' => 'a'[0]
        patterns.Add( "'.{1}'" );
        replacements.Add( "$0[0]" );


        // remove f to Float values
        // actually not really needed, it works with the f
        patterns.Add( "([0-9]{1}(\\.[0-9]+)?)(f|F)" );
        replacements.Add( "$1" );

        DoReplacements();


        // remove @ in  'string aVariable = @"a string";"  and add a extra \ to the existing \
        pattern = "(="+optWS+")@(?<text>"+optWS+"\"[^;]+;)";
        allMatches = Regex.Matches( convertedCode, pattern );

        foreach (Match aMatch in allMatches ) {
            string newText = aMatch.Value.Replace ("@\"", "\"").Replace ("\\", "\\\\" );
            convertedCode = convertedCode.Replace( aMatch.Value, newText );
        }
    } // end of method Variable()


    // ----------------------------------------------------------------------------------
    
    /// <summary> 
    /// Convert stuffs related to methods
    /// </summary>
    void Functions() {
        // ABSTRACT
        
        // first check for abtract methods, strip the abstract keyword while add curly bracket instead of the semi colon
        pattern = "(\\babstract"+oblWS+")(?<body>"+commonCharsWithSpace+oblWS+commonName+optWS+"\\(.*\\))("+optWS+";)";
        MatchCollection allFunctionsDeclarations = Regex.Matches(convertedCode, pattern);

        foreach (Match aFunctionDeclaration in allFunctionsDeclarations) {
            //Debug.Log( "Abstract method : "+aFunctionDeclaration.Value );
            string newFunc = aFunctionDeclaration.Groups["body"].Value+" {}";
            convertedCode = convertedCode.Replace( aFunctionDeclaration.Value, newFunc );
        }


        // INTERFACE

        pattern = "interface"+oblWS+commonName+optWS+"{";
        List<Match> allInterfaces = ReverseMatches (convertedCode, pattern);
        
        foreach (Match anInterface in allInterfaces) {
            Block interfaceBlock = new Block(anInterface, convertedCode);
            

            /*patterns.Add( "void"+oblWS+"(?<functionCorp>"+commonName+optWS+"\\("+argumentsChars+"\\)"+optWS+";)" );
            replacements.Add( "function ${functionCorp}" );
            patterns.Add( "(?<returnType>"+commonCharsWithSpace+")"+oblWS+"(?<functionCorp>"+commonName+optWS+"\\("+argumentsChars+"\\))(?<end>"+optWS+";)" );
            replacements.Add( "function ${functionCorp}: ${returnType}${end}" );*/


            pattern = "(?<returnType>"+commonCharsWithSpace+")"+oblWS+"(?<functionCorp>"+commonName+optWS+"\\("+argumentsChars+"\\))(?<end>"+optWS+";)";
            MatchCollection allFunctions = Regex.Matches(interfaceBlock.text, pattern);

            foreach (Match aFunction in allFunctions) {
                string type = aFunction.Groups["returnType"].Value;

                if (type ==  "void") 
                    type = "";
                else
                    type = ": "+type;

                string newFunction = "function "+aFunction.Groups["functionCorp"].Value+type+";";
                interfaceBlock.newText = interfaceBlock.newText.Replace( aFunction.Value, newFunction );
            }


            // interfaceBlock.newText = DoReplacements(interfaceBlock.text);
            convertedCode = convertedCode.Replace(interfaceBlock.text, interfaceBlock.newText);
            /*
            MatchCollection allFunctions = Regex.Matches(interfaceBlock.text, pattern);

            foreach (Match afunction in allFunctions) {
                interfaceBlock.newText = interfaceBlock.newText
            }*/
        }


        // FUNCTION DECLARATION

        // using a simple pattern/replacement regex as below match way too much things it shouldn't
        // So I need to check each match before allowing the replacement
        pattern = "(?<visibility>"+visibilityAndStatic+oblWS+")?(?<returnType>"+commonCharsWithSpace+")"+oblWS+"(?<functionName>"+commonName+")"+optWS+"(?<args>\\("+argumentsChars+"\\))(?<end>"+optWS+"{)"; 
        allFunctionsDeclarations = Regex.Matches(convertedCode, pattern);

        foreach (Match aFunctionDeclaration in allFunctionsDeclarations) {
            string visibility = aFunctionDeclaration.Groups["visibility"].Value;
            string returnType = aFunctionDeclaration.Groups["returnType"].Value;
            string functionName = aFunctionDeclaration.Groups["functionName"].Value;
            string args = aFunctionDeclaration.Groups["args"].Value;
            string end = aFunctionDeclaration.Groups["end"].Value;

            if (returnType == "else" && functionName == "if") // do not match else if () statement
                continue;

            if (returnType == "new") // do not match class instanciatton inside an if statement   ie : new Rect ()) {}  => it shouldn't anymore anyway, thanks to argumentsChars instead of ".*"
                continue;

            if (returnType.Contains ("override"))
                returnType = returnType.Replace("override", "").Trim();  // this will left the override keyword after the prefix

            // if we are there, it's really a function declaration that has to be converted
            if (returnType == "void" || returnType == "public")
                returnType = "";
            else
                returnType = ": "+returnType;

            string newFunctionDeclaration = visibility+"function "+functionName+args+returnType+end;
            convertedCode = convertedCode.Replace( aFunctionDeclaration.Value, newFunctionDeclaration );
        }


        // remove out keyword in arguments
        if (removeOutKeyword) {
            patterns.Add( "(,|\\()"+optWS+"out"+oblWS+"("+commonCharsWithoutComma+")" );
            replacements.Add( "$1$4" );
        }

        // remove ref keyword in arguments  => let in so it will throw an error and the dev can figure out what to do 
        if (removeRefKeyword) {
            patterns.Add( "(,|\\()"+optWS+"ref"+oblWS+"("+commonCharsWithoutComma+")" );
            replacements.Add( "$1$4" );
        }


        // PARAMETERS DECLARATION
        
        // if out and ref keyword where not removed before this point it would also convert them ("out hit" in Physics.Raycast() calls) or prevent the convertion ("ref aType aVar" as function argument)
        string refKeyword = "";
        if (removeRefKeyword)
            refKeyword = "(ref"+oblWS+")?";

        patterns.Add( "(?<begining>(\\(|,){1}"+optWS+")"+refKeyword+"(?<type>"+commonCharsWithoutComma+")"+oblWS+"(?<name>"+commonName+")"+optWS+"(?<end>\\)|,){1}" );
        replacements.Add( "${begining} ${name}: ${type}${end}" );
        // as regex doesn't overlap themselves, only half of the argument have been converted
        // I need to run the regex  a second time
        patterns.Add( "(?<begining>(\\(|,){1}"+optWS+")"+refKeyword+"(?<type>"+commonCharsWithoutComma+")"+oblWS+"(?<name>"+commonName+")"+optWS+"(?<end>\\)|,){1}" );
        replacements.Add( "${begining}${name}: ${type}${end}" );

        DoReplacements();


        // loop through functions and search for variable declaration that happend several times
        // leave only the first declaration because it would otherwise throw an error "BCE0067: There is already a local variable with the name 'younameit'."
        pattern = "function"+oblWS+"(?<blockName>"+commonName+")"+optWS+"\\("+argumentsChars+"\\)("+optWS+":"+optWS+commonCharsWithSpace+")?"+optWS+"{"; 
        List<Match> allReverseFunctionsDeclarations = ReverseMatches(convertedCode, pattern); // see comments below for why ReverseMatches() is used instead of Regex.Matches()

        foreach (Match aFunctionDeclaration in allReverseFunctionsDeclarations) {
            Block functionBlock = new Block(aFunctionDeclaration, convertedCode);

            pattern = "var"+oblWS+"(?<varName>"+commonName+")"+optWS+":"+optWS+"(?<varType>"+commonCharsWithSpace+")"+optWS+"(?<ending>(=|;|in))";
            List<Match> allVariablesDeclarations = ReverseMatches( functionBlock.text, pattern );

            foreach (Match aVariableDeclaration in allVariablesDeclarations) {
                string varName = aVariableDeclaration.Groups["varName"].Value;
                string varType = EscapeRegexChars( aVariableDeclaration.Groups["varType"].Value );
                string ending = aVariableDeclaration.Groups["ending"].Value; 

                // how many time this variable is (still) declared in the function ?
                pattern = "var"+oblWS+varName+optWS+":"+optWS+varType;
                int declarationsCount = Regex.Matches( functionBlock.newText, pattern ).Count;

                if (declarationsCount <= 1) // no need to go forward with this particular variable
                    continue;
                
                // it's at least the second time that this variable is declared in the function, it will throw an error in the Unity console
                // so remove the declaration if it is just of type    var aVar: aType;
                // or replace the declaration by the var name (if a value is set at the same time (var aVar: aType = whatever; => aVar = whatever) or in a foreach loop)

                // here I can't replace the declaration in functionBlock with String.Replace() because it could match several declaration at the same time
                // I have to use Insert and Remove here, that's why the function and variable declaration are looped backward with ReverseMatches()

                // remove old declaration 
                functionBlock.newText = functionBlock.newText.Remove( aVariableDeclaration.Index, aVariableDeclaration.Length );

                // add the new one (if needed)
                if (ending == "=" || ending == "in")
                    functionBlock.newText = functionBlock.newText.Insert( aVariableDeclaration.Index, varName+" "+ending );
            }

            convertedCode = convertedCode.Replace( functionBlock.text, functionBlock.newText );
        }
    } // end of method Functions


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Convert Properties declarations
    /// </summary>
    void Properties () {
        // CONVERT PROPERTIES

        pattern = "(?<visibility>"+visibilityAndStatic+oblWS+")?(?<override>\\boverride"+oblWS+")?(?<blockType>"+commonCharsWithSpace+")"+oblWS+"(?<blockName>"+commonName+")"+optWS+"{";
        List<Match> allProperties = ReverseMatches(convertedCode, pattern);

        foreach (Match aProp in allProperties) {
            // first check if this is really a property declaration
            string[] forbiddenBlockTypes = {"enum ", "class", "extends", "implements", "new", "else", "struct", "interface"};
            
            bool isAPropDeclaration = true;
            string blockType = aProp.Groups["blockType"].Value;
           
            foreach (string type in forbiddenBlockTypes) {
                //if ( blockType.Contains( type ) ) { // can't use blockType.Trim() == type
                if (Regex.Match( blockType, "\\b"+type+"\\b" ).Success ) {
                    isAPropDeclaration = false;
                    break;
                }
            }

            if ( ! isAPropDeclaration)
                continue;
 

            string[] forbiddenBlockNames = {"get", "set", "else", "if"};

            foreach (string name in forbiddenBlockNames) {
                //if (aProp.Groups["blockName"].Value.Contains( name ) ) {
                if (Regex.Match( aProp.Groups["blockName"].Value, "\\b"+name+"\\b" ).Success) {
                    isAPropDeclaration = false;
                    break;
                }
            }

            if ( ! isAPropDeclaration)
                continue;

            if ( ! IsAValidName(blockType) || !IsAValidName(aProp.Groups["blockName"].Value))
                continue;

            // Ok now we are sure this is a property declaration
            Block PropBlock = new Block(aProp, convertedCode);
            PropBlock.type = PropBlock.type.Replace( "override", "" );
            //Debug.Log ("property : "+aProp.Value+" | "+PropBlock.text);

            string property = "";
            string visibility = "";

            // search for the getter
            pattern = "get"+optWS+"({|;)";
            Match getterMatch = Regex.Match( PropBlock.text, pattern );

            if (getterMatch.Success) {
                visibility = aProp.Groups["visibility"].Value; // the getter has the visibilty of the property

                // if the match value contains a curly bracket, it's not an empty getter, so I have to get its content
                if (getterMatch.Value.Contains("{") ) {
                    Block getterBlock = new Block(getterMatch, PropBlock.text);

                    property += visibility+"function get "+PropBlock.name+"(): "+PropBlock.type+" ";
                    property += getterBlock.text+EOL;
                }
                else { // empty getter "get;"
                    property += 
                    visibility+"function get "+PropBlock.name+"(): "+PropBlock.type+" {"+EOL
                        +"\treturn "+PropBlock.name.ToLower ()+";"+EOL
                    +"}"+EOL;
                }
            }

            // now search for the setter
            pattern = "(?<visibility>(protected|private|public)"+oblWS+")?set"+optWS+"({|;)";
            Match setterMatch = Regex.Match( PropBlock.text, pattern );

            if (setterMatch.Success) {
                visibility = setterMatch.Groups["visibility"].Value;
                if (visibility == "" )
                    visibility = "public ";

                if (setterMatch.Value.Contains("{") ) {
                    Block setterBlock = new Block( setterMatch, PropBlock.text );

                    property +=  visibility+"function set "+PropBlock.name+"(value: "+PropBlock.type+") ";
                    property += setterBlock.text+EOL;
                }
                else { // empty setter "set;"
                    property += 
                    visibility+"function set "+PropBlock.name+"(value: "+PropBlock.type+") {"+EOL
                        +"\t"+PropBlock.name.ToLower ()+" = value;"+EOL
                    +"}"+EOL;
                }
            }

            string cSharpProperty = aProp.Value.Replace( "{", PropBlock.text );

            convertedCode = convertedCode.Replace( cSharpProperty, property ); // replace property block by the new(s) function(s)
        } // end lopping on properties
    }


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Add the keyword private when no visibility (or just static) is set (the default visibility is public in JS but private in C#)
    /// Works also for functions, classes and enums
    /// </summary>
    void AddVisibility () {
        
        // the default visibility for variable and functions is public in JS but private in C# 
        // => add the keyword private when no visibility (or just static) is set 
        /*patterns.Add( "([;{}\\]>/]{1}"+optWS+")(?<item>(var|function|enum|class)"+oblWS+")" );
        replacements.Add( "$1private ${item}" );

        patterns.Add( "(\\*"+optWS+")(?<item>(var|function|enum|class)"+oblWS+")" ); // add a / after \\*
        replacements.Add( "$1private ${item}" );

        patterns.Add( "(//.*"+optWS+")(?<item>(var|function|enum|class)"+oblWS+")" ); // after a comment
        replacements.Add( "$1private ${item}" );

        patterns.Add( "((\\#else|\\#endif)"+oblWS+")(?<item>(var|function|enum|class)"+oblWS+")" );
        replacements.Add( "$1private ${item}" );


        // static
        patterns.Add( "([;{}\\]]+"+optWS+")static"+oblWS+"((var|function)"+oblWS+")" );
        replacements.Add( "$1private static $4" );

        patterns.Add( "(\\*"+optWS+")static"+oblWS+"((var|function)"+oblWS+")" );  // add a / after \\*
        replacements.Add( "$1private static $4" );

        patterns.Add( "(//.*"+optWS+")static"+oblWS+"((var|function)"+oblWS+")" );
        replacements.Add( "$1private static $4" );

        patterns.Add( "((\\#else|\\#endif)"+oblWS+")((var|function)"+oblWS+")" );
        replacements.Add( "$1private static $4" );

        DoReplacements();*/

        // do not match protected or private that precede declaration
        // stil match public and static, as we need to check if a visibility keyword precede static itself
        // [^ed\\W]{1}"+oblWS+"(?<type>var|function) matche everything where a non white space charac that is not e or d
        // is followed by at least one white space
        pattern = "[^ed\\s]{1}"+oblWS+"(?<type>var|function)"+oblWS+commonName+"\\b"; 
        List<Match> allDeclarations = ReverseMatches( convertedCode, pattern );
        
        foreach (Match declaration in allDeclarations) {
            // public or static
            if (declaration.Value[0] == 'c') { 
                // if static, need to check before it if a visibility is set
                string keyword = convertedCode.Substring( declaration.Index-5, 6 );
                //Debug.Log(keyword);

                if (keyword == "static") {
                    // same here
                    // do not match when public, private or protected precede
                    pattern = "[^ecd\\s]{1}"+oblWS+"\\bstati"+declaration.Value; 
                    Match staticMatch = Regex.Match( convertedCode, pattern );
                    // using a regex here instead of look for the letter two character before static
                    // allows to have multiple space between the visibility and the static keyword

                    if (staticMatch.Success )
                        convertedCode = convertedCode.Replace( staticMatch.Value, staticMatch.Value.Replace("static ", "private static ") );
                    // else = no match, a visibility keyword precede the static keyword
                }

                continue;
            }

            string type = declaration.Groups["type"].Value+" ";
            convertedCode = convertedCode.Replace( declaration.Value, declaration.Value.Replace(type, "private "+type) );
        }
        
        
        // all variables gets a public or static public visibility but this shouldn't happend inside functions, so remove that
        pattern = "function"+oblWS+"(?<blockName>"+commonName+")"+optWS+"\\(.*\\)("+optWS+":"+optWS+commonCharsWithSpace+")?"+optWS+"{";
        List<Match> allFunctions = ReverseMatches( convertedCode, pattern );

        foreach (Match aFunction in allFunctions) {
            Block function = new Block(aFunction, convertedCode);

            if (function.isEmpty)
                continue;

            patterns.Add( "\\bprivate"+oblWS+"(static"+oblWS+"var\\b)" );
            replacements.Add( "$2" );
            patterns.Add( "(\\bstatic"+oblWS+")?private"+oblWS+"var\\b" );
            replacements.Add( "$1var" );

            function.newText = DoReplacements( function.text );
            convertedCode = convertedCode.Replace( function.text, function.newText );
        }
    } // end AddVisibility ()
} // end of class CSharpToUnityScript_Main
