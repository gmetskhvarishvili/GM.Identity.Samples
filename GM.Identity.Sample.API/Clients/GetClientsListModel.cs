using FluentValidation;
using GM.API.Models;

using System;
using System.ComponentModel.DataAnnotations;
using GM.Identity.Sample.Common.Resources;
namespace GM.Identity.Sample.API.Clients;

/// <summary>Get clients list model.</summary>
public class GetClientsListModel : GetBaseListModel
{
    /// <summary>The id.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Id")]
    public Guid? Id { get; set; }
    /// <summary>The name.</summary>
    [Display(ResourceType = typeof(StringResource), Name = "Name")]
    public string? Name { get; set; }
}

public class GetClientsListModelValidator : AbstractValidator<GetClientsListModel>;
