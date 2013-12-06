<?php

	$secret = "mupfi";
	$cachedir = "cache";
	
	// end of configuration

	if(isset($_POST["data"]) && $_POST["key"] == $secret) {
	
		if(isset($_POST["compression"]) && $_POST["compression"] == "true") {
			$data = gzinflate(base64_decode($_POST["data"]));
			if($data == false) {
				echo "Unable to inflate!";
				die();
			}
		}
		else
			$data = stripslashes($_POST["data"]);

		$rebuild = false;
				
		if((int)$_POST["sessionid"] > 0 && (int)$_POST["subsessionid"] > 0) {
			$filename = $cachedir ."/". $_POST["sessionid"] ."-". $_POST["subsessionid"]. "-". $_POST["sessionnum"] ."-". $_POST["type"] .".json";
			if(!is_file($filename))
				$rebuild = true;
			$fp = fopen($filename, 'w+');
			fwrite($fp, $data, strlen($data));
			fclose($fp);
		}
		else {
			echo "Session ID error!";
		}
		
		if($rebuild)
			rebuild_list();
	}
	else if(strlen($_GET["refresh"]) > 0) {
		rebuild_list();
	}
	else if ($_GET["phpinfo"])
		phpinfo();
	else if ($_POST["key"] != $secret)
		echo "Key error!";
	else
		echo "General error!";

	function rebuild_list() {
		global $cachedir;
		
		if ($handle = opendir($cachedir)) {
			while (false !== ($file = readdir($handle))) {
				if ($file != "." && $file != "..") {
					$path_parts = pathinfo($cachedir . "/". $file);
					if($path_parts["extension"] == "json") {
						$parts = explode("-", $path_parts["filename"]);
						if(count($parts) == 4)
							$jsons[] = $parts;
					}
				}
			}
			closedir($handle);
		}

		$data = array2json($jsons);
		$fp = fopen($cachedir ."/list.json", "w+");
		fwrite($fp, $data, strlen($data));
		fclose($fp);
	}
	
	/* 
		PHP4 support 
		http://www.bin-co.com/php/scripts/array2json/
	*/
	function array2json($arr) { 
		if(function_exists("json_encode")) return json_encode($arr); //Lastest versions of PHP already has this functionality.
		$parts = array(); 
		$is_list = false; 

		//Find out if the given array is a numerical array 
		$keys = array_keys($arr); 
		$max_length = count($arr)-1; 
		if(($keys[0] == 0) and ($keys[$max_length] == $max_length)) {//See if the first key is 0 and last key is length - 1
			$is_list = true; 
			for($i=0; $i<count($keys); $i++) { //See if each key correspondes to its position 
				if($i != $keys[$i]) { //A key fails at position check. 
					$is_list = false; //It is an associative array. 
					break; 
				} 
			} 
		} 

		foreach($arr as $key=>$value) { 
			if(is_array($value)) { //Custom handling for arrays 
				if($is_list) $parts[] = array2json($value); /* :RECURSION: */ 
				else $parts[] = '"' . $key . '":' . array2json($value); /* :RECURSION: */ 
			} else { 
				$str = ''; 
				if(!$is_list) $str = '"' . $key . '":'; 

				//Custom handling for multiple data types 
				if(is_numeric($value)) $str .= $value; //Numbers 
				elseif($value === false) $str .= 'false'; //The booleans 
				elseif($value === true) $str .= 'true'; 
				else $str .= '"' . addslashes($value) . '"'; //All other things 
				// :TODO: Is there any more datatype we should be in the lookout for? (Object?) 

				$parts[] = $str; 
			} 
		} 
		$json = implode(',',$parts); 
		 
		if($is_list) return '[' . $json . ']';//Return numerical JSON 
		return '{' . $json . '}';//Return associative JSON 
	} 

?>
