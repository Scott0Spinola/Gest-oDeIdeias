using GestãoIdeas.Models;

namespace GestãoIdeas.DTOs;

public class IdeaDTO
{

     public int Id{get; set;}
    public string? Name{get; set;}
    public string? Description{get; set;}
    public DateTime CreatedAt{get; set;}
    public IdeaState State{get; set;}
    public int Priority{get; set;}
    
    public IdeaDTO(){}

    public IdeaDTO(Idea idea)=> 
    (Id, Name, Description, CreatedAt, State, Priority) = (idea.Id, idea.Name, idea.Description, idea.CreatedAt, idea.State, idea.Priority);
};
