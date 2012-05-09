class MyDebug {
	
	static var type: String = "default";
	private static var doDebug: boolean = false;


	static function Print (_type: String, msg: String) {
		if (_type == type)
			Debug.Log (msg);
	}

	static function BeginDebug () {
		doDebug = true;
	}

	static function EndDebug () {
		doDebug = false;
	}

	static function Print (msg: String) {
		if (doDebug)
			Debug.Log (msg);
	}
}