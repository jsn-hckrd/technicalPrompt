using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int DepartmentId { get; set; } // FK to Department.Id
        public string Position { get; set; }
        public int? BadgeNumber { get; set; }
        public DateTime? HiredDate { get; set; }
        public float ProductivityScore { get; set; }
    }
}
