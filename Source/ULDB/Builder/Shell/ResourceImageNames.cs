// ULDB: finds which Properties.Resources image a bitmap is, so menus and dialog buttons can show
// ULDB's Breeze-style icon for it. Upstream sets item.Image = Resources.Reset and the like, and
// form .resx files carry their own copies of the same pictures, so the bitmap's pixels are the
// only reliable key: each resource image is hashed once and looked up by content.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using SD = System.Drawing;
using SDI = System.Drawing.Imaging;

namespace ULDB.Shell
{
	internal static class ResourceImageNames
	{
		private static readonly ConditionalWeakTable<SD.Image, string> cache = new ConditionalWeakTable<SD.Image, string>();
		private static readonly Dictionary<ulong, string> names = new Dictionary<ulong, string>();
		private static readonly HashSet<Assembly> scanned = new HashSet<Assembly>();
		private static readonly object sync = new object();

		/// <summary>"res:Name" for an image that is one of the loaded assemblies' resource images, else null.</summary>
		public static string KeyFor(SD.Image image)
		{
			if(!(image is SD.Bitmap bitmap) || image.Width <= 0 || image.Height <= 0 || image.Width > 64 || image.Height > 64) return null;
			lock(sync)
			{
				if(cache.TryGetValue(image, out string key)) return key.Length > 0 ? key : null;
				ScanNewAssemblies();
				key = names.TryGetValue(Hash(bitmap), out string name) ? KeyForName(name) : "";
				cache.AddOrUpdate(image, key);
				return key.Length > 0 ? key : null;
			}
		}

		// A plugin's resource can share a name with a different core picture (BuilderEffects' "Update" is
		// a re-roll arrow, the core's is check-for-updates): "res:Assembly/Name" wins when it has an icon.
		private static string KeyForName(string qualified)
		{
			string own = "res:" + qualified;
			return BreezeIcons.Has(own) ? own : "res:" + qualified.Substring(qualified.IndexOf('/') + 1);
		}

		// Plugins load after the main window is built, so new assemblies are picked up on later lookups.
		private static void ScanNewAssemblies()
		{
			foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				if(asm.IsDynamic || !scanned.Add(asm)) continue;
				// Only assemblies with tools/resxgen resources (the core and ported plugins)
				if(!asm.GetManifestResourceNames().Any(n => n.StartsWith("ULDB.Resources.", StringComparison.Ordinal))) continue;
				Type[] types;
				try { types = asm.GetTypes(); }
				catch(ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }

				foreach(Type t in types)
				{
					if(t.Name != "Resources" || t.Namespace == null || !t.Namespace.EndsWith(".Properties", StringComparison.Ordinal)) continue;
					foreach(PropertyInfo p in t.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
					{
						if(p.PropertyType != typeof(SD.Bitmap)) continue;
						try
						{
							using(SD.Bitmap b = (SD.Bitmap)p.GetValue(null))
							{
								if(b == null || b.Width > 64 || b.Height > 64) continue;
								ulong h = Hash(b);
								if(!names.ContainsKey(h)) names[h] = asm.GetName().Name + "/" + p.Name;
							}
						}
						catch(Exception) { } // A missing or unreadable resource just has no key
					}
				}
			}
		}

		// FNV-1a over the size and pixels; fully transparent pixels count as one value.
		private static unsafe ulong Hash(SD.Bitmap b)
		{
			ulong h = 14695981039346656037UL;
			void Mix(uint v) { h = (h ^ v) * 1099511628211UL; }
			Mix((uint)b.Width);
			Mix((uint)b.Height);
			SDI.BitmapData data = b.LockBits(new SD.Rectangle(0, 0, b.Width, b.Height), SDI.ImageLockMode.ReadOnly, SDI.PixelFormat.Format32bppArgb);
			try
			{
				for(int y = 0; y < b.Height; y++)
				{
					uint* row = (uint*)((byte*)data.Scan0 + y * data.Stride);
					for(int x = 0; x < b.Width; x++) Mix((row[x] >> 24) == 0 ? 0 : row[x]);
				}
			}
			finally { b.UnlockBits(data); }
			return h;
		}
	}
}
