using System.ComponentModel;
using BackendAgent.Models;
using Microsoft.SemanticKernel;

namespace BackendAgent.Plugins;

/// <summary>
/// HRPlugin — Semantic Kernel plugin that gives the agent access to employee data.
///
/// The [KernelFunction] + [Description] attributes are what the LLM reads to decide
/// which function to call. This replaces the manual routing prompt from v1.
///
/// FunctionChoiceBehavior.Auto() in AgentService means GPT-4o-mini sees these
/// function signatures and calls them autonomously — no hardcoded routing needed.
/// </summary>
public class HRPlugin
{
    private readonly Dictionary<string, EmployeeInfo> _employees;

    public HRPlugin(Dictionary<string, EmployeeInfo> employees)
    {
        _employees = employees;
    }

    [KernelFunction]
    [Description("Returns the leave/holiday/vacation/PTO/annual leave balance (days remaining) for a named employee. Use for any question about time off or leave days.")]
    public string GetLeaveBalance(
        [Description("The first name of the employee, e.g. John, Sarah, Mike, Emily")] string employeeName)
    {
        var key = _employees.Keys
            .FirstOrDefault(k => k.Equals(employeeName, StringComparison.OrdinalIgnoreCase));

        if (key is null)
            return $"Employee '{employeeName}' was not found. Known employees: {string.Join(", ", _employees.Keys)}.";

        return $"{key} has {_employees[key].LeaveBalance} leave days remaining." +
               $" [Powered by: HRPlugin.GetLeaveBalance → Skills/leave/SKILL.md]";
    }

    [KernelFunction]
    [Description("Returns the department, team, and profile information for a named employee.")]
    public string GetEmployeeInfo(
        [Description("The first name of the employee, e.g. John, Sarah, Mike, Emily")] string employeeName)
    {
        var key = _employees.Keys
            .FirstOrDefault(k => k.Equals(employeeName, StringComparison.OrdinalIgnoreCase));

        if (key is null)
            return $"Employee '{employeeName}' was not found. Known employees: {string.Join(", ", _employees.Keys)}.";

        var emp = _employees[key];
        return $"{key} works in the {emp.Department} department and has {emp.LeaveBalance} leave days remaining." +
               $" [Powered by: HRPlugin.GetEmployeeInfo → Skills/employee/SKILL.md]";
    }
}
