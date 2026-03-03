using System.ComponentModel.DataAnnotations;
using GestãoIdeas.Models;
namespace GestãoIdeas.DTOs;

public record CreateIdea(

   [Required]
   [MaxLength(100)]
   string Name,

   [MaxLength(500)]
   string Description,

   [Required]
   IdeaState State,

   [Range(1, 5)]
   int Priority

);


    

