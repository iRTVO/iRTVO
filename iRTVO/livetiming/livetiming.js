/*!
 * iRTVO Live Timing v1.1 nightlies
 * http://code.google.com/p/irtvo/
 *
 * Copyright 2011, Jari Ylimäinen
 * Licensed under GNU General Public License Version 3
 * http://www.gnu.org/licenses/gpl.html
 *
 */

$(document).ready(function() {
	
	/*
			CONFIGURATION
	*/
	var cols = new Array("pos", "numberPlate", "name", "car", "completedLaps", "fastLap", "previouslap", "diff", "gap");
	var colNames = new Array("", "#", "Name", "Car", "Lap", "Fastest", "Previous", "Int", "Gap");
	var updateFreqStandings = 5;
	var updateFreqSessions = 2 * updateFreqStandings;
	var cachedir = "cache";
	
	var drivers = new Array();
	var standings = new Array();
	var sessions = new Array();
	var track = new Array();
	var cars = new Array();
	var classes = new Array();
	var sessionNum = 0;
	var sessionId = 0;
	var subSessionId = 0;
	var table = document.getElementById("standings");
	
	$.ajaxSetup({
	  "error":function(event, request, settings){ 
		$("#debug").append("AJAX error: event:"+event+" request:"+ request +" settings:"+settings+"\n");
	  }});
	  
	addRow = function(table, cells){ 
		var row = table.insertRow(-1);
		for(i = 0; i < cells; i++) {
			var cell = row.insertCell(-1);
		}
	}
	
	loadDrivers = function(){ 
		$.getJSON(cachedir + '/'+ sessionId +'-'+ subSessionId +'-drivers.json', function(data) {
			drivers = new Array();
			for(i = 0; i < data.length; i++) {
				if(data[i]["userId"] > 0) {
					drivers.push(data[i]);
				}
			}
		});
	}
	
	loadStandings = function(){ 
		$.getJSON(cachedir + '/'+ sessionId +'-'+ subSessionId +'-'+ sessionNum +'-standing.json', function(data) {
			standings = data[sessionNum];
			
			if(standings.length != table.tBodies[0].rows.length) {
				rebuildTable();
			}
			
			for(i = 0; i < table.tBodies[0].rows.length; i++) {
				var row = table.tBodies[0].rows[i];
				var stand = standings[i];
								
				if(stand != undefined) {
					if(sessions[sessionNum]["official"] == false) {
						stand["id"] -= 1;
					}

					var driver = drivers[i];
					if(stand != undefined) {
						driver = drivers[stand["id"]];
					}
					
					for(j = 0; j < cols.length; j++) {
						var cell = row.cells[j];
						
						if(cols[j] == "pos") {
							cell.innerText = (i+1) + ".";
						} 
						else if(cols[j] == "diff" && stand != undefined) {
							if(sessions[sessionNum]["type"] == 6) { // race
								if(stand["lapDiff"] > 0) {
									row.cells[j].innerText = stand["lapDiff"] + " lap";
									if(parseInt(stand["lapDiff"]) > 1) {
										cell.innerText += "s";
									}
								}
								else {
									cell.innerText = secondsToHms(parseFloat(stand["diff"]) - parseFloat(standings[0]["diff"]), true);
								}
							}
							else { // non-race
								cell.innerText = secondsToHms(parseFloat(stand["diff"]) - parseFloat(standings[0]["diff"]), true);
							}
						}
						else if(cols[j] == "gap" && stand != undefined) {
							if(i>0) {
								cell.innerText = secondsToHms(parseFloat(stand["diff"]) - parseFloat(standings[i-1]["diff"]), true);
							}
							else {
								cell.innerText = "-.--";
							}
						}
						else if((cols[j] == "fastLap" || cols[j] == "previouslap") && stand != undefined) {
							cell.innerText = secondsToHms(parseFloat(stand[cols[j]]), true);
						}
						else if(cols[j] == "completedLaps" && stand != undefined) {
							cell.innerText = parseInt(stand[cols[j]]);
						}
						else if(cols[j] == "car") {
							if(stand != undefined) {
								cell.innerText = cars[drivers[stand["id"]]["car"]];
							}
							else {
								cell.innerText = "--";
							}
						}
						else if(cols[j] == "class") {
							if(stand != undefined) {
								cell.innerText = classes[drivers[stand["id"]]["car"]];
							}
							else {
								cell.innerText = "--";
							}
						}
						else if(isKeyInArray(stand, cols[j]) != false && stand != undefined) {
							cell.innerText = stand[cols[j]];
						}
						else if(isKeyInArray(driver, cols[j]) != false) {
							cell.innerText = driver[cols[j]];
						}
						else {
							cell.innerText = "--";
						}
					}
				}
			}
		});
	}
	
	loadSessions = function(){ 
		$.getJSON(cachedir + '/'+ sessionId +'-'+ subSessionId +'-sessions.json', function(data) {
			sessions = data;
			
			//for(i = 0; i < sessions.length; i++) {
			//	if(sessions[i]["type"] != 0)
			//		sessionNum = i;
			//}
			
			if(sessions[sessionNum] != undefined) {
				if($("#timeRemaining").attr("float") == undefined || 
					sessions[sessionNum]["timeRemaining"] < parseFloat($("#timeRemaining").attr("float"))) {
					if(sessions[sessionNum]["timeRemaining"] < 4 * 60 * 60) {
						$("#timeRemaining").text(secondsToHms(sessions[sessionNum]["timeRemaining"], false));
					}
					else {
						$("#timeRemaining").text("-.--");
					}
					$("#timeRemaining").attr("float", sessions[sessionNum]["timeRemaining"]);
				}
			
				if(parseInt(sessions[sessionNum]["laps"]) < 32767) {
					$("#lap").text(1 + parseInt(sessions[sessionNum]["laps"]) - parseInt(sessions[sessionNum]["lapsRemaining"]));
					$("#totalLaps").text(sessions[sessionNum]["laps"]);
				}
				else {
					$("#lap").text("-");
					$("#totalLaps").text("-");
				}
				
				$("#sessiontype").text(sessionTypes[sessions[sessionNum]["type"]]);
				
				switch(sessions[sessionNum]["state"]) {
					case 1:
						$("#sessionstate").text("gridding");
						break;
					case 2:
						$("#sessionstate").text("warmup");
						break;
					case 3:
						$("#sessionstate").text("pace lap");
						break;
					case 4:
						$("#sessionstate").text("racing");
						break;
					case 5:
						$("#sessionstate").text("checkered");
						break;
					case 6:
						$("#sessionstate").text("cool down");
						break;
					default:
						$("#sessionstate").text("-");
				}
				
				switch(sessions[sessionNum]["flag"]) {
					case 1:
						$("#flag").text("yellow");
						break;
					case 2:
						$("#flag").text("red");
						break;
					default:
						$("#flag").text("green");
				}
			}
		});
	}
	
	loadTrack = function(){ 
		$.getJSON(cachedir + '/'+ sessionId +'-'+ subSessionId +'-track.json', function(data) {
			track = data;
			$("#track").text(track["name"]);
		});
	}
	
	loadCars = function(){ 
		$.getJSON(cachedir + '/'+ sessionId +'-'+ subSessionId +'-cars.json', function(data) {
			cars = data;
		});
	}
	
	loadClasses = function(){ 
		$.getJSON(cachedir + '/'+ sessionId +'-'+ subSessionId +'-classes.json', function(data) {
			classes = data;
		});
	}
	
	rebuildTable = function() {
		$("#standings tbody").html("");
		for(i = 0; i < standings.length; i++) {
			var row = table.tBodies[0].insertRow(-1);
			for(j = 0; j < cols.length; j++) {
				var cell = row.insertCell(-1);
			}
		}
	}
	
	updateSessionTimers = function() {
		if($("#timeRemaining").attr("float") != undefined) {
			var time = parseFloat($("#timeRemaining").attr("float"));
			
			time -= 1;
			if(time < 0) {
				time = 0;
			}
			else if(time < 4 * 60 * 60) {
				$("#timeRemaining").attr("float", time);
				$("#timeRemaining").text(secondsToHms(time, false));
			}
			else {
				$("#timeRemaining").text("-.--");
			}
			
		}
	}
	
	$("#sessionSelection").change(function () {
        if($("select option").length > 0) {
			$("select option:selected").each(function () {
				sessionId = $(this).attr("sessionid");
				subSessionId = $(this).attr("subsessionid");
				sessionNum = $(this).attr("sessionnum");
			});
			reload();
		}
    }).change();

	// init
	// write header
	var header = new String();
	for (var i = 0; i < cols.length; i++) {
		if(colNames[i] != undefined) {
			header += '<th>' + colNames[i]  + '</th>';
		}
		else {
			header += '<th>' + cols[i]  + '</th>';
		}
	}
	
	// load sessions
	$.getJSON(cachedir + '/list.json', function(data) {
			for(i = 0; i < data.length; i++) {
				$("#sessionSelection").append('<option sessionid="'+ data[i][0] +'" subsessionid="'+ data[i][1] +'" sessionnum="'+ data[i][2] +'">Session '+ data[i][0] +' - '+ data[i][2] +'</option>');
				if(i == data.length-1) {
					sessionId = data[i][0];
					subSessionId = data[i][1];
					sessionNum = data[i][2];
				}
			}
			reload();
		});
	
	reload = function() {
		rebuildTable();
		loadDrivers();
		loadSessions();
		loadTrack();
		loadCars();
		loadClasses();
		loadStandings();
	}
	
	$("#standings thead").append('<tr>' + header + '</tr>');
	/*
	loadDrivers();
	loadSessions();
	loadTrack();
	loadStandings();
	
	reload();
	*/
	
	setInterval(updateSessionTimers, 1000);
	setInterval(loadDrivers, 60000);
	setInterval(loadSessions, updateFreqSessions * 1000);
	setInterval(loadStandings, updateFreqStandings * 1000);
});


var sessionTypes = new Array(
	"invalid",
	"testing",
	"practice",
	"practice",
	"qualify",
	"qualify",
	"race",
	"grid"
);

// Modified from http://snipplr.com/view.php?codeview&id=12299
function isKeyInArray(arr, val) {
	inArray = false;
	i = 0;
	for (key in arr) {
		i++
		if (val == key)
			inArray = i;
	}
	
	return inArray;
}

// Modified from http://snipplr.com/view.php?codeview&id=20348
function secondsToHms(d, showMs) {
	d = Number(d);
	var h = Math.floor(d / 3600);
	var m = Math.floor(d % 3600 / 60);
	var s = Math.floor(d % 3600 % 60);
	var ms = Math.round((d % 3600 % 60)*1000)/1000;
	
	if(d <= 0 || h > 6)
		return "-.--";
	else {
		var output = new String();
		
		if(h > 0)
			output += h + ":";
		
		if(h > 0 && (m < 10 && m > 0))
			output += "0" + m + ":";
		else if(m > 0)
			output += m + ":";
	
		if(showMs) {
			if(ms < 10)
				output += "0";
			output += number_format(ms, 3);
		}
		else {
			if(ms < 10)
				output += "0";
			output += s;
		}
			
		return output;
	}
}

// http://phpjs.org/functions/number_format:481
function number_format (number, decimals, dec_point, thousands_sep) {
    number = (number + '').replace(/[^0-9+\-Ee.]/g, '');
    var n = !isFinite(+number) ? 0 : +number,
        prec = !isFinite(+decimals) ? 0 : Math.abs(decimals),
        sep = (typeof thousands_sep === 'undefined') ? ',' : thousands_sep,
        dec = (typeof dec_point === 'undefined') ? '.' : dec_point,
        s = '',
        toFixedFix = function (n, prec) {
            var k = Math.pow(10, prec);
            return '' + Math.round(n * k) / k;
        };
    // Fix for IE parseFloat(0.55).toFixed(0) = 0;
    s = (prec ? toFixedFix(n, prec) : '' + Math.round(n)).split('.');
    if (s[0].length > 3) {
        s[0] = s[0].replace(/\B(?=(?:\d{3})+(?!\d))/g, sep);
    }
    if ((s[1] || '').length < prec) {
        s[1] = s[1] || '';
        s[1] += new Array(prec - s[1].length + 1).join('0');
    }
    return s.join(dec);
}
