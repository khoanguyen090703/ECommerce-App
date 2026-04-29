namespace ECommerce.Web.ViewModels;

public class UserAvatarViewModel
{
    public string FullName { get; set; } = "Người dùng";

    public string Email { get; set; } = string.Empty;

    public string AvatarUrl { get; set; } = "/images/avatar.webp";
}
