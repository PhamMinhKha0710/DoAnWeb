namespace DoAnWeb.ViewModels
{
    public class LogoutViewModel
    {
        public string ReturnUrl { get; set; }

        // If you want to add a confirmation message or reason
        public string LogoutReason { get; set; }

        // If you want to remember the user for easier login next time
        public bool RememberUser { get; set; }
    }
}
