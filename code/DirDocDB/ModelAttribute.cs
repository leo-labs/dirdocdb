using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirDocDB {
	public class ModelAttribute : Attribute {
		public readonly string Name;

		public ModelAttribute(string name = null) {
			this.Name = name;
		}
	}
}
