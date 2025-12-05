using AnimalChippingWebApi.Core.Models;

namespace ServiceLayer.Admin.Dto;

public class UpdateAnimalTypeDto
{
    public AnimalType OldAnimalType { get; set; }
    public AnimalType NewAnimalType { get; set; }
}