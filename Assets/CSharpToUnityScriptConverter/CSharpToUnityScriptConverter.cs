/// <summary>
/// CSharpToUnityScriptConverter class for Unity3D
///
/// This class handle the convertion of code from C# to UnityScript.
/// Used by the "C# to UnityScript" extension for Unity3D.
///
/// Use instructions :
/// 
/// Initiate the converter by creating an instance of it.
/// You may pass a directory (relative to the Application.dataPath) to the contructor.
/// It will look up the scripts in the directory and read all data types (class, struct, enum, interface) (even in .js and .boo scripts), 
/// which may contributes to a better conversion than if you don't do it.
/// 
/// Then call the Convert(string inputCode) method with the input code (in C#) to be converted as parameter
/// The converted code in UnityScript is returned bu this method Convert() but is also available in the public member "convertedCode"
/// 
/// Version : 1.0
/// Release : 02 december 2012 
///
/// Created by Florent POUJOL
/// florent.poujol@gmail.com
/// http://www.florent-poujol.fr/en
/// Profile on Unity's forums : http://forum.unity3d.com/members/23148-Lion
/// </summary>


using UnityEngine; 
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions; // Regex.Replace(), Match, Matches, MatchCollection...
using System.IO; // Directory File StreamReader/Writer


public class CSharpToUnityScriptConverter: RegexUtilities 
{

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
    public static bool convertMultipleVarDeclaration = true;
    public static bool removeRefKeyword = true;
    public static bool removeOutKeyword = true;


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Constructor and main method
    /// </summary>
    /// <param name="sourceDirectory">The directory where to look for files</param>
    public CSharpToUnityScriptConverter(string sourceDirectory)
    {
        importedAssemblies.Clear();
        projectClasses.Clear();
        unityClasses.Clear();

        // set dataTypes, with regularTypes + collections
        dataTypes = regularTypes.TrimEnd(')')+"|"+collections.TrimStart('(');

        // reading unity classes
        TextAsset file = (TextAsset)Resources.Load("UnityClasses", typeof(TextAsset));

        if (file != null)
        {
            StringReader reader = new StringReader(file.text);
            string line = "";

            while (true)
            {
                line = reader.ReadLine();
                if (line == null)
                    break;

                unityClasses.Add(line.Trim());
                unityClasses.Add(line.Trim()+"\\[\\]"); // array version
            }

            reader.Close();
        }
        else
            Debug.LogWarning("CSharpToUnityScriptConverter() : File UnityClasses.txt not found inside a Resources directory.");


        // adding UnityClasses to dataTypes
        foreach (string _class in unityClasses)
            dataTypes = dataTypes.Replace(")", "|"+_class+")");


        // loop trough all poject's file, extract the data types (classes, enums and structs)
        if (sourceDirectory != "") // allow to skip that part (ie : for the demo)
        {
            string[] paths = Directory.GetFiles(Application.dataPath+sourceDirectory, "*.cs", SearchOption.AllDirectories);
            
            foreach (string scriptPath in paths)
            {
                StreamReader sreader = new StreamReader(scriptPath);
                string scriptContent = sreader.ReadToEnd();
                sreader.Close();

                pattern = "\\b(?<type>class|interface|struct|enum)"+oblWS+"(?<name>"+commonName+"\\b)";
                MatchCollection allDataTypes = Regex.Matches(scriptContent, pattern);

                foreach (Match aDataType in allDataTypes)
                {
                    string name = aDataType.Groups["name"].Value;

                    // discard results where the first letter is lowercase
                    if (name[0] == char.ToLower(name[0]))
                        continue;

                    dataTypes = dataTypes.Replace(")", "|"+name+")");
                    dataTypes = dataTypes.Replace(")", "|"+name+"\\[\\])"); // array version

                    if (aDataType.Groups["type"].Value == "class")
                        projectClasses.Add(name);
                }
            }
        }

       // Debug.Log ("Data types : "+dataTypes);
    }

    public CSharpToUnityScriptConverter() : this("") {}


    //----------------------------------------------------------------------------------

    // key: random string, value: comment being replaced
    Dictionary<string, string> commentStrings = new Dictionary<string, string>(); 

    /// <summary>
    /// Create a random string
    /// </summary>
    string GetRandomString()
    {
        string randomString = "#comment#";
        string alphabet = "abcdeghijklmnopqrstuvwxyzABCDEGHIJKLMNOPQRSTUVWXYZ0123456789";
        // I removed f and F in alphabet because the F in patterns like [number]F 
        // will be stripped by the conversion of float values

        while (randomString.Length < 29)
        {
            int number = (int)Random.Range(0, alphabet.Length-1);
            randomString += alphabet[number].ToString();
        }

        randomString += "#/comment#";
        return randomString;
    }


    //----------------------------------------------------------------------------------

    /// <summary>
    ///  Main method that perform generic conversion and call the other method for specific conversion
    /// Assume at the beginning that convertedCode is the C# code to be converted
    /// convertedCode
    /// </summary>
    /// <param name="inputCode">The code in C# to be converted in UnityScript</param>
    /// <returns>The converted code in UnityScript</returns>
    public string Convert(string inputCode)
    {
        convertedCode = inputCode;

        // GET RID OF COMMENTS

        commentStrings.Clear();

        // block comment
        // find all block comments (allow nested comments)
        int openedCommentBlocks = 0;
        List<string> commentBlocks = new List<string>();
        int commentBlockIndex = -1;
        bool inACommentBlock = false;

        for (int i = 0; i < convertedCode.Length; i++)
        {
            if (convertedCode[i] == '/' && convertedCode[i+1] == '*' && convertedCode[i+2] != '/') 
            {
                if (!inACommentBlock) 
                {
                    inACommentBlock = true;
                    commentBlocks.Add("");
                    commentBlockIndex++; 
                }

                openedCommentBlocks++;
            }

            if (convertedCode[i] == '*' && convertedCode[i+1] == '/') 
            {
                openedCommentBlocks--;

                if (openedCommentBlocks == 0) 
                {
                    inACommentBlock = false;
                    commentBlocks[commentBlockIndex] += "*/";
                }
            }

            if (inACommentBlock)
                commentBlocks[commentBlockIndex] += convertedCode[i].ToString();
        }

        foreach (string commentBlock in commentBlocks) 
        {
            string randomString = GetRandomString();
            while (commentStrings.ContainsKey(randomString))
                randomString = GetRandomString();

            convertedCode = convertedCode.Replace(commentBlock, randomString);
            commentStrings.Add(randomString, commentBlock);
        }


        // single line comments
        
        // LINE ENDING
        // windows : \r\n
        // Linux :   \n
        // mac :     \r
        string[] lines = convertedCode.Split('\n');

        if (lines.Length == 1) // file has Mac line ending
            lines = convertedCode.Split('\r');
        
        foreach (string line in lines)
        {
            pattern = "//.*$";
            Match comment = Regex.Match(line, pattern);

            if (comment.Success)
            {
                if (comment.Value.Trim() == "//" || comment.Value.Trim() == "///") // commented line does not have any character
                    continue; // continue because it would convert every // in the file and mess up with the following comments

                string randomString = GetRandomString();
                while (commentStrings.ContainsKey(randomString))
                    randomString = GetRandomString();

                convertedCode = convertedCode.Replace(comment.Value, randomString);
                commentStrings.Add(randomString, comment.Value);
            }
            else
            {
                pattern = "#(region|REGION|define|DEFINE).*$";
                Match match = Regex.Match(line, pattern);

                if (match.Success)
                    convertedCode = convertedCode.Replace(match.Value, "");
            }
        }


        // #REGION
        patterns.Add("\\#(endregion|ENDREGION)");
        replacements.Add("");

        // GENERIC COLLECTIONS
        // Add a dot before the opening chevron  List.<float>
        patterns.Add(genericCollections+optWS+"<");
        replacements.Add("$1$2.<");

        // Add a whitespace between two closing chevron   Dictionary.<string,List<string> > 
        //patterns.Add("("+genericCollections+optWS+"\\.<.+)>>");
        // don't this pattern is used by mask or layerMask related things ? => only the opposite <<
        patterns.Add(">>");
        replacements.Add("> >");
        patterns.Add(">>");
        replacements.Add("> >");

        // LOOPS
        // foreach(bla in bla) => for(bla in bla)
        patterns.Add("foreach("+optWS+"\\(.+"+oblWS+"in"+oblWS+".+\\))");
        replacements.Add("for$1");


        // GETCOMPONENT (& Co)
        // GetComponent<T>() => GetComponent.<T>()
        //patterns.Add("(\\b(AddComponent|GetComponent|GetComponents|GetComponentInChildren|GetComponentsInChildren)"+optWS+")(?<type><"+optWS+commonChars+optWS+">)");
        patterns.Add("(\\b(AddComponent|GetComponent|GetComponents|GetComponentInChildren|GetComponentsInChildren)"+optWS+")<");
        replacements.Add("$1.<");


        // FINDOBJECTOFTYPE
        // (Type)FindObjectOfType(typeof(Type)) => FindObjectOfType(Type)
        patterns.Add("(\\("+optWS+commonCharsWithSpace+optWS+"\\)"+optWS+")?FindObjectOfType"+optWS+
            "\\("+optWS+"typeof"+optWS+"\\("+optWS+"(?<type>"+commonCharsWithSpace+")"+optWS+"\\)"+optWS+"\\)");
        replacements.Add("FindObjectOfType(${type})");


        // ABSTRACT
        //patterns.Add("((public|private|protected|static)"+oblWS+")abstract"+oblWS);
        //replacements.Add("$1");
        patterns.Add("\\babstract"+oblWS+"class\\b");
        replacements.Add("class");
        // abstract methods are dealt with at the beginning of Function().
        // What about abstract variables or properties ?


        // VIRTUAL
        patterns.Add("(\\b(public|private|protected|static)"+oblWS+")virtual\\b");
        replacements.Add("$1");


        // YIELDS
        // must remove the return keyword after yield
        // yield return null; => yield ;
        // yield return new WaitForSecond(1.0f); => yield WaitForSecond(1.0f);
        patterns.Add("yield"+optWS+"return"+optWS+"(null|0|new)");
        replacements.Add("yield ");

        patterns.Add("yield"+optWS+"return"+optWS+"StartCoroutine");
        replacements.Add("StartCoroutine");

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


        // DELEGATE
        // comment out delegate declaration
        // then convert delegate names by their US equivalent

        pattern = "("+visibility+oblWS+")?delegate"+oblWS+"(?<type>"+commonCharsWithSpace+")"+oblWS+"(?<name>"+commonName+")"+optWS+"\\((?<args>"+argumentsChars+")\\)"+optWS+";";
        MatchCollection allDelegates = Regex.Matches(convertedCode, pattern);

        foreach (Match aDelegate in allDelegates) 
        {
            string type = aDelegate.Groups["type"].Value;
            string name = aDelegate.Groups["name"].Value;
            string delegateArgs = aDelegate.Groups["args"].Value;
            string USArgs = "";

            if (delegateArgs.Trim() != "") 
            {
                string[] args = delegateArgs.Split(',');

                foreach (string arg in args) 
                {
                    string tempArg = arg.Trim();
                    // parameters are in UnityScript-style by now
    
                    string argType = tempArg.Substring(tempArg.IndexOf(':')+1).Trim();
                    USArgs += argType+", ";
                }

                USArgs = USArgs.TrimEnd(' ', ',');
            }

            if (type != "void")
                type = ":"+type;
            else
                type = "";

            // a delegate name can be used with an "instanciation" syntax when setting the value of a variable to a method
            // ie variable = new DelegateName(MethodName)
            patterns.Add("="+optWS+"new"+oblWS+name+optWS+"\\("+optWS+"(?<methodName>"+commonCharsWithSpace+")"+optWS+"\\)");
            replacements.Add("= ${methodName}");

            // or when setting a variable's type
            // ie : DelegateName variable;
            // =>  var variable: function();
            patterns.Add("\\b"+name+"\\b");
            replacements.Add("function("+USArgs+")"+type);

            // comment line
            convertedCode = convertedCode.Replace(aDelegate.Value, "/* "+aDelegate.Value+" */");
        }
        // will mess up if a delegate is a type in a delegate


        // STRING AND BOOL
        
        //patterns.Add( "("+commonName+optWS+":"+optWS+")?string(?<end>("+optWS+"(\\[[0-9,\\s]*\\])+)?"+optWS+"(=|;|,|\\)|in|{))");
        patterns.Add("((:|new)"+optWS+")?string(?<end>("+optWS+"(\\["+commonNameWithSpaceAndComa+"\\])+)?)");
        replacements.Add("$1String${end}");

        patterns.Add("((:|new)"+optWS+")?bool(?<end>("+optWS+"\\[("+commonNameWithSpaceAndComa+"\\])+)?)");
        replacements.Add("$1boolean${end}");

        // with arrays
        // string
        //patterns.Add( "(new"+oblWS+")string(?<end>("+optWS+"\\[[0-9,]+\\])?"+optWS+"(=|;|,|\\)|in|{))");
        //replacements.Add( "$1String${end}");

        // with generic collections
        patterns.Add("((<|,)"+optWS+")string(?<end>("+optWS+"\\[[,\\s]*\\])?"+optWS+"(>|,))");
        replacements.Add( "$1String${end}");

        patterns.Add( "((<|,)"+optWS+")bool(?<end>("+optWS+"\\[[,\\s]*\\])?"+optWS+"(>|,))");
        replacements.Add( "$1boolean${end}");


        // JAGGED ARRAYS 
        // new type[num][]   =>   new array.<type[]>(num)
        string numberOrCommonChars = "([A-Za-z0-9<>,'\"_\\. ]*)"; // this is commonCharsWithSpace, which may begin by a number, and without square brackets

        patterns.Add("(?<varName>\\bvar"+oblWS+commonName+optWS+")(:"+optWS+commonChars+optWS+"\\["+optWS+"\\]"+optWS+"\\["+optWS+"\\]"+optWS+")?="+
            optWS+"new"+oblWS+"(?<type>"+commonChars+")"+optWS+"\\[(?<number>"+numberOrCommonChars+")?\\]"+optWS+"\\["+optWS+"\\]");
        replacements.Add("${varName} = new array.<${type}[]>(${number})");
        
        

        DoReplacements();

        //convertedCode = "#pragma strict"+EOL+convertedCode;

        // REPLACING COMMENTS
        foreach (KeyValuePair<string, string> comment in commentStrings) 
        {
            //Debug.Log("key="+comment.Key+ " value="+comment.Value);
            convertedCode = convertedCode.Replace(comment.Key, comment.Value);
        }

        return convertedCode;
    } // end of method Convert()

    
    // ----------------------------------------------------------------------------------

    /// <summary> 
    /// Convert stuffs related to classes : declaration, inheritance, parent constructor call, Assembly imports
    /// </summary>
    void Classes() 
    {
        
        // CLASSES DECLARATION
        pattern = "(\\bclass"+oblWS+commonName+")(?<keyword>"+optWS+":"+optWS+")(?<parent>"+commonName+")"+optWS+"{";
        MatchCollection allClasses = Regex.Matches(convertedCode, pattern);

        foreach (Match aClass in allClasses) 
        {
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
        allClasses = Regex.Matches(convertedCode, pattern);

        foreach (Match aClass in allClasses)
        {
            string parent = aClass.Groups["parent"].Value;
            string keyword = " extends ";
            
            if (!unityClasses.Contains(parent) && !projectClasses.Contains(parent) && parent[0] == 'I')
                keyword = " implements ";

            string newClassDeclaration = aClass.Value.Replace( aClass.Groups["keyword"].Value, keyword);

            // if keyword is implements, there is nothing more to do
            // but if keyword is extends, needs to add "implements" after the parent and strip the first coma (between implements and the first interface)
            if (keyword == " extends ") 
            {
                newClassDeclaration = newClassDeclaration.Replace( parent, parent+" implements ");
                string newInterfaces = aClass.Groups["interfaces"].Value.TrimStart(' ', ','); // remove space and coma
                newClassDeclaration = newClassDeclaration.Replace( aClass.Groups["interfaces"].Value, newInterfaces );
            }

            convertedCode = convertedCode.Replace(aClass.Value, newClassDeclaration);
        }


        // PARENT AND ALTERNATE CONSTRUCTOR CALL

        // loop the classes declarations in the file
        pattern = "\\bclass"+oblWS+"(?<blockName>"+commonName+")"+
        "("+oblWS+"extends"+oblWS+commonName+")?[^{]*{";
        List<Match> allReverseClasses = ReverseMatches( convertedCode, pattern );
        
        foreach (Match aClass in allReverseClasses) 
        {
            Block classBlock = new Block(aClass, convertedCode);
            classBlock.newText = classBlock.text;

            if (classBlock.isEmpty)
                continue;

            MatchCollection allConstructors;

            // look for constructors in the class that call the parent constructor
            // if the class declaration doesn't contains "extends", a constructor has no parent to call
            if (classBlock.declaration.Contains("extends")) 
            { 
                // all constructors in this class
                pattern = "\\bpublic"+optWS+"(?<blockName>"+classBlock.name+")"+optWS+"\\([^\\)]*\\)(?<base>"+optWS+":"+optWS+"base"+optWS+"\\((?<args>[^\\)]*)\\))"+optWS+"{";
                allConstructors = Regex.Matches( classBlock.text, pattern ); 

                foreach (Match aConstructor in allConstructors) 
                {
                    // remove :base() from the constructor declaration
                    string newConstructor = aConstructor.Value.Replace( aConstructor.Groups["base"].Value, "");
                    // add super(); to the constructor body
                    string super = "{"+EOL+"super("+aConstructor.Groups["args"]+");";
                    newConstructor = newConstructor.Replace( "{", super );
                    
                    classBlock.newText = classBlock.newText.Replace( aConstructor.Value, newConstructor );
                }
            }


            // look for constructors in this class that call others constructors of the same class
            pattern = "\\bpublic"+optWS+"(?<blockName>"+classBlock.name+")"+optWS+"\\(.*\\)(?<this>"+optWS+":"+optWS+"this"+optWS+"\\((?<args>.*)\\))"+optWS+"{";
            allConstructors = Regex.Matches( classBlock.newText, pattern );

            foreach (Match aConstructor in allConstructors) 
            {
                // remove :this() from the constructor declaration
                string newConstructor = aConstructor.Value.Replace( aConstructor.Groups["this"].Value, "");
                // add Classname() to the constructor body
                string super = "{"+EOL+classBlock.name+"("+aConstructor.Groups["args"]+");";
                newConstructor = newConstructor.Replace( "{", super );
                
                classBlock.newText = classBlock.newText.Replace( aConstructor.Value, newConstructor );
            }
            
            // we won't do more search/replace for this class 
            // now replace in convertedCode, the old classBlock.text by the new
            convertedCode = convertedCode.Replace( classBlock.text, classBlock.newText );
        } // end looping through classes in that file


        // ATTRIBUTES

        // no script, no params
        patterns.Add("\\["+optWS+"(?<attribute>RPC|HideInInspector|Serializable|System.NonSerialized|SerializeField)"+optWS+"\\]");
        replacements.Add("@${attribute}");

        // no script, with params
        patterns.Add("\\["+optWS+"(?<attribute>DrawGizmo|Conditional|MenuItem|System.Obsolete)"+optWS+"(?<params>\\(.*\\))"+optWS+"\\]");
        replacements.Add("@${attribute}${params}");
        
        // require component  need to remove typeof()
        // why don't we need to do that with CustomEditor ??
        patterns.Add("\\["+optWS+"RequireComponent"+optWS+"\\("+optWS+"typeof"+optWS+"\\((?<type>"+commonName+")\\)"+optWS+"\\)"+optWS+"\\]");
        replacements.Add("@script RequireComponent(${type})");

        // script + params
        string attributes = "(?<attributes>AddComponentMenu|ContextMenu|ExecuteInEditMode|ImageEffectOpaque|"+
        "ImageEffectTransformsToLDR|NotConvertedAttribute|NotRenamedAttribute|System.Serializable|"+
        "CanEditMultipleObjects|CustomEditor|PostProcessAttribute|PreferenceItem)";
        patterns.Add("\\["+optWS+attributes+optWS+"(?<params>\\(.*\\))?"+optWS+"\\]");
        replacements.Add("@script ${attributes}${params}");

        
        // STRUCT
        patterns.Add("\\bstruct"+oblWS+commonName+optWS+"{");
        replacements.Add("class $2 extends System.ValueType {");


        // base. => super.      
        patterns.Add("\\bbase"+optWS+"\\.");
        replacements.Add("super$1.");

        DoReplacements();


        // ASSEMBLY IMPORT
        
        // assemblies with aliases
        // they are not supported in UnityScript, 
        // so remove the alias and replace the alias by the full name in the file
        pattern = "\\busing"+oblWS+"(?<alias>"+commonName+")"+optWS+"="+optWS+"(?<assemblyName>"+commonNameWithSpace+")"+optWS+";";
        MatchCollection allImports = Regex.Matches(convertedCode, pattern);

        foreach (Match import in allImports) 
        {
            string alias = import.Groups["alias"].Value.Replace(" ", "");
            string assemblyName = import.Groups["assemblyName"].Value.Replace(" ", "");

            convertedCode = convertedCode.Replace(import.Value, "using "+assemblyName+";");
            convertedCode = convertedCode.Replace(alias+".", assemblyName+".");
        }

        // in UnityScript, each assembly has to be imported once per project, or it will throw a warning in he Unity console for each duplicate assembly import
        // so keep track of the assemblies already imported in the project (in one of the previous file) and comment out the duplicate
        pattern = "\\busing"+oblWS+"(?<assemblyName>"+commonNameWithSpace+")"+optWS+";";
        allImports = Regex.Matches(convertedCode, pattern);

        foreach (Match import in allImports) 
        {
            string assemblyName = import.Groups["assemblyName"].Value.Replace(" ", "");

            if (importedAssemblies.Contains(assemblyName)) 
            {
                convertedCode = convertedCode.Replace(import.Value, "// "+import.Value);
            }
            else 
            {
                convertedCode = convertedCode.Replace(import.Value, "import "+assemblyName+";");
                importedAssemblies.Add(assemblyName);
            }
        }
    } // end of method Classes


    // ----------------------------------------------------------------------------------

    /// </summary>
    /// Used in Variables²(), Functions() and Properties() method below
    /// Check if the input text has 3 or more spaces
    /// </summary>
    /// <return>True if the input text has 3 or mor space, false otherwise</returns>
    bool HasToMuchSpaces(string text) 
    {
        text = text.Trim();

        // check the number of spaces
        int spaceCount = 0;

        foreach (char letter in text) 
        {            
            if (letter == ' ')
                spaceCount++;

            if (spaceCount >= 3) 
            {
                Log("IsAValidName() : Invalid name="+text);
                return true;
            }
        }

        return false;
    }

    
    // ----------------------------------------------------------------------------------
	
    /// <summary> 
    /// Convert stuffs related to variable declarations
    /// </summary>
    void Variables() 
    {
        // CONST just remove the keyword
        patterns.Add("(\\bconst"+oblWS+")(?<end>"+commonCharsWithSpace+oblWS+commonName+")"); 
        replacements.Add("${end}");

        DoReplacements();

        // MULTIPLE INLINE VARIABLE DECLARATION     "Type varName, varName = foo;"
        if (convertMultipleVarDeclaration) 
        {
            // using dataTypes here, instead of commonChars or commonCharsWithSpace drastically reduces the number of false positiv returned by the regex
            // the pattern stop the match all the first semi-colon after the first coma
            
            // 27/11/2012 added a semi colon in the first square brackets that fixed a lot of bugs
            pattern = "(?<varType>\\b"+dataTypes+")"+oblWS+"(?<varList>"+commonName+optWS+"(=[^,;]+)?,[^;]+);";
            MatchCollection allDeclarations = Regex.Matches(convertedCode, pattern);

            foreach (Match aDeclaration in allDeclarations) 
            {
                string match = aDeclaration.Value;

                // match with an opening bracket but no closing bracket are method or class declaration
                // unless there is no semi-colon at all within the brackets
                if (match.Contains("{") && ! match.Contains("}"))
                    continue;
                
                // when the match begins inside method parameters, the opening parenthesis will not be matched
                // so either we won't find an opening parenthesis (when no parenthesis before the first semi-colon, it is an interface/abstract methods)
                // either the first opening parenthesis will be after the first closing parenthesis
                if (
                    (match.Contains(")") && ! match.Contains("(")) ||
                    (match.Contains("(") && match.IndexOf(')') < match.IndexOf('('))
                ) {
                    continue;
                }
                
                // parse varList to know where to cut
                // ie not inside a method call or array or multidim array
                List<string> varList = new List<string>();
                varList.Add("");
                int varListIndex = 0;
                char lastLetter = ' ';

                int openedParenthesis = 0;
                int openedCurlyBracket = 0;
                int openedSquareBracket = 0;
                bool inAString = false;
                bool inAChar = false;

                foreach (char letter in aDeclaration.Groups["varList"].Value)
                {
                    switch (letter) 
                    {
                        case '(': openedParenthesis++; break;
                        case ')': openedParenthesis--; break;

                        case '{': openedCurlyBracket++; break;
                        case '}': openedCurlyBracket--; break;
                       
                        case '[': openedSquareBracket++; break;
                        case ']': openedSquareBracket--; break;

                        case '"': 
                            if (lastLetter.ToString()+letter.ToString() != "\\\"")
                                inAString = !inAString;
                            break;
                        
                        case '\'': inAChar = !inAChar; break;
                    }

                    if (letter == ',' &&
                        openedParenthesis == 0 && 
                        openedCurlyBracket == 0 &&
                        openedSquareBracket == 0 && 
                        ! inAString && ! inAChar)
                    {
                        // we are outside of any struture, this must be a coma between variable
                        varList.Add("");
                        varListIndex++;
                        continue;
                    }
                    
                    varList[varListIndex] += letter.ToString();
                    lastLetter = letter;
                }
                
                if (varList.Count > 1) 
                // if varList.Count == 1,   no coma outside a structure has been found in the match, so that must not be what we are loking for
                {
                    string varType = aDeclaration.Groups["varType"].Value;
                    string newSyntax = "";

                    foreach (string varName in varList) 
                    {
                        if (varName.Contains("=")) 
                        {
                            // add the varType beetween the varName and the equal sign
                            // variable = value,  =>  var variable: type = value
                            string varDeclaration = varName.Replace("=", ": "+varType+" =");
                            newSyntax += "var "+varDeclaration.Trim()+";"+EOL;
                        }
                        else 
                            newSyntax += "var "+varName.Trim()+": "+varType+";"+EOL; 
                    }

                    convertedCode = convertedCode.Replace(aDeclaration.Value, newSyntax);
                }
            }
        } // end if convertMultipleVarDeclaration
 

        // VAR DECLARATION IN FOREACH LOOP
        patterns.Add("(?<varType>\\b"+commonCharsWithSpace+")"+oblWS+"(?<varName>"+commonName+")(?<in>"+oblWS+"in"+oblWS+")");
        replacements.Add("var ${varName}: ${varType}${in}");

        DoReplacements();


        // VAR DECLARATION WITHOUT VALUE

        // conversion will mess up if the declaration is not preceded by a visibility keyword 
        // but by something else like @HideInInspector or @System.Serialize which will be part of varType
        // that's why I use two patterns here instead of making the "visibility" optionnal in a single pattern
        string[] tempPatterns = { "(?<visibility>"+visibilityAndStatic+oblWS+")(?<varType>"+commonCharsWithSpace+")"+oblWS+"(?<varName>"+commonName+")(?<end>"+optWS+";)",
        "(?<varType>"+commonCharsWithSpace+")"+oblWS+"(?<varName>"+commonName+")(?<end>"+optWS+";)" };

        foreach (string tempPattern in tempPatterns)
        {
            MatchCollection allVariables = Regex.Matches(convertedCode, tempPattern);

            foreach (Match aVariable in allVariables) 
            {
                string visibility = aVariable.Groups["visibility"].Value;
                string type = aVariable.Groups["varType"].Value;
                string name = aVariable.Groups["varName"].Value;
                string end = aVariable.Groups["end"].Value;

                // IsAValidName
                if (HasToMuchSpaces(visibility) || HasToMuchSpaces(type) || HasToMuchSpaces(name))
                    continue;

                /* list of pattern discarded by forbiddenTypes
                    
                    import UnityEgine; => var UnityEngine: import;      same with using
                    return something: => var something: return;
                    (in for loop)   i < something; => var something: i <;
                        cannont add < and > in forbiddenTypes as it would discard generic things
                    
                    #endif
                    variable = foo;
                    => #var variable: endif = foo;   (will do that with #else and #if too)

                    as Type; => var Type: as;
                 */
                
                // first check if type does contains some pattern
                List<string> forbiddenTypes = new List<string>( 
                    new string[] {
                        "<=", ">="
                        //"import", "return", "yield return", "yield", "as", "else", "<", "<=", ">", ">="
                        }
                );

                bool isNotVarDeclaration = false;
                foreach (string forbiddenType in forbiddenTypes) 
                {
                    if (type.Trim().Contains(forbiddenType))
                        isNotVarDeclaration = true;
                }

                if (isNotVarDeclaration)
                    continue;

                // discard for loop patterns
                if (type.TrimEnd().EndsWith("<") || (type.Contains(">") && !type.Contains("<")))
                    continue;

                //----------
                // check if type is equal to some patterns
                forbiddenTypes = new List<string>( 
                    new string[] { 
                        "import", "using", "return", "if", "else", "endif", "as", "else return" // "yield return", "yield"
                        }
                );

                if (forbiddenTypes.Contains(type.Trim()) || type.Contains(" = ") )
                    continue;
                // some value setting got treated like var declaration   variable = value; => var variable: = value;

                // discard matches like "othervar as Type;" that got converted to "var Type: othervar as;"
                if (type.TrimEnd().EndsWith(" as")) // 25/10/2012  why such is not already discarded in the foreach loop above ?
                    continue;

                //
                pattern = aVariable.Value.Replace("HideInInspector ", "").Replace("System.NonSerialized ", "");
                type = type.Replace("HideInInspector ", "").Replace("System.NonSerialized ", "");

                convertedCode = convertedCode.Replace(pattern, visibility+"var "+name+": "+type+end);
            }
        }

        // VAR DECLARATION WITH VALUE
        tempPatterns = new string[] { "(?<visibility>"+visibilityAndStatic+oblWS+")(?<varType>"+commonCharsWithSpace+")"+oblWS+"(?<varName>\\b"+commonName+")(?<varValue>"+optWS+"="+optWS+"[^;]+;)",
        "(?<varType>"+commonCharsWithSpace+")"+oblWS+"(?<varName>\\b"+commonName+")(?<varValue>"+optWS+"="+optWS+"[^;]+;)" };
        
        foreach (string tempPattern in tempPatterns) 
        {
            MatchCollection allVariables = Regex.Matches(convertedCode, tempPattern);

            foreach (Match aVariable in allVariables) 
            {
                string visibility = aVariable.Groups["visibility"].Value;
                string type = aVariable.Groups["varType"].Value.Replace( "HideInInspector", "").Replace( "System.NonSerialized", "");
                string name = aVariable.Groups["varName"].Value;
                string varValue = aVariable.Groups["varValue"].Value;

                if (HasToMuchSpaces(visibility) || HasToMuchSpaces(type) || HasToMuchSpaces(name))
                    continue;

                /* list of pattern discarded by forbiddenTypes
                    using Type=Assembly; => var Type: using=Assembly;

                    #endif
                    variable = foo;
                    => #var variable: endif = foo;   (will do that with #else and #if too)

                    var variable = something;  =>  var holder: var = ...
                 */
                
                List<string> forbiddenTypes = new List<string>( 
                    new string[] { 
                        //"return", "yield return", "as", "else"
                         }
                );

                bool isNotAVarDelcaration = false;
                foreach (string forbiddenType in forbiddenTypes) 
                {
                    if (type.Trim().Contains(forbiddenType))
                        isNotAVarDelcaration = true;
                }

                if (isNotAVarDelcaration)
                    continue;

                //----------
                forbiddenTypes = new List<string>( 
                    new string[] { "if", "else", "endif", "using", "var"
                        //"return", "yield return", "as", "else"
                         }
                );

                if (forbiddenTypes.Contains(type.Trim()))
                    continue;
                
                // when HideInInspector or System.NonSerialized are n the same line as the var declaration
                pattern = aVariable.Value.Replace("HideInInspector ", "").Replace("System.NonSerialized ", "");

                convertedCode = convertedCode.Replace(pattern, visibility+"var "+name+": "+type+varValue);
            }
        }
        
        
        // CASTING

        // using dataTypes prevent to match if patterns like "if(var1) var2;"
        pattern = "\\("+optWS+"(?<type>"+dataTypes+")"+optWS+"\\)"+optSpaces+"\\((?<castedExp>.+)"+optWS+"(?<end>(;|}))";
        bool firstPattern = true;

        while (firstPattern)
        {
            MatchCollection allCasts = Regex.Matches(convertedCode, pattern);

            if (allCasts.Count <= 0) 
            {
                pattern = "\\("+optWS+"(?<type>"+dataTypes+")"+optWS+"\\)"+optSpaces+"(?<castedExp>"+commonChars+")";
                // don't allow space when there is no parenthesis
                allCasts = Regex.Matches(convertedCode, pattern);
                firstPattern = false; // will exit the loop at the end
            }

            foreach (Match aCast in allCasts) 
            {
                string type = aCast.Groups["type"].Value;
                string castedExp = aCast.Groups["castedExp"].Value; // casted expression
                string firstPatternEnd = aCast.Groups["end"].Value;;

                if (firstPattern) 
                {
                    // the first pattern match more than the casted expression
                    // we will find the casted expression based on the parenthesis
                    int openedParenthesis = 1;
                    string afterCastExp = castedExp;
                    castedExp = "";
                    
                    foreach (char letter in afterCastExp) 
                    {
                        if (letter == '(')
                            openedParenthesis++;

                        if (letter == ')') 
                        {
                            openedParenthesis--;

                            if (openedParenthesis == 0 ) // we have reached the final closing parenthesis
                                break;
                        }

                        castedExp += letter.ToString();
                    }

                    // castedExp contains only the content of the casted expression without the parenthesis 
                    // remove that from afterCastExp and you get what was behind the casted expression
                    // to happend to firstPatternEnd
                    afterCastExp = afterCastExp.Replace(castedExp+")", "");
                    firstPatternEnd = afterCastExp+firstPatternEnd;
                }

                if (castedExp.EndsWith(","))
                {
                    castedExp = castedExp.TrimEnd(',');
                    firstPatternEnd = ","+firstPatternEnd;
                }

                if (regularTypes.Contains(type.Trim())) 
                { 
                    if (type.Trim() == "int")
                        castedExp = "parseInt("+castedExp+")"+firstPatternEnd;
                    else if (type.Trim() == "float")
                        castedExp = "parseFloat("+castedExp+")"+firstPatternEnd;

                    convertedCode = convertedCode.Replace(aCast.Value, castedExp);
                }
                else
                {
                    // (Type)(aftercast) => aftercast as Type
                    convertedCode = convertedCode.Replace( aCast.Value, castedExp+" as "+type+firstPatternEnd );
                }
            } // end foreach (Match ...
        } // end while(true)

        // fixing some casting 

        //  Method as Type([...]);   that must be   Method([...]) as Type;
        patterns.Add("\\b(?<method>"+commonName+")"+oblWS+"as"+oblWS+"(?<type>"+commonName+")"+optWS+"(?<args>\\(.*\\))"+optWS+";");
        replacements.Add("${method}${args} as ${type};");




        // ARRAYS

        // string[] array1; => var array1: String[] (already done)
        // string[,] array2 = new string[5,6];  => var array2: Strint[] = new String[5,6];

        //string[] array3 = new string[] { "machin", "machin"};
        //bool[] array4 = { false, true};
            // both => var array3: String[] = { }

        // string[,] array2 = new string[5,6];  => var array2: Strint[] = new String[5,6];
        //patterns.Add("\\b(?<type>"+commonName+optWS+"\\[,*\\])"+oblWS+"(?<name>"+commonName+")"+optWS+"=");
        // replacements.Add("");

        // new Type[] {};    =>   {};
        patterns.Add("(?<equal>="+optWS+")?new"+oblWS+commonName+optWS+"(\\[[0-9, ]*\\])+(?<end>"+optWS+"\\{)");
        replacements.Add("${equal}${end}");

        DoReplacements(); 

        // replace curly brackets by square bracket
        pattern = "(?s)((=|return|\\()"+optWS+")(?<arrayContent>\\{.*\\})(?<end>"+optWS+"(;|\\)))"; // the (?s) means that the dot represent every character, including new line \n
        
        while (true)
        {
            allMatches = Regex.Matches(convertedCode, pattern);

            if (allMatches.Count <= 0)
                break; // get out of the loop when no more pattern is matched
            
            foreach (Match aMatch in allMatches)
            {
                // The pattern will have matched more than we need
                // as with cast above, find the matching closing curly bracket
                // and replace curly bracket by square bracket inside that array content only
                int openedBrackets = 0;
                string CSarrayContent = "";
                
                foreach (char letter in aMatch.Groups["arrayContent"].Value)
                {
                    if (letter == '{')
                        openedBrackets++;

                    if (letter == '}') 
                    {
                        openedBrackets--;

                        if (openedBrackets == 0 ) { // we have reached the final closing bracket
                            CSarrayContent += letter.ToString();
                            break;
                        }
                    }

                    CSarrayContent += letter.ToString();
                }
                
                string USarrayContent = CSarrayContent.Replace("{", "[").Replace("}", "]");
                convertedCode = convertedCode.Replace(CSarrayContent, USarrayContent);
            }
        }


        // LITTERAL CHAR 'a' => 'a'[0]
        patterns.Add("'.{1}'");
        replacements.Add("$0[0]");


        // remove f to Float values
        // actually not really needed, it works with the f
        patterns.Add("([0-9]{1}(\\.[0-9]+)?)(f|F)");
        replacements.Add("$1");

        DoReplacements();
        

        // remove @ in  'string aVariable = @"a string";"  and add a extra \ to the existing \
        pattern = "(="+optWS+")@(?<text>"+optWS+"\"[^;]+;)";
        allMatches = Regex.Matches(convertedCode, pattern);

        foreach (Match aMatch in allMatches ) 
        {
            string newText = aMatch.Value.Replace ("@\"", "\""); // .Replace ("\\", "\\\\")
            convertedCode = convertedCode.Replace( aMatch.Value, newText );
        }
    } // end of method Variable()


    // ----------------------------------------------------------------------------------
    
    /// <summary> 
    /// Convert stuffs related to methods
    /// </summary>
    void Functions() 
    {
        // ABSTRACT
        
        // first check for abtract methods, strip the abstract keyword while add curly bracket instead of the semi colon
        pattern = "(\\babstract"+oblWS+")(?<body>"+commonCharsWithSpace+oblWS+commonName+optWS+"\\(.*\\))("+optWS+";)";
        MatchCollection allFunctionsDeclarations = Regex.Matches(convertedCode, pattern);

        foreach (Match aFunctionDeclaration in allFunctionsDeclarations) 
        {
            //Debug.Log( "Abstract method : "+aFunctionDeclaration.Value );
            string newFunc = aFunctionDeclaration.Groups["body"].Value+" {}";
            convertedCode = convertedCode.Replace( aFunctionDeclaration.Value, newFunc );
        }


        // INTERFACE

        pattern = "interface"+oblWS+commonName+optWS+"{";
        List<Match> allInterfaces = ReverseMatches (convertedCode, pattern);
        
        foreach (Match anInterface in allInterfaces) 
        {
            Block interfaceBlock = new Block(anInterface, convertedCode);

            if (interfaceBlock.isEmpty)
                continue;

            // convert function
            pattern = "(?<returnType>"+commonCharsWithSpace+")"+oblWS+"(?<functionCorp>"+commonName+optWS+"\\("+argumentsChars+"\\))(?<end>"+optWS+";)";
            MatchCollection allFunctions = Regex.Matches(interfaceBlock.text, pattern);

            foreach (Match aFunction in allFunctions) 
            {
                string type = aFunction.Groups["returnType"].Value;

                if (type ==  "void") 
                    type = "";
                else
                    type = ": "+type;

                string newFunction = "[interface no visibility]function "+aFunction.Groups["functionCorp"].Value+type+";";
                interfaceBlock.newText = interfaceBlock.newText.Replace( aFunction.Value, newFunction );
            }

            //convertedCode = convertedCode.Replace(interfaceBlock.text, interfaceBlock.newText);


            // convert properties
            pattern = "(?<blockType>"+commonCharsWithSpace+")"+oblWS+"(?<blockName>"+commonName+")"+optWS+"{";
            List<Match> allProperties = ReverseMatches(interfaceBlock.text, pattern);

            foreach (Match property in allProperties) 
            {
                string type = property.Groups["blockType"].Value;
                string name = property.Groups["blockName"].Value;

                if (HasToMuchSpaces(type) || HasToMuchSpaces(name))
                    continue;
                
                // Ok now we are sure this is a property declaration
                Block PropBlock = new Block(property, interfaceBlock.text);
                if (PropBlock.isEmpty)
                    continue;

                string USProperty = "";

                // search for the getter
                pattern = "get"+optWS+";";
                Match getterMatch = Regex.Match(PropBlock.text, pattern);

                if (getterMatch.Success)
                    USProperty += "[interface no visibility]function get "+name+"(): "+type+";";

                // now search for the setter
                pattern = "set"+optWS+";";
                Match setterMatch = Regex.Match(PropBlock.text, pattern);

                if (setterMatch.Success) 
                {
                    if (USProperty != "")
                        USProperty += EOL;

                    USProperty += "[interface no visibility]function set "+name+"(value: "+type+");";
                }
                
                string cSharpProperty = property.Value.Replace( "{", PropBlock.text );
                interfaceBlock.newText = interfaceBlock.newText.Replace( cSharpProperty, USProperty );
            }
            
            convertedCode = convertedCode.Replace(interfaceBlock.text, interfaceBlock.newText);
        }


        // FUNCTION DECLARATION

        // using a simple pattern/replacement regex as below match way too much things it shouldn't
        // So I need to check each match before allowing the replacement
        pattern = "(?<visibility>"+visibilityAndStatic+oblWS+")?(?<returnType>"+commonCharsWithSpace+")"+oblWS+"(?<functionName>"+commonName+")"+optWS+"(?<args>\\("+argumentsChars+"\\))(?<end>"+optWS+"{)"; 
        allFunctionsDeclarations = Regex.Matches(convertedCode, pattern);

        foreach (Match aFunctionDeclaration in allFunctionsDeclarations) 
        {
            string visibility = aFunctionDeclaration.Groups["visibility"].Value;
            string returnType = aFunctionDeclaration.Groups["returnType"].Value;
            string functionName = aFunctionDeclaration.Groups["functionName"].Value;
            string args = aFunctionDeclaration.Groups["args"].Value;
            string end = aFunctionDeclaration.Groups["end"].Value;

            if (returnType == "else" && functionName == "if") // do not match else if () statement
                continue;

            if (returnType == "new") // do not match class instanciatton inside an if statement   ie : new Rect ()) => it shouldn't anymore anyway, thanks to argumentsChars instead of ".*"
                continue;

            if (returnType.Contains("override"))
                returnType = returnType.Replace("override", "").Trim();  // this will left the override keyword after the prefix

            if (returnType.Contains("new"))
                returnType = returnType.Replace("new", "").Trim();  // this will left the override keyword after the prefix

            // if we are there, it's really a function declaration that has to be converted
            if (returnType == "void")
                returnType = "";
            else if (returnType == "public" || returnType == "private") // a constructor
            { 
                visibility = returnType+" ";
                returnType = "";
            }
            
            else
                returnType = ": "+returnType;

            string newFunctionDeclaration = visibility+"function "+functionName+args+returnType+end;
            convertedCode = convertedCode.Replace( aFunctionDeclaration.Value, newFunctionDeclaration );
        }


        // remove out keyword in arguments
        if (removeOutKeyword) 
        {
            patterns.Add("(,|\\()"+optWS+"out"+oblWS+"("+commonCharsWithoutComma+")");
            replacements.Add("$1$4");
        }

        // remove ref keyword in arguments  => let in so it will throw an error and the dev can figure out what to do 
        if (removeRefKeyword) 
        {
            patterns.Add("(,|\\()"+optWS+"ref"+oblWS+"("+commonCharsWithoutComma+")");
            replacements.Add("$1$4");
        }


        // PARAMETERS DECLARATION

        // if out and ref keyword where not removed before this point it would also convert them ("out hit" in Physics.Raycast() calls) or prevent the convertion ("ref aType aVar" as function argument)
        string refKeyword = "";
        if (removeRefKeyword)
            refKeyword = "(ref"+oblWS+")?";

        patterns.Add("(?<begining>(\\(|,){1}"+optWS+")"+refKeyword+"(?<type>"+commonCharsWithoutComma+")"+oblWS+"(?<name>"+commonName+")"+optWS+"(?<end>\\)|,){1}");
        replacements.Add("${begining}${name}: ${type}${end}");
        // as regex doesn't overlap themselves, only half of the argument have been converted
        // I need to run the regex  a second time
        patterns.Add("(?<begining>(\\(|,){1}"+optWS+")"+refKeyword+"(?<type>"+commonCharsWithoutComma+")"+oblWS+"(?<name>"+commonName+")"+optWS+"(?<end>\\)|,){1}");
        replacements.Add("${begining}${name}: ${type}${end}");

        DoReplacements();


        // loop through functions and search for variable declaration that happend several times
        // leave only the first declaration because it would otherwise throw an error "BCE0067: There is already a local variable with the name 'younameit'."
        pattern = "function"+oblWS+"(?<blockName>"+commonName+")"+optWS+"\\("+argumentsChars+"\\)("+optWS+":"+optWS+commonCharsWithSpace+")?"+optWS+"{"; 
        List<Match> allReverseFunctionsDeclarations = ReverseMatches(convertedCode, pattern); // see comments below for why ReverseMatches() is used instead of Regex.Matches()

        foreach (Match aFunctionDeclaration in allReverseFunctionsDeclarations) 
        {
            Block functionBlock = new Block(aFunctionDeclaration, convertedCode);

            if (functionBlock.isEmpty)
                continue;

            pattern = "var"+oblWS+"(?<varName>"+commonName+")"+optWS+":"+optWS+"(?<varType>"+commonCharsWithSpace+")"+optWS+"(?<ending>(=|;|in))";
            List<Match> allVariablesDeclarations = ReverseMatches( functionBlock.text, pattern );

            foreach (Match aVariableDeclaration in allVariablesDeclarations) 
            {
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
    void Properties() 
    {
        // CONVERT PROPERTIES

        pattern = "(?<visibility>"+visibilityAndStatic+oblWS+")?(?<override>\\boverride"+oblWS+")?(?<blockType>"+commonCharsWithSpace+")"+oblWS+"(?<blockName>"+commonName+")"+optWS+"{";
        List<Match> allProperties = ReverseMatches(convertedCode, pattern);

        foreach (Match aProp in allProperties) 
        {
            // first check if this is really a property declaration
            List<string> forbiddenBlockTypes = new List<string>(new string[] {
                "enum", "class", "extends", "implements", "new", "else", "struct", "interface"
            });

            bool isAPropDeclaration = true;
            string blockType = aProp.Groups["blockType"].Value;

            foreach (string type in forbiddenBlockTypes) 
            {
                if (blockType.Contains(type)) 
                {
                    isAPropDeclaration = false;
                    break;
                }
            }

            if (!isAPropDeclaration)
                continue;
            
            if (forbiddenBlockTypes.Contains(blockType.Trim()))
                continue;

            string[] forbiddenBlockNames = {"get", "set", "else", "if"};

            foreach (string name in forbiddenBlockNames) 
            {
                if (Regex.Match( aProp.Groups["blockName"].Value, "\\b"+name+"\\b" ).Success) 
                {
                    isAPropDeclaration = false;
                    break;
                }
            }

            if (!isAPropDeclaration)
                continue;

            if (HasToMuchSpaces(blockType) || HasToMuchSpaces(aProp.Groups["blockName"].Value))
                continue;

            // Ok now we are sure this is a property declaration
            Block PropBlock = new Block(aProp, convertedCode);
            if (PropBlock.isEmpty)
                continue;

            PropBlock.type = PropBlock.type.Replace( "override", "").Replace("abstract", "");
            //Debug.Log ("property : "+aProp.Value+" | "+PropBlock.text);

            string property = "";
            string visibility = "";

            // search for the getter
            pattern = "get"+optWS+"({|;)";
            Match getterMatch = Regex.Match(PropBlock.text, pattern);

            if (getterMatch.Success) 
            {
                visibility = aProp.Groups["visibility"].Value; // the getter has the visibilty of the property

                // if the match value contains a curly bracket, it's not an empty getter, so I have to get its content
                if (getterMatch.Value.Contains("{")) 
                {
                    Block getterBlock = new Block(getterMatch, PropBlock.text);

                    if (getterBlock.isEmpty)
                        continue;

                    property += visibility+"function get "+PropBlock.name+"(): "+PropBlock.type+" ";
                    property += getterBlock.text+EOL;
                }
                else { // empty getter "get;"
                    property += 
                    visibility+"function get "+PropBlock.name+"(): "+PropBlock.type+EOL+
                    "{"+EOL+
                        "\treturn "+PropBlock.name.ToLower ()+";"+EOL+
                    "}"+EOL;
                }
            }

            // now search for the setter
            pattern = "(?<visibility>(protected|private|public)"+oblWS+")?set"+optWS+"({|;)";
            Match setterMatch = Regex.Match(PropBlock.text, pattern);

            if (setterMatch.Success) 
            {
                visibility = setterMatch.Groups["visibility"].Value;
                if (visibility == "")
                    visibility = "public ";

                if (setterMatch.Value.Contains("{")) 
                {
                    Block setterBlock = new Block(setterMatch, PropBlock.text);

                    if (setterBlock.isEmpty)
                        continue;

                    property +=  visibility+"function set "+PropBlock.name+"(value: "+PropBlock.type+") ";
                    property += setterBlock.text+EOL;
                }
                else { // empty setter "set;"
                    property += 
                    visibility+"function set "+PropBlock.name+"(value: "+PropBlock.type+")"+EOL+
                    "{"+EOL+
                        "\t"+PropBlock.name.ToLower ()+" = value;"+EOL+
                    "}"+EOL;
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
    void AddVisibility()
    {
        // do not match protected or private that precede declaration
        // stil match public and static, as we need to check if a visibility keyword precede static itself
        // [^ed\\W]{1}"+oblWS+"(?<type>var|function) matche everything where a non white space charac that is not e or d
        // is followed by at least one white space
        pattern = "[^ed\\s]{1}"+oblWS+"(?<type>var|function)"+oblWS+commonName+"\\b"; 
        List<Match> allDeclarations = ReverseMatches( convertedCode, pattern );
        
        foreach (Match declaration in allDeclarations) 
        {
            // public or static
            if (declaration.Value[0] == 'c') 
            { 
                // if static, need to check before it if a visibility is set
                string keyword = convertedCode.Substring( declaration.Index-5, 6 );
                //Debug.Log(keyword);

                if (keyword == "static") 
                {
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
        
        
        // all variables gets a private or static private visibility but this shouldn't happend inside functions and properties, so remove that
        pattern = "function"+oblWS+"((get|set)"+oblWS+")?(?<blockName>"+commonName+")"+optWS+"\\([^{]*\\)("+optWS+":"+optWS+commonCharsWithSpace+")?"+optWS+"{";
        List<Match> allFunctions = ReverseMatches(convertedCode, pattern);

        foreach (Match aFunction in allFunctions) 
        {
            Block function = new Block(aFunction, convertedCode);

            if (function.isEmpty)
                continue;

            patterns.Add("\\bprivate"+oblWS+"(static"+oblWS+"var\\b)");
            replacements.Add("$2");
            patterns.Add("(\\bstatic"+oblWS+")?private"+oblWS+"var\\b");
            replacements.Add("$1var");

            function.newText = DoReplacements(function.text);
            convertedCode = convertedCode.Replace(function.text, function.newText);
        }



        // remove [interface no visibility] inside interface
        patterns.Add("\\[interface no visibility\\]");
        replacements.Add("");

        DoReplacements();
    } // end AddVisibility ()
} // end of class CSharpToUnityScript_Main
