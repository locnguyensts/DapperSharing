using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dapper1.Model
{
	public class Supplier
	{
		public int SupplierID { get; set; }
		public string SupplierName { get; set; }
		public string ContactName { get; set; }
		public List<Product> Products { get; set; }
	}
}
