namespace ExamSystem.API;

/// <summary>
/// 系统角色常量，与 Domain.Enums.UserRole 保持一致。
/// 用于 [Authorize(Roles = Roles.Admin)] 等特性。
/// </summary>
public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin      = "Admin";
    public const string Teacher    = "Teacher";
    public const string Student    = "Student";

    /// <summary>超级管理员 + 普通管理员</summary>
    public const string SuperAdminOrAdmin = SuperAdmin + "," + Admin;

    /// <summary>管理员（含超级）+ 教师</summary>
    public const string AdminOrTeacher = SuperAdmin + "," + Admin + "," + Teacher;

    /// <summary>全部角色</summary>
    public const string All = SuperAdmin + "," + Admin + "," + Teacher + "," + Student;
}
