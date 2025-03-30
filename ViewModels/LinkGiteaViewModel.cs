using System.ComponentModel.DataAnnotations;

namespace DoAnWeb.ViewModels
{
    public class LinkGiteaViewModel
    {
        [Display(Name = "Create New Account")]
        public bool CreateNewAccount { get; set; }
        
        [Display(Name = "Gitea Username")]
        public string GiteaUsername { get; set; }
        
        [Display(Name = "Gitea Password")]
        public string GiteaPassword { get; set; }
    }
} 