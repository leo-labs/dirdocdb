using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirDocDB {
	public class FieldAttribute : Attribute {
		public readonly string Name;

		public FieldAttribute(string name = null) {
			this.Name = name;
		}
	}
}
