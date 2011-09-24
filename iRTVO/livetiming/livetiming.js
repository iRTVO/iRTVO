/*!
 * iRTVO Web Timing
 * http://code.google.com/p/irtvo/
 *
 * Copyright 2011, Jari Ylimäinen
 * Licensed under GNU General Public License Version 3
 * http://www.gnu.org/licenses/gpl.html
 *
 */

$(document).ready(function() {

	var sessionNum = 0;
	var sessionId = 0;
	var subSessionId = 0;
	var sessionType = "";
	var manualSelection = false;
	var forceUpdate = true;
	var table = document.getElementById("standings");
	
	$.ajaxSetup({
	  "error":function(event, request, settings){ 
			$("#debug").append("AJAX error: event:"+event+" request:"+ request +" settings:"+settings+"\n");
	  }
	});
	  
	addRow = function(table, cells){ 
		var row = table.insertRow(-1);
		for(i = 0; i < cells; i++) {
			var cell = row.insertCell(-1);
		}
	}
	
	loadData = function(){ 
		if($("#sessionstate").text() != "cooldown" || forceUpdate == true) {
			forceUpdate = false;
			$.getJSON(cachedir + '/'+ sessionId +'-'+ subSessionId +'-'+ sessionNum + '-' + sessionType + '.json' + disablecache(), function(data) {
			
				$("#track").text(data["trackname"]);
				$("#sessiontype").text(data["sessiontype"]);
				$("#sessionstate").text(data["sessionstate"]);
				$("#lap").text(data["currentlap"]);
				$("#totalLaps").text(data["totallaps"]);
				$("#flag").text(data["sessionflag"]);
				$("#cautions").text(data["cautions"]);
				$("#cautionlaps").text(data["cautionlaps"]);
				$("#fastestlap").text(data["fastestlap"] + " (" + data["fastestdriver"] + " on lap "+ data["fastestlapnum"] +")");
				
				var time = parseFloat(data["timeremaining"]);
				$("#timeRemaining").text(secondsToHms(time, false));
				$("#timeRemaining").attr("float", time)
			
				if(data["drivers"].length != table.tBodies[0].rows.length) {
					$("#standings tbody").html("");
					for(j = 0; j < data["drivers"].length; j++) {
						var row = table.tBodies[0].insertRow(-1);
						for(k = 0; k < cols.length; k++) {
							var cell = row.insertCell(-1);
						}
					}
				}
				
				for(i = 0; i < table.tBodies[0].rows.length; i++) {
					var row = table.tBodies[0].rows[i];
					var driver = data.drivers[i];
					
					if(driver["retired"] == true) {
						row.className = "retired";
					}
					else {
						row.className = "";
					}
					
					for(j = 0; j < cols.length; j++) {
						var cell = row.cells[j];
						if(cols[j].substring(0, 6) == "sector") {
							var sectornum = parseInt(cols[j].substring(6, 7));
							if(driver["sectors"][sectornum] != undefined) {
								cell.innerHTML = driver["sectors"][sectornum];
							}
							else {
								cell.innerHTML = "-.--";
							}
						}
						else {
							cell.innerHTML = driver[cols[j]];
						}
					}
				}
				$("#standings tbody tr:nth-child(odd)").addClass("odd");
			});
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
				sessionType = $(this).attr("sessiontype");
				manualSelection = true;
			});
			forceUpdate = true;
			loadData();
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
	updateSessionSelection = function() {
		$.getJSON(cachedir + '/list.json' + disablecache(), function(data) {
			data.sort(sessionSorter);
			if ($("#sessionSelection").length) {
				if(data.length != $("#sessionSelection").find('option').size()) {
					$("#sessionSelection").empty();
					for(i = 0; i < data.length; i++) {
						$("#sessionSelection").append('<option sessionid="'+ data[i][0] +'" subsessionid="'+ data[i][1] +'" sessionnum="'+ data[i][2] +'" sessiontype="'+ data[i][3] +'">Session '+ data[i][0] +' - '+ data[i][2] +'</option>');
						if(manualSelection == false && i == data.length-1) {
							if(sessionId != data[i][0] || subSessionId != data[i][1] || sessionNum != data[i][2]) {
								sessionId = data[i][0];
								subSessionId = data[i][1];
								sessionNum = data[i][2];
								sessionType = data[i][3];
								forceUpdate = true;
								sessionTypes = new Array();
								loadData();
							}
						}
					}
				}
			}
			else {
				// initial load
				if(sessionId == 0)
					load = true;
					
				sessionId = data[data.length-1][0];
				subSessionId = data[data.length-1][1];
				sessionNum = data[data.length-1][2];
				sessionType = data[data.length-1][3];
				
				if(load)
					loadData();
			}
			
			$("#sessions").empty();
			for(i = 0; i < data.length; i++) {
				if(sessionId == data[i][0]) {
					$("#sessions").append('<a href="#" onclick="setSessionNum('+ data[i][2] +', \''+ data[i][3] +'\')">'+ data[i][3] +'</a> ');
				}
			}
		});
	}
	
	setSessionNum = function(num, type) {
		sessionNum = num;
		sessionType = type;
		forceUpdate = true;
		loadData();
	}
	
	$("#standings thead").append('<tr>' + header + '</tr>');
	
	setInterval(updateSessionTimers, 1000);
	setInterval(loadData, updateFreq * 1000);
	setInterval(updateSessionSelection, 10 * 1000);
	
	// initial load
	updateSessionSelection();
});

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

function disablecache() {
	if(navigator.appName == 'Microsoft Internet Explorer') {
		var d = new Date();
		return "?nocache="+ d.getTime();
	}
	else {
		return "";
	}
}

function sessionSorter(a, b) {
	return (a[0]*10+a[2]) - (b[0]*10+b[2]);
}
