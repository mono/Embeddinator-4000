using System;
using System.IO;
using System.Reflection;

namespace Embeddinator
{
	public static class SystemCheck
	{
		static void GetVersions (string product, out Version min, out Version max, out string url)
		{
			min = null;
			max = null;
			url = null;

			using (var res = typeof (SystemCheck).Assembly.GetManifestResourceStream ("Make.config")) {
				using (var reader = new StreamReader (res)) {
					var contents = reader.ReadToEnd ();
					var lines = contents.Split ('\n');
					foreach (var line in lines) {
						var eq = line.IndexOf ('=');
						if (eq == -1)
							continue;
						var key = line.Substring (0, eq);
						var value = line.Substring (eq + 1);
						var minLine = $"MIN_{product}_VERSION";
						var maxLine = $"MAX_{product}_VERSION";
						var urlLine = $"MIN_{product}_URL";
						if (key == minLine) {
							min = Version.Parse (value);
						} else if (key == maxLine) {
							max = Version.Parse (value);
						} else if (key == urlLine) {
							url = value;
						}
					}
				}
			}

			if (min == null || max == null || url == null)
				throw ErrorHelper.CreateError (99, $"Internal error: could not find version information for {product}. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).");
		}

		static Version ReadVersionInfo (string path)
		{
			if (!File.Exists (path))
				return null;
			var contents = File.ReadAllText (path);
			return Version.Parse (contents);
		}

		static void VerifyVersion (string name, string product, Version version)
		{
			Version min_version, max_version;
			string url;
			GetVersions (name, out min_version, out max_version, out url);

			if (version == null)
				throw ErrorHelper.CreateError (14, $"Could not find {product} ({product} {min_version} is required).");

			if (version < min_version)
				throw ErrorHelper.CreateError (15, $"Could not find a valid version of {product} (found {version}, but at least {min_version} is required).");

			// Don't test max version for now, we have fairly strict XI/XM versioning to ensure builds
			// aren't broken on bots both for future and past commits (we must correctly downgrade when 
			// building older commits), but it will become annoying for customers (and us) to need to have a
			// very particular XI/XM version installed.
			// This should become better once we get closer to a stable version.

			Console.WriteLine ($"Found {product} {version} (between {min_version} and {max_version})");
		}

		public static void VerifyXamariniOS ()
		{
			VerifyVersion ("XI", "Xamarin.iOS", ReadVersionInfo ("/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/Version"));
		}

		public static void VerifyXamarinMac ()
		{
			VerifyVersion ("XM", "Xamarin.Mac", ReadVersionInfo ("/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/Version"));
		}

		public static void VerifyMono ()
		{
			Type type = Type.GetType ("Mono.Runtime");
			var displayName = type.GetMethod ("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
			var versionString = (string) displayName.Invoke (null, null);
			var version = Version.Parse (versionString.Substring (0, versionString.IndexOf (' ')));
			VerifyVersion ("MONO", "Mono", version);
		}
	}
}
