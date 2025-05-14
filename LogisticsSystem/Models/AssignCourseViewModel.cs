using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LogisticsSystem.Models
{
    public class AssignCourseViewModel
    {
        [Required(ErrorMessage = "Driver is required.")]
        public string DriverId { get; set; }

        [BindNever]
        public string ShipperId { get; set; }

        [Required(ErrorMessage = "Starting Point is required.")]
        public string StartingPoint { get; set; }

        [Required(ErrorMessage = "Destination is required.")]
        public string Destination { get; set; }

        public string Description { get; set; }

        [BindNever]
        public List<User> Drivers { get; set; }

        [Display(Name = "Supply Document (PDF)")]
        public IFormFile Document { get; set; }
    }
} 