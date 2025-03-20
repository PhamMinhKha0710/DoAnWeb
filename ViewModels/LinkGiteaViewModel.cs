using System.ComponentModel.DataAnnotations;

namespace DoAnWeb.ViewModels
{
    public class LinkGiteaViewModel
    {
        [Display(Name = "Confirm Link")]
        public bool ConfirmLink { get; set; }
        
        public string ReturnUrl { get; set; }
    }
} 