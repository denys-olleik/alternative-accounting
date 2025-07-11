﻿using FluentValidation;
using FluentValidation.Results;
using System.Collections.Generic;
using System.Linq;

namespace Accounting.Models.UserViewModels
{
  public class UpdateUserViewModel
  {
    private string? email;
    private string? firstName;
    private string? lastName;

    public int UserID { get; set; }
    public string? Email
    {
      get => email;
      set => email = value?.Trim();
    }

    public string? FirstName
    {
      get => firstName;
      set => firstName = value?.Trim();
    }

    public string? LastName
    {
      get => lastName;
      set => lastName = value?.Trim();
    }

    public List<OrganizationViewModel> AvailableOrganizations { get; set; } = new();
    public List<string> AvailableRoles { get; set; } = new();
    public List<string> SelectedRoles { get; set; } = new();
    public string? SelectedOrganizationIdsCsv { get; set; }

    public int CurrentRequestingUserId { get; set; }
    public ValidationResult ValidationResult { get; set; } = new();
    public List<string> DatabaseRoles { get;  set; } = new();

    public class OrganizationViewModel
    {
      public int OrganizationID { get; set; }
      public string? Name { get; set; }
    }

    public class UpdateUserViewModelValidator : AbstractValidator<UpdateUserViewModel>
    {
      public UpdateUserViewModelValidator()
      {
        
      }
    }
  }
}