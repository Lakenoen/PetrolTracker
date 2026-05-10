var submit = document.getElementById("filter-submit");
const data = {
	Gop: 'and',
	Filters: [
		{
			Op: 'equal',
			Field: 'GasStations.Rating',
			Value: '0',
		},
		{
			Gop: 'or',
			Filters: [
				{
					Op: 'equal',
					Field: 'GasStations.Name',
					Value: "'газпромнефть №276'",
				},
				{
					Op: 'equal',
					Field: 'GasStations.Name',
					Value: "'газпромнефть №633'",
				}
			]
		}
	]
};
submit.addEventListener('click', function (e) {
	fetch('https://localhost:7157', {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json; charset=utf-8'
		},
		body: JSON.stringify(data)
	});
});


var currentPlacemark;
var map;
ymaps.ready(function () {
	console.log('API ��������, ������� �����...');

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