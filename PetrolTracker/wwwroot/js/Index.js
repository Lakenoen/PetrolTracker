const data = {
	Gop: 'and',
	Filters: [
		
	]
};

var filtertButton = document.getElementById("filter-submit");
var bottomSheet = document.getElementById('bottomSheet');
var submit = document.getElementById('submit-button');
var close = document.getElementById('close-button');
var petrols = document.querySelectorAll('.choses-btn');
var stations = document.getElementById('station-filter-input');
var minSlider = document.getElementById('min-slider');
var maxSlider = document.getElementById('max-slider');
var filterPetrols = [];

petrols.forEach(function (petrol) {
	petrol.addEventListener('click', function (e) {
		petrol.classList.add('active');
		filterPetrols.push(petrol);
	})
})

filtertButton.addEventListener('click', function (e) {
	bottomSheet.classList.add('active');
	document.body.style.overflow = 'hidden';
});

close.addEventListener('click', function (e) {
	bottomSheet.classList.remove('active');
	document.body.style.overflow = '';
});

submit.addEventListener('click', function (e) {
	if (filterPetrols.length > 0) {
		petrolFilter = {
			Gop: 'or',
			Filters: [

			]
		};
		filterPetrols.forEach(function (p) {
			petrolFilter.Filters.push({
				Value : p.innerText,
				Field : "Petrols.Name",
				Op : "equal",
				Type: "str"
			});
		});
		data.Filters.push(petrolFilter);
	}


	if(stations.value.length > 0){
		stationName = {
			Value : stations.value,
			Field : "GasStations.Name",
			Op : "like",
			Type: "str"
		};
		data.Filters.push(stationName);
	}

	priceFilter = {
		Value : minSlider.value + '\t' + maxSlider.value,
		Field : "Petrols.Price",
		Op : "between",
		Type: "num"
	}
	
	
	document.querySelector('.scroll_div').innerHTML = '';
	fetch('http://localhost:5290', {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json; charset=utf-8'
		},
		body: JSON.stringify(data)
	}).then(response => {
		if (response.redirected) {
			window.location.href = response.url;
		}
	});
});

function sliderInut(){
	if (parseInt(minSlider.value) >= parseInt(maxSlider.value)) {
   		minSlider.value = maxSlider.value;
  	}
}


var currentPlacemark;
var map;
ymaps.ready(function () {
	map = new ymaps.Map("map", {
		center: [55.751574, 37.573856],
		zoom: 10
	});
});

const boxes = document.querySelectorAll('.GasStation');
boxes.forEach(box => {
	box.addEventListener('click', function (e) {
		var loc = this.querySelector('.location').innerText;
		ymaps.geocode(loc).then(function (res) {
			const firstGeoObject = res.geoObjects.get(0);
			const coordinates = firstGeoObject.geometry.getCoordinates();

			if (currentPlacemark != 'undefined')
				map.geoObjects.remove(currentPlacemark);
			currentPlacemark = new ymaps.Placemark(coordinates);
			map.geoObjects.add(currentPlacemark);
		});
	});
});