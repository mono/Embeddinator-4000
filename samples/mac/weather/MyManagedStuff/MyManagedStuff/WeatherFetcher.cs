﻿using System;
using System.Json;
using System.Net;

namespace XAM {
	public class WeatherFetcher {

		static string urlTemplate = @"https://query.yahooapis.com/v1/public/yql?q=select%20item.condition%20from%20weather.forecast%20where%20woeid%20in%20(select%20woeid%20from%20geo.places(1)%20where%20text%3D%22{0}%2C%20{1}%22)&format=json&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys";
		public string City { get; private set; }
		public string State { get; private set; }


		public WeatherFetcher (string city, string state)
		{
			City = city;
			State = state;
		}

		public WeatherResult GetWeather ()
		{
			try {
				using (var wc = new WebClient ()) {
					var url = string.Format (urlTemplate, City, State);
					var str = wc.DownloadString (url);
					var json = JsonValue.Parse (str)["query"]["results"]["channel"]["item"]["condition"];
					var result = new WeatherResult (json["temp"], json["text"]);
					return result;
				}
			}
			catch (Exception ex) {
				// Log some of the exception messages
				Console.WriteLine (ex?.Message);
				Console.WriteLine (ex?.InnerException?.Message);
				Console.WriteLine (ex?.InnerException?.InnerException?.Message);

				return null;
			}

		}
	}

	public class WeatherResult {
		public string Temp { get; private set; }
		public string Text { get; private set; }

		public WeatherResult (string temp, string text)
		{
			Temp = temp;
			Text = text;
		}
	}
}
