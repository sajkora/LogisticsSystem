using System.Collections.Generic;
using LogisticsSystem.Models;

namespace LogisticsSystem.Models
{
    public class ManageUsersViewModel
    {
        public List<User> AdminUsers { get; set; }
        public List<User> ShipperUsers { get; set; }
        public List<User> DriverUsers { get; set; }
    }
} 