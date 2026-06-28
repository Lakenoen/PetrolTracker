using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace DbManager
{
    public class Geocoder
    {
        private readonly string _key;
        private const string _baseQueryCoord = "https://geocode-maps.yandex.ru/1.x/?apikey={0}&geocode={1},{2}&format=json";
        private const string _baseQueryAddr = "https://geocode-maps.yandex.ru/1.x/?apikey={0}&geocode={1}&format=json";
        private readonly HttpClient _client;

        public string Referer {get;set;} = "localhost";
        public Geocoder(string key)
        {
            _key = key;
            _client = new HttpClient();
        }

        public List<string> GetAddress(double latitude, double longitude)
        {
            string query = string.Format(_baseQueryCoord, _key, latitude.ToString(), longitude.ToString());
            using HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, query);
            req.Headers.Add(Referer, "localhost");
            using HttpResponseMessage resp = _client.Send(req);
            if(resp.StatusCode != HttpStatusCode.OK)
                throw new ApplicationException($"Geocoder error: status code {resp.StatusCode}");
            
            string content = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            List<string> res = new List<string>();
            JsonDocument doc = JsonDocument.Parse(content);
            foreach ( JsonElement el in doc.RootElement.GetProperty("response")
                .GetProperty("GeoObjectCollection")
                .GetProperty("featureMember").EnumerateArray())
            {
                JsonElement geoObject = el.GetProperty("GeoObject");
                string? addr = geoObject.GetProperty("metaDataProperty")
                    .GetProperty("GeocoderMetaData")
                    .GetProperty("Address")
                    .GetProperty("formatted").GetString();

                if(addr is not null)
                    res.Add(addr);
            }

            return res;
        }

        public (double latitude, double longitude) GetCoord(string address)
        {
            if(address is null || address.Count() == 0)
                return (0, 0);

            string query = string.Format(_baseQueryAddr, _key, address);
            using HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, query);
            req.Headers.Add("Referer", "localhost");
            using HttpResponseMessage resp = _client.Send(req);
            if(resp.StatusCode != HttpStatusCode.OK)
                throw new ApplicationException($"Geocoder error: status code {resp.StatusCode}");
            
            string content = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            JsonDocument doc = JsonDocument.Parse(content);
            foreach ( JsonElement el in doc.RootElement.GetProperty("response")
                .GetProperty("GeoObjectCollection")
                .GetProperty("featureMember").EnumerateArray())
            {
                JsonElement geoObject = el.GetProperty("GeoObject");
                string? coord = geoObject.GetProperty("Point").GetProperty("pos").GetString();

                if(coord is not null){
                    string[] coordsArr = coord.Split(' ');
                    coordsArr[0] = coordsArr[0].Replace('.',',');
                    coordsArr[1] = coordsArr[1].Replace('.',',');
                    return (double.Parse(coordsArr[0]), double.Parse(coordsArr[1]));
                }
            }

            return (0, 0);
        }

    }
}