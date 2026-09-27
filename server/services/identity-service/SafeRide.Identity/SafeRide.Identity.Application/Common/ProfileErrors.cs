namespace SafeRide.Identity.Application.Common;

public static class ProfileErrors
{
    public static readonly Error EmptyFile = new("Profile.EmptyFile", "Choose a file to upload.");

    public static readonly Error FileTooLarge = new(
        "Profile.FileTooLarge",
        "Photos must be smaller than 2 MB."
    );

    public static readonly Error NotAnImage = new(
        "Profile.NotAnImage",
        "That file is not a JPEG, PNG or WebP image."
    );
}
