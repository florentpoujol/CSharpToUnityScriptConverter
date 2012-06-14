using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;


public class CSharpToUnityScript_Functions: CSharpToUnityScript {
	                                      
    /// <summary> 
    /// Convert stuffs related to functions : declaration
    /// </summary>
    public static void Functions () {
        
        // function declaration ...
        // using a simple pattern/replacement regex as below match way too much things it shouldn't
        // So I need to check each match before allowing the replacement

        // patterns.Add ( commonChars+oblWS+commonName+optWS+"(\\(.*\\))"+optWS+"{" ); // here I don't care if the method is public, private, static, abstract or whatever since a signature is always composed of a type followed by the name of the method
        // replacements.Add ( "function $3$5: $1 {" );

        pattern = "(?<returnType>"+commonChars+")"+oblWS+"(?<functionName>"+commonName+")"+optWS+"(\\("+argumentsChars+"\\))("+optWS+"{)"; // match two words followed by a set of parenthesis followed by an opening curly bracket
        List<Match> allFunctionsDeclarations = ReverseMatches (script.text, pattern);

        

        foreach (Match aFunctionDeclaration in allFunctionsDeclarations) {
            string returnType = aFunctionDeclaration.Groups["returnType"].Value;
            string functionName = aFunctionDeclaration.Groups["functionName"].Value;
            

            //Debug.Log ("returnType="+returnType+" | functionName="+functionName);

            if ( returnType == "else" && functionName == "if") // do not match else if () statement
                continue;

            if ( returnType == "new" ) // do not match class instanciatton inside an if statement   ie : new Rect ()) {}  => it shouldn't anymore anyway, thanks to argumentsChars instead of ".*"
                continue;

            // if we are there, it's really a function declaration that has to be converted
            patterns.Add ( "("+returnType+")"+oblWS+"("+functionName+")"+optWS+"(\\("+argumentsChars+"\\))("+optWS+"{)" ); 
            // I can't use aFunctionDeclaration.Value as the pattern because square brackets that may be found in the argments (if some args ar arrays) wouldn't be escaped and would cause an exception

            switch ( returnType ) {
                case "void" : replacements.Add ( "function $3$5$7" ); continue;
                case "string" : replacements.Add ( "function $3$5: String$7" ); continue;
                case "string[]" : replacements.Add ( "function $3$5: String[]$7" ); continue; 
                case "bool" : replacements.Add ( "function $3$5: boolean$7" ); continue; 
                case "bool[]" : replacements.Add ( "function $3$5: boolean[]$7" ); continue; 
                case "public" /* it's a constructor */ : replacements.Add ( "$1 function $3$5$7" ); continue;
            }
            
            
            

            // if we are there, it's that the functiondeclaration has nothing special
            replacements.Add ( "function $3$5: $1$7" );
        }




        /*patterns.Add ( ": void"+optWS+"{" );
        replacements.Add ( "" );

        patterns.Add ( ": string(("+optWS+"\\["+optWS+"\\])?"+optWS+"{)" );
        replacements.Add ( ": String$1" );

        patterns.Add ( ": bool(("+optWS+"\\["+optWS+"\\])?"+optWS+"{)" );
        replacements.Add ( ": boolean$1" );*/


        // remove out keyword in arguments
        patterns.Add ( ","+optWS+"out("+oblWS+commonName+")" );
        replacements.Add ( ",$2" );

        // remove ref keyword in arguments
        patterns.Add ( ","+optWS+"ref("+oblWS+commonCharsWithoutComma+")" );
        replacements.Add ( ",$2" );


        // arguments declaration      if out and ref keyword where not removed before this point it would also convert them ("out hit" in Physics.Raycast() calls) or prevent the convertion ("ref aType aVar" as function argument)
        patterns.Add ( "(\\(|,){1}"+optWS+commonCharsWithoutComma+oblWS+commonName+optWS+"(\\)|,){1}" );
        replacements.Add ( "$1$2$5: $3$6$7" );
        // as regex doesn't overlap themselves, only half of the argument have been converted
        // I need to run the regex  a second time
        patterns.Add ( "(\\(|,){1}"+optWS+commonCharsWithoutComma+oblWS+commonName+optWS+"(\\)|,){1}" );
        replacements.Add ( "$1$2$5: $3$6$7" );


        // string
        patterns.Add ( "("+commonName+optWS+":"+optWS+")string(("+optWS+"\\["+optWS+"\\])?"+optWS+"(,|\\)))" );
        replacements.Add ( "$1String$5" );

        // bool
        patterns.Add ( "("+commonName+optWS+":"+optWS+")bool(("+optWS+"\\["+optWS+"\\])?"+optWS+"(,|\\)))" );
        replacements.Add ( "$1boolean$5" );


        DoReplacements ();


        // loop through function and search for variable declaration that happend several times
        // leave only the first declaration

        pattern = "function"+oblWS+commonName+optWS+"\\("+argumentsChars+"\\)("+optWS+":"+optWS+commonChars+")?"+optWS+"{"; 
        allFunctionsDeclarations = ReverseMatches (script.text, pattern);

        foreach (Match aFunctionDeclaration in allFunctionsDeclarations) {
            Block functionBlock = new Block (aFunctionDeclaration, script.text);

            pattern = "var"+oblWS+"(?<varName>"+commonName+")"+optWS+":"+optWS+"(?<varType>"+commonChars+")"+optWS+"(?<ending>(=|;))"; // don't match var declaration in foreach loop
            List<Match> allVariablesDeclarations = ReverseMatches (functionBlock.text, pattern);

            foreach (Match aVariableDeclaration in allVariablesDeclarations) {
                string varName = aVariableDeclaration.Groups["varName"].Value;
                string varType = aVariableDeclaration.Groups["varType"].Value.Replace ("[]", "" );//Replace ("[", "\\[").Replace ("]", "\\]")
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

            // replace old function text by new one in script.text
            script.text = script.text.Replace (functionBlock.text, functionBlock.newText);
        } // end lopp function declarations
    } // end Functions()
} // end class CSharpToUnityScript_Functions
