using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data;
using AutoMapper;
using Data.Entities;
using Service.DTOs;
namespace Service.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
        
            CreateMap<Category, CategoryDto>(); 
            CreateMap<CreateUpdateCategoryDto, Category>(); 

        
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.CategoryNames, opt => opt.MapFrom(src => src.ProductCategories.Select(pc => pc.Category.Name).ToList())); // Map category names

          
            CreateMap<CreateUpdateProductDto, Product>()
                .ForMember(dest => dest.ProductCategories, opt => opt.Ignore()); 


           
        }
    }
}
