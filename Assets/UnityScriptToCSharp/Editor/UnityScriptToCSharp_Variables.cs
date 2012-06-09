
using System.Collections.Generic;
using System.Text.RegularExpressions; 

public class UnityScriptToCSharp_Variables: UnityScriptToCSharp {
	/// <summary>
    /// Search for and convert variables declarations
    /// </summary>
    public static void Variables () {
        // add an f at the end of a float (and double) value 
        patterns.Add ( "([0-9]+\\.{1}[0-9]+)(f|F){0}" );
        replacements.Add ( "$1f" );


        // arrays

            // replace square brackets by curly brackets
            // works for variable declaration and litteral array in foreach loop "foreach(type var in {array})"

            patterns.Add ( "(=|in)"+optWS+"\\[(.*)\\]"+optWS+"(;|\\))" );
            replacements.Add ( "$1$2{$3}$4$5" );

            // replace signle quotation marks by double quotation marks (between brackets)
                patterns.Add ( "({|,){1}"+optWS+"'{1}"+commonCharsWithoutComma+"'{1}"+optWS+"(}|,){1}" );
                replacements.Add ( "$1$2\"$3\"$4$5" );
                // now, as regex doesn't overlap themselves, only half of the argument have been convertes
                // I need to run the regex again
                patterns.Add ( "({|,){1}"+optWS+"'{1}"+commonCharsWithoutComma+"'{1}"+optWS+"(}|,){1}" );
                replacements.Add ( "$1$2\"$3\"$4$5" );


            // array with type declaration (with or without value setting)
            // arrays with type declaration without space    "string[]" instead of "string [ ]"" are already converted because [ and ] are among commonChars

                

                // general case
                patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+commonName+optWS+"\\["+optWS+"\\]"+optWS+"(=|;)" );
                replacements.Add ( "$5[] $2$8$9" );


            // arrays with value setting but no type declaration. Have to guess the type with the value's look
            // square brackets have already been converted to curly brackets by now

                // string
                patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"{"+optWS+"((\"|'){1}"+commonCharsWithoutComma+"(\"|'){1}"+optWS+",?"+optWS+")*"+optWS+"})" );
                replacements.Add ( "string[] $2" );

                // char
                patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"{"+optWS+"((\"|'){1}"+commonCharsWithoutComma+"(\"|'){1}"+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]"+optWS+",?"+optWS+")*"+optWS+"})" );
                replacements.Add ( "char[] $2" );

                // replace "a'[0] and "a"[0] by 'a" in char[] declaration
                patterns.Add ( "({|,){1}"+optWS+"(\"|'){1}"+commonCharsWithoutComma+"(\"|'){1}"+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]"+optWS+"(}|,){1}" );
                replacements.Add ( "$1$2'$4'$9$10" );
                // now, as regex doesn't overlap themselves, only half of the argument have been converted
                // I need to run the regex again
                patterns.Add ( "({|,){1}"+optWS+"(\"|'){1}"+commonCharsWithoutComma+"(\"|'){1}"+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]"+optWS+"(}|,){1}" );
                replacements.Add ( "$1$2'$4'$9$10" );

                // bool
                patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"{"+optWS+"((true|false)"+optWS+",?"+optWS+")*"+optWS+"})" );
                replacements.Add ( "bool[] $2" );

                //int
                patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"{"+optWS+"(-?[0-9]+"+optWS+",?"+optWS+")*"+optWS+"})" );
                replacements.Add ( "int[] $2" );

                //float
                patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"{"+optWS+"(-?[0-9]+\\.{1}[0-9]+(f|F){1}"+optWS+",?"+optWS+")*"+optWS+"})" );
                replacements.Add ( "float[] $2" );
 

            // empty arrays declarations without type declaration  bool[] _array11 = new bool[ 4] ;
            
                // string
                patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"new"+oblWS+")String("+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]"+optWS+";)" );
                replacements.Add ( "string[] $2string$7" );

                // // replace leftover ": string[" or "new string["
                // // patterns.Add ( "(new|:)"+optWS+"string("+optWS+"\\[)" );
                // // replacements.Add ( "$1$2string$3" );

                // bool
                patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"new"+oblWS+")boolean("+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]"+optWS+";)" );
                replacements.Add ( "bool[] $2bool$7" );
            

                // general case
                patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"new"+oblWS+commonName+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]"+optWS+";)" );
                replacements.Add ( "$7[] $2" );


        // ----------------------------------------------------------------------------------


        // variable with type declaration but no value setting      string test;

            // particular case : string     string _string2;
            patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"String"+optWS+";" );
            replacements.Add ( "string $2$5;" );

            // particular case : Boolean     bool _bool2;
            // works also for bool with value setting
            // patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"bool"+optWS+";" );
            // replacements.Add ( "bool $2$5;" );

            // general case :     int _int3;    also works for arrays   int[] _array;
            // patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+commonChars+optWS+";" );
            // replacements.Add ( "$5 $2$6;" );

            // others cases than string are actually handled below :

        // variables with type declaration and value setting    int test = 5;

            // particular case : string    put string lowercase and replace "' by ""       string _string6 = "& ";
            patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"String"+optWS+"="+optWS+"(\"|'){1}(.*)(\"|'){1}"+optWS+";" );
            replacements.Add ( "string $2$5=$6\"$8\"$10;" );

            // particular case : char       char _char4 = 'a'[0];    char _char5 = "a';    => char _char4 = 'a";
            patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"char"+optWS+"="+optWS+"(\"|'){1}(.*)(\"|'){1}"+optWS+"\\["+optWS+"[0-9]+"+optWS+"\\]"+optWS+";" );
            replacements.Add ( "char $2$5=$6'$8'$13;" );

            // particular case : bool    bool _bool3 = true;
            patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"boolean"+optWS+"(=|;|in)" );
            replacements.Add ( "bool $2$5$6" );

            // particular case : float
			//patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"float"+optWS+"="+optWS+"(-?[0-9]+\\.[0-9]+(f|F){1})"+optWS+";" );
			//replacements.Add ( "float $2$5=$6$7$8;" );

            // general case for variable with type definition.   
            // Also works in foreach loops 
            patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+commonChars+optWS+"(=|;|in)" );
            replacements.Add ( "$5 $2$6$7" );
            // This would Also works for some arrays when there is not space around and between square brackets
            // but it would also really fuck up with string[]

			// remove the f at the end of value when it's a double
			patterns.Add ("(double"+oblWS+commonName+optWS+"="+optWS+"-?[0-9]+\\.[0-9]+)(f|F){1}"+optWS+";");
			replacements.Add ("$1$7;");


        // variable with value setting but no type declaration. Have to guess the type with the value's look
    
            // string
            patterns.Add ( "var"+oblWS+commonName+optWS+"="+optWS+"(\"|'){1}(.*)(\"|'){1}"+optWS+";" );
            replacements.Add ( "string $2$3=$4\"$6\"$8;" );

            // char    char _char2 = 'a'[0];    var _char3 = "a';
            patterns.Add ( "var"+oblWS+commonName+optWS+"="+optWS+"(\"|'){1}(.*)(\"|'){1}"+optWS+"(\\["+optWS+"[0-9]+"+optWS+"\\])"+optWS+";" );
            replacements.Add ( "char $2$3=$4'$6'$12;" );

            // bool
            patterns.Add ( "var("+oblWS+commonName+optWS+"="+optWS+"(true|false)"+optWS+";)" );
            replacements.Add ( "bool$1" );

            // int
            patterns.Add ( "var("+oblWS+commonName+optWS+"="+optWS+"-?[0-9]+"+optWS+";)" ); // long will be converted to int
            replacements.Add ( "int$1" );

            // float     value already contains an f or F   3.5f  or 1.0f
            patterns.Add ( "var("+oblWS+commonName+optWS+"="+optWS+"-?[0-9]+\\.{1}[0-9]+(f|F){1}"+optWS+";)" );
            replacements.Add ( "float$1" );
        

        // variable without type declaration or value setting

            // if a variable name begins by "is" there is a chance that's a bool
            patterns.Add ( "var("+oblWS+"is"+commonName+optWS+";)" );
            replacements.Add ( "bool$1" );

            // if a variable name ends by "Rect" there is a chance that's a Rect
            patterns.Add ( "var("+oblWS+commonName+"Rect"+optWS+";)" );
            replacements.Add ( "Rect$1" );

            // one thing I could do is guessing the type the first time the value is set


        //--------------------


        // other types (classes instantiation)    Test _test3 = new Test();
        // "new" keywords are already added everywhere they are needed by the method "AddNewKeyword()"
        patterns.Add ( "var"+oblWS+"("+commonName+optWS+"="+optWS+"new"+oblWS+commonChars+optWS+"\\((.*);)" );
        replacements.Add ( "$7 $2" );

		
        //--------------------


        // var declaration vithout a type and the value comes from a function :
        // The type can be resolved if the function exists in itemsAndTypes (see below) or if the function declaration is done in the script, 
        // As UnityScript allows not to specify which type returns a function, wait until the functions declarations are processed (in UnityScriptToCSharp_Functions.Functions()) to try to convert those variables

        // meanwhile, check for values and function calls that are within itemsAndTypes
        foreach (KeyValuePair<string, string> item in itemsAndTypes) {
            if (script.text.Contains (item.Key)) { // it just reduce the number of elements in patterns and replacements lists
                
                // if the item is a method
                patterns.Add ( "var("+oblWS+commonName+optWS+"="+optWS+item.Key+optWS+"(\\(.*\\)"+optWS+")?;)" );
                replacements.Add ( item.Value+"$1" );
                
                // if the item is a variable
                //patterns.Add ( "var("+oblWS+commonName+optWS+"="+optWS+item.Key+optWS+";)" );
                //replacements.Add ( item.Value+"$1" );
            }
        }
        // about the same code is run again in Function()
        

        //--------------------


        //casting while using the Instanciate method
        patterns.Add ( "("+commonName+oblWS+commonName+optWS+"="+optWS+")(Instantiate"+optWS+"\\()" );
        replacements.Add ( "$1($2)$7" );

        
        //--------------------


        // patching time !
        // sometimes float value gets 2 f ??????
            patterns.Add ("([0-9]+\\.{1}[0-9]+)(f|F){2,}");
            replacements.Add ("$1f");


        DoReplacements ();
    } // end Variables()


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Convert Properties declarations
    /// </summary>
    public static void Properties () {
        // change string return type to string, works also for functions (properties in JS are functions)
        patterns.Add ( "(\\)"+optWS+":"+optWS+")String" );
        replacements.Add ( "$1string" );

        // change bool return type to bool
        patterns.Add ( "(\\)"+optWS+":"+optWS+")boolean" );
        replacements.Add ( "$1bool" );

        DoReplacements ();


        //--------------------


        // first, get all property getters (if a property exists, I assume that a getter always exists for it)
        pattern = "(?<visibility>public|private|protected)"+oblWS+"function"+oblWS+"get"+oblWS+"(?<propName>"+commonName+")"+optWS+"\\("+optWS+"\\)"+optWS+":"+optWS+"(?<returnType>"+commonChars+")"+optWS+
        "{"+optWS+"return"+oblWS+"(?<varName>"+commonName+")"+optWS+";"+optWS+"}";
        List<Match> allGetters = ReverseMatches (script.text, pattern);
        
        string unModifiedScript = script.text;

        foreach (Match aGetter in allGetters) {
            string propName = aGetter.Groups["propName"].Value;
            string variableName = aGetter.Groups["varName"].Value;

            // now I have all infos nedded to start building the property in C#
            // match.Groups[1].Value is the getter's visibility. It always exists by now because it has been added by the AddVisibility() method above
            // match.Groups[10].Value is the getter's return type
            string property = aGetter.Groups["visibility"].Value+" "+aGetter.Groups["returnType"].Value+" "+propName+" {"+EOL 
            +"\t\tget { return "+variableName+"; }"+EOL; // getter


            // now look for the corresponding setter
            pattern = "(public|private|protected)"+oblWS+"function"+oblWS+"set"+oblWS+propName+".*}";
            Match theSetter = Regex.Match (unModifiedScript, pattern);

            if (theSetter.Success)
                property += "\t\t"+theSetter.Groups[1].Value+" set { "+variableName+" = value; }"+EOL; // setter

            property +="\t}"+EOL; // property closing bracket


            // now do the modifs in the script
            script.text = script.text.Replace (aGetter.Value, property); // replace getter by property

            if (theSetter.Success)
                script.text = script.text.Replace (theSetter.Value, ""); // remove setter if it existed
        }
    } // end Properties ()



    // ----------------------------------------------------------------------------------

    /// <summary> 
    /// Now that all functions have a return type, try to convert the few variables whose value are set from a function
    /// </summary>
    public static void VariablesTheReturn () {
        pattern = "var"+oblWS+"("+commonName+optWS+"="+optWS+commonName+optWS+"\\()";
        List<Match> allVariableDeclarations = ReverseMatches (script.text, pattern);

        foreach (Match aVarDeclaration in allVariableDeclarations) {
            // look for the function declaration that match the function name
            pattern = commonChars+oblWS+aVarDeclaration.Groups[6].Value+optWS+"\\("; // aVarDeclaration.Groups[6].Value is the function name
            Match theFunction = Regex.Match (script.text, pattern); // quid if the same function name return sevral types of values ??

            if (theFunction.Success) // function.Groups[1].Value is the return type
                script.text = script.text.Replace (aVarDeclaration.Value, aVarDeclaration.Value.Replace ("var ", theFunction.Groups[1].Value+" "));
        }
    }
} // end class UnityScriptToCSharp_Variables
