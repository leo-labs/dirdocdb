using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirDocDB {
	public interface IDB {
		void Store(Entity item);

		bool Delete(Type t, string id);

		T Find<T>(string id) where T: Entity;

		IEnumerable<T> GetAll<T>() where T : Entity;
		
		void SetConverter<T>(Func<T, string> encoder, Func<string, T> decoder);
	}
}
