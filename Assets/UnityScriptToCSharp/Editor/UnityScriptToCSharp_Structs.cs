
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public struct Block {
	public Match match;

	public int startIndex;
	public int endIndex; // index of the opening and closing bracket of block in reftext
	public string refText; //

	public string name; // name of the function or class
	public string declaration; // full block's declaration (up util the opening bracket) usually the match's value
	public string text; // text inside the block between the opening and closing bracket
	public string newText;

    public bool isEmpty; // tell wether text is empty or not

	// ----------------------------------------------------------------------------------

	/// <summary>
	/// 
	/// </summary>
	/// <param name="match"></param>
	/// <param name="refText"></param>
	public Block (Match match, string refText) {
		this.match = match;
		declaration = match.Value;
		name = match.Groups[2].Value; // can't do that now, it depends of the regex, or I coult use match.Groups["name"]
		this.refText = refText;
		
		startIndex = match.Index + match.Length - 1;
		endIndex = 0;
		text = "";
		newText = "";
        isEmpty = true;

		endIndex = this.GetEndOfBlockIndex ();
		
        if (endIndex == -1)
            return;

		if (endIndex <= startIndex) {
			Debug.LogWarning ("Block::Block() : endIndex <= startIndex. Can't get block text. match=["+match.Value+"] startIndex=["+startIndex+"] endIndex=["+endIndex+"] refText=["+refText+"].");
			return;
		}

		text = refText.Substring (startIndex, endIndex-startIndex);
        isEmpty = (text.Trim() == "");
	}


	// ----------------------------------------------------------------------------------

	/// <summary>
	/// 
	/// </summary>
	int GetEndOfBlockIndex () {
        int openedBrackets = 0;

		for (int i = startIndex; i < refText.Length; i++) {
			//Debug.Log ("char at index["+i+" =["+refText[i]+"]");
			if (refText[i] == '{')
				openedBrackets++;

			if (refText[i] == '}') {
				openedBrackets--;

				if (openedBrackets == 0)
					return i+1;
			}
		}

		// no matching closing bracket has been found
		Debug.LogError ("Block::GetEndOfBlockIndex() : No matching closing bracket has been found ! Returning -1. match=["+match.Value+"] startIndex=["+startIndex+"] ["+
			refText[startIndex-1]+"|"+refText[startIndex]+"|"+refText[startIndex+1]+"] text=["+refText+"].");
		return -1;
	}
}


// ----------------------------------------------------------------------------------


public struct Script { // can't use FILE since it's a class in System.IO
	public string path;
	public string name;
	public string text;
	public string newText;

	public Script (string _path, string _text) {
		path = _path.Remove (_path.Length-3); // remove ".js"
		
		int lastIndexof = path.LastIndexOf ("\\");
        name = path.Substring (lastIndexof+1); // isolate the name
        path = path.Replace (name, "");// the path end by a slash
		
		text = _text;
		newText = "";
	}
}


// ----------------------------------------------------------------------------------


public enum ProjectItemCategory {
	Class,
	Method,
	Member,
	Variable
	//Argument
}

public class ProjectItem { // structs anoy me sometimes
	public static List<ProjectItem> projectItems = new List<ProjectItem> (); // list of all project items
	//public static List<ProjectItem> classes; // list of classes

	public List<ProjectItem> methods = new List<ProjectItem> (); // list of methods if it's a class
	public List<ProjectItem> members = new List<ProjectItem> (); // list of variables if it's a class
	public List<ProjectItem> variables = new List<ProjectItem> (); // list of variables and arguments if it's a method

	public ProjectItemCategory category;

	public string _class; // owner class if it's a ethod or a variable
	public string method; // owner method if it's an argument or a method variable
	public string name;
	public string type;


	//--------------------


	
	// a class
	public ProjectItem (string className) { // a class
		this.category = ProjectItemCategory.Class;
		this.name = className;
		projectItems.Add (this);
	}

	// a method
	public ProjectItem (string className, string methodName, string methodType) {
		this.category = ProjectItemCategory.Method;
		this._class = className;
		this.name = methodName;
		this.type = methodType;

		if (this.type == "")
			this.type = "undefined";

		//ProjectItem _class = GetClass (className);
		GetClass (className).methods.Add (this);

		projectItems.Add (this);
	}

	// a variable (class variable = member or method variable) or an argument
	public ProjectItem (string className, string methodName, string variableName, string variableType) {
		this._class = className;
		this.method = methodName;
		this.name = variableName;
		this.type = variableType;

		if (this.type == "")
			this.type = "undefined";

		if (methodName == "") {
			this.category = ProjectItemCategory.Member;
			GetClass (className).members.Add (this);
		}
		else {
			this.category = ProjectItemCategory.Variable;
			GetMethod (className, methodName).variables.Add (this);
		}
				
		projectItems.Add (this);
	}


	//--------------------


	public ProjectItem GetItem (string name) {
		foreach (ProjectItem item in projectItems) {
			if (item.name == name)
				return item;
		}

		Debug.LogError ("ProjectItem::GetItem : couldn't find the item. name=["+name+"]");
		return null;
	}


	public ProjectItem GetItem (string _class, string method) {
		foreach (ProjectItem item in projectItems) {
			if (item._class == _class && item.method == method)
				return item;
		}

		Debug.LogError ("ProjectItem::GetItem : couldn't find the item. class=["+_class+"] method=["+method+"]");
		return null;
	}

	public ProjectItem GetItem (string _class, string method, string name) {
		foreach (ProjectItem item in projectItems) {
			if (item._class == _class && item.method == method && item.name == name)
				return item;
		}

		Debug.LogError ("ProjectItem::GetItem() : couldn't find the item. class=["+_class+"] method=["+method+"] name=["+name+"]");
		return null;
	}




	public ProjectItem GetClass (string className) {
		foreach (ProjectItem item in projectItems) {
			if (item.category == ProjectItemCategory.Class && item.name == className)
				return item;
		}

		Debug.LogError ("ProjectItem::GetClass() : couldn't find the item. className=["+className+"]");
		return null;
	}


	public ProjectItem GetMethod (string className, string methodName) {
		foreach (ProjectItem item in projectItems) {
			if (item.category == ProjectItemCategory.Method && item._class == className && item.name == methodName)
				return item;
		}

		Debug.LogError ("ProjectItem::GetMethod() : couldn't find the item. className=["+className+"] methodName=["+methodName+"]");
		return null;
	}
}