using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class Todo
    {
        public int Id { get; set; }
        public int AssigneeId { get; set; } // FK to Employee.Id
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
    }
}
