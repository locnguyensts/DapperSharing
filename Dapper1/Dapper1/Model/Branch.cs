using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dapper1.Model
{
    public class Branch
    {
        public Branch()
        {
            empList = new List<Employee>();
        }
        public int Id { get; set; }

        public string nAMe { get; set; }

        public string Address { get; set; }

        public string PhoneNumber { get; set; }

        public Employee Employee { get; set; }

        public List<Employee> empList { get; set; }
    }
}
