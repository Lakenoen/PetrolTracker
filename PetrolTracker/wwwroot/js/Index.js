// === фильтр цена ===
let minSpan = document.getElementById('minVal');
let maxSpan = document.getElementById('maxVal');
const slider = document.getElementById('slider');
noUiSlider.create(slider, {
    start: [parseFloat(minSpan.innerHTML), parseFloat(maxSpan.innerHTML)],
    connect: true,
    step: 0.1,
    range: { min: parseFloat(minSpan.innerHTML), max: parseFloat(maxSpan.innerHTML) }
});
slider.noUiSlider.on('update', (values) => {
    minSpan.innerHTML = values[0];
    maxSpan.innerHTML = values[1];
});

// === фильтр райтинг азс ===
let minSpanStation = document.getElementById('minValRatingStation');
let maxSpanStation = document.getElementById('maxValRatingStation');
const slider_station = document.getElementById('slider-station')
noUiSlider.create(slider_station, {
    start: [0, 5],
    connect: true,
    step: 1,
    range: { min: 0, max: 5 }
});
slider_station.noUiSlider.on('update', (values) => {
    minSpanStation.innerHTML = values[0];
    maxSpanStation.innerHTML = values[1];
});

// === фильтр райтинг топлива ===
let minSpanPetrol = document.getElementById('minValRatingPetrol');
let maxSpanPetrol  = document.getElementById('maxValRatingPetrol');
const slider_petrol = document.getElementById('slider-petrol')
noUiSlider.create(slider_petrol, {
    start: [0, 5],
    connect: true,
    step: 1,
    range: { min: 0, max: 5 }
});
slider_petrol.noUiSlider.on('update', (values) => {
    minSpanPetrol.innerHTML = values[0];
    maxSpanPetrol.innerHTML = values[1];
});

// === Карта ===
var currentPlacemark;
var map;

ymaps.ready(function () {
    map = new ymaps.Map("map", {
        center: [55.751574, 37.573856],
        zoom: 10
    });
});

// Клик по карточке — метка на карте
document.querySelectorAll('.GasStation').forEach(function (card) {
    card.addEventListener('click', function () {
        document.querySelectorAll('.GasStation').forEach(c => c.classList.remove('active'));
        this.classList.add('active');

        var loc = this.querySelector('.location').innerText;
        ymaps.geocode(loc).then(function (res) {
            var firstGeoObject = res.geoObjects.get(0);
            var coordinates = firstGeoObject.geometry.getCoordinates();

            ymaps.geolocation.get({
                provider: 'auto',
                autoReverseGeocode: true
            }).then( resultLocation => {
                map.geoObjects.removeAll();
                var selfLocation = resultLocation.geoObjects.get(0).geometry.getCoordinates();

                if(selfLocation == 'undefined' || selfLocation.length == 0)
                    throw new Error("Error load location");
                
                var multiRoute = new ymaps.multiRouter.MultiRoute(
                {
                    referencePoints: [
                        selfLocation,
                        loc
                    ]
                }, 
                {
                    wayPointDraggable: false,
                    boundsAutoApply: true,
                    routingMode: 'driving' 
                });

                map.geoObjects.add(multiRoute);
            }).catch( error => {
                map.geoObjects.removeAll();
                var currentPlacemark = new ymaps.Placemark(coordinates,{
                    draggable: false
                });
                map.geoObjects.add(currentPlacemark);
                map.setCenter(coordinates, 14, { duration: 500 });
            });

        });
    });
});


let overlay = document.querySelector('.modal-overlay');
document.querySelector('.open-filter-button').addEventListener('click', function(e){
    overlay.style.display = 'flex';
});
overlay.addEventListener('click', function(e){
    if (e.target === overlay)
        overlay.style.display = 'none';
});

// === Фильтры (клиентская сторона) ===

document.querySelectorAll('.allow-fuels').forEach((el)=>{
    el.addEventListener('click', (e)=> {
        if(!el.classList.contains('allow-fuels-active')){
            el.classList = ['allow-fuels-active'];

            const url = new URL(window.location.origin + window.location.pathname);
            url.searchParams.append('petrols1', JSON.stringify(
                [
                    {
                        name: 'АИ 92'
                    }            
                ]
            ));

            fetch(url).then(resp => resp.json());
        }else
            el.classList = ['allow-fuels'];
    });
});

function applyFilters() {
    const data = {
        Gop: 'and',
        Filters: [
            
        ]
    };

    var nameFilter  = document.getElementById('filter-name').value.toLowerCase().trim();
    var addressFilter = document.getElementById('filter-address').value.toLowerCase().trim();
    var priceMin    = parseFloat(minSpan.innerHTML);
    var priceMax    = parseFloat(maxSpan.innerHTML);
    var minRatingStation    = parseInt(minSpanStation.innerHTML);
    var maxRatingStation    = parseInt(maxSpanStation.innerHTML);
    var minRatingPetrol     = parseInt(minSpanPetrol.innerHTML);
    var maxRatingPetrol     = parseInt(maxSpanPetrol.innerHTML);

    priceFilter = {
            Value : priceMin + '\t' + priceMax,
            Field : "Petrols.Price",
            Op : "between",
            Type: "num"
        };
    data.Filters.push(priceFilter);

    ratingStationFilter = {
            Value : minRatingStation + '\t' + maxRatingStation,
            Field : "GasStations.Rating",
            Op : "between",
            Type: "num"
        };
    data.Filters.push(ratingStationFilter);

    ratingPetrolFilter = {
            Value : minRatingPetrol + '\t' + maxRatingPetrol,
            Field : "GasStationPetrol.Rating",
            Op : "between",
            Type: "num"
        };
    data.Filters.push(ratingPetrolFilter);

    if(nameFilter){
        stationName = {
            Value : nameFilter,
            Field : "GasStations.Name",
            Op : "like",
            Type: "str"
        };
        data.Filters.push(stationName);
    }

    if(addressFilter){
        stationAddr = {
            Value : addressFilter,
            Field : "GasStations.Address",
            Op : "like",
            Type: "str"
        };
        data.Filters.push(stationAddr);
    }
    
    (function (){
        petrolFilter = {
            Gop: 'or',
            Filters: [

            ]
        };

        document.querySelectorAll('.allow-fuels-active').forEach((allowFuel)=>{
            let fuelName = allowFuel.querySelector(".fuel-name").innerText;
            petrolFilter.Filters.push({
                Value : fuelName,
                Field : "Petrols.Name",
                Op : "equal",
                Type: "str"
            });
        });

        if(petrolFilter.Filters.length > 0)
            data.Filters.push(petrolFilter);
    })();

    if(data.Filters.length == 0)
        return;

    document.querySelector('.scroll_div').innerHTML = '';
    const url = new URL(window.location.origin + window.location.pathname);
    url.searchParams.set('filter', JSON.stringify(data));
    window.location.href = url.href;
}

function resetFilters() {
    window.location.href = window.location.origin + window.location.pathname;
}

document.getElementById('filter-submit').addEventListener('click', applyFilters);
document.getElementById('filter-reset').addEventListener('click', resetFilters);
