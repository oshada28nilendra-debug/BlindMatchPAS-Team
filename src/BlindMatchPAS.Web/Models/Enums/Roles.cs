namespace BlindMatchPAS.Web.Models.Enums
{
    public static class Roles
    {
        public const string Student = "Student";
        public const string Supervisor = "Supervisor";
        public const string ModuleLeader = "ModuleLeader";
        public const string SystemAdmin = "SystemAdmin";

        public static readonly string[] AllRoles = { Student, Supervisor, ModuleLeader, SystemAdmin };
    }
}
