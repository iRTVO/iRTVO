/*
 * Custom JavaScript 
 */

$(function() {

	// get last commit
	$.getJSON( "https://api.github.com/repos/irtvo/irtvo/commits", function( data ) {
		item = data.shift();
		var title = item.commit.message.split("\n").shift();
		$('#last-commit').html('Last commit: <strong><a title="'+ item.commit.author.name +' @ '+ item.commit.author.date +'" href="'+ item.html_url +'">'+ title +'</a></strong>');
	});

});

function toggleDownload(id) {
	// hide all
	$('.old-version').addClass('hidden');
	
	// show selected
	$(id).removeClass('hidden');
}
