using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections;

namespace DirDocDB {
	class SimpleFileDB : IDB {
		public const string SEQ = ".seq";

		public readonly DirectoryInfo BaseDir;

		struct Converter {
			public Func<object, string> Encoder;
			public Func<string, object> Decoder;
		}

		Dictionary<Type, Converter> converters = new Dictionary<Type, Converter>();

		public SimpleFileDB(string basepath) {
			this.BaseDir = new DirectoryInfo(basepath);
			if (!BaseDir.Exists)
				BaseDir.Create();

			SetConverter<string>(s => s, s => s);
		}

		DirectoryInfo getDirForType(Type t) {
			var attr = t.GetCustomAttribute<ModelAttribute>();

			if (attr == null)
				throw new InvalidOperationException("The type '" + t.Name + "' has no Model-Attribute attached!");

			var dirname = attr.Name ?? t.Name;
			var dir = BaseDir.EnumerateDirectories(dirname).SingleOrDefault();

			if (dir == null)
				dir = BaseDir.CreateSubdirectory(dirname);

			return dir;
		}

		DirectoryInfo getDirForObj(DirectoryInfo tdir, ref string key, bool create = false) {
			if (key == null) {
				getKey(tdir, out key);
			}

			var dir = tdir.EnumerateDirectories(key).SingleOrDefault();

			if (dir == null && create)
				dir = tdir.CreateSubdirectory(key);

			return dir;
		}

		void getKey(DirectoryInfo tdir, out string key) {
			using (var fs = new FileStream(tdir.FullName + Path.DirectorySeparatorChar + SEQ, FileMode.OpenOrCreate)) {
				if (fs.Length == 0) {
					key = "1";
				} else {
					using (var sr = new StreamReader(fs, Encoding.UTF8, false, 1024, true))
						key = (int.Parse(sr.ReadToEnd()) + 1).ToString();
					fs.SetLength(0);
				}

				using (var sw = new StreamWriter(fs))
					sw.Write(key);
			}
		}

		public void Store(Entity item) {
			var type = item.GetType();
			var tdir = getDirForType(type);

			var odir = getDirForObj(tdir, ref item.Key, true);

			foreach (var file in odir.EnumerateFiles())
				file.Delete();

			foreach (var field in getFieldsOfType(type)) {
				var attr = field.GetCustomAttribute<FieldAttribute>(true);

				if (attr == null)
					continue;

				object value = field.GetValue(item);
				string toWrite;

				if (value == null)
					continue;
				else if (value.GetType().IsArray) {
					Converter converter;

					if (!converters.TryGetValue(value.GetType().GetElementType(), out converter))
						throw new InvalidOperationException("The type '" + value.GetType().GetElementType().Name + "' cannot be stored and there is no converter available!");

					toWrite = string.Join("\r\n", (value as Array).Cast<object>().Select(converter.Encoder));
				} else {
					Converter converter;

					if (!converters.TryGetValue(value.GetType(), out converter))
						throw new InvalidOperationException("The type '" + value.GetType().Name + "' cannot be stored and there is no converter available!");

					toWrite = converter.Encoder(value);
				}

				using (var fs = new FileStream(odir.FullName + Path.DirectorySeparatorChar + (attr.Name ?? field.Name), FileMode.OpenOrCreate)) {
					if (fs.Length > 0)
						fs.SetLength(0);
					using (var sw = new StreamWriter(fs))
						sw.Write(toWrite);
				}
			}
		}

		private static FieldInfo[] getFieldsOfType(Type type) {
			return type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}

		public bool Delete(Type t, string id) {
			var tdir = getDirForType(t);

			if (id == null)
				return false;

			var odir = getDirForObj(tdir, ref id);

			if (odir != null) {
				odir.Delete(true);
				return true;
			}

			return false;
		}

		T parse<T>(DirectoryInfo odir) where T : Entity {
			var type = typeof(T);

			var ctor = type.GetConstructor(new Type[0]);

			if (ctor == null)
				throw new InvalidOperationException("The type '" + type.Name + "' specifies no parameterless constructor.");

			T instance;

			try {
				instance = ctor.Invoke(new object[0]) as T;
			} catch (Exception ex) {
				throw new TargetInvocationException("The constructor for type '" + type.Name + "' threw an exception!", ex);
			}

			instance.Key = odir.Name;

			foreach (var field in getFieldsOfType(type)) {
				var attr = field.GetCustomAttribute<FieldAttribute>(true);

				if (attr == null)
					continue;

				var fname = odir.FullName + Path.DirectorySeparatorChar + (attr.Name ?? field.Name);

				if (!File.Exists(fname))
					continue;

				string data;
				using (var sr = new StreamReader(new FileStream(fname, FileMode.Open))) {
					data = sr.ReadToEnd();
				}

				object valueToAssign;

				if (field.FieldType.IsArray) {
					var at = field.FieldType.GetElementType();

					Converter converter;

					if (!converters.TryGetValue(at, out converter))
						throw new InvalidOperationException("The type '" + at.Name + "' cannot be stored and there is no converter available!");

					var datas = data.Split(new [] { "\r\n" }, StringSplitOptions.None).Select(converter.Decoder).ToArray();

					var array = Array.CreateInstance(at, datas.Length);

					datas.CopyTo(array, 0);

					valueToAssign = array;
				} else {
					Converter converter;

					if (!converters.TryGetValue(field.FieldType, out converter))
						throw new InvalidOperationException("The type '" + field.FieldType.Name + "' cannot be stored and there is no converter available!");

					valueToAssign = converter.Decoder(data);
				}

				field.SetValue(instance, valueToAssign);
			}

			return instance;
		}

		public T Find<T>(string id) where T : Entity {
			var type = typeof(T);
			var tdir = getDirForType(type);

			if(tdir == null)
				return null;

			var odir = getDirForObj(tdir, ref id);
			if(odir == null)
				return null;
			return parse<T>(odir);

		}

		public IEnumerable<T> GetAll<T>() where T : Entity {
			var type = typeof(T);
			var tdir = getDirForType(type);

			foreach (var odir in tdir.EnumerateDirectories())
				yield return parse<T>(odir);
		}

		public void SetConverter<T>(Func<T, string> encoder, Func<string, T> decoder) {
			this.converters[typeof(T)] = new Converter() { Encoder = obj => encoder((T)obj), Decoder = str => decoder(str) };
		}

	}
}
