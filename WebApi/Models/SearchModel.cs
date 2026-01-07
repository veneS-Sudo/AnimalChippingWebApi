using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace AnimalChippingWebApi.Models;

public abstract class SearchModel
{
    private int _from;
    private int _size;
    
    [FromQuery(Name = "email")]
    [EmailAddress]
    public string? Email { get; set; }

    [FromQuery(Name = "from")]
    public int? From
    {
        get => _from;
        set => _from = value ?? 0;
    }

    [FromQuery(Name = "size")]
    public int? Size
    {
        get => _size;
        set => _size = value ?? 10;
    }}