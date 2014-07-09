using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirDocDB {
	public static class DB {
		public static IDB GetSimpleFileDB(string basepath) {
			return new SimpleFileDB(basepath);
		}

		public static T[] Find<T>(this IDB db, IEnumerable<string> ids) where T : Entity {
			return ids.Select(id => db.Find<T>(id)).ToArray();
		}

		public static T[] Find<T>(this IDB db, params string[] ids) where T : Entity {
			return Find<T>(db, ids.AsEnumerable());
		}

		public static bool Delete<T>(this IDB db, string id) where T : Entity {
			return db.Delete(typeof(T), id);
		}

		public static bool Delete<T>(this IDB db, Entity item) where T : Entity {
			return Delete<T>(db, item.Key);
		}
	}
}
