using DirDocDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDDTesting {
	class Program {
		static void Main(string[] args) {
			var db = DB.GetSimpleFileDB("./data");
			db.SetConverter<int>(i => i.ToString(), s => int.Parse(s));

			var auto = new Auto() { Key = "fiat", Name = "SuperCrazy", Alter = 12, Passagiere = new[] { "me", "you", "marley" } };

			Console.WriteLine("Key: " + auto.Key);

			db.Store(auto);

			Console.WriteLine("Key: " + auto.Key);
			Console.Write("Continue?");
			Console.ReadLine();

			var nauto = db.Find<Auto>(auto.Key);
			if(nauto != null)
				Console.WriteLine(string.Join(",", nauto.Passagiere));
			db.Delete<Auto>(nauto);

			var nnauto = db.Find<Auto>(auto.Key);
			if(nnauto != null)
				Console.WriteLine(string.Join(",", nnauto.Passagiere));
			
			Console.Write("Close?");
			Console.ReadLine();
		}

		[Model]
		class Auto : Entity {
			[Field]
			public string Name;

			[Field]
			public int Alter;

			[Field]
			public string[] Passagiere;
		}
	}
}
