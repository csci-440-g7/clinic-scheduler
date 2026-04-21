namespace ClinicScheduler.Web;

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string ClinicManager = "ClinicManager";
    public const string Staff = "Staff";
    public const string Therapist = "Therapist";
    public const string Patient = "Patient";

    public const string StaffOrAbove = Admin + "," + ClinicManager + "," + Staff + "," + Therapist;
    public const string AdminOrManager = Admin + "," + ClinicManager;
}
