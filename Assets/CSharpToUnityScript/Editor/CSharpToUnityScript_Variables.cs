
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;


public class CSharpToUnityScript_Variables: CSharpToUnityScript {
	                                      
    /// <summary> 
    /// Convert stuffs related to functions : declaration
    /// </summary>
    public static void Variables () {
        
        // multiple inline var declaration of the same type : "Type varName, varName = foo;"
        pattern = "(?<type>"+commonChars+")"+oblWS+"(?<varList>"+commonName+optWS+"(="+optWS+commonCharsWithoutComma+optWS+")?,"+optWS+".*);"; // won't match if the value is a mehod or a class instanciation or a dictionnary
        List<Match> allDeclarations = ReverseMatches (script.text, pattern);

        foreach (Match aDeclaration in allDeclarations){
            // split the varlist using the coma
            string[] varList = aDeclaration.Groups["varList"].Value.Split (',');
            string type = aDeclaration.Groups["type"].Value;

            string newSyntax = "";

            foreach (string varName in varList) {
                if (varName.Contains ("=")) {
                    // add the type beetween the varName and the equal sign
                    string varDeclaration = varName.Replace ("=", ": "+type+" =");
                    newSyntax += "var "+varDeclaration.Trim ()+";"+EOL;
                }
                else 
                    newSyntax += "var "+varName.Trim ()+" :"+type+";"+EOL;
            }

            script.text = script.text.Replace (aDeclaration.Value, newSyntax);
        }


        // " as AType;" => ";"
        // "as aType" might appear in other location
        patterns.Add ( oblWS+"as"+oblWS+commonChars+optWS+";" );
        replacements.Add ( ";" );

        // var declaration with value
        patterns.Add ( commonChars+oblWS+commonName+optWS+"(=.*;)" );
        replacements.Add ( "var $3: $1$4$5" );

        // var declaration without value   
        patterns.Add ( commonChars+oblWS+commonName+optWS+";" ); 
        replacements.Add ( "var $3: $1$4;" );

        // var declaration in foreach loop 
        patterns.Add ( commonChars+oblWS+commonName+"("+oblWS+"in"+oblWS+")" );
        replacements.Add ( "var $3: $1$4" );


        /// just a test
        patterns.Add ("\\["+optWS+"\\]"+optWS+"\\["+optWS+"\\]"); // C# 2D arrays
        replacements.Add ("[,]");




        // pathcing   converting var declaration leads to some garbage
           // assembly imports
           patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"import"+optWS+";" ); // will mess up if a custom class is named "import"...
           replacements.Add ( "import $2;" );

           // returned values
           patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"return"+optWS+";" );
           replacements.Add ( "return $2;" );

           // "else aVar = aValue;" got converted in  "var aVar: else = aValue;"
           patterns.Add ( "var"+oblWS+commonName+optWS+":"+optWS+"else"+optWS+"=" );
           replacements.Add ( "else $2 =" );


        


        // casting
        // we can make it a little more secure by checking that the to commonChars are the same
        patterns.Add ( "(var"+oblWS+commonName+optWS+":"+optWS+commonChars+optWS+"="+optWS+")\\("+optWS+commonChars+optWS+"\\)("+optWS+commonName+optWS+";)" );
        replacements.Add ( "$1$12" );

        patterns.Add ( "((\\(|=)"+optWS+")\\("+optWS+commonChars+optWS+"\\)("+optWS+commonName+")" ); // match if and loop without brackets !!
        replacements.Add ( "$1$7" );



        // string      works with var declaraion with or without value (the same is run after that the function argument are converted)
        patterns.Add ( "("+commonName+optWS+":"+optWS+")string(("+optWS+"\\["+optWS+"\\])?"+optWS+"(=|;))" );
        replacements.Add ( "$1String$5" );

        // bool
        patterns.Add ( "("+commonName+optWS+":"+optWS+")bool(("+optWS+"\\["+optWS+"\\])?"+optWS+"(=|;))" );
        replacements.Add ( "$1boolean$5" );


        // arrays

            // replace curly brackets by square bracket
            patterns.Add ( "(="+optWS+commonName+optWS+"){(.*)}("+optWS+";)" );
            replacements.Add ( "$1[$2]$3" );


        // char 'a' => 'a'[0]
        patterns.Add ( "'[a-zA-z0-9]{1}'" );
        replacements.Add ( "$0[0]" );


        // remove f to Float values
        // actually not needed, in works with the f
        patterns.Add ( "([0-9]{1}(\\.[0-9]{1})?)(f|F)" );
        replacements.Add ( "$1" );





        DoReplacements ();
    } // end Functions()


    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Convert Properties declarations
    /// </summary>
    public static void Properties () {
            
        // find properties
        pattern = "((?<visibility>public|private|protected)"+oblWS+")?(?<blockType>"+commonChars+")"+oblWS+"(?<blockName>"+commonName+")"+optWS+"{";
        List<Match> allProperties = ReverseMatches (script.text, pattern);

        foreach (Match aProp in allProperties) {
            string[] types = {"enum", "class", "extends", "new", "else"};
            List<string> forbiddenBlockTypes = new List<string> (types);

            if (forbiddenBlockTypes.Contains ( aProp.Groups["blockType"].Value) )
                continue;

            string[] names = {"get", "set", "else", "if"};
            List<string> forbiddenBlockNames = new List<string> (names);

            if (forbiddenBlockNames.Contains ( aProp.Groups["blockName"].Value) )
                continue;


            Block PropBlock = new Block (aProp, script.text);
            Debug.Log ("property : "+aProp.Value+" | "+PropBlock.text);

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

            Debug.Log ("new prop : "+property);
            string cSharpProperty = aProp.Value.Replace ("{", PropBlock.text);

            script.text = script.text.Replace (cSharpProperty, property); // replace property block by the new(s) function(s)
        } // end lopping on properties
    } // end of method Properties()
} // end of class CSharpToUnityScript_Functions
