#pragma strict

function Start () {
	var _var: String = "123";

	DoSomething (Array(_var));
	Debug.Log ("var="+_var);
}

function DoSomething (var1: Array) {
	var var2 = var1[0];
	var1[0] = "abc";
}

function Update () {

}