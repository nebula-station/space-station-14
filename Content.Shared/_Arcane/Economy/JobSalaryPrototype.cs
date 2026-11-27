// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/accb2613b4ea35df58380cc8c7c82cfb2968670a/Content.Arcane.Shared/Economy/JobSalaryPrototype.cs

using Content.Shared.Cargo.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._Arcane.Economy;

/// <summary>
///     Salary of a job, the ID is the job ID.
/// </summary>
[Prototype]
public sealed partial class JobSalaryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public int Salary;

    /// <summary>
    ///     Station department account the salary is paid from and the department of the employee.
    ///     Without one the salary is paid by Nanotrasen itself.
    /// </summary>
    [DataField]
    public ProtoId<CargoAccountPrototype>? Department;
}
